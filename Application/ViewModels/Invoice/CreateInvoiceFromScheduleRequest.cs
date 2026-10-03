using System;

namespace Application.ViewModels.Invoice;

/// <summary>
/// Build an open invoice + invoice_items from <see cref="Fees.StudentFeeDashboardDto"/> fee heads
/// when the student has no existing open invoices for the academic year (fees desk / walk-in collection).
/// </summary>
public class CreateInvoiceFromScheduleRequest
{
    public Guid StudentId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid? InstallmentId { get; set; }
}
