using TIAExporter.Tia;

namespace TIAExporter.Tests;

internal static class TiaProjectFileTypesTests
{
    internal static void Run()
    {
        TestZap17SupportedAsArchive();
        TestAp17SupportedAsProject();
        TestUnsupportedExtensionRejected();
        TestVersionParsedFromArchiveExtension();
        TestShortArchiveRetrieveRootUsesTempFolder();

        Console.WriteLine("[PASS] All TiaProjectFileTypes tests passed.");
    }

    private static void TestZap17SupportedAsArchive()
    {
        const string path = @"C:\Projects\Line1.zap17";
        Assert(TiaProjectFileTypes.IsSupported(path), nameof(TestZap17SupportedAsArchive) + "_supported");
        Assert(TiaProjectFileTypes.IsArchive(path), nameof(TestZap17SupportedAsArchive) + "_archive");
    }

    private static void TestAp17SupportedAsProject()
    {
        const string path = @"C:\Projects\Line1.ap17";
        Assert(TiaProjectFileTypes.IsSupported(path), nameof(TestAp17SupportedAsProject) + "_supported");
        Assert(!TiaProjectFileTypes.IsArchive(path), nameof(TestAp17SupportedAsProject) + "_not_archive");
    }

    private static void TestUnsupportedExtensionRejected()
    {
        Assert(!TiaProjectFileTypes.IsSupported(@"C:\Projects\Line1.zip"), nameof(TestUnsupportedExtensionRejected));
    }

    private static void TestVersionParsedFromArchiveExtension()
    {
        AssertEqual(17, TiaProjectFileTypes.GetVersionFromExtension(@"C:\Projects\Line1.zap17"), nameof(TestVersionParsedFromArchiveExtension));
    }

    private static void TestShortArchiveRetrieveRootUsesTempFolder()
    {
        var root = TiaProjectFileTypes.GetShortArchiveRetrieveRoot();
        Assert(root.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase), nameof(TestShortArchiveRetrieveRootUsesTempFolder) + "_temp");
        Assert(root.Contains(TiaProjectFileTypes.TempArchiveRetrieveFolderName), nameof(TestShortArchiveRetrieveRootUsesTempFolder) + "_folder");
        Assert(root.Length < 80, nameof(TestShortArchiveRetrieveRootUsesTempFolder) + "_short");
    }

    private static void Assert(bool condition, string testName)
    {
        if (!condition) throw new Exception($"[FAIL] {testName}");
    }

    private static void AssertEqual<T>(T expected, T actual, string testName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"[FAIL] {testName}: expected '{expected}', got '{actual}'");
        }
    }
}
