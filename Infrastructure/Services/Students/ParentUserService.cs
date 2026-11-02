using Application.Abstractions.Services.Students;
using Application.ViewModels.ParentUser;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Students
{
    /// <summary>
    /// Links a parent login to a child (parent_users). The link decides which children's fees, results and
    /// attendance a parent can see, so the rules are strict:
    ///   - the student exists;
    ///   - Username is the email of an existing Parent login of this school (Identity);
    ///   - the same parent is linked to a child only once (siblings each get their own link);
    ///   - passwords live in Identity: a PasswordHash sent by a client is never stored.
    /// </summary>
    public class ParentUserService : CrudService<ParentUser, ParentUserRequest, ParentUserResponse>, IParentUserService
    {
        private readonly IStudentRepository _students;
        private readonly IParentAccounts _parentAccounts;

        public ParentUserService(ILogger<ParentUserService> logger, IParentUserRepository repository, IMapper mapper,
            IStudentRepository students, IParentAccounts parentAccounts)
            : base(repository, mapper, logger)
        {
            _students = students;
            _parentAccounts = parentAccounts;
        }

        protected override string EntityName => "ParentUser";

        protected override Task ValidateCreateAsync(ParentUserRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ParentUser existing, ParentUserRequest request) => ValidateAsync(request, existing.Id);

        protected override void BeforeSave(ParentUser entity)
        {
            entity.Username = entity.Username?.Trim().ToLowerInvariant();
            entity.PasswordHash = null;
        }

        private async Task ValidateAsync(ParentUserRequest request, Guid? exceptId)
        {
            if (request.StudentId is not { } studentId || !await _students.IsExistAsync(s => s.Id == studentId))
                throw RuleViolation($"Student {request.StudentId} does not exist.");

            var username = request.Username?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(username) || !await _parentAccounts.IsParentLoginAsync(username))
                throw RuleViolation($"'{request.Username}' is not a parent login of this school. Create the parent's login first.");

            if (await Repository.IsExistAsync(p => p.StudentId == studentId && p.Username == username && p.Id != exceptId))
                throw Conflict("This parent is already linked to the student.");
        }
    }
}
