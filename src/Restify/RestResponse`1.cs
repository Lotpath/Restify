using System.Net.Http;

namespace Restify
{   
    public class RestResponse<T> : RestResponse
    {
        public RestResponse(HttpResponseMessage responseMessage, T data)
            : base(responseMessage)
        {
            Data = data;
        }

        public T Data { get; private set; }
    }
}