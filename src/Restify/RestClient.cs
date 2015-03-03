using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Restify
{
    public class RestClient : IRestClient
    {
        private Uri _defaultBaseUrl;
        private ProductInfoHeaderValue _userAgent;
        private Func<AuthenticationHeaderValue> _authenticationHeaderFactory;
        private readonly Dictionary<string, IEnumerable<string>> _headers = new Dictionary<string, IEnumerable<string>>();
        private ISerializer _serializer = new Serializer();

        public RestClient(Action<RestClientConfigurer> configure)
        {
            configure(new RestClientConfigurer(this));
        }

        public async Task<RestResponse> GetAsync(string route)
        {
            return await SendAsync(HttpMethod.Get, route);
        }

        public async Task<RestResponse<TData>> GetAsync<TData>(string route)
        {
            return await SendAsync<TData>(HttpMethod.Get, route);
        }

        public async Task<RestResponse<TData>> PostAsync<TData>(string route, object payload = null)
        {
            return await SendAsync<TData>(HttpMethod.Post, route, payload);
        }

        public async Task<RestResponse<TData>> PutAsync<TData>(string route, object payload = null)
        {
            return await SendAsync<TData>(HttpMethod.Put, route, payload);
        }

        public async Task<RestResponse> DeleteAsync<TData>(string route)
        {
            return await SendAsync(HttpMethod.Delete, route);
        }

        private async Task<RestResponse> SendAsync(HttpMethod method, string route)
        {
            using (var response = await SendAsync(method, route, null))
            {
                return new RestResponse(response);
            }
        }

        private async Task<RestResponse<TData>> SendAsync<TData>(HttpMethod method, string route, object payload = null)
        {
            if (typeof(TData) == typeof(Stream))
            {
                var response = await SendAsync(method, route, PrepareContent(payload));
                if (!response.IsSuccessStatusCode)
                {
                    return new RestResponse<TData>(response, default(TData));
                }
                var stream = new AdaptedStream(response, await response.Content.ReadAsStreamAsync());
                return new RestResponse<Stream>(response, stream) as RestResponse<TData>;
            }

            using (var response = await SendAsync(method, route, PrepareContent(payload)))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return new RestResponse<TData>(response, default(TData));
                }

                if (typeof(TData) == typeof(byte[]))
                {
                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    return new RestResponse<byte[]>(response, byteArray) as RestResponse<TData>;
                }
                if (typeof(TData) == typeof(string))
                {
                    var stringData = await response.Content.ReadAsStringAsync();
                    return new RestResponse<string>(response, stringData) as RestResponse<TData>;
                }

                var json = await response.Content.ReadAsStringAsync();

                var data = _serializer.Deserialize<TData>(json);

                return new RestResponse<TData>(response, data);
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, HttpContent content)
        {
            var fullyQualifiedUrl = GetFullyQualifiedUri(route);

            using (var client = BuildClient())
            {
                using (var request = new HttpRequestMessage(method, fullyQualifiedUrl))
                {
                    request.Content = content;

                    return await client.SendAsync(request);
                }
            }
        }

        private HttpContent PrepareContent(object payload = null)
        {
            if (payload == null)
            {
                return null;
            }

            if (payload is IDictionary<string, string>)
            {
                return new FormUrlEncodedContent((IDictionary<string, string>)payload);
            }

            if (payload is string)
            {
                return new StringContent(payload.ToString());
            }

            if (payload is byte[])
            {
                return new ByteArrayContent((byte[])payload);
            }

            if (payload is Stream)
            {
                return new StreamContent((Stream)payload);
            }

            return new JsonContent(_serializer, payload);
        }

        private Uri GetFullyQualifiedUri(string path)
        {
            Uri fullUri;
            if (Uri.TryCreate(path, UriKind.Absolute, out fullUri))
            {
                // an absolute url bypasses use of the default base url
                return fullUri;
            }

            // Using NancyFx conventions: Request relative paths start with prepended slashes.
            if (!path.StartsWith("/"))
            {
                throw new ArgumentException("path must begin with a '/' character");
            }

            // However, HttpClient does not want a trailing slash so we remove it here
            path = path.Substring(1);

            return new Uri(_defaultBaseUrl, path);
        }

        private HttpClient BuildClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (_userAgent != null)
            {
                client.DefaultRequestHeaders.UserAgent.Add(_userAgent);
            }
            if (_authenticationHeaderFactory != null)
            {
                client.DefaultRequestHeaders.Authorization = _authenticationHeaderFactory();
            }
            if (_headers.Any())
            {
                foreach (var header in _headers)
                {
                    if (header.Value.Count() == 1)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value.First());
                    }
                    else
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            }

            return client;
        }

        public class RestClientConfigurer
        {
            private readonly RestClient _client;

            protected internal RestClientConfigurer(RestClient client)
            {
                _client = client;
            }

            /// <summary>
            /// Configure the base url path for the rest client.
            /// </summary>
            /// <param name="baseUrl">Must be a valid Url with no trailing '/' character</param>
            public RestClientConfigurer DefaultBaseUrl(string baseUrl)
            {
                // Using NancyFx conventions: Base path cannnot contain a trailing slash. 
                if (baseUrl.EndsWith("/"))
                {
                    throw new ArgumentException("Url cannot end with a trailing slash '/' character");
                }

                // However, HttpClient wants a trailing slash so we add one here
                _client._defaultBaseUrl = new Uri(baseUrl + "/");
                return this;
            }

            /// <summary>
            /// Configure the UserAgent Http Header
            /// </summary>
            /// <param name="name"></param>
            /// <param name="version"></param>
            /// <returns></returns>
            public RestClientConfigurer UserAgent(string name, string version = null)
            {
                _client._userAgent = new ProductInfoHeaderValue(new ProductHeaderValue(name, version));
                return this;
            }

            /// <summary>
            /// Configure the Authorization Http Header
            /// </summary>
            /// <param name="scheme"></param>
            /// <param name="parameter"></param>
            /// <returns></returns>
            public RestClientConfigurer Authorization(string scheme, Func<string> parameter)
            {
                _client._authenticationHeaderFactory = () => new AuthenticationHeaderValue(scheme, parameter());
                return this;
            }

            /// <summary>
            /// Add a default request header
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public RestClientConfigurer Header(string key, string value)
            {
                _client._headers[key] = new[] { value };
                return this;
            }

            /// <summary>
            /// Add a default request header
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public RestClientConfigurer Header(string key, IEnumerable<string> value)
            {
                _client._headers[key] = value;
                return this;
            }

            /// <summary>
            /// Specify an alternate serializer to replace the default ISerializer implementation
            /// </summary>
            /// <param name="serializer"></param>
            /// <returns></returns>
            public RestClientConfigurer Serializer(ISerializer serializer)
            {
                _client._serializer = serializer;
                return this;
            }
        }

        private class AdaptedStream : Stream
        {
            private readonly HttpResponseMessage _message;
            private readonly Stream _stream;

            public AdaptedStream(HttpResponseMessage message, Stream stream)
            {
                _message = message;
                _stream = stream;
            }

            public override void Flush()
            {
                _stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
            }

            public override bool CanRead
            {
                get { return _stream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _stream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _stream.CanWrite; }
            }

            public override long Length
            {
                get { return _stream.Length; }
            }

            public override long Position
            {
                get { return _stream.Position; }
                set { _stream.Position = value; }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                _message.Dispose();
                _stream.Dispose();
            }
        }
    }
}