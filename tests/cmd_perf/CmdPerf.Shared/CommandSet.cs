namespace CmdPerf.Shared;

[Flags]
public enum EnableFlags
{
    None              = 0,
    HasDocument       = 1 << 0,
    HasSelection      = 1 << 1,
    IsDebugging       = 1 << 2,
    HasProject        = 1 << 3,
    CanUndo           = 1 << 4,
    CanRedo           = 1 << 5,
    IsNotReadOnly     = 1 << 6,
    HasClipboard      = 1 << 7,
    HasMultipleDocs   = 1 << 8,
    IsBuilding        = 1 << 9,
}

public record CommandDef(
    string Id,
    string Label,
    string? IconGlyph,
    string Group,
    EnableFlags Requires
);

/// <summary>
/// 500 realistic commands following VS Code / Visual Studio patterns.
/// </summary>
public static class CommandSet
{
    public static readonly string[] FlagNames =
    [
        "HasDocument", "HasSelection", "IsDebugging", "HasProject", "CanUndo",
        "CanRedo", "IsNotReadOnly", "HasClipboard", "HasMultipleDocs", "IsBuilding",
    ];

    public static readonly EnableFlags[] FlagValues =
    [
        EnableFlags.HasDocument, EnableFlags.HasSelection, EnableFlags.IsDebugging,
        EnableFlags.HasProject, EnableFlags.CanUndo, EnableFlags.CanRedo,
        EnableFlags.IsNotReadOnly, EnableFlags.HasClipboard, EnableFlags.HasMultipleDocs,
        EnableFlags.IsBuilding,
    ];

    public static readonly CommandDef[] All = BuildAll();

    private static CommandDef[] BuildAll()
    {
        var cmds = new List<CommandDef>(500);

        // ── File (50) ───────────────────────────────────────────────
        cmds.Add(new("file.newFile",        "New File",            "Document",   "file", EnableFlags.None));
        cmds.Add(new("file.newWindow",      "New Window",          "NewWindow",  "file", EnableFlags.None));
        cmds.Add(new("file.open",           "Open File...",        "OpenFile",   "file", EnableFlags.None));
        cmds.Add(new("file.openFolder",     "Open Folder...",      "OpenLocal",  "file", EnableFlags.None));
        cmds.Add(new("file.openRecent",     "Open Recent",         "History",    "file", EnableFlags.None));
        cmds.Add(new("file.save",           "Save",                "Save",       "file", EnableFlags.HasDocument));
        cmds.Add(new("file.saveAs",         "Save As...",          "SaveAs",     "file", EnableFlags.HasDocument));
        cmds.Add(new("file.saveAll",        "Save All",            "SaveAll",    "file", EnableFlags.HasDocument));
        cmds.Add(new("file.close",          "Close Editor",        "ChromeClose","file", EnableFlags.HasDocument));
        cmds.Add(new("file.closeAll",       "Close All Editors",   null,         "file", EnableFlags.HasDocument));
        cmds.Add(new("file.closeOthers",    "Close Other Editors", null,         "file", EnableFlags.HasMultipleDocs));
        cmds.Add(new("file.closeSaved",     "Close Saved Editors", null,         "file", EnableFlags.HasDocument));
        cmds.Add(new("file.revert",         "Revert File",         null,         "file", EnableFlags.HasDocument));
        cmds.Add(new("file.print",          "Print...",            "Print",      "file", EnableFlags.HasDocument));
        cmds.Add(new("file.autoSave",       "Auto Save",           null,         "file", EnableFlags.None));
        cmds.Add(new("file.preferences",    "Preferences",         "Settings",   "file", EnableFlags.None));
        cmds.Add(new("file.keyboardShortcuts","Keyboard Shortcuts", "Keyboard",  "file", EnableFlags.None));
        cmds.Add(new("file.userSnippets",   "User Snippets",       null,         "file", EnableFlags.None));
        cmds.Add(new("file.exit",           "Exit",                null,         "file", EnableFlags.None));
        // Recent files sub-items
        for (int i = 1; i <= 10; i++)
            cmds.Add(new($"file.recent{i}", $"Recent File {i}",    null, "file", EnableFlags.None));
        // Additional file commands to reach ~50
        for (int i = 0; i < 21; i++)
            cmds.Add(new($"file.ext{i}",    $"File Action {i + 1}", null, "file", EnableFlags.HasDocument));

        // ── Edit (60) ───────────────────────────────────────────────
        cmds.Add(new("edit.undo",           "Undo",                "Undo",       "edit", EnableFlags.CanUndo));
        cmds.Add(new("edit.redo",           "Redo",                "Redo",       "edit", EnableFlags.CanRedo));
        cmds.Add(new("edit.cut",            "Cut",                 "Cut",        "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.copy",           "Copy",                "Copy",       "edit", EnableFlags.HasSelection));
        cmds.Add(new("edit.paste",          "Paste",               "Paste",      "edit", EnableFlags.HasClipboard | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.delete",         "Delete",              "Delete",     "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.selectAll",      "Select All",          null,         "edit", EnableFlags.HasDocument));
        cmds.Add(new("edit.find",           "Find",                "Find",       "edit", EnableFlags.HasDocument));
        cmds.Add(new("edit.replace",        "Replace",             "FindAndReplace","edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.findInFiles",    "Find in Files",       null,         "edit", EnableFlags.HasProject));
        cmds.Add(new("edit.replaceInFiles", "Replace in Files",    null,         "edit", EnableFlags.HasProject));
        cmds.Add(new("edit.toggleLineComment","Toggle Line Comment",null,        "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.toggleBlockComment","Toggle Block Comment",null,      "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.indentLine",     "Indent Line",         null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.outdentLine",    "Outdent Line",        null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.moveLineUp",     "Move Line Up",        null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.moveLineDown",   "Move Line Down",      null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.copyLineUp",     "Copy Line Up",        null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.copyLineDown",   "Copy Line Down",      null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.deleteLine",     "Delete Line",         null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.trimWhitespace", "Trim Trailing Whitespace",null,     "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.sortLinesAsc",   "Sort Lines Ascending", null,        "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.sortLinesDesc",  "Sort Lines Descending",null,        "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.joinLines",      "Join Lines",          null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.transformUpper", "Transform to Uppercase",null,       "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.transformLower", "Transform to Lowercase",null,       "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.transformTitle", "Transform to Title Case",null,      "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.emmet",          "Emmet: Expand Abbreviation",null,   "edit", EnableFlags.HasDocument));
        cmds.Add(new("edit.formatDocument", "Format Document",     null,         "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("edit.formatSelection","Format Selection",    null,         "edit", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        // Additional edit commands to reach ~60
        for (int i = 0; i < 30; i++)
            cmds.Add(new($"edit.ext{i}",    $"Edit Action {i + 1}", null, "edit", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));

        // ── View (50) ───────────────────────────────────────────────
        cmds.Add(new("view.explorer",       "Explorer",            "FolderOpen", "view", EnableFlags.None));
        cmds.Add(new("view.search",         "Search",              "Find",       "view", EnableFlags.None));
        cmds.Add(new("view.sourceControl",  "Source Control",      "BranchFork2","view", EnableFlags.None));
        cmds.Add(new("view.debug",          "Debug",               "Bug",        "view", EnableFlags.None));
        cmds.Add(new("view.extensions",     "Extensions",          "OEM",        "view", EnableFlags.None));
        cmds.Add(new("view.terminal",       "Terminal",            "CommandPrompt","view",EnableFlags.None));
        cmds.Add(new("view.output",         "Output",              null,         "view", EnableFlags.None));
        cmds.Add(new("view.problems",       "Problems",            null,         "view", EnableFlags.None));
        cmds.Add(new("view.debugConsole",   "Debug Console",       null,         "view", EnableFlags.None));
        cmds.Add(new("view.zoomIn",         "Zoom In",             "ZoomIn",     "view", EnableFlags.None));
        cmds.Add(new("view.zoomOut",        "Zoom Out",            "ZoomOut",    "view", EnableFlags.None));
        cmds.Add(new("view.resetZoom",      "Reset Zoom",          null,         "view", EnableFlags.None));
        cmds.Add(new("view.fullScreen",     "Full Screen",         "FullScreen", "view", EnableFlags.None));
        cmds.Add(new("view.zenMode",        "Zen Mode",            null,         "view", EnableFlags.None));
        cmds.Add(new("view.minimap",        "Toggle Minimap",      null,         "view", EnableFlags.HasDocument));
        cmds.Add(new("view.breadcrumbs",    "Toggle Breadcrumbs",  null,         "view", EnableFlags.HasDocument));
        cmds.Add(new("view.renderWhitespace","Toggle Render Whitespace",null,    "view", EnableFlags.HasDocument));
        cmds.Add(new("view.wordWrap",       "Toggle Word Wrap",    null,         "view", EnableFlags.HasDocument));
        cmds.Add(new("view.statusBar",      "Toggle Status Bar",   null,         "view", EnableFlags.None));
        cmds.Add(new("view.activityBar",    "Toggle Activity Bar", null,         "view", EnableFlags.None));
        cmds.Add(new("view.sideBar",        "Toggle Side Bar",     null,         "view", EnableFlags.None));
        cmds.Add(new("view.panel",          "Toggle Panel",        null,         "view", EnableFlags.None));
        for (int i = 0; i < 28; i++)
            cmds.Add(new($"view.ext{i}",    $"View Option {i + 1}", null, "view", EnableFlags.None));

        // ── Navigate (40) ───────────────────────────────────────────
        cmds.Add(new("nav.goToFile",        "Go to File...",       "OpenFile",   "navigate", EnableFlags.HasProject));
        cmds.Add(new("nav.goToLine",        "Go to Line...",       null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.goToSymbol",      "Go to Symbol...",     null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.goToDefinition",  "Go to Definition",    null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.goToDeclaration", "Go to Declaration",   null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.goToTypeDefinition","Go to Type Definition",null,      "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.goToImplementation","Go to Implementation",null,       "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.findAllReferences","Find All References",null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.peekDefinition",  "Peek Definition",     null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.back",            "Go Back",             "Back",       "navigate", EnableFlags.None));
        cmds.Add(new("nav.forward",         "Go Forward",          "Forward",    "navigate", EnableFlags.None));
        cmds.Add(new("nav.nextError",       "Next Error",          null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.prevError",       "Previous Error",      null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.nextChange",      "Next Change",         null,         "navigate", EnableFlags.HasDocument));
        cmds.Add(new("nav.prevChange",      "Previous Change",     null,         "navigate", EnableFlags.HasDocument));
        for (int i = 0; i < 25; i++)
            cmds.Add(new($"nav.ext{i}",     $"Navigate Action {i + 1}", null, "navigate", EnableFlags.HasDocument));

        // ── Build (30) ──────────────────────────────────────────────
        cmds.Add(new("build.build",         "Build Solution",      "Build",      "build", EnableFlags.HasProject));
        cmds.Add(new("build.rebuild",       "Rebuild Solution",    null,         "build", EnableFlags.HasProject));
        cmds.Add(new("build.clean",         "Clean Solution",      null,         "build", EnableFlags.HasProject));
        cmds.Add(new("build.buildProject",  "Build Project",       null,         "build", EnableFlags.HasProject));
        cmds.Add(new("build.cancel",        "Cancel Build",        "Cancel",     "build", EnableFlags.IsBuilding));
        cmds.Add(new("build.batchBuild",    "Batch Build...",      null,         "build", EnableFlags.HasProject));
        for (int i = 0; i < 24; i++)
            cmds.Add(new($"build.ext{i}",   $"Build Action {i + 1}", null, "build", EnableFlags.HasProject));

        // ── Debug (50) ──────────────────────────────────────────────
        cmds.Add(new("debug.start",         "Start Debugging",     "Play",       "debug", EnableFlags.HasProject));
        cmds.Add(new("debug.startNoDebug",  "Start Without Debugging","PlaybackRate1x","debug",EnableFlags.HasProject));
        cmds.Add(new("debug.stop",          "Stop Debugging",      "Stop",       "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.restart",       "Restart Debugging",   "Refresh",    "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.stepOver",      "Step Over",           null,         "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.stepInto",      "Step Into",           null,         "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.stepOut",       "Step Out",            null,         "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.continue",      "Continue",            null,         "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.toggleBreakpoint","Toggle Breakpoint", null,         "debug", EnableFlags.HasDocument));
        cmds.Add(new("debug.newBreakpoint", "New Breakpoint",      null,         "debug", EnableFlags.HasDocument));
        cmds.Add(new("debug.deleteAll",     "Delete All Breakpoints",null,       "debug", EnableFlags.None));
        cmds.Add(new("debug.disableAll",    "Disable All Breakpoints",null,      "debug", EnableFlags.None));
        cmds.Add(new("debug.addWatch",      "Add Watch",           null,         "debug", EnableFlags.IsDebugging));
        cmds.Add(new("debug.callStack",     "Show Call Stack",     null,         "debug", EnableFlags.IsDebugging));
        for (int i = 0; i < 36; i++)
            cmds.Add(new($"debug.ext{i}",   $"Debug Action {i + 1}", null, "debug", i < 18 ? EnableFlags.IsDebugging : EnableFlags.HasProject));

        // ── Test (30) ───────────────────────────────────────────────
        cmds.Add(new("test.runAll",         "Run All Tests",       "Play",       "test", EnableFlags.HasProject));
        cmds.Add(new("test.runFailed",      "Run Failed Tests",    null,         "test", EnableFlags.HasProject));
        cmds.Add(new("test.debugTests",     "Debug Tests",         "Bug",        "test", EnableFlags.HasProject));
        cmds.Add(new("test.coverage",       "Run with Coverage",   null,         "test", EnableFlags.HasProject));
        cmds.Add(new("test.cancelRun",      "Cancel Test Run",     "Cancel",     "test", EnableFlags.IsBuilding));
        for (int i = 0; i < 25; i++)
            cmds.Add(new($"test.ext{i}",    $"Test Action {i + 1}", null, "test", EnableFlags.HasProject));

        // ── Tools (40) ──────────────────────────────────────────────
        cmds.Add(new("tools.commandPalette","Command Palette...",  "CommandPrompt","tools",EnableFlags.None));
        cmds.Add(new("tools.options",       "Options...",          "Settings",   "tools", EnableFlags.None));
        cmds.Add(new("tools.extensions",    "Extensions...",       "OEM",        "tools", EnableFlags.None));
        cmds.Add(new("tools.snippets",      "User Snippets...",    null,         "tools", EnableFlags.None));
        cmds.Add(new("tools.formatDoc",     "Format Document",     null,         "tools", EnableFlags.HasDocument | EnableFlags.IsNotReadOnly));
        cmds.Add(new("tools.formatSel",     "Format Selection",    null,         "tools", EnableFlags.HasSelection | EnableFlags.IsNotReadOnly));
        cmds.Add(new("tools.nuget",         "NuGet Package Manager",null,        "tools", EnableFlags.HasProject));
        cmds.Add(new("tools.terminal",      "Open Terminal",       "CommandPrompt","tools",EnableFlags.None));
        for (int i = 0; i < 32; i++)
            cmds.Add(new($"tools.ext{i}",   $"Tool Action {i + 1}", null, "tools", i < 16 ? EnableFlags.HasProject : EnableFlags.None));

        // ── Window (30) ─────────────────────────────────────────────
        cmds.Add(new("window.newWindow",    "New Window",          "NewWindow",  "window", EnableFlags.None));
        cmds.Add(new("window.splitRight",   "Split Editor Right",  null,         "window", EnableFlags.HasDocument));
        cmds.Add(new("window.splitDown",    "Split Editor Down",   null,         "window", EnableFlags.HasDocument));
        cmds.Add(new("window.closeGroup",   "Close Editor Group",  null,         "window", EnableFlags.HasMultipleDocs));
        cmds.Add(new("window.toggleSidebar","Toggle Sidebar",      null,         "window", EnableFlags.None));
        cmds.Add(new("window.togglePanel",  "Toggle Panel",        null,         "window", EnableFlags.None));
        for (int i = 0; i < 24; i++)
            cmds.Add(new($"window.ext{i}",  $"Window Action {i + 1}", null, "window", i < 12 ? EnableFlags.HasDocument : EnableFlags.None));

        // ── Help (20) ───────────────────────────────────────────────
        cmds.Add(new("help.about",          "About",               "Info",       "help", EnableFlags.None));
        cmds.Add(new("help.docs",           "Documentation",       "Library",    "help", EnableFlags.None));
        cmds.Add(new("help.releaseNotes",   "Release Notes",       null,         "help", EnableFlags.None));
        cmds.Add(new("help.reportIssue",    "Report Issue",        null,         "help", EnableFlags.None));
        cmds.Add(new("help.checkUpdates",   "Check for Updates",   null,         "help", EnableFlags.None));
        cmds.Add(new("help.tipsTricks",     "Tips and Tricks",     null,         "help", EnableFlags.None));
        for (int i = 0; i < 14; i++)
            cmds.Add(new($"help.ext{i}",    $"Help Topic {i + 1}", null, "help", EnableFlags.None));

        // Pad to exactly 500 with extension commands
        while (cmds.Count < 500)
        {
            int idx = cmds.Count - 400;
            var group = idx % 2 == 0 ? "tools" : "edit";
            cmds.Add(new($"ext.cmd{idx}", $"Extension Command {idx}", null, group,
                idx % 3 == 0 ? EnableFlags.HasDocument : EnableFlags.None));
        }

        return cmds.ToArray();
    }

    /// <summary>Count how many commands are enabled for the given flags.</summary>
    public static int CountEnabled(EnableFlags flags)
    {
        int count = 0;
        foreach (var cmd in All)
            if (cmd.Requires == EnableFlags.None || (cmd.Requires & flags) == cmd.Requires)
                count++;
        return count;
    }

    /// <summary>Check if a specific command is enabled for the given flags.</summary>
    public static bool IsEnabled(CommandDef cmd, EnableFlags flags) =>
        cmd.Requires == EnableFlags.None || (cmd.Requires & flags) == cmd.Requires;
}
