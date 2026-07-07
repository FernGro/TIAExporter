using TIAExporter.Tests;

Console.WriteLine("TIAExporter unit tests");
Console.WriteLine("======================");

var failed = false;

try { LicenseExceptionHelperTests.Run(); }
catch (Exception ex) { Console.Error.WriteLine(ex.Message); failed = true; }

try { TiaProjectFileTypesTests.Run(); }
catch (Exception ex) { Console.Error.WriteLine(ex.Message); failed = true; }

try { CmdbCraImportTests.Run(); }
catch (Exception ex) { Console.Error.WriteLine(ex.Message); failed = true; }

if (failed)
{
    Console.Error.WriteLine("\n[FAIL] One or more tests failed.");
    return 1;
}

Console.WriteLine("\n[PASS] All tests passed.");
return 0;
