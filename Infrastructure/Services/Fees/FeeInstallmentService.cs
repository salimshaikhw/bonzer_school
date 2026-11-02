using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Fees;
using Application.ViewModels.FeeInstallment;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Fees
{
    /// <summary>
    /// Fee installments of a class in an academic year (1st term due 15 June, ...). Rules: class (and its branch)
    /// and academic year exist; installment numbers are unique per class and year; the due date lies within the
    /// academic year. Date order and late-fee values are checked by FeeInstallmentRequestValidator.
    /// </summary>
    public class FeeInstallmentService : CrudService<FeeInstallment, FeeInstallmentRequest, FeeInstallmentResponse>, IFeeInstallmentService
    {
        private readonly IAcademicStructure _structure;
        private readonly IAcademicYearRepository _years;

        public FeeInstallmentService(ILogger<FeeInstallmentService> logger, IFeeInstallmentRepository repository, IMapper mapper,
            IAcademicStructure structure, IAcademicYearRepository years)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _years = years;
        }

        protected override string EntityName => "FeeInstallment";

        protected override Task ValidateCreateAsync(FeeInstallmentRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(FeeInstallment existing, FeeInstallmentRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(FeeInstallmentRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            if (request.BranchId != null && request.BranchId != cls.BranchId)
                throw RuleViolation("The installment's class is not part of its branch.");
            request.BranchId = cls.BranchId;

            var year = request.AcademicYearId is { } yearId ? await _years.FindAsync(y => y.Id == yearId) : null;
            if (year == null)
                throw RuleViolation($"Academic year {request.AcademicYearId} does not exist.");
            if (request.DueDate is { } due
                && ((year.StartDate is { } start && due < start) || (year.EndDate is { } end && due > end)))
                throw RuleViolation($"The due date must fall within academic year {year.YearName}.");

            if (await Repository.IsExistAsync(i => i.ClassId == cls.Id && i.AcademicYearId == year.Id
                                                   && i.InstallmentNo == request.InstallmentNo && i.Id != exceptId))
                throw Conflict($"Installment {request.InstallmentNo} already exists for this class and year.");
        }
    }
}
