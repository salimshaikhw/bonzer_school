using System;
using System.Threading.Tasks;

namespace Application.Common.Helpers.FireAndForget
{
    public interface IFireAndForgetService
    {
        void Fire<T>(Action<T> bullet, Action<Exception> handler = null);
        void FireAsync<T>(Func<T, Task> bullet, Action<Exception> handler = null);
    }
}