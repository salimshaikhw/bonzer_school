using Application.Abstractions.Services;
using Application.ViewModels.EmailSettings;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EmailsController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    // Existing simple send (unchanged)
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.To))
            return BadRequest("Invalid email request.");

        try
        {
            await _emailService.SendEmailAsync(request.To, request.Subject, request.Body);
            return Ok(new { message = "Email sent successfully!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // 1) Attach an existing PDF from disk
    [HttpPost("send-with-file")]
    public async Task<IActionResult> SendWithFile([FromBody] EmailRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.To)) return BadRequest("Invalid email request.");

        var filePath = @"C:\files\report.pdf"; // change to your path
        if (!System.IO.File.Exists(filePath)) return NotFound("PDF not found.");

        using var fs = System.IO.File.OpenRead(filePath);
        var attachment = new EmailAttachment
        {
            FileName = Path.GetFileName(filePath),
            Content = fs,
            ContentType = "application/pdf"
        };

        try
        {
            await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, new[] { attachment });
            return Ok(new { message = "Email sent with PDF." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // 2) Accept uploaded PDF (IFormFile) and send it
    [HttpPost("send-with-upload")]
    public async Task<IActionResult> SendWithUpload([FromForm] string to, [FromForm] string subject, [FromForm] string body, IFormFile? pdf)
    {
        if (string.IsNullOrEmpty(to)) return BadRequest("Recipient required.");
        if (pdf == null || pdf.Length == 0) return BadRequest("PDF required.");

        using var ms = new MemoryStream();
        await pdf.CopyToAsync(ms);
        ms.Position = 0;

        var attachment = new EmailAttachment
        {
            FileName = Path.GetFileName(pdf.FileName),
            Content = ms,
            ContentType = pdf.ContentType ?? "application/pdf"
        };

        try
        {
            await _emailService.SendEmailAsync(to, subject, body, new[] { attachment });
            return Ok(new { message = "Email sent with uploaded PDF." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // 3) Generate a PDF in memory and send it (example using QuestPDF)
    [HttpPost("send-generated-pdf")]
    public async Task<IActionResult> SendGeneratedPdf([FromBody] EmailRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.To)) return BadRequest("Invalid email request.");

        // Generate PDF bytes (see helper below)
        var pdfBytes = PdfGenerator.GenerateSamplePdfBytes();

        using var ms = new MemoryStream(pdfBytes);
        var attachment = new EmailAttachment
        {
            FileName = "report.pdf",
            Content = ms,
            ContentType = "application/pdf"
        };

        try
        {
            await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, new[] { attachment });
            return Ok(new { message = "Email sent with generated PDF." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}