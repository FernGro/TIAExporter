using System.Collections;
using System.Reflection;
using Siemens.Engineering;

namespace TIAExporter.Tia;

internal static class TiaReflection
{
    public static bool? GetBool(object? instance, params string[] names)
    {
        var value = GetValue(instance, names);
        if (value == null)
        {
            return null;
        }

        if (value is bool boolean)
        {
            return boolean;
        }

        return bool.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    public static string? GetString(object? instance, params string[] names)
    {
        var value = GetValue(instance, names);
        return value?.ToString();
    }

    public static object? GetValue(object? instance, params string[] names)
    {
        if (instance == null)
        {
            return null;
        }

        foreach (var name in names)
        {
            try
            {
                var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    return property.GetValue(instance);
                }

                var attribute = instance.GetType().GetMethod("GetAttribute", [typeof(string)]);
                if (attribute != null)
                {
                    return attribute.Invoke(instance, [name]);
                }
            }
            catch
            {
                // Attribute availability varies by TIA object; callers log context where needed.
            }
        }

        return null;
    }

    public static Dictionary<string, string?> GetReadableAttributes(object? instance)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (instance == null)
        {
            return result;
        }

        try
        {
            var method = instance.GetType().GetMethod("GetAttributes", [typeof(AttributeAccessOptions)]);
            if (method != null)
            {
                var values = method.Invoke(instance, [AttributeAccessOptions.ReadOnly | AttributeAccessOptions.ReadWrite]);
                foreach (var entry in Enumerate(values))
                {
                    var key = GetString(entry, "Key");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    result[key!] = ToStableString(GetValue(entry, "Value"));
                }
            }
        }
        catch
        {
            // Fall back to attribute infos below.
        }

        if (result.Count > 0)
        {
            return result;
        }

        try
        {
            var infos = instance.GetType().GetMethod("GetAttributeInfos", Type.EmptyTypes)?.Invoke(instance, null);
            foreach (var info in Enumerate(infos))
            {
                var name = GetString(info, "Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var value = GetValue(instance, name!);
                result[name!] = ToStableString(value);
            }
        }
        catch
        {
            // Best-effort snapshot only.
        }

        return result;
    }

    public static IEnumerable<object> Enumerate(object? collection)
    {
        if (collection is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }

    public static object? GetServiceByFullName(object provider, string serviceFullName)
    {
        var serviceType = typeof(TiaPortal).Assembly.GetType(serviceFullName);
        if (serviceType == null)
        {
            return null;
        }

        var method = provider.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => x.Name == "GetService" && x.IsGenericMethodDefinition && x.GetParameters().Length == 0);
        if (method == null)
        {
            return null;
        }

        try
        {
            return method.MakeGenericMethod(serviceType).Invoke(provider, null);
        }
        catch
        {
            return null;
        }
    }

    public static bool TryExport(object instance, string filePath)
    {
        var result = TryExportWithOptions(instance, filePath, out var error);
        if (result)
        {
            return true;
        }

        if (error != null)
        {
            throw error;
        }

        return false;
    }

    public static bool TryExportWithOptions(object instance, string filePath, out Exception? lastError, out string? optionUsed)
    {
        lastError = null;
        optionUsed = null;
        var method = instance.GetType().GetMethod("Export", [typeof(FileInfo), typeof(ExportOptions)]);
        if (method == null)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        Exception? firstError = null;
        Exception? firstLicenseError = null;
        Exception? firstNonLicenseError = null;
        foreach (var option in GetExportOptionsInPreferredOrder())
        {
            try
            {
                method.Invoke(instance, [new FileInfo(filePath), option]);
                optionUsed = option.ToString();
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                lastError = ex.InnerException;
                firstError ??= lastError;
                if (LicenseExceptionHelper.IsLicenseMissingException(lastError))
                {
                    firstLicenseError ??= lastError;
                }
                else
                {
                    firstNonLicenseError ??= lastError;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
                firstError ??= lastError;
                if (LicenseExceptionHelper.IsLicenseMissingException(lastError))
                {
                    firstLicenseError ??= lastError;
                }
                else
                {
                    firstNonLicenseError ??= lastError;
                }
            }
        }

        lastError = firstNonLicenseError ?? firstLicenseError ?? firstError ?? lastError;
        return false;
    }

    private static IEnumerable<ExportOptions> GetExportOptionsInPreferredOrder()
    {
        var values = Enum.GetValues(typeof(ExportOptions)).Cast<ExportOptions>().ToList();
        foreach (var preferredName in new[] { "WithDefaults", "None" })
        {
            var index = values.FindIndex(x => string.Equals(x.ToString(), preferredName, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                var preferred = values[index];
                values.RemoveAt(index);
                yield return preferred;
            }
        }

        foreach (var value in values)
        {
            yield return value;
        }
    }

    public static bool TryExportWithOptions(object instance, string filePath, out Exception? lastError)
    {
        return TryExportWithOptions(instance, filePath, out lastError, out _);
    }

    public static bool TryExportAsDocument(object instance, string filePath, out Exception? error, out string? methodUsed)
    {
        error = null;
        methodUsed = null;
        var methods = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.Name.IndexOf("ExportAsDocument", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        x.Name.IndexOf("ExportAsDocuments", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        foreach (var method in methods)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                var parameters = method.GetParameters();
                object?[] args;
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(FileInfo))
                {
                    args = [new FileInfo(filePath)];
                }
                else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(FileInfo) && parameters[1].ParameterType == typeof(ExportOptions))
                {
                    args = [new FileInfo(filePath), ExportOptions.WithDefaults];
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(DirectoryInfo))
                {
                    args = [new DirectoryInfo(Path.GetDirectoryName(filePath)!)];
                }
                else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(DirectoryInfo) && parameters[1].ParameterType == typeof(ExportOptions))
                {
                    args = [new DirectoryInfo(Path.GetDirectoryName(filePath)!), ExportOptions.WithDefaults];
                }
                else
                {
                    continue;
                }

                method.Invoke(instance, args);
                methodUsed = method.Name;
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                error = ex.InnerException;
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        return false;
    }

    public static string? ToStableString(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || type.IsEnum)
        {
            return value.ToString();
        }

        return GetString(value, "Name", "FullName", "Id") ?? value.ToString();
    }
}
