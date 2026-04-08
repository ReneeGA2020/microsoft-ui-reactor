namespace CmdPerf.Shared;

public sealed class CmdCliOptions
{
    public bool Headless { get; set; }
    public int DurationSeconds { get; set; } = 10;
    public string Scenario { get; set; } = "all"; // mount, toggle, bulk, all
    public int Iterations { get; set; } = 50;

    public static CmdCliOptions Parse(string[] args)
    {
        var opts = new CmdCliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--headless":
                    opts.Headless = true;
                    break;
                case "--duration" when i + 1 < args.Length:
                    opts.DurationSeconds = int.Parse(args[++i]);
                    break;
                case "--scenario" when i + 1 < args.Length:
                    opts.Scenario = args[++i];
                    break;
                case "--iterations" when i + 1 < args.Length:
                    opts.Iterations = int.Parse(args[++i]);
                    break;
            }
        }
        return opts;
    }
}
