using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        // Removed IGenericRepository and Repository<T> as EFGenericRepository is not used.
        Task<int> Commit();
        void Rollback();
    }
}
