using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using NPOI.XSSF.UserModel;

namespace API.Services;

/// <summary>Builds an .xlsx catalog of API endpoints from controller reflection + optional XML docs.</summary>
public static class ApiEndpointsExcelExporter
{
    private const string SheetName = "API Endpoints";

    public static byte[] BuildExcel(Assembly apiAssembly)
    {
        var xmlSummaries = LoadXmlSummaries(apiAssembly);
        var rows = CollectRows(apiAssembly, xmlSummaries);

        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet(SheetName);
        var header = sheet.CreateRow(0);
        var headers = new[] { "Controller Name", "Method Name", "HTTP Verb", "Route", "Parameters", "Description" };
        for (var i = 0; i < headers.Length; i++)
            header.CreateCell(i).SetCellValue(headers[i]);

        var rowIndex = 1;
        foreach (var r in rows)
        {
            var row = sheet.CreateRow(rowIndex++);
            row.CreateCell(0).SetCellValue(r.ControllerShortName);
            row.CreateCell(1).SetCellValue(r.MethodName);
            row.CreateCell(2).SetCellValue(r.HttpVerb);
            row.CreateCell(3).SetCellValue(r.Route);
            row.CreateCell(4).SetCellValue(r.Parameters);
            row.CreateCell(5).SetCellValue(r.Description);
        }

        for (var c = 0; c < headers.Length; c++)
            sheet.AutoSizeColumn(c);

        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    private sealed record RowData(
        string ControllerShortName,
        string MethodName,
        string HttpVerb,
        string Route,
        string Parameters,
        string Description);

    private static List<RowData> CollectRows(Assembly assembly, Dictionary<string, string> xmlSummaries)
    {
        var list = new List<RowData>();
        var controllerTypes = assembly.GetExportedTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var type in controllerTypes)
        {
            var shortName = type.Name.EndsWith("Controller", StringComparison.Ordinal)
                ? type.Name[..^"Controller".Length]
                : type.Name;

            var routeTemplate = type.GetCustomAttribute<RouteAttribute>()?.Template ?? "api/[controller]";
            var basePath = routeTemplate.Replace("[controller]", shortName, StringComparison.OrdinalIgnoreCase);

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var verb = GetHttpVerb(method);
                if (verb == null)
                    continue;

                var actionTemplate = GetActionRouteTemplate(method);
                var route = CombineRoute(basePath, actionTemplate);

                var parameters = FormatParameters(method);
                var description = GetDescription(type, method, xmlSummaries);

                list.Add(new RowData(shortName, method.Name, verb, route, parameters, description));
            }
        }

        return list;
    }

    private static string CombineRoute(string basePath, string actionTemplate)
    {
        basePath = basePath.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(actionTemplate))
            return basePath;
        return basePath + "/" + actionTemplate.Trim().TrimStart('/');
    }

    private static string GetActionRouteTemplate(MethodInfo method)
    {
        return method.GetCustomAttribute<HttpGetAttribute>()?.Template
            ?? method.GetCustomAttribute<HttpPostAttribute>()?.Template
            ?? method.GetCustomAttribute<HttpPutAttribute>()?.Template
            ?? method.GetCustomAttribute<HttpPatchAttribute>()?.Template
            ?? method.GetCustomAttribute<HttpDeleteAttribute>()?.Template
            ?? string.Empty;
    }

    private static string? GetHttpVerb(MethodInfo method)
    {
        if (method.GetCustomAttribute<HttpGetAttribute>() != null) return "GET";
        if (method.GetCustomAttribute<HttpPostAttribute>() != null) return "POST";
        if (method.GetCustomAttribute<HttpPutAttribute>() != null) return "PUT";
        if (method.GetCustomAttribute<HttpPatchAttribute>() != null) return "PATCH";
        if (method.GetCustomAttribute<HttpDeleteAttribute>() != null) return "DELETE";
        return null;
    }

    private static string FormatParameters(MethodInfo method)
    {
        var parts = new List<string>();
        foreach (var p in method.GetParameters())
        {
            if (p.ParameterType == typeof(CancellationToken))
                continue;

            var name = p.Name ?? "param";
            if (p.GetCustomAttribute<FromBodyAttribute>() != null)
                parts.Add($"body: {p.ParameterType.Name} {name}");
            else if (p.GetCustomAttribute<FromFormAttribute>() != null)
                parts.Add($"form: {name}");
            else if (p.GetCustomAttribute<FromQueryAttribute>() != null)
                parts.Add($"query: {name} ({p.ParameterType.Name})");
            else if (p.GetCustomAttribute<FromRouteAttribute>() != null)
                parts.Add($"route: {name} ({p.ParameterType.Name})");
            else if (p.GetCustomAttribute<FromHeaderAttribute>() != null)
                parts.Add($"header: {name}");
            else
                parts.Add($"{p.ParameterType.Name} {name}");
        }

        return parts.Count == 0 ? "(none)" : string.Join("; ", parts);
    }

    private static string GetDescription(Type controllerType, MethodInfo method, Dictionary<string, string> xmlSummaries)
    {
        var prefix = $"M:{controllerType.FullName}.{method.Name}";
        foreach (var kv in xmlSummaries)
        {
            if (kv.Key.StartsWith(prefix + "(", StringComparison.Ordinal) || kv.Key.Equals(prefix, StringComparison.Ordinal))
                return kv.Value;
        }

        foreach (var kv in xmlSummaries)
        {
            if (kv.Key.Contains($".{controllerType.Name}.{method.Name}(", StringComparison.Ordinal))
                return kv.Value;
        }

        return string.Empty;
    }

    private static Dictionary<string, string> LoadXmlSummaries(Assembly assembly)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
            if (!File.Exists(path))
                return map;

            var doc = XDocument.Load(path);
            foreach (var member in doc.Descendants("member"))
            {
                var nameAttr = member.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(nameAttr) || !nameAttr.StartsWith("M:", StringComparison.Ordinal))
                    continue;
                var summary = member.Element("summary")?.Value?.Trim();
                if (string.IsNullOrEmpty(summary))
                    continue;
                summary = string.Join(" ", summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                map[nameAttr] = summary;
            }
        }
        catch
        {
            // ignore missing or invalid XML
        }

        return map;
    }
}
