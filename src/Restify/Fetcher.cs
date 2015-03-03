using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restify
{
    public class Fetcher
    {
        private readonly IRestClient _restClient;
        private readonly IDataGateway _dataGateway;
        private readonly INetworkService _networkService;

        public Fetcher(IRestClient restClient, IDataGateway dataGateway, INetworkService networkService)
        {
            _restClient = restClient;
            _dataGateway = dataGateway;
            _networkService = networkService;
        }

        public async Task<IList<TData>> FetchAsync<TData, TSpecification>(TSpecification specification = null, FetchStrategy strategy = FetchStrategy.ApiThenCache)
            where TData : new()
            where TSpecification : class, ISpecification
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

        private async Task<IList<TData>> FetchFromApiAndAddToCacheAsync<TData>(ISpecification specification)
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

        private async Task<IList<TData>> FetchFromLocalCache<TData>(ISpecification specification)
            where TData : new()
        {
            return await _dataGateway.Fetch<TData>(specification);
        }

        private async Task SaveToLocalCache<T>(IEnumerable<T> items)
        {
            await _dataGateway.InsertOrReplaceAll(items);
        }
    }
}