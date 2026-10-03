using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using AutoMapper;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class BonafideCertificateService : IBonafideCertificateService
{
    private readonly IBonafideCertificateRepository _bonafideRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IAcademicYearRepository _yearRepo;
    private readonly IClassRepository _classRepo;
    private readonly IDivisionRepository _divisionRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<BonafideCertificateService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IBonafideNumberGenerator _bonafideNumberGenerator;

    public BonafideCertificateService(
        IBonafideCertificateRepository bonafideRepo,
        IStudentRepository studentRepo,
        IAcademicYearRepository yearRepo,
        IClassRepository classRepo,
        IDivisionRepository divisionRepo,
        IMapper mapper,
        ILogger<BonafideCertificateService> logger,
        ICacheService cacheService,
        IBonafideNumberGenerator bonafideNumberGenerator)
    {
        _bonafideRepo = bonafideRepo;
        _studentRepo = studentRepo;
        _yearRepo = yearRepo;
        _classRepo = classRepo;
        _divisionRepo = divisionRepo;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _bonafideNumberGenerator = bonafideNumberGenerator;
    }

    public async Task<BonafideCertificateResponse> CreateAsync(BonafideCertificateRequest request)
    {
        try
        {
            _logger.LogInformation("Creating bonafide certificate for student {StudentId}", request.StudentId);

            var student = await _studentRepo.GetByIdAsync(request.StudentId);
            var academicYearId = request.AcademicYearId ?? student.AcademicYearId
                                 ?? throw new InvalidOperationException("Student has no AcademicYear; provide AcademicYearId.");
            var classId = request.ClassId ?? student.ClassId
                          ?? throw new InvalidOperationException("Student has no Class; provide ClassId.");

            // Design Pattern: Strategy (Behavioral) via IBonafideNumberGenerator
            var (bonafideNumber, nextSeq) = await _bonafideNumberGenerator.GenerateAsync(academicYearId, classId);

            var entity = new BonafideCertificate
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                AcademicYearId = academicYearId,
                ClassId = classId,
                DivisionId = request.DivisionId ?? student.DivisionId,
                BranchId = student.BranchId,
                BonafideNumber = bonafideNumber,
                Sequence = nextSeq,
                IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Purpose = request.Purpose,
                Remarks = request.Remarks,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                IsDuplicate = request.IsDuplicate,
                CreatedAt = DateTime.UtcNow
            };

            await _bonafideRepo.AddAsync(entity);

            var dto = _mapper.Map<BonafideCertificateResponse>(entity);
            dto.StudentName = $"{student.FirstName} {student.LastName}".Trim();
            dto.AdmissionNo = student.AdmissionNo;
            dto.AcademicYearName = (await _yearRepo.GetByIdAsync(academicYearId))?.YearName;
            dto.ClassName = (await _classRepo.GetByIdAsync(classId))?.Name;
            if (entity.DivisionId.HasValue)
            {
                var div = await _divisionRepo.GetByIdAsync(entity.DivisionId.Value);
                dto.DivisionName = div?.Name;
            }

            var byIdKey = $"bonafide:{dto.Id}";
            var byStudentKey = $"bonafide:student:{student.Id}";
            _cacheService.Set(byIdKey, dto);
            _cacheService.Remove(byStudentKey);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bonafide certificate for student {StudentId}", request.StudentId);
            throw new Application.Exceptions.BaseServiceException(
                "Error creating bonafide certificate",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task<BonafideCertificateResponse?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"bonafide:{id}";

        try
        {
            _logger.LogInformation("Fetching bonafide certificate by id {Id}", id);

            if (_cacheService.Exists(cacheKey))
            {
                _logger.LogInformation("Returning bonafide certificate {Id} from cache", id);
                return _cacheService.Get<BonafideCertificateResponse>(cacheKey);
            }

            var entity = await _bonafideRepo.FindAsync(t => t.Id == id);
            if (entity == null) return null;

            var dto = _mapper.Map<BonafideCertificateResponse>(entity);
            _cacheService.Set(cacheKey, dto);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bonafide certificate by id {Id}", id);
            throw new Application.Exceptions.BaseServiceException(
                $"Error fetching bonafide certificate by id: {id}",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task<IEnumerable<BonafideCertificateResponse>> GetByStudentAsync(Guid studentId)
    {
        var cacheKey = $"bonafide:student:{studentId}";

        try
        {
            _logger.LogInformation("Fetching bonafide certificates for student {StudentId}", studentId);

            if (_cacheService.Exists(cacheKey))
            {
                _logger.LogInformation("Returning bonafide certificates for student {StudentId} from cache", studentId);
                return _cacheService.Get<IEnumerable<BonafideCertificateResponse>>(cacheKey);
            }

            var list = await _bonafideRepo.FindAllAsync(t => t.StudentId == studentId);
            var dtos = _mapper.Map<IEnumerable<BonafideCertificateResponse>>(list);
            _cacheService.Set(cacheKey, dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bonafide certificates for student {StudentId}", studentId);
            throw new Application.Exceptions.BaseServiceException(
                $"Error fetching bonafide certificates for student: {studentId}",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }
}
