using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restify
{
    public interface IFetcher
    {
        Task<IEnumerable<TData>> FetchAsync<TData>(ISpecification specification, FetchStrategy strategy = FetchStrategy.ApiThenCache)
            where TData : new();
    }

    public class Fetcher : IFetcher
    {
        private readonly IRestClient _restClient;
        private readonly ICacheManager _cacheManager;
        private readonly INetworkService _networkService;

        public Fetcher(IRestClient restClient, ICacheManager cacheManager, INetworkService networkService)
        {
            _restClient = restClient;
            _cacheManager = cacheManager;
            _networkService = networkService;
        }

        public async Task<IEnumerable<TData>> FetchAsync<TData>(ISpecification specification, FetchStrategy strategy = FetchStrategy.ApiThenCache)
            where TData : new()
        {
            if (!_networkService.IsConnected)
            {
                strategy = FetchStrategy.CacheOnly;
            }

            try
            {
                var items = new List<TData>();

                if (strategy == FetchStrategy.ApiThenCache)
                {
                    var inbound = await FetchFromApiAndAddToCacheAsync<TData>(specification);
                    items.AddRange(inbound);
                }
                else
                {
                    var inbound = await FetchFromLocalCache<TData>(specification);
                    items.AddRange(inbound);
                }

                return items;
            }
            catch (Exception ex)
            {
                throw new RestifyException("An exception occurred while attempting to fetch data.", ex);
            }
        }

        private async Task<IEnumerable<TData>> FetchFromApiAndAddToCacheAsync<TData>(ISpecification specification)
            where TData : new()
        {
            var response = await FetchFromApi<TData>(specification);

            if (response.IsSuccessStatusCode)
            {
                await SaveToLocalCache(response.Data);
                return response.Data;
            }
            else
            {
                return await FetchFromLocalCache<TData>(specification);
            }
        }

        private async Task<RestResponse<List<TData>>> FetchFromApi<TData>(ISpecification specification) where TData : new()
        {
            var apiPath = specification.ApiPath();
            var response = await _restClient.GetAsync<List<TData>>(apiPath);
            return response;
        }

        private async Task<IEnumerable<TData>> FetchFromLocalCache<TData>(ISpecification specification)
            where TData : new()
        {
            return await _cacheManager.QueryAsync<TData>(specification);
        }

        private async Task SaveToLocalCache<T>(IEnumerable<T> items)
        {
            await _cacheManager.AddOrReplaceAllAsync(items);
        }
    }
}