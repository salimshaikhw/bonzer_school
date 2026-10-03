using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Mark;
using AutoMapper;
using Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class MarkService : IMarkService
    {
        private readonly ILogger<MarkService> _logger;
        private readonly IMarkRepository _markRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MarkService(
            ILogger<MarkService> logger,
            IMarkRepository markRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _markRepo = markRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MarkResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Marks");
            var entities = await _markRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<MarkResponse>>(entities);
        }

        public async Task<IEnumerable<MarkResponse>> GetByStudentIdAsync(Guid studentId)
        {
            _logger.LogInformation("Fetching Marks for student {StudentId}", studentId);
            var rows = await _markRepo.FindAllAsync(m => m.StudentId == studentId);
            var ordered = (rows ?? new List<Mark>())
                .OrderByDescending(m => m.EnteredAt)
                .ThenBy(m => m.ExamSubjectComponentId)
                .ToList();
            return _mapper.Map<IEnumerable<MarkResponse>>(ordered);
        }

        public async Task<MarkResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Mark by id: {id}");
            var entity = await _markRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<MarkResponse>(entity);
        }

        public async Task<MarkResponse> CreateAsync(MarkRequest request)
        {
            _logger.LogInformation("Creating Mark");
            var entity = _mapper.Map<Mark>(request);

            await _markRepo.AddAsync(entity);
            // Commit is deferred
            return _mapper.Map<MarkResponse>(entity);
        }

        public async Task<MarkResponse> UpdateAsync(Guid id, MarkRequest request)
        {
            _logger.LogInformation($"Updating Mark id: {id}");
            var entity = await _markRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _markRepo.UpdateAsync(entity);
            // Commit is deferred
            return _mapper.Map<MarkResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Mark id: {id}");
            await _markRepo.DeleteAsync(id);
            // Commit is deferred
            return true;
        }

        /// <summary>
        /// Call this method after all create/update/delete operations to persist changes as a single transaction.
        /// </summary>
        public async Task<int> CommitAsync()
        {
            _logger.LogInformation("Committing all staged changes for Mark");
            return await _unitOfWork.Commit();
        }
    }
}