using System.Threading.Tasks;

namespace Restify
{
    public interface IRestClient
    {
        Task<RestResponse> GetAsync(string route);
        Task<RestResponse<TData>> GetAsync<TData>(string route);
        Task<RestResponse<TData>> PostAsync<TData>(string route, object payload = null);
        Task<RestResponse<TData>> PutAsync<TData>(string route, object payload = null);
        Task<RestResponse> DeleteAsync<TData>(string route);
    }
}