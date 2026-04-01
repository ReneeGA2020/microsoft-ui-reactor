namespace StressPerf.Shared;

public sealed class CliOptions
{
    public double Percent { get; set; } = 10;
    public int DurationSeconds { get; set; } = 10;
    public bool Headless { get; set; }

    public static CliOptions Parse(string[] args)
    {
        var opts = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--percent" when i + 1 < args.Length:
                    opts.Percent = double.Parse(args[++i]);
                    break;
                case "--duration" when i + 1 < args.Length:
                    opts.DurationSeconds = int.Parse(args[++i]);
                    break;
                case "--headless":
                    opts.Headless = true;
                    break;
            }
        }
        return opts;
    }
}
