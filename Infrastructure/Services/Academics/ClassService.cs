using Application.Abstractions.Services.Academics;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Class;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Classes (standards / grades / years) of a branch. Rules: the branch exists; class names are unique
    /// within a branch; a class that still has divisions cannot be deleted.
    /// </summary>
    public class ClassService : CrudService<Class, ClassRequest, ClassResponse>, IClassService
    {
        private readonly IAcademicStructure _structure;
        private readonly IDivisionRepository _divisions;

        public ClassService(IClassRepository repository, IMapper mapper, IMemoryCacheHelper cache, ILogger<ClassService> logger,
            IAcademicStructure structure, IDivisionRepository divisions)
            : base(repository, mapper, logger, cache)
        {
            _structure = structure;
            _divisions = divisions;
        }

        protected override string EntityName => "Class";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        public async Task<IEnumerable<ClassResponse>> GetByBranchIdAsync(Guid branchId) =>
            Mapper.Map<IEnumerable<ClassResponse>>(await Repository.GetAllAsync(c => c.BranchId == branchId));

        protected override Task ValidateCreateAsync(ClassRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Class existing, ClassRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(Class existing)
        {
            if (await _divisions.IsExistAsync(d => d.ClassId == existing.Id))
                throw Conflict($"Class '{existing.Name}' still has divisions. Delete or move them first.");
        }

        protected override void BeforeSave(Class entity) => entity.Name = entity.Name?.Trim();

        private async Task ValidateAsync(ClassRequest request, Guid? exceptId)
        {
            await _structure.EnsureBranchExistsAsync(request.BranchId);

            var name = request.Name?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(c => c.BranchId == request.BranchId && c.Name != null && c.Name.ToLower() == name && c.Id != exceptId))
                throw Conflict($"Class '{request.Name!.Trim()}' already exists in this branch.");
        }
    }
}
