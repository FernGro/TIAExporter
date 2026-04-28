using TIAExporter.Tia;

namespace TIAExporter.Tests;

internal sealed class LicenseNotFoundException : Exception
{
    public LicenseNotFoundException(string message) : base(message) { }
}

internal sealed class FakeLicenseException : Exception
{
    public FakeLicenseException(string message) : base(message) { }
}

internal static class LicenseExceptionHelperTests
{
    internal static void Run()
    {
        TestIsLicenseMissingException_ByExactTypeName();
        TestIsLicenseMissingException_SimilarTypeName_NotEnough();
        TestIsLicenseMissingException_ByMessage_LicenseMissing();
        TestIsLicenseMissingException_ByMessage_NecessaryLicense();
        TestIsLicenseMissingException_Null();
        TestIsLicenseMissingException_OtherException();

        TestExtractMissingLicenseName_StandardMessage();
        TestExtractMissingLicenseName_NoMarker_FallsBackToDefault();

        TestLicenseSuspectReason_ContainsLicenseName();
        TestLicenseSuspectReason_Null_ContainsFallback();

        TestLicenseRequiredAction_ContainsAlmHint();
        TestLicenseRequiredAction_ContainsSessionHint();

        TestTruncateMessage_ShortMessage_Unchanged();
        TestTruncateMessage_LongMessage_Truncated();
        TestTruncateMessage_ExactLength_Unchanged();

        Console.WriteLine("[PASS] All LicenseExceptionHelper tests passed.");
    }

    private static void TestIsLicenseMissingException_ByExactTypeName()
    {
        var ex = new LicenseNotFoundException("something failed");
        Assert(LicenseExceptionHelper.IsLicenseMissingException(ex), nameof(TestIsLicenseMissingException_ByExactTypeName));
    }

    private static void TestIsLicenseMissingException_SimilarTypeName_NotEnough()
    {
        var ex = new FakeLicenseException("something failed");
        Assert(!LicenseExceptionHelper.IsLicenseMissingException(ex), nameof(TestIsLicenseMissingException_SimilarTypeName_NotEnough));
    }

    private static void TestIsLicenseMissingException_ByMessage_LicenseMissing()
    {
        var ex = new Exception("The license is missing for this operation.");
        Assert(LicenseExceptionHelper.IsLicenseMissingException(ex), nameof(TestIsLicenseMissingException_ByMessage_LicenseMissing));
    }

    private static void TestIsLicenseMissingException_ByMessage_NecessaryLicense()
    {
        var ex = new Exception("Necessary license 'STEP 7 Professional' is missing.");
        Assert(LicenseExceptionHelper.IsLicenseMissingException(ex), nameof(TestIsLicenseMissingException_ByMessage_NecessaryLicense));
    }

    private static void TestIsLicenseMissingException_Null()
    {
        Assert(!LicenseExceptionHelper.IsLicenseMissingException(null), nameof(TestIsLicenseMissingException_Null));
    }

    private static void TestIsLicenseMissingException_OtherException()
    {
        var ex = new InvalidOperationException("Block is not consistent.");
        Assert(!LicenseExceptionHelper.IsLicenseMissingException(ex), nameof(TestIsLicenseMissingException_OtherException));
    }

    private static void TestExtractMissingLicenseName_StandardMessage()
    {
        var ex = new Exception("Necessary license 'STEP 7 Professional' is missing.");
        var name = LicenseExceptionHelper.ExtractMissingLicenseName(ex);
        AssertEqual("STEP 7 Professional", name, nameof(TestExtractMissingLicenseName_StandardMessage));
    }

    private static void TestExtractMissingLicenseName_NoMarker_FallsBackToDefault()
    {
        var ex = new Exception("Some unrelated error message.");
        var name = LicenseExceptionHelper.ExtractMissingLicenseName(ex);
        AssertEqual("STEP 7 Professional", name, nameof(TestExtractMissingLicenseName_NoMarker_FallsBackToDefault));
    }

    private static void TestLicenseSuspectReason_ContainsLicenseName()
    {
        var reason = LicenseExceptionHelper.LicenseSuspectReason("STEP 7 Professional");
        Assert(reason.Contains("STEP 7 Professional"), nameof(TestLicenseSuspectReason_ContainsLicenseName));
        Assert(reason.Contains("not installed"), nameof(TestLicenseSuspectReason_ContainsLicenseName) + "_not_installed");
    }

    private static void TestLicenseSuspectReason_Null_ContainsFallback()
    {
        var reason = LicenseExceptionHelper.LicenseSuspectReason(null);
        Assert(reason.Contains("STEP 7 Professional"), nameof(TestLicenseSuspectReason_Null_ContainsFallback));
    }

    private static void TestLicenseRequiredAction_ContainsAlmHint()
    {
        var action = LicenseExceptionHelper.LicenseRequiredAction("STEP 7 Professional");
        Assert(action.Contains("Automation License Manager"), nameof(TestLicenseRequiredAction_ContainsAlmHint));
    }

    private static void TestLicenseRequiredAction_ContainsSessionHint()
    {
        var action = LicenseExceptionHelper.LicenseRequiredAction(null);
        Assert(action.Contains("Windows user/session"), nameof(TestLicenseRequiredAction_ContainsSessionHint));
    }

    private static void TestTruncateMessage_ShortMessage_Unchanged()
    {
        const string msg = "short";
        AssertEqual(msg, LicenseExceptionHelper.TruncateMessage(msg, 100), nameof(TestTruncateMessage_ShortMessage_Unchanged));
    }

    private static void TestTruncateMessage_LongMessage_Truncated()
    {
        var msg = new string('x', 400);
        var result = LicenseExceptionHelper.TruncateMessage(msg, 300);
        Assert(result.Length < 400, nameof(TestTruncateMessage_LongMessage_Truncated) + "_shorter");
        Assert(result.EndsWith("..."), nameof(TestTruncateMessage_LongMessage_Truncated) + "_ellipsis");
    }

    private static void TestTruncateMessage_ExactLength_Unchanged()
    {
        var msg = new string('x', 300);
        AssertEqual(msg, LicenseExceptionHelper.TruncateMessage(msg, 300), nameof(TestTruncateMessage_ExactLength_Unchanged));
    }

    private static void Assert(bool condition, string testName)
    {
        if (!condition) throw new Exception($"[FAIL] {testName}");
    }

    private static void AssertEqual(string expected, string actual, string testName)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new Exception($"[FAIL] {testName}: expected '{expected}', got '{actual}'");
        }
    }
}
