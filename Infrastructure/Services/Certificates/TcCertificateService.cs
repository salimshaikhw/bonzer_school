using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using AutoMapper;
using Infrastructure;
using Infrastructure.Services.Certificates.TcWorkflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Certificates;

public class TcCertificateService : ITcCertificateService
{
    private readonly ITcCertificateRepository _tcRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IAcademicYearRepository _yearRepo;
    private readonly IClassRepository _classRepo;
    private readonly IDivisionRepository _divisionRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<TcCertificateService> _logger;
    private readonly ICacheService _cacheService;
    private readonly ITcNumberGenerator _tcNumberGenerator;
    private readonly ITcApprovalHandler _tcApprovalChain;

    public TcCertificateService(
        ITcCertificateRepository tcRepo,
        IStudentRepository studentRepo,
        IAcademicYearRepository yearRepo,
        IClassRepository classRepo,
        IDivisionRepository divisionRepo,
        IMapper mapper,
        ILogger<TcCertificateService> logger,
        ICacheService cacheService,
        ITcNumberGenerator tcNumberGenerator,
        HeadClerkApprovalHandler headClerkApprovalHandler,
        ClassTeacherApprovalHandler classTeacherApprovalHandler,
        PrincipalApprovalHandler principalApprovalHandler,
        IssueTcHandler issueTcHandler,
        RejectTcHandler rejectTcHandler)
    {
        _tcRepo = tcRepo;
        _studentRepo = studentRepo;
        _yearRepo = yearRepo;
        _classRepo = classRepo;
        _divisionRepo = divisionRepo;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _tcNumberGenerator = tcNumberGenerator;

        // Design Pattern: Chain of Responsibility (Behavioral) for TC approval workflow
        _tcApprovalChain = headClerkApprovalHandler;
        headClerkApprovalHandler
            .SetNext(classTeacherApprovalHandler)
            .SetNext(principalApprovalHandler)
            .SetNext(issueTcHandler)
            .SetNext(rejectTcHandler);
    }

    public async Task<TcCertificateResponse> CreateAsync(TcCertificateRequest request)
    {
        try
        {
            _logger.LogInformation("Creating transfer certificate for student {StudentId}", request.StudentId);

            var student = await _studentRepo.GetByIdAsync(request.StudentId);
            var academicYearId = request.AcademicYearId ?? student.AcademicYearId
                                 ?? throw new Application.Exceptions.BaseServiceException("The student has no academic year; provide AcademicYearId.", Application.Common.ExceptionCodes.Validation);
            var classId = request.ClassId ?? student.ClassId
                          ?? throw new Application.Exceptions.BaseServiceException("The student has no class; provide ClassId.", Application.Common.ExceptionCodes.Validation);
            await EnsureTcCanBeRequestedAsync(student, request.IsDuplicate);

            // Design Pattern: Strategy (Behavioral) via ITcNumberGenerator
            var (tcNumber, nextSeq) = await _tcNumberGenerator.GenerateAsync(academicYearId, classId);

            var entity = new TcCertificate
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                AcademicYearId = academicYearId,
                ClassId = classId,
                DivisionId = request.DivisionId ?? student.DivisionId,
                BranchId = student.BranchId,
                TcNumber = tcNumber,
                Sequence = nextSeq,
                IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = request.Reason,
                Remarks = request.Remarks,
                IsDuplicate = request.IsDuplicate,
                IssuedByStaffId = request.IssuedByStaffId,
                Conduct = request.Conduct,
                FeesCleared = request.FeesCleared ?? true,
                CreatedAt = DateTime.UtcNow
            };

            await _tcRepo.AddAsync(entity);

            var dto = _mapper.Map<TcCertificateResponse>(entity);
            dto.StudentName = $"{student.FirstName} {student.LastName}".Trim();
            dto.AdmissionNo = student.AdmissionNo;
            dto.AcademicYearName = (await _yearRepo.GetByIdAsync(academicYearId))?.YearName;
            dto.ClassName = (await _classRepo.GetByIdAsync(classId))?.Name;
            if (entity.DivisionId.HasValue)
            {
                var div = await _divisionRepo.GetByIdAsync(entity.DivisionId.Value);
                dto.DivisionName = div?.Name;
            }

            // Cache by id and invalidate per-student list
            var byIdKey = $"tc:{dto.Id}";
            var byStudentKey = $"tc:student:{student.Id}";
            _cacheService.Set(byIdKey, dto);
            _cacheService.Remove(byStudentKey);

            return dto;
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error creating transfer certificate",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    /// <summary>
    /// An original TC needs a student who may leave (StudentLifecycle) and no other open or issued original;
    /// a duplicate (copy of a lost TC) needs an issued original.
    /// </summary>
    private async Task EnsureTcCanBeRequestedAsync(Student student, bool isDuplicate)
    {
        var originals = await _tcRepo.FindAllAsync(t => t.StudentId == student.Id && !t.IsDuplicate && t.Status != "REJECTED");
        if (isDuplicate)
        {
            if (!originals.Any(t => t.Status == "ISSUED"))
                throw new Application.Exceptions.BaseServiceException("A duplicate TC needs an issued original TC.", Application.Common.ExceptionCodes.Operation);
            return;
        }
        if (originals.Count > 0)
            throw new Application.Exceptions.BaseServiceException(
                $"The student already has a TC ({originals[0].TcNumber}, {originals[0].Status}). Reject it first or request a duplicate.",
                Application.Common.ExceptionCodes.Operation);
        var reason = Application.Students.StudentLifecycle.WhyNot(student, Application.Students.StudentAction.IssueTc);
        if (reason != null)
            throw new Application.Exceptions.BaseServiceException(reason, Application.Common.ExceptionCodes.Operation);
    }

    public async Task<TcCertificateResponse?> GetByIdAsync(Guid id)
    {
        const string cachePrefix = "tc:";
        var cacheKey = $"{cachePrefix}{id}";

        try
        {
            _logger.LogDebug("Fetching transfer certificate by id {Id}", id);

            if (_cacheService.Exists(cacheKey))
            {
                _logger.LogInformation("Returning transfer certificate {Id} from cache", id);
                return _cacheService.Get<TcCertificateResponse>(cacheKey);
            }

            var entity = await _tcRepo.FindAsync(t => t.Id == id);
            if (entity == null) return null;

            var dto = _mapper.Map<TcCertificateResponse>(entity);
            _cacheService.Set(cacheKey, dto);
            return dto;
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                $"Error fetching transfer certificate by id: {id}",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task<IEnumerable<TcCertificateResponse>> GetByStudentAsync(Guid studentId)
    {
        var cacheKey = $"tc:student:{studentId}";

        try
        {
            _logger.LogDebug("Fetching transfer certificates for student {StudentId}", studentId);

            if (_cacheService.Exists(cacheKey))
            {
                _logger.LogInformation("Returning transfer certificates for student {StudentId} from cache", studentId);
                return _cacheService.Get<IEnumerable<TcCertificateResponse>>(cacheKey);
            }

            var list = await _tcRepo.FindAllAsync(t => t.StudentId == studentId);
            var dtos = _mapper.Map<IEnumerable<TcCertificateResponse>>(list);
            _cacheService.Set(cacheKey, dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                $"Error fetching transfer certificates for student: {studentId}",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    // ----------------------------------------------------------------------
    // Workflow implementation (using PostgreSQL functions from TC script)
    // ----------------------------------------------------------------------

    public async Task InitiateClearanceAsync(Guid tcId)
    {
        try
        {
            _logger.LogInformation("Initiating TC clearance for {TcId}", tcId);
            await _tcRepo.ExecuteSqlRawAsync("SELECT initiate_tc_clearance({0})", tcId);
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error initiating transfer certificate clearance",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task HeadClerkApproveAsync(Guid tcId, Guid staffId)
    {
        try
        {
            _logger.LogInformation("Head clerk approval for TC {TcId} by staff {StaffId}", tcId, staffId);
            await _tcApprovalChain.HandleAsync(new TcApprovalRequest
            {
                TcId = tcId,
                StaffId = staffId,
                Action = "HEAD_CLERK_APPROVE"
            });
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error during head clerk approval",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task ClassTeacherApproveAsync(Guid tcId, Guid staffId)
    {
        try
        {
            _logger.LogInformation("Class teacher approval for TC {TcId} by staff {StaffId}", tcId, staffId);
            await _tcApprovalChain.HandleAsync(new TcApprovalRequest
            {
                TcId = tcId,
                StaffId = staffId,
                Action = "CLASS_TEACHER_APPROVE"
            });
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error during class teacher approval",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task PrincipalApproveAsync(Guid tcId, Guid staffId)
    {
        try
        {
            _logger.LogInformation("Principal approval for TC {TcId} by staff {StaffId}", tcId, staffId);
            await _tcApprovalChain.HandleAsync(new TcApprovalRequest
            {
                TcId = tcId,
                StaffId = staffId,
                Action = "PRINCIPAL_APPROVE"
            });
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error during principal approval",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task IssueAsync(Guid tcId, Guid staffId)
    {
        try
        {
            _logger.LogInformation("Issuing TC {TcId} by staff {StaffId}", tcId, staffId);
            await _tcApprovalChain.HandleAsync(new TcApprovalRequest
            {
                TcId = tcId,
                StaffId = staffId,
                Action = "ISSUE"
            });
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error issuing transfer certificate",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task RejectAsync(Guid tcId, Guid staffId, string reason)
    {
        try
        {
            _logger.LogInformation("Rejecting TC {TcId} by staff {StaffId}", tcId, staffId);
            await _tcApprovalChain.HandleAsync(new TcApprovalRequest
            {
                TcId = tcId,
                StaffId = staffId,
                Action = "REJECT",
                Reason = reason
            });
        }
        catch (Exception ex)
        {
            throw new Application.Exceptions.BaseServiceException(
                "Error rejecting transfer certificate",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }
}
