using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface ICacheService
    {
        bool Exists(string key);
        T Get<T>(string key);
        void Set<T>(string key, T value);
        void Remove(string key);
    }
}
