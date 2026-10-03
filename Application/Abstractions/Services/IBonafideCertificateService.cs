using Application.ViewModels.Certificates;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services;

public interface IBonafideCertificateService
{
    Task<BonafideCertificateResponse> CreateAsync(BonafideCertificateRequest request);
    Task<BonafideCertificateResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<BonafideCertificateResponse>> GetByStudentAsync(Guid studentId);
}
