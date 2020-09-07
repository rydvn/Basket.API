using Basket.API.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Basket.API.Data
{
    public class HttpClientDataContext : IHttpClientDataContext
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _clientConfiguration;
        private readonly HttpClient _client;

        public HttpClientDataContext(IConfiguration configuration, HttpClient client)
        {
            _configuration = configuration;
            _client = client;
            _clientConfiguration = _configuration.GetSection("DummyHttpClient");
        }

        public async Task<HttpResponseMessage> GetPostCommentsAsync(int id)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(_client.BaseAddress, string.Format(_clientConfiguration.GetValue<string>("PostComments"), id)),
                Method = HttpMethod.Get,
            };

            return await _client.SendAsync(httpRequestMessage);
        }

        public async Task<HttpResponseMessage> DummyMethodAsync(dynamic request)
        {
            string payload = JsonSerializer.Serialize(request);

            var httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(_client.BaseAddress, _clientConfiguration.GetValue<string>("Method1ApiUrl")),
                Content = new StringContent(payload),
                Method = null // HttpMethod.Head / HttpMethod.Get / HttpMethod.Put / HttpMethod.Post / HttpMethod.Delete
            };

            return await _client.SendAsync(httpRequestMessage);
        }
    }
}
