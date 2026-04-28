using System.Windows.Forms;
using TIAExporter.App;
using TIAExporter.Gui;
using TIAExporter.Logging;

namespace TIAExporter;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.WriteLine(CliOptions.HelpText);
            return 2;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(CliOptions.HelpText);
            return 0;
        }

        using var logger = ExportLogger.CreateConsoleLogger();
        var app = new ExportApplication(logger);
        var result = app.Run(new ExportSettings(
            options.ProjectPath!,
            options.OutputPath!,
            options.Headless,
            options.DiagnosticsOnly,
            options.CompileCheck,
            options.CompileBeforeExport,
            options.IncludeHmi,
            options.IncludeDrives,
            options.IncludeLibraries,
            options.IncludeDocuments,
            options.Strict));

        Console.WriteLine(result.StatusText);
        return result.Success ? 0 : 1;
    }
}
