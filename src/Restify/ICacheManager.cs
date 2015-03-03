using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restify
{
    public interface ICacheManager
    {
        Task<TData> GetAsync<TData>(object primaryKey)
            where TData : new();

        Task<IEnumerable<TData>> QueryAsync<TData>(ISpecification specification)
            where TData : new();

        Task AddOrReplaceAllAsync<T>(IEnumerable<T> items);

        Task Purge<T>();

        Task RemoveAsync<TData>(object primaryKey);
    }
}