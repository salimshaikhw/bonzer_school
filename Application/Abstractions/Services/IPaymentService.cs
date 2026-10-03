using Application.ViewModels.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentResponse>> GetAllAsync();
        Task<PaymentResponse> GetByIdAsync(Guid id);
        Task<PaymentResponse> CreateAsync(PaymentRequest request);
        Task<PaymentResponse> UpdateAsync(Guid id, PaymentRequest request);
        Task<bool> DeleteAsync(Guid id);
        /// <summary>Get receipt data for a payment (for slip print or re-print). One receipt per payment.</summary>
        Task<PaymentReceiptDto?> GetReceiptAsync(Guid paymentId);
    }
}