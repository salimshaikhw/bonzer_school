using Application.Abstractions.Services.Academics;
using Application.ViewModels.Timetable;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Timetable periods. Rules:
    ///   - class, division, subject and teacher exist and fit together; the branch is the class's branch;
    ///   - a class / division has one subject per day and period (also a unique index);
    ///   - a teacher cannot teach two classes in the same day and period of an academic year.
    /// </summary>
    public class TimetableService : CrudService<Timetable, TimetableRequest, TimetableResponse>, ITimetableService
    {
        private readonly IAcademicStructure _structure;

        public TimetableService(ILogger<TimetableService> logger, ITimetableRepository repository, IMapper mapper, IAcademicStructure structure)
            : base(repository, mapper, logger)
        {
            _structure = structure;
        }

        protected override string EntityName => "Timetable";

        protected override Task ValidateCreateAsync(TimetableRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Timetable existing, TimetableRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(TimetableRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);
            await _structure.EnsureStaffExistsAsync(request.TeacherId);
            if (request.BranchId is { } branchId && branchId != cls.BranchId)
                throw RuleViolation("The timetable branch must be the class's branch.");
            request.BranchId = cls.BranchId;

            if (await Repository.IsExistAsync(t => t.ClassId == request.ClassId && t.DivisionId == request.DivisionId
                                                   && t.AcademicYearId == request.AcademicYearId && t.DayOfWeek == request.DayOfWeek
                                                   && t.PeriodNo == request.PeriodNo && t.Id != exceptId))
                throw Conflict($"Period {request.PeriodNo} on day {request.DayOfWeek} is already scheduled for this class.");

            if (request.TeacherId != null
                && await Repository.IsExistAsync(t => t.TeacherId == request.TeacherId && t.AcademicYearId == request.AcademicYearId
                                                      && t.DayOfWeek == request.DayOfWeek && t.PeriodNo == request.PeriodNo
                                                      && t.Id != exceptId))
                throw Conflict($"The teacher already teaches another class in period {request.PeriodNo} on day {request.DayOfWeek}.");
        }
    }
}
