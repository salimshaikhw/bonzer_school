using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.AcademicYear;

namespace Application.Abstractions.Services
{
    public interface IAcademicYearService
    {
        Task<IEnumerable<AcademicYearResponse>> GetAllAsync();
        Task<AcademicYearResponse?> GetByIdAsync(Guid id);
        Task<AcademicYearResponse> CreateAsync(AcademicYearRequest input);
        Task<AcademicYearResponse> UpdateAsync(Guid id, AcademicYearRequest input);
        Task DeleteAsync(Guid id);
    }
}
