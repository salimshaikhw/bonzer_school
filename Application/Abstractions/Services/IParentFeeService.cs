using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.ViewModels.Fees;
using Application.ViewModels.Payment;

namespace Application.Abstractions.Services;

public interface IParentFeeService
{
    Task<IReadOnlyList<ParentLinkedStudentDto>> GetMyStudentsAsync(string? parentEmail, CancellationToken cancellationToken = default);

    Task<bool> CanAccessStudentForFeesAsync(
        Guid studentId,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenInvoiceDto>> GetOpenInvoicesAsync(
        Guid studentId,
        Guid academicYearId,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentHistoryRowDto>> GetPaymentHistoryAsync(
        Guid studentId,
        Guid academicYearId,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> RecordParentPaymentAsync(
        Guid studentId,
        ParentPayRequest request,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default);

    /// <summary>Create invoice from class fee map when needed, then record payment (same rules as <see cref="RecordParentPaymentAsync"/>).</summary>
    Task<PaymentResponse?> CollectAndPayAsync(
        Guid studentId,
        ParentCollectPayRequest request,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default);
}
