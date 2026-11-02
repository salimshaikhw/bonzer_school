using Application.Abstractions.Repositories;
using Application.Common;
using Application.Common.Helpers.MemoryCache;
using Application.Exceptions;
using AutoMapper;
using Domain.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Common;

/// <summary>
/// Template Method base for services that create, read, update and delete one entity type.
/// The flow is fixed here (load -> check business rules -> map -> save -> invalidate cache -> log);
/// a derived service only supplies what differs: the rule hooks <see cref="ValidateCreateAsync"/>,
/// <see cref="ValidateUpdateAsync"/>, <see cref="ValidateDeleteAsync"/>, and optionally caching.
///
/// Request shape (required fields, lengths, formats) is checked earlier by FluentValidation validators in
/// Application/Validation; the hooks are for rules that need data, e.g. "name must be unique".
/// Not found is reported by the repository (BaseServiceException ItemNotFound -> 404).
/// </summary>
public abstract class CrudService<TEntity, TRequest, TResponse>
    where TEntity : BaseEntity
{
    protected CrudService(IRepository<TEntity> repository, IMapper mapper, ILogger logger, IMemoryCacheHelper? cache = null)
    {
        Repository = repository;
        Mapper = mapper;
        Logger = logger;
        Cache = cache;
    }

    protected IRepository<TEntity> Repository { get; }
    protected IMapper Mapper { get; }
    protected ILogger Logger { get; }
    protected IMemoryCacheHelper? Cache { get; }

    /// <summary>Name used in messages, logs and cache keys, e.g. "Branch".</summary>
    protected abstract string EntityName { get; }

    /// <summary>How long reads are cached (tenant-scoped by IMemoryCacheHelper); null disables caching.</summary>
    protected virtual TimeSpan? CacheDuration => null;

    public virtual async Task<IEnumerable<TResponse>> GetAllAsync()
    {
        return await CachedAsync(AllKey, async () =>
            Mapper.Map<IEnumerable<TResponse>>(await Repository.GetAllAsync()).ToList());
    }

    public virtual async Task<TResponse> GetByIdAsync(Guid id)
    {
        return await CachedAsync(ItemKey(id), async () =>
            Mapper.Map<TResponse>(await Repository.GetByIdAsync(id)));
    }

    public virtual async Task<TResponse> CreateAsync(TRequest request)
    {
        await ValidateCreateAsync(request);

        var entity = Mapper.Map<TEntity>(request);
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        BeforeSave(entity);

        await Repository.AddAsync(entity);
        Invalidate(entity.Id);
        Logger.LogInformation("Created {EntityType} {EntityId}", EntityName, entity.Id);
        return Mapper.Map<TResponse>(entity);
    }

    public virtual async Task<TResponse> UpdateAsync(Guid id, TRequest request)
    {
        var entity = await Repository.GetByIdAsync(id);
        await ValidateUpdateAsync(entity, request);

        Mapper.Map(request, entity);
        entity.Id = id; // a request can never move the row to another id
        BeforeSave(entity);

        await Repository.UpdateAsync(entity);
        Invalidate(id);
        Logger.LogInformation("Updated {EntityType} {EntityId}", EntityName, id);
        return Mapper.Map<TResponse>(entity);
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await Repository.GetByIdAsync(id);
        await ValidateDeleteAsync(entity);

        await Repository.DeleteAsync(id);
        Invalidate(id);
        Logger.LogInformation("Deleted {EntityType} {EntityId}", EntityName, id);
        return true;
    }

    /// <summary>Business rules for a new row. Throw <see cref="RuleViolation"/> or <see cref="Conflict"/>.</summary>
    protected virtual Task ValidateCreateAsync(TRequest request) => Task.CompletedTask;

    /// <summary>Business rules for changing <paramref name="existing"/> (still unchanged) with <paramref name="request"/>.</summary>
    protected virtual Task ValidateUpdateAsync(TEntity existing, TRequest request) => Task.CompletedTask;

    /// <summary>Business rules for deleting. Rows still referenced elsewhere are also refused by the database (409).</summary>
    protected virtual Task ValidateDeleteAsync(TEntity existing) => Task.CompletedTask;

    /// <summary>Last chance to set derived or defaulted fields before the row is saved.</summary>
    protected virtual void BeforeSave(TEntity entity)
    {
    }

    /// <summary>The request breaks a rule (400).</summary>
    protected static BaseServiceException RuleViolation(string message) => new(message, ExceptionCodes.Validation);

    /// <summary>The request clashes with existing data or the current state (409).</summary>
    protected static BaseServiceException Conflict(string message) => new(message, ExceptionCodes.Operation);

    private string AllKey => $"{EntityName}:all";

    private string ItemKey(Guid id) => $"{EntityName}:{id:N}";

    private async Task<T> CachedAsync<T>(string key, Func<Task<T>> load)
    {
        if (Cache == null || CacheDuration is not { } duration)
            return await load();

        var cached = Cache.GetCache<T>(key);
        if (cached != null)
            return cached;

        var value = await load();
        Cache.SetCache(key, value, duration);
        return value;
    }

    private void Invalidate(Guid id)
    {
        if (Cache == null || CacheDuration == null)
            return;
        Cache.RemoveCache(AllKey);
        Cache.RemoveCache(ItemKey(id));
    }
}
