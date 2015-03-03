using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using Xunit;

namespace Restify.Tests
{
    public class FetcherTests
    {
        private readonly IRestClient _client;
        private readonly IDataGateway _gateway;
        private readonly INetworkService _networkService;
        private readonly List<Product> _products = new List<Product>();
        private readonly Fetcher _fetcher;

        public FetcherTests()
        {
            _client = A.Fake<IRestClient>();
            _gateway = A.Fake<IDataGateway>();
            _networkService = A.Fake<INetworkService>();
            _products.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "widget"
                });

            A.CallTo(() => _client.GetAsync<List<Product>>(A<string>.That.Contains("w")))
             .Returns(new RestResponse<List<Product>>(new HttpResponseMessage(), _products));

            A.CallTo(() => _client.GetAsync<List<Product>>(A<string>.That.Contains("x")))
             .Returns(new RestResponse<List<Product>>(new HttpResponseMessage
                 {
                     StatusCode = HttpStatusCode.BadRequest
                 }, new List<Product>()));

            A.CallTo(() => _client.GetAsync<List<Product>>(A<string>.That.Contains("*")))
                .Throws(new Exception("oh noes!"));

            _fetcher = new Fetcher(_client, _gateway, _networkService);
        }

        [Fact]
        public async Task when_disconnected()
        {
            A.CallTo(() => _networkService.IsConnected)
             .Returns(false);

            var response = await _fetcher
                                     .FetchAsync<Product, ProductSpecification>(
                                         new ProductSpecification
                                             {
                                                 NameStartsWith = "w"
                                             });

            A.CallTo(() => _networkService.IsConnected)
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => _gateway.Fetch<Product>(A<ISpecification>._))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public async Task when_connected_and_cache_only_fetch_strategy_is_requested()
        {
            A.CallTo(() => _networkService.IsConnected)
             .Returns(true);

            var response = await _fetcher
                                     .FetchAsync<Product, ProductSpecification>(
                                         new ProductSpecification
                                         {
                                             NameStartsWith = "w"
                                         }, FetchStrategy.CacheOnly);

            A.CallTo(() => _networkService.IsConnected)
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => _gateway.Fetch<Product>(A<ISpecification>._))
                .MustHaveHappened();
        }

        [Fact]
        public async Task when_connected_and_default_fetch_strategy_is_employed()
        {
            A.CallTo(() => _networkService.IsConnected)
             .Returns(true);

            var response = await _fetcher
                                     .FetchAsync<Product, ProductSpecification>(
                                         new ProductSpecification
                                         {
                                             NameStartsWith = "w"
                                         });

            A.CallTo(() => _networkService.IsConnected)
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => _gateway.Fetch<Product>(A<ISpecification>._))
                .MustNotHaveHappened();

            A.CallTo(() => _client.GetAsync<List<Product>>(A<string>._))
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => _gateway.InsertOrReplaceAll(A<IEnumerable<Product>>._))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public async Task when_connected_but_api_returns_error_status_code()
        {
            A.CallTo(() => _networkService.IsConnected)
             .Returns(true);

            var response = await _fetcher
                                     .FetchAsync<Product, ProductSpecification>(
                                         new ProductSpecification
                                         {
                                             NameStartsWith = "x"
                                         });

            A.CallTo(() => _networkService.IsConnected)
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => _client.GetAsync<List<Product>>(A<string>._))
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => _gateway.InsertOrReplaceAll(A<IEnumerable<Product>>._))
                .MustNotHaveHappened();

            A.CallTo(() => _gateway.Fetch<Product>(A<ISpecification>._))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public async Task when_connected_but_rest_client_throws()
        {
            A.CallTo(() => _networkService.IsConnected)
             .Returns(true);

            var ex = await AssertEx.RecordAsync(async () =>
                {
                    await _fetcher
                        .FetchAsync<Product, ProductSpecification>(
                            new ProductSpecification
                                {
                                    NameStartsWith = "*"
                                });
                });

            Assert.IsType<RestifyException>(ex);
        }

        [Fact]
        public async Task when_disconnected_and_data_gateway_throws()
        {
            A.CallTo(() => _networkService.IsConnected)
             .Returns(false);

            A.CallTo(() => _gateway.Fetch<Product>(A<ISpecification>._))
             .Throws(new Exception("oh noes!"));

            var ex = await AssertEx.RecordAsync(async () =>
            {
                await _fetcher
                    .FetchAsync<Product, ProductSpecification>(
                        new ProductSpecification
                        {
                            NameStartsWith = "w"
                        });
            });

            Assert.IsType<RestifyException>(ex);
        }

        public class Product
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class ProductSpecification : ISpecification
        {
            public string NameStartsWith { get; set; }

            public string ApiPath()
            {
                var sb = new StringBuilder();
                sb.Append("/products");
                sb.Append(ApiQueryString());
                return sb.ToString();
            }

            private string ApiQueryString()
            {
                if (!string.IsNullOrEmpty(NameStartsWith))
                {
                    return string.Format("?nameStartsWith={0}", NameStartsWith);
                }
                return null;
            }

            public string SqlQuery()
            {
                return string.Format("where name like '{0}%'", NameStartsWith);
            }

            public IList<object> SqlParameters()
            {
                return new[] { NameStartsWith };
            }
        }
    }

    public class AssertEx
    {
        public static async Task<Exception> RecordAsync(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        public static async Task ThrowsAsync<TException>(Func<Task> func)
        {
            var expected = typeof(TException);
            Type actual = null;
            try
            {
                await func();
            }
            catch (Exception e)
            {
                actual = e.GetType();
            }
            Assert.Equal(expected, actual);
        }
    }
}