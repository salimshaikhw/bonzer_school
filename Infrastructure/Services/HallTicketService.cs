using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class HallTicketService : IHallTicketService
{
    private readonly IHallTicketRepository _hallTicketRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IExamRepository _examRepo;
    private readonly IExamSubjectRepository _examSubjectRepo;
    private readonly ISubjectRepository _subjectRepo;
    private readonly IAcademicYearRepository _yearRepo;
    private readonly IClassRepository _classRepo;
    private readonly IDivisionRepository _divisionRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<HallTicketService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IHallTicketNumberGenerator _hallTicketNumberGenerator;

    public HallTicketService(
        IHallTicketRepository hallTicketRepo,
        IStudentRepository studentRepo,
        IExamRepository examRepo,
        IExamSubjectRepository examSubjectRepo,
        ISubjectRepository subjectRepo,
        IAcademicYearRepository yearRepo,
        IClassRepository classRepo,
        IDivisionRepository divisionRepo,
        IMapper mapper,
        ILogger<HallTicketService> logger,
        ICacheService cacheService,
        IHallTicketNumberGenerator hallTicketNumberGenerator)
    {
        _hallTicketRepo = hallTicketRepo;
        _studentRepo = studentRepo;
        _examRepo = examRepo;
        _examSubjectRepo = examSubjectRepo;
        _subjectRepo = subjectRepo;
        _yearRepo = yearRepo;
        _classRepo = classRepo;
        _divisionRepo = divisionRepo;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _hallTicketNumberGenerator = hallTicketNumberGenerator;
    }

    public async Task<HallTicketResponse> GenerateAsync(HallTicketRequest request)
    {
        try
        {
            _logger.LogInformation("Generating hall ticket for student {StudentId} and exam {ExamId}", request.StudentId, request.ExamId);

            var student = await _studentRepo.GetByIdAsync(request.StudentId);
            var exam = await _examRepo.GetByIdAsync(request.ExamId);

            var existing = await _hallTicketRepo.FindAsync(h => h.StudentId == request.StudentId && h.ExamId == request.ExamId);
            if (existing != null)
                return await BuildResponse(existing, student, exam);

            // Design Pattern: Strategy (Behavioral) via IHallTicketNumberGenerator
            var ticketNumber = await _hallTicketNumberGenerator.GenerateAsync(
                request.ExamId,
                exam.Name,
                student.AdmissionNo,
                student.Id);

            var entity = new HallTicket
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                ExamId = request.ExamId,
                AcademicYearId = exam.AcademicYearId ?? student.AcademicYearId,
                ClassId = exam.ClassId ?? student.ClassId,
                DivisionId = exam.DivisionId ?? student.DivisionId,
                BranchId = exam.BranchId ?? student.BranchId,
                TicketNumber = ticketNumber,
                SeatNumber = request.SeatNumber,
                ExamCentre = request.ExamCentre,
                GeneratedAt = DateTime.UtcNow
            };

            await _hallTicketRepo.AddAsync(entity);

            // Invalidate cached hall ticket for this student+exam
            var cacheKey = $"hallticket:student:{request.StudentId}:exam:{request.ExamId}";
            _cacheService.Remove(cacheKey);

            return await BuildResponse(entity, student, exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hall ticket for student {StudentId} and exam {ExamId}", request.StudentId, request.ExamId);
            throw new Application.Exceptions.BaseServiceException(
                "Error generating hall ticket",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    private async Task<HallTicketResponse> BuildResponse(HallTicket entity, Student student, Exam exam)
    {
        var dto = _mapper.Map<HallTicketResponse>(entity);
        dto.StudentName = $"{student.FirstName} {student.LastName}".Trim();
        dto.AdmissionNo = student.AdmissionNo;
        dto.ExamName = exam.Name;
        dto.ExamStartDate = exam.StartDate;
        dto.ExamEndDate = exam.EndDate;
        if (entity.AcademicYearId.HasValue)
        {
            var y = await _yearRepo.GetByIdAsync(entity.AcademicYearId.Value);
            dto.AcademicYearName = y?.YearName;
        }
        if (entity.ClassId.HasValue)
        {
            var c = await _classRepo.GetByIdAsync(entity.ClassId.Value);
            dto.ClassName = c?.Name;
        }
        if (entity.DivisionId.HasValue)
        {
            var d = await _divisionRepo.GetByIdAsync(entity.DivisionId.Value);
            dto.DivisionName = d?.Name;
        }

        var examSubjects = await _examSubjectRepo.FindAllAsync(es => es.ExamId == entity.ExamId);
        foreach (var es in examSubjects.Where(es => es.SubjectId.HasValue))
        {
            var sub = await _subjectRepo.GetByIdAsync(es.SubjectId!.Value);
            dto.Papers.Add(new HallTicketPaperDto
            {
                SubjectId = es.SubjectId.Value,
                SubjectName = sub?.Name,
                ExamDate = es.ExamDate,
                StartTime = es.StartTime,
                EndTime = es.EndTime
            });
        }
        return dto;
    }

    public async Task<HallTicketResponse?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"hallticket:{id}";

        try
        {
            _logger.LogInformation("Fetching hall ticket by id {Id}", id);

            if (_cacheService.Exists(cacheKey))
            {
                _logger.LogInformation("Returning hall ticket {Id} from cache", id);
                return _cacheService.Get<HallTicketResponse>(cacheKey);
            }

            var entity = await _hallTicketRepo.FindAsync(h => h.Id == id);
            if (entity == null) return null;

            var student = await _studentRepo.GetByIdAsync(entity.StudentId);
            var exam = await _examRepo.GetByIdAsync(entity.ExamId);
            var dto = await BuildResponse(entity, student, exam);

            _cacheService.Set(cacheKey, dto);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hall ticket by id {Id}", id);
            throw new Application.Exceptions.BaseServiceException(
                $"Error fetching hall ticket by id: {id}",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }

    public async Task<HallTicketResponse?> GetByStudentAndExamAsync(Guid studentId, Guid examId)
    {
        var cacheKey = $"hallticket:student:{studentId}:exam:{examId}";

        try
        {
            _logger.LogInformation("Fetching hall ticket for student {StudentId} and exam {ExamId}", studentId, examId);

            if (_cacheService.Exists(cacheKey))
            {
                _logger.LogInformation("Returning hall ticket for student {StudentId} and exam {ExamId} from cache", studentId, examId);
                return _cacheService.Get<HallTicketResponse>(cacheKey);
            }

            var entity = await _hallTicketRepo.FindAsync(h => h.StudentId == studentId && h.ExamId == examId);
            if (entity == null) return null;

            var student = await _studentRepo.GetByIdAsync(entity.StudentId);
            var exam = await _examRepo.GetByIdAsync(entity.ExamId);
            var dto = await BuildResponse(entity, student, exam);

            _cacheService.Set(cacheKey, dto);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hall ticket for student {StudentId} and exam {ExamId}", studentId, examId);
            throw new Application.Exceptions.BaseServiceException(
                $"Error fetching hall ticket for student {studentId} and exam {examId}",
                ex,
                Application.Common.ExceptionCodes.Database);
        }
    }
}
