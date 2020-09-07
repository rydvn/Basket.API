using Basket.API.Data.Interfaces;
using Basket.API.Models.Domain;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Basket.API.Data
{
    public class RedisCacheDataContext : IRedisCacheDataContext
    {
        private readonly IConfiguration _config;
        private readonly IDistributedCache _distributedCache;
        private readonly string _redisCacheEnviromentPrefix;

        public RedisCacheDataContext(IConfiguration config, IDistributedCache distributedCache)
        {
            _config = config;
            _distributedCache = distributedCache;
            _redisCacheEnviromentPrefix = _config.GetSection("RedisCache").GetValue<string>("Prefix");
        }

        public async Task<CachedProduct> GetProductAsync(int id)
        {
            CachedProduct response = null;

            var cacheData = await _distributedCache.GetStringAsync($"{_redisCacheEnviromentPrefix}{id}");
            if (!string.IsNullOrEmpty(cacheData))
            {
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<CachedProduct>(cacheData);
            }

            return response;
        }

        public async Task SetProductAsync(CachedProduct product)
        {
            await _distributedCache.SetStringAsync($"{_redisCacheEnviromentPrefix}{product.Id}", JsonConvert.SerializeObject(product));
        }
    }
}
