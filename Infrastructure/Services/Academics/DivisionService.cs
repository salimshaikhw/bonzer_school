using Application.Abstractions.Services.Academics;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Division;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Divisions (sections A, B, ...) of a class. Rules: the class exists; the division's branch is always
    /// the class's branch (taken from the class, a different one is rejected); names are unique per class.
    /// </summary>
    public class DivisionService : CrudService<Division, DivisionRequest, DivisionResponse>, IDivisionService
    {
        private readonly IAcademicStructure _structure;

        public DivisionService(IDivisionRepository repository, ILogger<DivisionService> logger, IMapper mapper,
            IMemoryCacheHelper cache, IAcademicStructure structure)
            : base(repository, mapper, logger, cache)
        {
            _structure = structure;
        }

        protected override string EntityName => "Division";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        public async Task<IEnumerable<DivisionResponse>> GetByClassIdAsync(Guid classId) =>
            Mapper.Map<IEnumerable<DivisionResponse>>(await Repository.GetAllAsync(d => d.ClassId == classId));

        protected override Task ValidateCreateAsync(DivisionRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Division existing, DivisionRequest request) => ValidateAsync(request, existing.Id);

        protected override void BeforeSave(Division entity)
        {
            entity.Name = entity.Name?.Trim();
            entity.Code = string.IsNullOrWhiteSpace(entity.Code) ? null : entity.Code.Trim();
        }

        private async Task ValidateAsync(DivisionRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            if (request.BranchId is { } branchId && branchId != cls.BranchId)
                throw RuleViolation("A division must be in the same branch as its class.");
            request.BranchId = cls.BranchId; // derived, so the two can never disagree

            var name = request.Name?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(d => d.ClassId == cls.Id && d.Name != null && d.Name.ToLower() == name && d.Id != exceptId))
                throw Conflict($"Division '{request.Name!.Trim()}' already exists in class '{cls.Name}'.");
        }
    }
}
