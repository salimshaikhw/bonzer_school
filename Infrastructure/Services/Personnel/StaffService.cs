using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Personnel;
using Application.ViewModels.Staff;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Personnel
{
    /// <summary>
    /// Staff members. Rules:
    ///   - the branch exists, and the department when given;
    ///   - staff codes are unique within a branch (also a filtered unique index); emails are unique;
    ///   - new staff are active unless stated otherwise;
    ///   - staff still assigned to classes, subjects or timetable periods cannot be deleted: deactivate them
    ///     (IsActive = false) so attendance, marks and history keep their teacher.
    /// Field formats and dates are checked by StaffRequestValidator.
    /// </summary>
    public class StaffService : CrudService<Staff, StaffRequest, StaffResponse>, IStaffService
    {
        private readonly IAcademicStructure _structure;
        private readonly IDepartmentRepository _departments;
        private readonly IClassTeacherMapRepository _classTeachers;
        private readonly ITeacherSubjectMapRepository _teacherSubjects;
        private readonly ITimetableRepository _timetable;

        public StaffService(ILogger<StaffService> logger, IStaffRepository repository, IMapper mapper, IAcademicStructure structure,
            IDepartmentRepository departments, IClassTeacherMapRepository classTeachers, ITeacherSubjectMapRepository teacherSubjects,
            ITimetableRepository timetable)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _departments = departments;
            _classTeachers = classTeachers;
            _teacherSubjects = teacherSubjects;
            _timetable = timetable;
        }

        protected override string EntityName => "Staff";

        protected override Task ValidateCreateAsync(StaffRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Staff existing, StaffRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(Staff existing)
        {
            var id = existing.Id;
            if (await _classTeachers.IsExistAsync(m => m.StaffId == id)
                || await _teacherSubjects.IsExistAsync(m => m.StaffId == id)
                || await _timetable.IsExistAsync(t => t.TeacherId == id))
                throw Conflict("This staff member is assigned to classes, subjects or timetable periods. Deactivate them instead of deleting.");
        }

        protected override void BeforeSave(Staff entity)
        {
            entity.FirstName = entity.FirstName?.Trim();
            entity.LastName = entity.LastName?.Trim();
            entity.StaffCode = string.IsNullOrWhiteSpace(entity.StaffCode) ? null : entity.StaffCode.Trim().ToUpperInvariant();
            entity.Email = string.IsNullOrWhiteSpace(entity.Email) ? null : entity.Email.Trim().ToLowerInvariant();
            entity.IsActive ??= true;
        }

        private async Task ValidateAsync(StaffRequest request, Guid? exceptId)
        {
            await _structure.EnsureBranchExistsAsync(request.BranchId);
            if (request.DeptId is { } deptId && !await _departments.IsExistAsync(d => d.Id == deptId))
                throw RuleViolation($"Department {deptId} does not exist.");

            var code = request.StaffCode?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(code)
                && await Repository.IsExistAsync(s => s.BranchId == request.BranchId && s.StaffCode == code && s.Id != exceptId))
                throw Conflict($"Staff code '{code}' is already used in this branch.");

            var email = request.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(email)
                && await Repository.IsExistAsync(s => s.Email == email && s.Id != exceptId))
                throw Conflict($"Another staff member already uses {email}.");
        }
    }
}
