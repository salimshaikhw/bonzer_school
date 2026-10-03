using Application.ViewModels.Certificates;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services;

public interface ITcCertificateService
{
    Task<TcCertificateResponse> CreateAsync(TcCertificateRequest request);
    Task<TcCertificateResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<TcCertificateResponse>> GetByStudentAsync(Guid studentId);

    // Workflow operations
    Task InitiateClearanceAsync(Guid tcId);

    // High-level steps (these internally use tc_transition)
    Task HeadClerkApproveAsync(Guid tcId, Guid staffId);
    Task ClassTeacherApproveAsync(Guid tcId, Guid staffId);
    Task PrincipalApproveAsync(Guid tcId, Guid staffId);
    Task IssueAsync(Guid tcId, Guid staffId);

    Task RejectAsync(Guid tcId, Guid staffId, string reason);
}
