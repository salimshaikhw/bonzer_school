using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using AutoMapper;
using Infrastructure;
using Infrastructure.Services.TcWorkflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

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
                                 ?? throw new InvalidOperationException("Student has no AcademicYear; provide AcademicYearId.");
            var classId = request.ClassId ?? student.ClassId
                          ?? throw new InvalidOperationException("Student has no Class; provide ClassId.");

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
            _logger.LogError(ex, "Error creating transfer certificate for student {StudentId}", request.StudentId);
            throw new Application.Exceptions.BaseServiceException(
                "Error creating transfer certificate",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task<TcCertificateResponse?> GetByIdAsync(Guid id)
    {
        const string cachePrefix = "tc:";
        var cacheKey = $"{cachePrefix}{id}";

        try
        {
            _logger.LogInformation("Fetching transfer certificate by id {Id}", id);

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
            _logger.LogError(ex, "Error fetching transfer certificate by id {Id}", id);
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
            _logger.LogInformation("Fetching transfer certificates for student {StudentId}", studentId);

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
            _logger.LogError(ex, "Error fetching transfer certificates for student {StudentId}", studentId);
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
            _logger.LogError(ex, "Error initiating TC clearance for {TcId}", tcId);
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
            _logger.LogError(ex, "Error during head clerk approval for TC {TcId}", tcId);
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
            _logger.LogError(ex, "Error during class teacher approval for TC {TcId}", tcId);
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
            _logger.LogError(ex, "Error during principal approval for TC {TcId}", tcId);
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
            _logger.LogError(ex, "Error issuing TC {TcId}", tcId);
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
            _logger.LogError(ex, "Error rejecting TC {TcId}", tcId);
            throw new Application.Exceptions.BaseServiceException(
                "Error rejecting transfer certificate",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }
}
