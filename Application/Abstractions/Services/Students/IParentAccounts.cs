using System.Threading.Tasks;

namespace Application.Abstractions.Services.Students
{
    /// <summary>Parent logins (ASP.NET Identity, catalog database) as seen from the current tenant.</summary>
    public interface IParentAccounts
    {
        /// <summary>True when <paramref name="email"/> is a login of the current tenant holding the Parent role.</summary>
        Task<bool> IsParentLoginAsync(string email);
    }
}
