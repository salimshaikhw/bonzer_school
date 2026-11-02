using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Fees;
using Application.ViewModels.FeeStructure;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Fees
{
    /// <summary>
    /// Amount of a fee head for a class (optionally one division). Rules: class and fee head exist, the division
    /// belongs to the class, and a head is priced once per class / division.
    /// </summary>
    public class FeeStructureService : CrudService<FeeStructure, FeeStructureRequest, FeeStructureResponse>, IFeeStructureService
    {
        private readonly IAcademicStructure _structure;
        private readonly IFeeHeadRepository _feeHeads;

        public FeeStructureService(ILogger<FeeStructureService> logger, IFeeStructureRepository repository, IMapper mapper,
            IAcademicStructure structure, IFeeHeadRepository feeHeads)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _feeHeads = feeHeads;
        }

        protected override string EntityName => "FeeStructure";

        protected override Task ValidateCreateAsync(FeeStructureRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(FeeStructure existing, FeeStructureRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(FeeStructureRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);
            if (request.FeeHeadId is not { } headId || !await _feeHeads.IsExistAsync(h => h.Id == headId))
                throw RuleViolation($"Fee head {request.FeeHeadId} does not exist.");

            if (await Repository.IsExistAsync(f => f.ClassId == cls.Id && f.DivisionId == request.DivisionId
                                                   && f.FeeHeadId == headId && f.Id != exceptId))
                throw Conflict("This fee head already has an amount for the class; update it instead.");
        }
    }
}
