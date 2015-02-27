using System.Net.Http;

namespace Restify
{
    public class RestResponse
    {
        public RestResponse(HttpResponseMessage responseMessage)
        {
            StatusCode = (int)responseMessage.StatusCode;
            IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
        }

        public int StatusCode { get; private set; }
        public bool IsSuccessStatusCode { get; private set; }
    }
}