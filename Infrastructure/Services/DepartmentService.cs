using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Department;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ILogger<DepartmentService> _logger;
        private readonly IDepartmentRepository _departmentRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public DepartmentService(
            ILogger<DepartmentService> logger,
            IDepartmentRepository departmentRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _departmentRepo = departmentRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<DepartmentResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Departments");
            var entities = await _departmentRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<DepartmentResponse>>(entities);
        }

        public async Task<DepartmentResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Department by id: {id}");
            var entity = await _departmentRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<DepartmentResponse>(entity);
        }

        public async Task<DepartmentResponse> CreateAsync(DepartmentRequest request)
        {
            _logger.LogInformation("Creating Department");
            var entity = _mapper.Map<Department>(request);

            await _departmentRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Departments:all");

            return _mapper.Map<DepartmentResponse>(entity);
        }

        public async Task<DepartmentResponse> UpdateAsync(Guid id, DepartmentRequest request)
        {
            _logger.LogInformation($"Updating Department id: {id}");
            var entity = await _departmentRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _departmentRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Departments:all");
            _cacheService.Remove($"Department:{id}");

            return _mapper.Map<DepartmentResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Department id: {id}");
            await _departmentRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("Departments:all");
            _cacheService.Remove($"Department:{id}");

            return true;
        }
    }
}