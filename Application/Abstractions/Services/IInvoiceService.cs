using Application.ViewModels.Invoice;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceResponse>> GetAllAsync();
        Task<InvoiceResponse> GetByIdAsync(Guid id);
        Task<InvoiceResponse> CreateAsync(InvoiceRequest request);
        Task<InvoiceResponse> CreateFromStudentScheduleAsync(CreateInvoiceFromScheduleRequest request);
        Task<InvoiceStatementDto?> GetInvoiceStatementAsync(Guid invoiceId);
        Task<InvoiceResponse> UpdateAsync(Guid id, InvoiceRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}