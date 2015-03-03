using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite.Net.Async;

namespace Restify
{
    public class DataGateway : IDataGateway
    {
        private readonly SQLiteAsyncConnection _conn;

        public DataGateway(SQLiteAsyncConnection conn)
        {
            _conn = conn;
        }

        public async Task<IList<TData>> Fetch<TData>(ISpecification specification)
            where TData : new()
        {
            var sql = specification.SqlQuery();
            var parameters = specification.SqlParameters();
            return await _conn.QueryAsync<TData>(sql, parameters);
        }

        public async Task InsertOrReplaceAll<T>(IEnumerable<T> items)
        {
            await _conn.InsertOrReplaceAllAsync(items);
        }
    }
}