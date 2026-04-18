import * as vscode from "vscode";
import * as cp from "child_process";
import * as path from "path";
import * as fs from "fs";
import * as http from "http";

let previewProcess: cp.ChildProcess | undefined;
let capturePort: number | undefined;
let panel: vscode.WebviewPanel | undefined;
let statusBarItem: vscode.StatusBarItem;
let outputChannel: vscode.OutputChannel;
let editorChangeDisposable: vscode.Disposable | undefined;

let currentCsprojPath: string | undefined;
let currentComponents: string[] = [];
let currentComponentName: string | undefined;
let currentFilePath: string | undefined;
let isLaunching = false;
let legacyPreviewArgs = false;

let extensionContext: vscode.ExtensionContext | undefined;

export function activate(context: vscode.ExtensionContext) {
  extensionContext = context;
  outputChannel = vscode.window.createOutputChannel("Reactor Preview");

  statusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  statusBarItem.command = "reactor.previewFocus";
  context.subscriptions.push(statusBarItem);

  context.subscriptions.push(
    vscode.commands.registerCommand("reactor.preview", () =>
      startAutoPreview(context)
    ),
    vscode.commands.registerCommand("reactor.previewConnect", () =>
      connectToPreview(context)
    ),
    vscode.commands.registerCommand("reactor.previewStop", stopPreview),
    vscode.commands.registerCommand("reactor.previewFocus", focusPreviewWindow)
  );
}

export function deactivate() {
  stopPreview();
  editorChangeDisposable?.dispose();
}

// -- Auto Preview ------------------------------------------------------------

async function startAutoPreview(context: vscode.ExtensionContext) {
  const editor = vscode.window.activeTextEditor;
  if (!editor || editor.document.languageId !== "csharp") {
    vscode.window.showWarningMessage(
      "Open a C# file containing a Reactor Component, then run this command."
    );
    return;
  }

  const csprojPath = await findCsprojFor(editor.document.uri.fsPath);
  if (!csprojPath) {
    vscode.window.showWarningMessage(
      "Could not find a .csproj file for this file."
    );
    return;
  }

  currentCsprojPath = csprojPath;
  currentFilePath = editor.document.uri.fsPath;

  // If we already have a running process for this project, just switch via HTTP
  if (previewProcess && capturePort) {
    const fileComponents = findAllComponentClasses(editor.document.getText());
    if (fileComponents.length > 0) {
      await switchComponentViaHttp(fileComponents[0]);
    }
    return;
  }

  // Launch the preview process — no component name needed, it defaults to the first one
  await launchPreviewProcess(context, csprojPath);

  // Watch for editor changes to switch components via HTTP
  if (!editorChangeDisposable) {
    editorChangeDisposable = vscode.window.onDidChangeActiveTextEditor(
      async (newEditor) => {
        if (!newEditor || newEditor.document.languageId !== "csharp") return;
        if (!panel || !capturePort) return;

        const newFilePath = newEditor.document.uri.fsPath;
        if (newFilePath === currentFilePath) return;

        const fileComponents = findAllComponentClasses(
          newEditor.document.getText()
        );
        if (fileComponents.length === 0) return;

        // Check if we need a new process (different csproj)
        const newCsproj = await findCsprojFor(newFilePath);
        if (!newCsproj) return;

        currentFilePath = newFilePath;

        if (newCsproj !== currentCsprojPath) {
          // Different project — need to relaunch
          currentCsprojPath = newCsproj;
          outputChannel.appendLine(
            `[reactor] Project changed to ${path.basename(newCsproj)}, relaunching...`
          );
          await launchPreviewProcess(extensionContext!, newCsproj);
          return;
        }

        // Same project — switch component via HTTP (instant)
        outputChannel.appendLine(
          `[reactor] Editor switched to ${path.basename(newFilePath)}, switching to ${fileComponents[0]}...`
        );
        await switchComponentViaHttp(fileComponents[0]);
      }
    );
  }
}

// -- Component Detection (regex fallback for file-level filtering) -----------

function findAllComponentClasses(text: string): string[] {
  const pattern =
    /class\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*Component(?:<[^>]*>)?\b/g;
  const results: string[] = [];
  let match: RegExpExecArray | null;

  while ((match = pattern.exec(text)) !== null) {
    const name = match[1];
    if (name === "Component") continue;
    results.push(name);
  }

  return results;
}

async function findCsprojFor(filePath: string): Promise<string | null> {
  let dir = path.dirname(filePath);
  const root = path.parse(dir).root;

  while (dir !== root) {
    const entries = await fs.promises.readdir(dir);
    const csproj = entries.find((e) => e.endsWith(".csproj"));
    if (csproj) {
      return path.join(dir, csproj);
    }
    dir = path.dirname(dir);
  }

  return null;
}

// -- Launch ------------------------------------------------------------------

async function launchPreviewProcess(
  context: vscode.ExtensionContext,
  csprojPath: string
) {
  if (isLaunching) {
    outputChannel.appendLine(`[reactor] Already launching, ignoring request`);
    return;
  }

  isLaunching = true;
  await killPreviewProcess();
  capturePort = undefined;

  const workspaceRoot =
    vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ??
    path.dirname(csprojPath);

  // Launch with --devtools run (no component name = default to first) and --vscode.
  // Reactor packages older than the devtools rename only expose --preview. We probe
  // stdout: a `[preview]` prefix means the target Reactor is pre-rename, so we kill
  // and relaunch with the legacy args. telemetry_event_name is kept for one release.
  const args = buildDevtoolsArgs(csprojPath, legacyPreviewArgs);

  outputChannel.appendLine(`[reactor] Launching: dotnet ${args.join(" ")}`);
  logTelemetry(legacyPreviewArgs ? "reactor_preview_launch" : "reactor_devtools_launch");

  previewProcess = cp.spawn("dotnet", args, {
    cwd: workspaceRoot,
    stdio: ["ignore", "pipe", "pipe"],
  });

  statusBarItem.text = `$(loading~spin) Reactor: Starting...`;
  statusBarItem.show();

  let sniffedPrefix = false;

  previewProcess.stdout?.on("data", (data: Buffer) => {
    const text = data.toString();
    outputChannel.append(text);

    if (!sniffedPrefix && !legacyPreviewArgs && !capturePort) {
      if (/^\s*\[preview\]/m.test(text) && !/\[devtools\]/.test(text)) {
        sniffedPrefix = true;
        outputChannel.appendLine(
          `[reactor] Target Reactor is pre-devtools — falling back to --preview --vscode`
        );
        legacyPreviewArgs = true;
        killPreviewProcess().then(() => {
          isLaunching = false;
          launchPreviewProcess(context, csprojPath);
        });
        return;
      }
      if (/\[devtools\]/.test(text)) sniffedPrefix = true;
    }

    const match = text.match(/CAPTURE_PORT=(\d+)/);
    if (match) {
      capturePort = parseInt(match[1], 10);
      isLaunching = false;
      outputChannel.appendLine(
        `[reactor] Capture server on port ${capturePort}`
      );

      // Fetch the component list from the running process
      fetchComponentsAndShow(context);
    }
  });

  previewProcess.stderr?.on("data", (data: Buffer) => {
    outputChannel.append(data.toString());
  });

  previewProcess.on("exit", (code) => {
    isLaunching = false;
    outputChannel.appendLine(
      `[reactor] Preview process exited with code ${code}`
    );
    statusBarItem.text = "$(circle-slash) Reactor Preview: Stopped";
    setTimeout(() => {
      if (!previewProcess) statusBarItem.hide();
    }, 5000);
    previewProcess = undefined;
    capturePort = undefined;
  });
}

function buildDevtoolsArgs(csprojPath: string, legacy: boolean): string[] {
  const tail = legacy ? ["--preview", "--vscode"] : ["--devtools", "run", "--vscode"];
  return ["watch", "run", "--project", csprojPath, "--", ...tail];
}

function logTelemetry(eventName: string) {
  // Telemetry transport not wired here; the extension's upstream harness reads the
  // output channel in dev. The duplicated event name keeps legacy aggregators working
  // for one release while the devtools name becomes primary.
  outputChannel.appendLine(`[reactor] telemetry: ${eventName}`);
}

/**
 * After the capture server is up, GET /components to populate the dropdown,
 * then show the preview panel.
 */
async function fetchComponentsAndShow(context: vscode.ExtensionContext) {
  if (!capturePort) return;

  try {
    const data = await httpGetJson<{
      components: string[];
      current: string | null;
    }>(`http://localhost:${capturePort}/components`);

    currentComponents = data.components;
    currentComponentName = data.current ?? data.components[0];

    statusBarItem.text = `$(eye) Reactor: ${currentComponentName}`;
    statusBarItem.tooltip = `Previewing ${currentComponentName} — port ${capturePort}\nClick to focus window`;

    showPreviewPanel(context, currentComponentName);
  } catch (err) {
    outputChannel.appendLine(`[reactor] Failed to fetch components: ${err}`);
    // Show panel anyway with whatever we have
    showPreviewPanel(context, "Preview");
  }
}

// -- Component Switching (HTTP, no restart) ----------------------------------

async function switchComponentViaHttp(componentName: string) {
  if (!capturePort) return;
  if (componentName === currentComponentName) return;

  try {
    const result = await httpPostJson<{ ok: boolean; error?: string }>(
      `http://localhost:${capturePort}/preview`,
      { component: componentName }
    );

    if (result.ok) {
      currentComponentName = componentName;
      statusBarItem.text = `$(eye) Reactor: ${componentName}`;
      statusBarItem.tooltip = `Previewing ${componentName} — port ${capturePort}\nClick to focus window`;

      if (panel) {
        panel.title = `Reactor: ${componentName}`;
        // Notify the webview to update the dropdown selection
        panel.webview.postMessage({
          type: "updateSelection",
          selected: componentName,
        });
      }

      outputChannel.appendLine(
        `[reactor] Switched to ${componentName} via HTTP`
      );
    } else {
      outputChannel.appendLine(
        `[reactor] Switch failed: ${result.error}`
      );
    }
  } catch (err) {
    outputChannel.appendLine(
      `[reactor] Failed to switch component: ${err}`
    );
  }
}

async function killPreviewProcess() {
  if (previewProcess) {
    const proc = previewProcess;
    const pid = proc.pid;
    previewProcess = undefined;
    capturePort = undefined;

    if (pid) {
      try {
        cp.execFileSync("taskkill", ["/T", "/F", "/PID", pid.toString()], { stdio: "ignore" });
      } catch {
        proc.kill();
      }

      await new Promise<void>((resolve) => {
        if (proc.exitCode !== null) {
          resolve();
          return;
        }
        const timeout = setTimeout(() => resolve(), 3000);
        proc.on("exit", () => {
          clearTimeout(timeout);
          resolve();
        });
      });
    }
  } else {
    capturePort = undefined;
  }
}

// -- Connect to existing Preview ---------------------------------------------

async function connectToPreview(context: vscode.ExtensionContext) {
  const portStr = await vscode.window.showInputBox({
    prompt:
      "Enter the capture server port (shown in the preview window title bar)",
    placeHolder: "e.g. 52431",
  });
  if (!portStr) return;

  const port = parseInt(portStr, 10);
  if (isNaN(port) || port < 1 || port > 65535) {
    vscode.window.showErrorMessage("Invalid port number.");
    return;
  }

  try {
    await httpGetJson(`http://localhost:${port}/status`);
  } catch {
    vscode.window.showErrorMessage(
      `Could not connect to capture server on port ${port}.`
    );
    return;
  }

  capturePort = port;
  statusBarItem.text = "$(eye) Reactor Preview";
  statusBarItem.tooltip = `Preview connected — port ${capturePort}. Click to focus window.`;
  statusBarItem.show();

  // Fetch components from the running server
  await fetchComponentsAndShow(context);
}

// -- WebView Panel -----------------------------------------------------------

function showPreviewPanel(
  context: vscode.ExtensionContext,
  componentName: string
) {
  if (panel) {
    panel.title = `Reactor: ${componentName}`;
    updatePanelHtml();
    panel.reveal(vscode.ViewColumn.Beside, true);
    return;
  }

  panel = vscode.window.createWebviewPanel(
    "reactorPreview",
    `Reactor: ${componentName}`,
    { viewColumn: vscode.ViewColumn.Beside, preserveFocus: true },
    {
      enableScripts: true,
      retainContextWhenHidden: true,
    }
  );

  panel.onDidDispose(() => {
    panel = undefined;
  });

  panel.webview.onDidReceiveMessage(async (msg) => {
    if (msg.type === "selectComponent" && msg.name) {
      outputChannel.appendLine(
        `[reactor] Component selected from dropdown: ${msg.name}`
      );
      await switchComponentViaHttp(msg.name);
    }
  });

  updatePanelHtml();
}

function updatePanelHtml() {
  if (!panel || !capturePort) return;
  panel.webview.html = getWebviewHtml(
    capturePort,
    currentComponents,
    currentComponentName ?? currentComponents[0]
  );
}

function getWebviewHtml(
  port: number,
  components: string[],
  selectedComponent: string
): string {
  const optionsHtml = components
    .map(
      (c) =>
        `<option value="${escapeHtml(c)}"${c === selectedComponent ? " selected" : ""}>${escapeHtml(c)}</option>`
    )
    .join("\n");

  const selectorHtml =
    components.length > 1
      ? `<select id="componentSelect" title="Select component to preview">${optionsHtml}</select>`
      : `<span class="component-name">${escapeHtml(selectedComponent)}</span>`;

  return /*html*/ `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
      background: var(--vscode-editor-background);
      color: var(--vscode-editor-foreground);
      font-family: var(--vscode-font-family);
      display: flex;
      flex-direction: column;
      height: 100vh;
      overflow: hidden;
    }
    .toolbar {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 12px;
      background: var(--vscode-titleBar-activeBackground);
      border-bottom: 1px solid var(--vscode-panel-border);
      flex-shrink: 0;
    }
    .toolbar button {
      background: var(--vscode-button-background);
      color: var(--vscode-button-foreground);
      border: none;
      padding: 4px 10px;
      cursor: pointer;
      font-size: 12px;
      border-radius: 2px;
      flex-shrink: 0;
    }
    .toolbar button:hover {
      background: var(--vscode-button-hoverBackground);
    }
    .toolbar select {
      background: var(--vscode-dropdown-background);
      color: var(--vscode-dropdown-foreground);
      border: 1px solid var(--vscode-dropdown-border);
      padding: 3px 6px;
      font-size: 12px;
      font-family: var(--vscode-font-family);
      border-radius: 2px;
      min-width: 0;
    }
    .component-name {
      font-size: 12px;
      font-weight: 600;
    }
    .status {
      font-size: 11px;
      opacity: 0.7;
      margin-left: auto;
      flex-shrink: 0;
    }
    .status.building {
      color: var(--vscode-charts-orange);
      opacity: 1;
    }
    .status.error {
      color: var(--vscode-errorForeground);
      opacity: 1;
    }
    .preview-container {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: auto;
      padding: 8px;
    }
    #preview {
      max-width: 100%;
      max-height: 100%;
      object-fit: contain;
      image-rendering: auto;
      border: 1px solid var(--vscode-panel-border);
      cursor: pointer;
    }
    #preview.stale {
      opacity: 0.5;
    }
    .placeholder {
      text-align: center;
      opacity: 0.5;
      font-size: 13px;
    }
  </style>
</head>
<body>
  <div class="toolbar">
    ${selectorHtml}
    <button id="focusBtn" title="Bring native preview window to front">Focus Window</button>
    <span id="status" class="status">Connecting...</span>
  </div>
  <div class="preview-container">
    <img id="preview" alt="Reactor Preview" style="display:none" />
    <div id="placeholder" class="placeholder">Waiting for first frame...</div>
  </div>

  <script>
    const vscode = acquireVsCodeApi();
    const PORT = ${port};
    const img = document.getElementById('preview');
    const placeholder = document.getElementById('placeholder');
    const statusEl = document.getElementById('status');
    const focusBtn = document.getElementById('focusBtn');
    const componentSelect = document.getElementById('componentSelect');

    let frameUrl = 'http://localhost:' + PORT + '/frame';
    let statusUrl = 'http://localhost:' + PORT + '/status';
    let focusUrl = 'http://localhost:' + PORT + '/focus';
    let failCount = 0;
    let visible = true;

    if (componentSelect) {
      componentSelect.addEventListener('change', (e) => {
        vscode.postMessage({
          type: 'selectComponent',
          name: e.target.value
        });
      });
    }

    // Listen for messages from the extension (e.g. update dropdown selection)
    window.addEventListener('message', (event) => {
      const msg = event.data;
      if (msg.type === 'updateSelection' && componentSelect) {
        componentSelect.value = msg.selected;
      }
      if (msg.type === 'updateComponents' && componentSelect) {
        componentSelect.innerHTML = msg.components
          .map(c => '<option value="' + c + '"' + (c === msg.selected ? ' selected' : '') + '>' + c + '</option>')
          .join('');
      }
    });

    document.addEventListener('visibilitychange', () => {
      visible = !document.hidden;
    });

    async function refreshFrame() {
      if (!visible) {
        setTimeout(refreshFrame, 500);
        return;
      }

      try {
        const resp = await fetch(frameUrl, { cache: 'no-store' });
        if (resp.ok && resp.status === 200) {
          const blob = await resp.blob();
          const url = URL.createObjectURL(blob);
          img.onload = () => URL.revokeObjectURL(url);
          img.src = url;
          img.style.display = 'block';
          img.classList.remove('stale');
          placeholder.style.display = 'none';
          failCount = 0;
        }
      } catch {
        failCount++;
        if (failCount > 30) {
          img.classList.add('stale');
          statusEl.textContent = 'Disconnected';
          statusEl.className = 'status error';
        }
      }

      setTimeout(refreshFrame, 100);
    }

    async function refreshStatus() {
      try {
        const resp = await fetch(statusUrl, { cache: 'no-store' });
        if (resp.ok) {
          const data = await resp.json();
          if (data.building) {
            statusEl.textContent = 'Building...';
            statusEl.className = 'status building';
          } else if (data.error) {
            statusEl.textContent = 'Error: ' + data.error;
            statusEl.className = 'status error';
          } else {
            statusEl.textContent = 'Live';
            statusEl.className = 'status';
          }
        }
      } catch { /* ignore */ }

      setTimeout(refreshStatus, 1000);
    }

    focusBtn.addEventListener('click', () => {
      fetch(focusUrl, { method: 'POST' }).catch(() => {});
    });

    img.addEventListener('click', () => {
      fetch(focusUrl, { method: 'POST' }).catch(() => {});
    });

    refreshFrame();
    refreshStatus();
  </script>
</body>
</html>`;
}

// -- Commands ----------------------------------------------------------------

async function stopPreview() {
  editorChangeDisposable?.dispose();
  editorChangeDisposable = undefined;
  await killPreviewProcess();
  currentCsprojPath = undefined;
  currentComponents = [];
  currentComponentName = undefined;
  currentFilePath = undefined;
  statusBarItem.hide();
  if (panel) {
    panel.dispose();
    panel = undefined;
  }
}

async function focusPreviewWindow() {
  if (!capturePort) {
    vscode.window.showWarningMessage("No preview is running.");
    return;
  }

  try {
    await httpPost(`http://localhost:${capturePort}/focus`);
  } catch {
    vscode.window.showWarningMessage("Could not focus preview window.");
  }
}

// -- HTML Helpers ------------------------------------------------------------

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

// -- HTTP Helpers ------------------------------------------------------------

function httpGetJson<T>(url: string): Promise<T> {
  return new Promise((resolve, reject) => {
    const req = http.get(url, (res) => {
      if (res.statusCode && (res.statusCode < 200 || res.statusCode >= 300)) {
        res.resume();
        reject(new Error(`HTTP ${res.statusCode}`));
        return;
      }
      let body = "";
      res.on("data", (chunk) => (body += chunk));
      res.on("end", () => {
        try {
          resolve(JSON.parse(body) as T);
        } catch (e) {
          reject(e);
        }
      });
    });
    req.on("error", reject);
    req.setTimeout(5000, () => {
      req.destroy();
      reject(new Error("timeout"));
    });
  });
}

function httpPostJson<T>(url: string, data: object): Promise<T> {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify(data);
    const req = http.request(
      url,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Content-Length": Buffer.byteLength(body),
        },
      },
      (res) => {
        let resBody = "";
        res.on("data", (chunk) => (resBody += chunk));
        res.on("end", () => {
          try {
            resolve(JSON.parse(resBody) as T);
          } catch (e) {
            reject(e);
          }
        });
      }
    );
    req.on("error", reject);
    req.setTimeout(5000, () => {
      req.destroy();
      reject(new Error("timeout"));
    });
    req.write(body);
    req.end();
  });
}

function httpPost(url: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const req = http.request(url, { method: "POST" }, (res) => {
      res.resume();
      res.on("end", resolve);
    });
    req.on("error", reject);
    req.end();
  });
}
