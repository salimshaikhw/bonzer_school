using System;
using System.Threading.Tasks;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.TcWorkflow
{
    // Design Pattern: Chain of Responsibility (Behavioral)
    // Base handler that forwards the request to the next handler if it cannot process it.
    public abstract class TcApprovalHandlerBase : ITcApprovalHandler
    {
        private ITcApprovalHandler? _next;

        public ITcApprovalHandler SetNext(ITcApprovalHandler next)
        {
            _next = next;
            return next;
        }

        public virtual async Task HandleAsync(TcApprovalRequest request)
        {
            if (_next != null)
            {
                await _next.HandleAsync(request);
            }
            else
            {
                throw new InvalidOperationException($"No TC approval handler found for action '{request.Action}'.");
            }
        }

        protected static bool CanHandle(string expectedAction, TcApprovalRequest request)
            => string.Equals(request.Action, expectedAction, StringComparison.OrdinalIgnoreCase);
    }

    // Concrete handler: head clerk approval step
    public class HeadClerkApprovalHandler : TcApprovalHandlerBase
    {
        private readonly ITcCertificateRepository _tcRepository;
        private readonly ILogger<HeadClerkApprovalHandler> _logger;

        public HeadClerkApprovalHandler(
            ITcCertificateRepository tcRepository,
            ILogger<HeadClerkApprovalHandler> logger)
        {
            _tcRepository = tcRepository;
            _logger = logger;
        }

        public override async Task HandleAsync(TcApprovalRequest request)
        {
            if (CanHandle("HEAD_CLERK_APPROVE", request))
            {
                _logger.LogInformation("ChainOfResponsibility: Head clerk handling TC {TcId}", request.TcId);
                await _tcRepository.ExecuteSqlRawAsync(
                    "SELECT tc_transition({0}, {1}, {2}, {3}, {4})",
                    request.TcId,
                    "PENDING_HEAD_CLERK",
                    "PENDING_CLASS_TEACHER",
                    request.StaffId,
                    "HEAD_CLERK");
                return;
            }

            await base.HandleAsync(request);
        }
    }

    // Concrete handler: class teacher approval step
    public class ClassTeacherApprovalHandler : TcApprovalHandlerBase
    {
        private readonly ITcCertificateRepository _tcRepository;
        private readonly ILogger<ClassTeacherApprovalHandler> _logger;

        public ClassTeacherApprovalHandler(
            ITcCertificateRepository tcRepository,
            ILogger<ClassTeacherApprovalHandler> logger)
        {
            _tcRepository = tcRepository;
            _logger = logger;
        }

        public override async Task HandleAsync(TcApprovalRequest request)
        {
            if (CanHandle("CLASS_TEACHER_APPROVE", request))
            {
                _logger.LogInformation("ChainOfResponsibility: Class teacher handling TC {TcId}", request.TcId);
                await _tcRepository.ExecuteSqlRawAsync(
                    "SELECT tc_transition({0}, {1}, {2}, {3}, {4})",
                    request.TcId,
                    "PENDING_CLASS_TEACHER",
                    "PENDING_PRINCIPAL",
                    request.StaffId,
                    "CLASS_TEACHER");
                return;
            }

            await base.HandleAsync(request);
        }
    }

    // Concrete handler: principal approval step
    public class PrincipalApprovalHandler : TcApprovalHandlerBase
    {
        private readonly ITcCertificateRepository _tcRepository;
        private readonly ILogger<PrincipalApprovalHandler> _logger;

        public PrincipalApprovalHandler(
            ITcCertificateRepository tcRepository,
            ILogger<PrincipalApprovalHandler> logger)
        {
            _tcRepository = tcRepository;
            _logger = logger;
        }

        public override async Task HandleAsync(TcApprovalRequest request)
        {
            if (CanHandle("PRINCIPAL_APPROVE", request))
            {
                _logger.LogInformation("ChainOfResponsibility: Principal handling TC {TcId}", request.TcId);
                await _tcRepository.ExecuteSqlRawAsync(
                    "SELECT tc_transition({0}, {1}, {2}, {3}, {4})",
                    request.TcId,
                    "PENDING_PRINCIPAL",
                    "APPROVED",
                    request.StaffId,
                    "PRINCIPAL");
                return;
            }

            await base.HandleAsync(request);
        }
    }

    // Concrete handler: issue TC step
    public class IssueTcHandler : TcApprovalHandlerBase
    {
        private readonly ITcCertificateRepository _tcRepository;
        private readonly ILogger<IssueTcHandler> _logger;

        public IssueTcHandler(
            ITcCertificateRepository tcRepository,
            ILogger<IssueTcHandler> logger)
        {
            _tcRepository = tcRepository;
            _logger = logger;
        }

        public override async Task HandleAsync(TcApprovalRequest request)
        {
            if (CanHandle("ISSUE", request))
            {
                _logger.LogInformation("ChainOfResponsibility: Issuing TC {TcId}", request.TcId);
                await _tcRepository.ExecuteSqlRawAsync(
                    "SELECT tc_transition({0}, {1}, {2}, {3}, {4})",
                    request.TcId,
                    "APPROVED",
                    "ISSUED",
                    request.StaffId,
                    "PRINCIPAL");
                return;
            }

            await base.HandleAsync(request);
        }
    }

    // Concrete handler: reject TC step
    public class RejectTcHandler : TcApprovalHandlerBase
    {
        private readonly ITcCertificateRepository _tcRepository;
        private readonly ILogger<RejectTcHandler> _logger;

        public RejectTcHandler(
            ITcCertificateRepository tcRepository,
            ILogger<RejectTcHandler> logger)
        {
            _tcRepository = tcRepository;
            _logger = logger;
        }

        public override async Task HandleAsync(TcApprovalRequest request)
        {
            if (CanHandle("REJECT", request))
            {
                _logger.LogInformation("ChainOfResponsibility: Rejecting TC {TcId}", request.TcId);
                await _tcRepository.ExecuteSqlRawAsync(
                    "SELECT tc_reject({0}, {1}, {2})",
                    request.TcId,
                    request.StaffId,
                    request.Reason ?? string.Empty);
                return;
            }

            await base.HandleAsync(request);
        }
    }
}

