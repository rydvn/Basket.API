using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Basket.API.Models.Domain;

namespace Basket.API.Data.Interfaces
{
    public interface IRedisCacheDataContext
    {
        Task<CachedProduct> GetProductAsync(int id);

        Task SetProductAsync(CachedProduct product);
    }
}
