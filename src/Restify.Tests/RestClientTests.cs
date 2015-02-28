using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Restify.Tests
{
    public class RestClientTests
    {
        private readonly RestClient _client;
        
        public RestClientTests()
        {
            _client = new RestClient(cfg => cfg.DefaultBaseUrl("http://jsonplaceholder.typicode.com"));
        }
        
        [Fact]
        public async Task can_get_a_success_response()
        {
            var response = await _client.GetAsync("/posts");

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task can_get_a_not_found_response()
        {
            var response = await _client.GetAsync("http://google.com/not_found");

            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(404, response.StatusCode);
        }

        [Fact]
        public async Task can_get_a_deserialized_json_payload()
        {
            var response = await _client.GetAsync<List<Post>>("/posts");

            Assert.True(response.Data.Count == 100);
        }

        [Fact]
        public async Task can_get_a_string_payload()
        {
            var response = await _client.GetAsync<string>("/posts");

            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task can_get_a_stream_payload()
        {
            var response = await _client.GetAsync<Stream>("/posts");

            Assert.NotNull(response.Data);
            Assert.NotEqual(0, response.Data.Length);

            var data = await new StreamReader(response.Data).ReadToEndAsync();

            Assert.NotNull(data);

            response.Data.Dispose();
        }

        [Fact]
        public async Task can_get_a_binary_payload()
        {
            var response = await _client.GetAsync<byte[]>("http://i.imgur.com/QRlAg0b.png");

            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task can_get_a_deserialized_dynamic_payload()
        {
            var response = await _client.GetAsync<dynamic>("https://vimeo.com/api/v2/tonyzhou/info.json");

            Assert.Equal("Tony Zhou", response.Data.display_name.ToString());
        }

        [Fact]
        public async Task can_get_a_deserialized_dictionary_payload()
        {
            var response = await _client.GetAsync<IDictionary<string,object>>("https://vimeo.com/api/v2/tonyzhou/info.json");

            Assert.Equal("Tony Zhou", response.Data["display_name"]);
        }

        [Fact]
        public async Task can_post()
        {
            var response = await _client.PostAsync<Post>("/posts", new Post
                {
                    UserId = 9999,
                    Title = "SF Giants",
                    Body = "2010, 2012, 2014 and counting"
                });

            Assert.NotEqual(0, response.Data.Id);
        }

        [Fact]
        public async Task can_put()
        {
            var response = await _client.PutAsync<Post>("/posts/1", new Post
                {
                    Id = 1,
                    UserId = 1,
                    Title = "SF Giants",
                    Body = "2010, 2012, 2014 and counting"
                });

            Assert.True(response.IsSuccessStatusCode);
        }  
        
        [Fact]
        public async Task can_delete()
        {
            var response = await _client.DeleteAsync<Post>("/posts/1");

            Assert.True(response.IsSuccessStatusCode);
        }

        public class Post
        {
            public int UserId { get; set; }
            public int Id { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }
        }
    }
}