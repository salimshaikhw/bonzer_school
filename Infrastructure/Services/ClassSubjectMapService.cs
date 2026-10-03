using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ClassSubjectMap;
using AutoMapper;
using Infrastructure;
using Microsoft.Extensions.Logging;

public class ClassSubjectMapService : IClassSubjectMapService
{
    private readonly ILogger<ClassSubjectMapService> _logger;
    private readonly IClassSubjectMapRepository _classSubjectRepo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public ClassSubjectMapService(
        ILogger<ClassSubjectMapService> logger,
        IClassSubjectMapRepository classSubjectRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService)
    {
        _logger = logger;
        _classSubjectRepo = classSubjectRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<ClassSubjectMapResponse>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all ClassSubjectMaps");
        var entities = await _classSubjectRepo.GetAllAsync();
        return _mapper.Map<IEnumerable<ClassSubjectMapResponse>>(entities);
    }

    public async Task<ClassSubjectMapResponse> GetByIdAsync(Guid id)
    {
        _logger.LogInformation($"Fetching ClassSubjectMap by id: {id}");
        var entity = await _classSubjectRepo.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<ClassSubjectMapResponse>(entity);
    }

    public async Task<ClassSubjectMapResponse> CreateAsync(ClassSubjectMapRequest request)
    {
        _logger.LogInformation("Creating ClassSubjectMap");
        var entity = _mapper.Map<ClassSubjectMap>(request);

        await _classSubjectRepo.AddAsync(entity);
        await _unitOfWork.Commit();

        return _mapper.Map<ClassSubjectMapResponse>(entity);
    }

    public async Task<ClassSubjectMapResponse> UpdateAsync(Guid id, ClassSubjectMapRequest request)
    {
        _logger.LogInformation($"Updating ClassSubjectMap id: {id}");
        var entity = await _classSubjectRepo.GetByIdAsync(id);
        if (entity == null) return null;

        _mapper.Map(request, entity);
        await _classSubjectRepo.UpdateAsync(entity);
        await _unitOfWork.Commit();

        return _mapper.Map<ClassSubjectMapResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation($"Deleting ClassSubjectMap id: {id}");
        await _classSubjectRepo.DeleteAsync(id);
        await _unitOfWork.Commit();
        return true;
    }
}