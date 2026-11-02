using Application.Abstractions.Services.Personnel;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Department;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Personnel
{
    /// <summary>Staff departments. Rules: names are unique; a department with staff cannot be deleted.</summary>
    public class DepartmentService : CrudService<Department, DepartmentRequest, DepartmentResponse>, IDepartmentService
    {
        private readonly IStaffRepository _staff;

        public DepartmentService(ILogger<DepartmentService> logger, IDepartmentRepository repository, IMapper mapper,
            IMemoryCacheHelper cache, IStaffRepository staff)
            : base(repository, mapper, logger, cache)
        {
            _staff = staff;
        }

        protected override string EntityName => "Department";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        protected override Task ValidateCreateAsync(DepartmentRequest request) => EnsureNameIsFreeAsync(request.Name, exceptId: null);

        protected override Task ValidateUpdateAsync(Department existing, DepartmentRequest request) => EnsureNameIsFreeAsync(request.Name, existing.Id);

        protected override async Task ValidateDeleteAsync(Department existing)
        {
            if (await _staff.IsExistAsync(s => s.DeptId == existing.Id))
                throw Conflict($"Department '{existing.Name}' still has staff. Move them to another department first.");
        }

        protected override void BeforeSave(Department entity) => entity.Name = entity.Name?.Trim();

        private async Task EnsureNameIsFreeAsync(string? requestedName, Guid? exceptId)
        {
            var name = requestedName?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(d => d.Name != null && d.Name.ToLower() == name && d.Id != exceptId))
                throw Conflict($"Department '{requestedName!.Trim()}' already exists.");
        }
    }
}
