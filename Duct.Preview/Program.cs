using Duct;
using Duct.Preview;
using IOPath = System.IO.Path;

if (args.Length < 2)
{
    Console.WriteLine("Duct Preview — Live component previewer");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run --project Duct.Preview -- <project.csproj> <ComponentTypeName>");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run --project Duct.Preview -- samples/TodoApp/TodoApp.csproj App");
    Console.WriteLine("  dotnet run --project Duct.Preview -- samples/apps/ductfiles/DuctFiles.csproj DuctFilesApp");
    return 1;
}

var projectPath = IOPath.GetFullPath(args[0]);
var componentTypeName = args[1];

if (!File.Exists(projectPath))
{
    Console.Error.WriteLine($"Error: Project file not found: {projectPath}");
    return 1;
}

Console.WriteLine($"[preview] Project:   {projectPath}");
Console.WriteLine($"[preview] Component: {componentTypeName}");
Console.WriteLine($"[preview] Watching for changes...");

var engine = new PreviewEngine(projectPath, componentTypeName);

DuctApp.Run(
    $"Duct Preview — {IOPath.GetFileName(projectPath)}",
    (ctx) =>
    {
        ctx.UseEffect(() =>
        {
            engine.Start();
            return () => engine.Stop();
        });

        var content = engine.RenderContent(ctx);

        if (!engine.IsBuilding)
            return content;

        // Overlay a semi-transparent wash with a spinner on top of the current content
        return UI.Grid(["*"], ["*"],
            // Layer 1: the actual content (or previous content / error)
            content,
            // Layer 2: building overlay
            UI.Border(
                UI.VStack(8,
                    UI.ProgressRing().Width(48).Height(48),
                    UI.Text("Building...").FontSize(14).Foreground("#ffffff")
                        .HAlign(Microsoft.UI.Xaml.HorizontalAlignment.Center)
                ).HAlign(Microsoft.UI.Xaml.HorizontalAlignment.Center)
                 .VAlign(Microsoft.UI.Xaml.VerticalAlignment.Center)
            ).Background("#C0000000") // semi-transparent black
        );
    },
    configure: host => host.Reconciler.Pool.Enabled = false
);

return 0;
