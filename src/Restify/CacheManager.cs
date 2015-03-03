using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite.Net.Async;

namespace Restify
{
    public class CacheManager : ICacheManager
    {
        private readonly SQLiteAsyncConnection _conn;

        public CacheManager(SQLiteAsyncConnection conn)
        {
            _conn = conn;
        }

        public async Task<TData> GetAsync<TData>(object primaryKey)
            where TData : new()
        {
            return await _conn.GetAsync<TData>(primaryKey);
        }

        public async Task<IEnumerable<TData>> QueryAsync<TData>(ISpecification specification)
            where TData : new()
        {
            var sql = specification.SqlQuery();
            var parameters = specification.SqlParameters();
            return await _conn.QueryAsync<TData>(sql, parameters);
        }

        public async Task AddOrReplaceAllAsync<TData>(IEnumerable<TData> items)
        {
            await _conn.InsertOrReplaceAllAsync(items);
        }

        public async Task Purge<TData>()
        {
            await _conn.DeleteAllAsync<TData>();
        }

        public async Task RemoveAsync<TData>(object primaryKey)
        {
            await _conn.DeleteAsync<TData>(primaryKey);
        }
    }
}