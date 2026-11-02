using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Exams;
using Application.ViewModels.Exam;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Exams of a class (optionally one division) in an academic year. Rules: exam type, academic year, branch,
    /// class and division exist and fit together; a locked exam can only be unlocked (no other change) and
    /// cannot be deleted. Dates and name are checked by ExamRequestValidator.
    /// </summary>
    public class ExamService : CrudService<Exam, ExamRequest, ExamResponse>, IExamService
    {
        private readonly IAcademicStructure _structure;
        private readonly IExamTypeRepository _examTypes;
        private readonly IAcademicYearRepository _years;

        public ExamService(ILogger<ExamService> logger, IExamRepository repository, IMapper mapper,
            IAcademicStructure structure, IExamTypeRepository examTypes, IAcademicYearRepository years)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _examTypes = examTypes;
            _years = years;
        }

        protected override string EntityName => "Exam";

        protected override Task ValidateCreateAsync(ExamRequest request) => ValidateAsync(request);

        protected override Task ValidateUpdateAsync(Exam existing, ExamRequest request)
        {
            if (existing.IsLocked == true && request.IsLocked != false)
                throw Conflict($"Exam '{existing.Name}' is locked. Unlock it (IsLocked = false) before changing it.");
            if (existing.IsLocked == true)
                return Task.CompletedTask; // unlocking: nothing else is checked
            return ValidateAsync(request);
        }

        protected override Task ValidateDeleteAsync(Exam existing)
        {
            ExamStructure.EnsureUnlocked(existing);
            return Task.CompletedTask;
        }

        protected override void BeforeSave(Exam entity) => entity.Name = entity.Name?.Trim();

        private async Task ValidateAsync(ExamRequest request)
        {
            if (request.ExamTypeId is { } typeId && !await _examTypes.IsExistAsync(t => t.Id == typeId))
                throw RuleViolation($"Exam type {typeId} does not exist.");
            if (request.AcademicYearId is { } yearId && !await _years.IsExistAsync(y => y.Id == yearId))
                throw RuleViolation($"Academic year {yearId} does not exist.");

            if (request.ClassId != null)
            {
                var cls = await _structure.GetClassAsync(request.ClassId);
                if (request.BranchId != null && request.BranchId != cls.BranchId)
                    throw RuleViolation("The exam's class is not part of its branch.");
                request.BranchId = cls.BranchId;
                await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);
            }
            else if (request.DivisionId != null)
            {
                throw RuleViolation("Select the class of the division.");
            }
            else if (request.BranchId != null)
            {
                await _structure.EnsureBranchExistsAsync(request.BranchId);
            }
        }
    }
}
