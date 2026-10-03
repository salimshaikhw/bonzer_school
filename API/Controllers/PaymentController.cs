using Application.Abstractions.Services;
using Application.ViewModels.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using API.Models;
using API.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _paymentService.GetAllAsync();
            return Ok(new ApiResponse<object>(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _paymentService.GetByIdAsync(id);
            return Ok(new ApiResponse<object>(result));
        }

        /// <summary>Get receipt data for this payment (for slip print or re-print). One slip per payment.</summary>
        [HttpGet("{id}/receipt")]
        public async Task<IActionResult> GetReceipt(Guid id)
        {
            var result = await _paymentService.GetReceiptAsync(id);
            if (result == null) return NotFound();
            return Ok(new ApiResponse<object>(result));
        }

        /// <summary>Get receipt as PDF. Use in payment history to view/download PDF per entry, or for accountant to share.</summary>
        /// <param name="id">Payment Id.</param>
        /// <param name="disposition">inline (default) = show in browser; attachment = download as file.</param>
        [HttpGet("{id}/receipt/pdf")]
        public async Task<IActionResult> GetReceiptPdf(Guid id, [FromQuery] string disposition = "inline")
        {
            var receipt = await _paymentService.GetReceiptAsync(id);
            if (receipt == null) return NotFound();

            var pdfBytes = ReceiptPdfGenerator.Generate(receipt);
            var fileName = $"receipt-{id:N}.pdf";
            var isInline = string.Equals(disposition, "inline", StringComparison.OrdinalIgnoreCase);

            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = isInline
            };
            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

            return File(pdfBytes, "application/pdf");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequest request)
        {
            var result = await _paymentService.CreateAsync(request);
            return Ok(new ApiResponse<object>(result));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PaymentRequest request)
        {
            var result = await _paymentService.UpdateAsync(id, request);
            return Ok(new ApiResponse<object>(result));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _paymentService.DeleteAsync(id);
            return Ok(new ApiResponse<object>(result));
        }
    }
}