using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restify
{
    public interface IDataGateway
    {
        Task<IList<TData>> Fetch<TData>(ISpecification specification)
            where TData : new();

        Task InsertOrReplaceAll<T>(IEnumerable<T> items);
    }
}