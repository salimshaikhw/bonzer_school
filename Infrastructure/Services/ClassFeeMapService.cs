using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ClassFeeMap;
using AutoMapper;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ClassFeeMapService : IClassFeeMapService
    {
        private readonly ILogger<ClassFeeMapService> _logger;
        private readonly IClassFeeMapRepository _classfeemap;

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ClassFeeMapService(
            ILogger<ClassFeeMapService> logger, 
            IClassFeeMapRepository classfeemap,
                        IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)

        {
            _logger = logger;
            _classfeemap = classfeemap;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }
        //
        public async Task<IEnumerable<ClassFeeMapResponse>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all ClassFeeMaps");
                string cacheKey = "ClassFeeMaps:all";

                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning ClassFeeMaps from cache");
                    return _cacheService.Get<IEnumerable<ClassFeeMapResponse>>(cacheKey);
                }

                var feeMaps = await _classfeemap.GetAllAsync();
                var result = _mapper.Map<IEnumerable<ClassFeeMapResponse>>(feeMaps);

                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ClassFeeMaps");
                throw new Application.Exceptions.BaseServiceException("Error fetching ClassFeeMaps", ex, Application.Common.ExceptionCodes.Database);
            }
        }


        public async Task<ClassFeeMapResponse> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Fetching ClassFeeMap by id: {id}");
                var feeMap = await _classfeemap.GetByIdAsync(id);

                if (feeMap == null)
                {
                    _logger.LogWarning($"ClassFeeMap not found: {id}");
                    return null;
                }

                return _mapper.Map<ClassFeeMapResponse>(feeMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching ClassFeeMap by id: {id}");
                throw new Application.Exceptions.BaseServiceException($"Error fetching ClassFeeMap by id: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }


        public async Task<ClassFeeMapResponse> CreateAsync(ClassFeeMapRequest request)
        {
            try
            {
                _logger.LogInformation("Creating ClassFeeMap");

                var feeMap = _mapper.Map<ClassFeeMap>(request);

                await _classfeemap.AddAsync(feeMap);
                await _unitOfWork.Commit();

                _cacheService.Remove("ClassFeeMaps:all");

                return _mapper.Map<ClassFeeMapResponse>(feeMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ClassFeeMap");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException("Error creating ClassFeeMap", ex, Application.Common.ExceptionCodes.Database);
            }
        }



        public async Task<ClassFeeMapResponse> UpdateAsync(Guid id, ClassFeeMapRequest request)
        {
            try
            {
                _logger.LogInformation($"Updating ClassFeeMap id: {id}");

                var feeMap = await _classfeemap.GetByIdAsync(id);
                if (feeMap == null)
                {
                    _logger.LogWarning($"ClassFeeMap not found: {id}");
                    return null;
                }

                _mapper.Map(request, feeMap); // AutoMapper updates entity with request values

                await _classfeemap.UpdateAsync(feeMap);
                await _unitOfWork.Commit();

                _cacheService.Remove("ClassFeeMaps:all");
                _cacheService.Remove($"ClassFeeMap:{id}");

                return _mapper.Map<ClassFeeMapResponse>(feeMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating ClassFeeMap id: {id}");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error updating ClassFeeMap id: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }


        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting ClassFeeMap id: {id}");

                await _classfeemap.DeleteAsync(id);
                await _unitOfWork.Commit();

                _cacheService.Remove("ClassFeeMaps:all");
                _cacheService.Remove($"ClassFeeMap:{id}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting ClassFeeMap id: {id}");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error deleting ClassFeeMap id: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

    }
}
