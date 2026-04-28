using TIAExporter.App;
using TIAExporter.Logging;

namespace TIAExporter.Gui;

internal sealed class MainForm : Form
{
    private readonly TextBox projectTextBox = new();
    private readonly TextBox outputTextBox = new();
    private readonly CheckBox uiCheckBox = new();
    private readonly CheckBox includeHmiCheckBox = new();
    private readonly CheckBox includeDrivesCheckBox = new();
    private readonly CheckBox includeLibrariesCheckBox = new();
    private readonly CheckBox includeDocumentsCheckBox = new();
    private readonly CheckBox strictCheckBox = new();
    private readonly Button exportButton = new();
    private readonly Button diagnosticsButton = new();
    private readonly Button openOutputButton = new();
    private readonly TextBox logTextBox = new();
    private readonly Label statusLabel = new();
    private readonly Label scoreLabel = new();
    private string? lastExportFolder;

    public MainForm()
    {
        Text = "TIAExporter";
        Width = 980;
        Height = 680;
        MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 8,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));

        AddLabel(root, "Projektdatei:", 0);
        projectTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(projectTextBox, 1, 0);
        root.Controls.Add(CreateBrowseProjectButton(), 2, 0);

        AddLabel(root, "Exportordner:", 1);
        outputTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(outputTextBox, 1, 1);
        root.Controls.Add(CreateBrowseFolderButton(), 2, 1);

        uiCheckBox.Text = "TIA mit Benutzeroberfläche starten";
        uiCheckBox.Checked = true;
        uiCheckBox.Dock = DockStyle.Left;
        root.Controls.Add(uiCheckBox, 1, 2);

        var optionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        includeHmiCheckBox.Text = "HMI"; includeHmiCheckBox.Checked = true;
        includeDrivesCheckBox.Text = "Drives"; includeDrivesCheckBox.Checked = true;
        includeLibrariesCheckBox.Text = "Libraries"; includeLibrariesCheckBox.Checked = true;
        includeDocumentsCheckBox.Text = "Documents"; includeDocumentsCheckBox.Checked = true;
        strictCheckBox.Text = "Strict";
        optionsPanel.Controls.AddRange(new Control[] { includeHmiCheckBox, includeDrivesCheckBox, includeLibrariesCheckBox, includeDocumentsCheckBox, strictCheckBox });
        root.SetColumnSpan(optionsPanel, 2);
        root.Controls.Add(optionsPanel, 1, 3);

        var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        exportButton.Text = "Export starten";
        exportButton.Width = 140;
        exportButton.Height = 30;
        exportButton.Click += (_, _) => StartExport(false);
        diagnosticsButton.Text = "Diagnostics-only";
        diagnosticsButton.Width = 140;
        diagnosticsButton.Height = 30;
        diagnosticsButton.Click += (_, _) => StartExport(true);
        openOutputButton.Text = "Ordner öffnen";
        openOutputButton.Width = 120;
        openOutputButton.Height = 30;
        openOutputButton.Enabled = false;
        openOutputButton.Click += (_, _) => OpenOutputFolder();
        buttonsPanel.Controls.AddRange(new Control[] { exportButton, diagnosticsButton, openOutputButton });
        root.SetColumnSpan(buttonsPanel, 2);
        root.Controls.Add(buttonsPanel, 1, 4);

        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Multiline = true;
        logTextBox.ScrollBars = ScrollBars.Both;
        logTextBox.ReadOnly = true;
        logTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        root.SetColumnSpan(logTextBox, 3);
        root.Controls.Add(logTextBox, 0, 5);

        statusLabel.Text = "Bereit";
        statusLabel.Dock = DockStyle.Fill;
        root.SetColumnSpan(statusLabel, 2);
        root.Controls.Add(statusLabel, 0, 6);

        scoreLabel.Text = "Score: -";
        scoreLabel.Dock = DockStyle.Fill;
        root.Controls.Add(scoreLabel, 2, 6);

        Controls.Add(root);
    }

    private static void AddLabel(TableLayoutPanel root, string text, int row)
    {
        var label = new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };
        root.Controls.Add(label, 0, row);
    }

    private Button CreateBrowseProjectButton()
    {
        var button = new Button { Text = "Durchsuchen", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "TIA Projekte (*.ap20;*.ap19;*.ap18;*.ap17;*.ap16;*.ap15)|*.ap20;*.ap19;*.ap18;*.ap17;*.ap16;*.ap15|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                projectTextBox.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(outputTextBox.Text))
                {
                    outputTextBox.Text = Path.Combine(Path.GetDirectoryName(dialog.FileName) ?? "", "TIA_Export");
                }
            }
        };
        return button;
    }

    private Button CreateBrowseFolderButton()
    {
        var button = new Button { Text = "Durchsuchen", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                outputTextBox.Text = dialog.SelectedPath;
            }
        };
        return button;
    }

    private void StartExport(bool diagnosticsOnly)
    {
        exportButton.Enabled = false;
        diagnosticsButton.Enabled = false;
        openOutputButton.Enabled = false;
        logTextBox.Clear();
        statusLabel.Text = "Export läuft...";
        scoreLabel.Text = "Score: -";

        var output = outputTextBox.Text;
        var settings = new ExportSettings(
            projectTextBox.Text,
            output,
            !uiCheckBox.Checked,
            diagnosticsOnly,
            false,
            false,
            includeHmiCheckBox.Checked,
            includeDrivesCheckBox.Checked,
            includeLibrariesCheckBox.Checked,
            includeDocumentsCheckBox.Checked,
            strictCheckBox.Checked);

        Task.Run(() =>
        {
            using var logger = ExportLogger.CreateGuiLogger(AppendLog);
            var app = new ExportApplication(logger);
            return app.Run(settings);
        }).ContinueWith(task =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(() =>
            {
                exportButton.Enabled = true;
                diagnosticsButton.Enabled = true;
                if (task.Exception != null)
                {
                    statusLabel.Text = "Fehler";
                    AppendLog(task.Exception.GetBaseException().Message);
                }
                else
                {
                    statusLabel.Text = task.Result.StatusText;
                    lastExportFolder = ResolveLastExportFolder(output);
                    openOutputButton.Enabled = Directory.Exists(lastExportFolder);
                    UpdateScoreLabel(lastExportFolder);
                }
            });
        });
    }

    private void OpenOutputFolder()
    {
        if (lastExportFolder == null || !Directory.Exists(lastExportFolder))
        {
            return;
        }

        System.Diagnostics.Process.Start("explorer.exe", lastExportFolder);
    }

    private static string? ResolveLastExportFolder(string output)
    {
        if (!Directory.Exists(output))
        {
            return output;
        }

        return Directory.EnumerateDirectories(output, "export_*")
            .OrderByDescending(Directory.GetCreationTimeUtc)
            .FirstOrDefault() ?? output;
    }

    private void UpdateScoreLabel(string? exportFolder)
    {
        if (string.IsNullOrWhiteSpace(exportFolder))
        {
            return;
        }

        var scorePath = Path.Combine(exportFolder!, "normalized", "export_quality_score.json");
        if (!File.Exists(scorePath))
        {
            return;
        }

        var text = File.ReadAllText(scorePath);
        var marker = "\"score\": ";
        var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return;
        }

        var start = index + marker.Length;
        var digits = new string(text.Skip(start).TakeWhile(char.IsDigit).ToArray());
        scoreLabel.Text = string.IsNullOrWhiteSpace(digits) ? "Score: -" : $"Score: {digits}/100";
    }

    private void AppendLog(string line)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(line));
            return;
        }

        logTextBox.AppendText(line + Environment.NewLine);
    }
}
