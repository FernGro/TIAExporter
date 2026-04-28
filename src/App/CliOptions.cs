namespace TIAExporter.App;

internal sealed class CliOptions
{
    public string? ProjectPath { get; private init; }
    public string? OutputPath { get; private init; }
    public bool Headless { get; private init; }
    public bool DiagnosticsOnly { get; private init; }
    public bool CompileCheck { get; private init; }
    public bool CompileBeforeExport { get; private init; }
    public bool IncludeHmi { get; private init; } = true;
    public bool IncludeDrives { get; private init; } = true;
    public bool IncludeLibraries { get; private init; } = true;
    public bool IncludeDocuments { get; private init; } = true;
    public bool Strict { get; private init; }
    public bool ShowHelp { get; private init; }

    public static string HelpText =>
        """
        TIAExporter

        GUI:
          TIAExporter.exe

        CLI:
          TIAExporter.exe --project "C:\Path\Project.ap20" --out "C:\TIA_Export" [options]

        Options:
          --project                 TIA Portal project file (*.ap20, *.ap19, *.ap18, *.ap17, *.ap16, *.ap15)
          --out                     Export target directory
          --headless                true starts TIA without UI. Default: false
          --diagnostics-only        Only create diagnostics and high-level metadata where possible. Default: false
          --compile-check           Try non-invasive compile/consistency discovery. Default: false
          --compile-before-export   Reserved; default false. Project is not modified automatically.
          --include-hmi             Default: true
          --include-drives          Default: true
          --include-libraries       Default: true
          --include-documents       Default: true
          --strict                  Exit non-zero if HIGH CRA readiness gaps exist. Default: false
          --help                    Show this help
        """;

    public static CliOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--help" or "-h" or "/?")
            {
                return new CliOptions { ShowHelp = true };
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument: {arg}");
            }

            var key = arg.Substring(2);
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Missing value for --{key}");
            }

            values[key] = args[++i];
        }

        values.TryGetValue("project", out var project);
        values.TryGetValue("out", out var output);
        var headless = ReadBool(values, "headless", false);
        var diagnosticsOnly = ReadBool(values, "diagnostics-only", false);
        var compileCheck = ReadBool(values, "compile-check", false);
        var compileBeforeExport = ReadBool(values, "compile-before-export", false);
        var includeHmi = ReadBool(values, "include-hmi", true);
        var includeDrives = ReadBool(values, "include-drives", true);
        var includeLibraries = ReadBool(values, "include-libraries", true);
        var includeDocuments = ReadBool(values, "include-documents", true);
        var strict = ReadBool(values, "strict", false);

        if (string.IsNullOrWhiteSpace(project))
        {
            throw new ArgumentException("Missing required option --project.");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ArgumentException("Missing required option --out.");
        }

        return new CliOptions
        {
            ProjectPath = project,
            OutputPath = output,
            Headless = headless,
            DiagnosticsOnly = diagnosticsOnly,
            CompileCheck = compileCheck,
            CompileBeforeExport = compileBeforeExport,
            IncludeHmi = includeHmi,
            IncludeDrives = includeDrives,
            IncludeLibraries = includeLibraries,
            IncludeDocuments = includeDocuments,
            Strict = strict
        };
    }

    private static bool ReadBool(Dictionary<string, string?> values, string key, bool defaultValue)
    {
        if (!values.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text))
        {
            return defaultValue;
        }

        if (!bool.TryParse(text, out var value))
        {
            throw new ArgumentException($"--{key} must be true or false.");
        }

        return value;
    }
}
