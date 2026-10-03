using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Download machine-generated API endpoint catalog as Excel.</summary>
[ApiController]
[Route("api/[controller]")]
public class ApiDocumentationController : ControllerBase
{
    /// <summary>
    /// Download Excel (.xlsx) listing all API actions: controller name, C# method name, HTTP verb, parameters, XML summary when available.
    /// Anonymous so you can fetch the catalog without a token (same convenience as Swagger in dev).
    /// </summary>
    [HttpGet("endpoints-excel")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult DownloadEndpointsExcel()
    {
        var asm = typeof(ApiDocumentationController).Assembly;
        var bytes = ApiEndpointsExcelExporter.BuildExcel(asm);
        const string fileName = "BonzerSchool_API_Endpoints.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
