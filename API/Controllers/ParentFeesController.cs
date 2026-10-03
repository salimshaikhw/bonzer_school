using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.Exceptions;
using Application.ViewModels.Fees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Parent (and staff) fee portal: linked students, open invoices, payment history, record payment with invoice allocation.
/// Parents are linked via <c>parent_users.Username</c> matching their Identity email.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParentFeesController : ControllerBase
{
    private readonly IParentFeeService _parentFees;
    private readonly IFeeStudentService _feeStudentService;

    public ParentFeesController(IParentFeeService parentFees, IFeeStudentService feeStudentService)
    {
        _parentFees = parentFees;
        _feeStudentService = feeStudentService;
    }

    private static List<string> RoleNames(ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    /// <summary>Children linked to the signed-in parent (Identity email = parent_users.Username).</summary>
    [HttpGet("my-students")]
    [ProducesResponseType(typeof(IReadOnlyList<ParentLinkedStudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyStudents(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Parent"))
            return Forbid();

        var email = User.Identity?.Name;
        var rows = await _parentFees.GetMyStudentsAsync(email, cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Fee dashboard for one student (same shape as /api/Fees/student-dashboard) after access check.</summary>
    [HttpGet("students/{studentId:guid}/fee-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFeeSummary(
        Guid studentId,
        [FromQuery] Guid academicYearId,
        CancellationToken cancellationToken)
    {
        var roles = RoleNames(User);
        if (!await _parentFees.CanAccessStudentForFeesAsync(studentId, User.Identity?.Name, roles, cancellationToken)
                .ConfigureAwait(false))
            return Forbid();

        var dto = await _feeStudentService.GetStudentDashboard(studentId, academicYearId).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("students/{studentId:guid}/open-invoices")]
    [ProducesResponseType(typeof(IReadOnlyList<OpenInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenInvoices(
        Guid studentId,
        [FromQuery] Guid academicYearId,
        CancellationToken cancellationToken)
    {
        var roles = RoleNames(User);
        var rows = await _parentFees.GetOpenInvoicesAsync(
                studentId,
                academicYearId,
                User.Identity?.Name,
                roles,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpGet("students/{studentId:guid}/payments")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentHistoryRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(
        Guid studentId,
        [FromQuery] Guid academicYearId,
        CancellationToken cancellationToken)
    {
        var roles = RoleNames(User);
        var rows = await _parentFees.GetPaymentHistoryAsync(
                studentId,
                academicYearId,
                User.Identity?.Name,
                roles,
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Record a payment against an existing invoice; updates invoice_items and creates payment_fee_lines when provided.</summary>
    [HttpPost("students/{studentId:guid}/pay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Pay(
        Guid studentId,
        [FromBody] ParentPayRequest body,
        CancellationToken cancellationToken)
    {
        if (body == null)
            return BadRequest("Body required.");

        var roles = RoleNames(User);
        try
        {
            var result = await _parentFees.RecordParentPaymentAsync(
                    studentId,
                    body,
                    User.Identity?.Name,
                    roles,
                    cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }
        catch (BaseServiceException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create an open invoice from the class fee map when the student has none for the year, then record payment (same fee line rules as Pay).
    /// </summary>
    [HttpPost("students/{studentId:guid}/collect-pay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CollectPay(
        Guid studentId,
        [FromBody] ParentCollectPayRequest body,
        CancellationToken cancellationToken)
    {
        if (body == null)
            return BadRequest("Body required.");

        var roles = RoleNames(User);
        try
        {
            var result = await _parentFees.CollectAndPayAsync(
                    studentId,
                    body,
                    User.Identity?.Name,
                    roles,
                    cancellationToken)
                .ConfigureAwait(false);
            return Ok(result);
        }
        catch (BaseServiceException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
