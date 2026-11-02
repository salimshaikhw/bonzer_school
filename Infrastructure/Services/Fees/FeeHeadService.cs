using Application.Abstractions.Services.Fees;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.FeeHead;
using AutoMapper;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Fees
{
    /// <summary>
    /// Fee heads (Tuition, Transport, Exam fee, ...). Rules: names and account codes are unique; a head used by a
    /// fee structure, class fee or payment cannot be deleted (payments must keep their breakdown).
    /// </summary>
    public class FeeHeadService : CrudService<FeeHead, FeeHeadRequest, FeeHeadResponse>, IFeeHeadService
    {
        private readonly BonzerSchoolDbContext _db;

        public FeeHeadService(ILogger<FeeHeadService> logger, IFeeHeadRepository repository, IMapper mapper, IMemoryCacheHelper cache, BonzerSchoolDbContext db)
            : base(repository, mapper, logger, cache)
        {
            _db = db;
        }

        protected override string EntityName => "FeeHead";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        protected override Task ValidateCreateAsync(FeeHeadRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(FeeHead existing, FeeHeadRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(FeeHead existing)
        {
            var id = existing.Id;
            if (await _db.FeeStructures.AnyAsync(f => f.FeeHeadId == id)
                || await _db.ClassFeeMaps.AnyAsync(f => f.FeeHeadId == id)
                || await _db.PaymentFeeLines.AnyAsync(l => l.FeeHeadId == id))
                throw Conflict($"Fee head '{existing.Name}' is used by fee structures or payments and cannot be deleted.");
        }

        protected override void BeforeSave(FeeHead entity)
        {
            entity.Name = entity.Name?.Trim();
            entity.AccountCode = string.IsNullOrWhiteSpace(entity.AccountCode) ? null : entity.AccountCode.Trim().ToUpperInvariant();
        }

        private async Task ValidateAsync(FeeHeadRequest request, Guid? exceptId)
        {
            var name = request.Name?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(h => h.Name != null && h.Name.ToLower() == name && h.Id != exceptId))
                throw Conflict($"Fee head '{request.Name!.Trim()}' already exists.");

            var code = request.AccountCode?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(code) && await Repository.IsExistAsync(h => h.AccountCode == code && h.Id != exceptId))
                throw Conflict($"Account code '{code}' is already used.");
        }
    }
}
