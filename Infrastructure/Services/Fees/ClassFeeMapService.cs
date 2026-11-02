using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Fees;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.ClassFeeMap;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Fees
{
    /// <summary>
    /// What a class pays for a fee head in an academic year (optionally only one division, optionally per
    /// installment). These rows are what FeeLedger charges students, so the rules are strict:
    ///   - class, division (of that class), academic year and fee head exist;
    ///   - one amount per class + division + year + head + installment (no double charging);
    ///   - the AcademicYear label is taken from the academic year, so the two can never disagree.
    /// The amount is checked by ClassFeeMapRequestValidator.
    /// </summary>
    public class ClassFeeMapService : CrudService<ClassFeeMap, ClassFeeMapRequest, ClassFeeMapResponse>, IClassFeeMapService
    {
        private readonly IAcademicStructure _structure;
        private readonly IAcademicYearRepository _years;
        private readonly IFeeHeadRepository _feeHeads;

        public ClassFeeMapService(ILogger<ClassFeeMapService> logger, IClassFeeMapRepository repository, IMapper mapper,
            IMemoryCacheHelper cache, IAcademicStructure structure, IAcademicYearRepository years, IFeeHeadRepository feeHeads)
            : base(repository, mapper, logger, cache)
        {
            _structure = structure;
            _years = years;
            _feeHeads = feeHeads;
        }

        protected override string EntityName => "ClassFeeMap";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        protected override Task ValidateCreateAsync(ClassFeeMapRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ClassFeeMap existing, ClassFeeMapRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(ClassFeeMapRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);

            var year = request.AcademicYearId is { } yearId ? await _years.FindAsync(y => y.Id == yearId) : null;
            if (year == null)
                throw RuleViolation($"Academic year {request.AcademicYearId} does not exist.");
            request.AcademicYear = year.YearName; // label follows the year

            if (request.FeeHeadId is not { } headId || !await _feeHeads.IsExistAsync(h => h.Id == headId))
                throw RuleViolation($"Fee head {request.FeeHeadId} does not exist.");

            if (await Repository.IsExistAsync(f => f.ClassId == cls.Id && f.DivisionId == request.DivisionId && f.AcademicYearId == year.Id
                                                   && f.FeeHeadId == headId && f.InstallmentNo == request.InstallmentNo && f.Id != exceptId))
                throw Conflict("This fee head already has an amount for the class, year and installment; update it instead.");
        }
    }
}
