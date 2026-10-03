using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.InstitutionType;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class InstitutionTypeService : IInstitutionTypeService
    {
        private readonly ILogger<InstitutionTypeService> _logger;
        private readonly IInstitutionTypeRepository _institutionTypeRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public InstitutionTypeService(
            ILogger<InstitutionTypeService> logger,
            IInstitutionTypeRepository institutionTypeRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _institutionTypeRepo = institutionTypeRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<InstitutionTypeResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all InstitutionTypes");
            var entities = await _institutionTypeRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<InstitutionTypeResponse>>(entities);
        }

        public async Task<InstitutionTypeResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching InstitutionType by id: {id}");
            var entity = await _institutionTypeRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<InstitutionTypeResponse>(entity);
        }

        public async Task<InstitutionTypeResponse> CreateAsync(InstitutionTypeRequest request)
        {
            _logger.LogInformation("Creating InstitutionType");
            var entity = _mapper.Map<InstitutionType>(request);

            await _institutionTypeRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("InstitutionTypes:all");

            return _mapper.Map<InstitutionTypeResponse>(entity);
        }

        public async Task<InstitutionTypeResponse> UpdateAsync(Guid id, InstitutionTypeRequest request)
        {
            _logger.LogInformation($"Updating InstitutionType id: {id}");
            var entity = await _institutionTypeRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _institutionTypeRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("InstitutionTypes:all");
            _cacheService.Remove($"InstitutionType:{id}");

            return _mapper.Map<InstitutionTypeResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting InstitutionType id: {id}");
            await _institutionTypeRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("InstitutionTypes:all");
            _cacheService.Remove($"InstitutionType:{id}");

            return true;
        }
    }
}