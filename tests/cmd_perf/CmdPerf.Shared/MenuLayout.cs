namespace CmdPerf.Shared;

/// <summary>
/// Defines the menu structure and toolbar layout. Items reference CommandDef.Id values.
/// </summary>
public static class MenuLayout
{
    public record MenuItem(string CommandId);
    public record Separator;
    public record SubMenu(string Label, object[] Items);

    /// <summary>Returns menu bar definition: (menuTitle, items[]) where items can be MenuItem, Separator, or SubMenu.</summary>
    public static (string Title, object[] Items)[] GetMenuBar()
    {
        var cmdById = CommandSet.All.ToDictionary(c => c.Id);

        // Helper: grab all commands matching a prefix, up to N
        object[] ItemsForGroup(string group, int max = 50) =>
            CommandSet.All.Where(c => c.Group == group).Take(max).Select(c => (object)new MenuItem(c.Id)).ToArray();

        return
        [
            ("File", BuildFileMenu()),
            ("Edit", BuildEditMenu()),
            ("View", BuildViewMenu()),
            ("Navigate", BuildNavMenu()),
            ("Build", BuildBuildMenu()),
            ("Debug", BuildDebugMenu()),
            ("Test", BuildTestMenu()),
            ("Tools", BuildToolsMenu()),
            ("Window", BuildWindowMenu()),
            ("Help", BuildHelpMenu()),
        ];
    }

    private static object[] BuildFileMenu()
    {
        var items = new List<object>
        {
            new MenuItem("file.newFile"), new MenuItem("file.newWindow"),
            new Separator(),
            new MenuItem("file.open"), new MenuItem("file.openFolder"),
            new SubMenu("Open Recent", Enumerable.Range(1, 10).Select(i => (object)new MenuItem($"file.recent{i}")).ToArray()),
            new Separator(),
            new MenuItem("file.save"), new MenuItem("file.saveAs"), new MenuItem("file.saveAll"),
            new Separator(),
            new MenuItem("file.close"), new MenuItem("file.closeAll"), new MenuItem("file.closeOthers"), new MenuItem("file.closeSaved"),
            new Separator(),
            new MenuItem("file.revert"),
            new MenuItem("file.autoSave"),
            new Separator(),
            new MenuItem("file.print"),
            new Separator(),
            new MenuItem("file.preferences"), new MenuItem("file.keyboardShortcuts"), new MenuItem("file.userSnippets"),
            new Separator(),
            new MenuItem("file.exit"),
        };
        // Add extension items
        for (int i = 0; i < 10; i++) items.Add(new MenuItem($"file.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildEditMenu()
    {
        var items = new List<object>
        {
            new MenuItem("edit.undo"), new MenuItem("edit.redo"),
            new Separator(),
            new MenuItem("edit.cut"), new MenuItem("edit.copy"), new MenuItem("edit.paste"), new MenuItem("edit.delete"),
            new Separator(),
            new MenuItem("edit.selectAll"),
            new MenuItem("edit.find"), new MenuItem("edit.replace"),
            new MenuItem("edit.findInFiles"), new MenuItem("edit.replaceInFiles"),
            new Separator(),
            new SubMenu("Advanced", new object[]
            {
                new MenuItem("edit.toggleLineComment"), new MenuItem("edit.toggleBlockComment"),
                new Separator(),
                new MenuItem("edit.indentLine"), new MenuItem("edit.outdentLine"),
                new MenuItem("edit.moveLineUp"), new MenuItem("edit.moveLineDown"),
                new MenuItem("edit.copyLineUp"), new MenuItem("edit.copyLineDown"),
                new MenuItem("edit.deleteLine"),
                new Separator(),
                new MenuItem("edit.trimWhitespace"), new MenuItem("edit.joinLines"),
            }),
            new SubMenu("Transform", new object[]
            {
                new MenuItem("edit.transformUpper"), new MenuItem("edit.transformLower"), new MenuItem("edit.transformTitle"),
                new MenuItem("edit.sortLinesAsc"), new MenuItem("edit.sortLinesDesc"),
            }),
            new Separator(),
            new MenuItem("edit.formatDocument"), new MenuItem("edit.formatSelection"),
            new MenuItem("edit.emmet"),
        };
        for (int i = 0; i < 15; i++) items.Add(new MenuItem($"edit.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildViewMenu()
    {
        var items = new List<object>
        {
            new MenuItem("view.explorer"), new MenuItem("view.search"), new MenuItem("view.sourceControl"),
            new MenuItem("view.debug"), new MenuItem("view.extensions"),
            new Separator(),
            new MenuItem("view.terminal"), new MenuItem("view.output"), new MenuItem("view.problems"), new MenuItem("view.debugConsole"),
            new Separator(),
            new SubMenu("Appearance", new object[]
            {
                new MenuItem("view.fullScreen"), new MenuItem("view.zenMode"),
                new Separator(),
                new MenuItem("view.statusBar"), new MenuItem("view.activityBar"), new MenuItem("view.sideBar"), new MenuItem("view.panel"),
                new Separator(),
                new MenuItem("view.zoomIn"), new MenuItem("view.zoomOut"), new MenuItem("view.resetZoom"),
            }),
            new Separator(),
            new MenuItem("view.minimap"), new MenuItem("view.breadcrumbs"),
            new MenuItem("view.renderWhitespace"), new MenuItem("view.wordWrap"),
        };
        for (int i = 0; i < 15; i++) items.Add(new MenuItem($"view.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildNavMenu()
    {
        var items = new List<object>
        {
            new MenuItem("nav.back"), new MenuItem("nav.forward"),
            new Separator(),
            new MenuItem("nav.goToFile"), new MenuItem("nav.goToLine"), new MenuItem("nav.goToSymbol"),
            new Separator(),
            new MenuItem("nav.goToDefinition"), new MenuItem("nav.goToDeclaration"),
            new MenuItem("nav.goToTypeDefinition"), new MenuItem("nav.goToImplementation"),
            new MenuItem("nav.findAllReferences"), new MenuItem("nav.peekDefinition"),
            new Separator(),
            new MenuItem("nav.nextError"), new MenuItem("nav.prevError"),
            new MenuItem("nav.nextChange"), new MenuItem("nav.prevChange"),
        };
        for (int i = 0; i < 8; i++) items.Add(new MenuItem($"nav.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildBuildMenu()
    {
        var items = new List<object>
        {
            new MenuItem("build.build"), new MenuItem("build.rebuild"), new MenuItem("build.clean"),
            new Separator(),
            new MenuItem("build.buildProject"),
            new MenuItem("build.batchBuild"),
            new Separator(),
            new MenuItem("build.cancel"),
        };
        for (int i = 0; i < 12; i++) items.Add(new MenuItem($"build.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildDebugMenu()
    {
        var items = new List<object>
        {
            new MenuItem("debug.start"), new MenuItem("debug.startNoDebug"),
            new Separator(),
            new MenuItem("debug.stop"), new MenuItem("debug.restart"),
            new Separator(),
            new MenuItem("debug.stepOver"), new MenuItem("debug.stepInto"), new MenuItem("debug.stepOut"), new MenuItem("debug.continue"),
            new Separator(),
            new SubMenu("Breakpoints", new object[]
            {
                new MenuItem("debug.toggleBreakpoint"), new MenuItem("debug.newBreakpoint"),
                new Separator(),
                new MenuItem("debug.deleteAll"), new MenuItem("debug.disableAll"),
            }),
            new Separator(),
            new MenuItem("debug.addWatch"), new MenuItem("debug.callStack"),
        };
        for (int i = 0; i < 18; i++) items.Add(new MenuItem($"debug.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildTestMenu()
    {
        var items = new List<object>
        {
            new MenuItem("test.runAll"), new MenuItem("test.runFailed"),
            new Separator(),
            new MenuItem("test.debugTests"), new MenuItem("test.coverage"),
            new Separator(),
            new MenuItem("test.cancelRun"),
        };
        for (int i = 0; i < 14; i++) items.Add(new MenuItem($"test.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildToolsMenu()
    {
        var items = new List<object>
        {
            new MenuItem("tools.commandPalette"),
            new Separator(),
            new MenuItem("tools.options"), new MenuItem("tools.extensions"),
            new Separator(),
            new SubMenu("Snippets", new object[]
            {
                new MenuItem("tools.snippets"),
            }),
            new MenuItem("tools.formatDoc"), new MenuItem("tools.formatSel"),
            new Separator(),
            new MenuItem("tools.nuget"),
            new MenuItem("tools.terminal"),
        };
        for (int i = 0; i < 20; i++) items.Add(new MenuItem($"tools.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildWindowMenu()
    {
        var items = new List<object>
        {
            new MenuItem("window.newWindow"),
            new Separator(),
            new MenuItem("window.splitRight"), new MenuItem("window.splitDown"),
            new MenuItem("window.closeGroup"),
            new Separator(),
            new MenuItem("window.toggleSidebar"), new MenuItem("window.togglePanel"),
        };
        for (int i = 0; i < 12; i++) items.Add(new MenuItem($"window.ext{i}"));
        return items.ToArray();
    }

    private static object[] BuildHelpMenu()
    {
        var items = new List<object>
        {
            new MenuItem("help.about"), new MenuItem("help.docs"),
            new Separator(),
            new MenuItem("help.releaseNotes"), new MenuItem("help.tipsTricks"),
            new Separator(),
            new MenuItem("help.reportIssue"), new MenuItem("help.checkUpdates"),
        };
        for (int i = 0; i < 8; i++) items.Add(new MenuItem($"help.ext{i}"));
        return items.ToArray();
    }

    /// <summary>30 toolbar commands — the most commonly used actions.</summary>
    public static readonly string[] ToolbarCommandIds =
    [
        "file.newFile", "file.open", "file.save", "file.saveAll",
        "edit.undo", "edit.redo",
        "edit.cut", "edit.copy", "edit.paste",
        "edit.find", "edit.replace",
        "nav.back", "nav.forward",
        "nav.goToFile", "nav.goToLine",
        "build.build", "build.cancel",
        "debug.start", "debug.stop", "debug.restart",
        "debug.stepOver", "debug.stepInto", "debug.stepOut",
        "test.runAll", "test.debugTests",
        "tools.commandPalette", "tools.terminal",
        "view.explorer", "view.search", "view.sourceControl",
    ];
}
