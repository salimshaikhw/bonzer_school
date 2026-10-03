using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.SchoolGroup;

namespace Application.Abstractions.Services
{
    public interface ISchoolGroupService
    {
        Task<IEnumerable<SchoolGroupDto>> GetAllAsync();
        Task<SchoolGroupDto?> GetByIdAsync(Guid id);
        Task<SchoolGroupDto> CreateAsync(CreateSchoolGroupInput input);
        Task<SchoolGroupDto> UpdateAsync(Guid id, UpdateSchoolGroupInput input);
        Task DeleteAsync(Guid id);
    }
}
