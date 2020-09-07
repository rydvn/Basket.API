using Basket.API.Models.Requests;
using Basket.API.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Basket.API.Models.Domain;

namespace Basket.API.Services.Interfaces
{
    public interface IBasketService
    {
        Task<BaseServiceResponse<Cart>> AddItemToBasketAsync(AddItemToBasketRequest request);
        Task<BaseServiceResponse<Cart>> GetCustomerOpenCartAsync(int customerId);
        Task<BaseServiceResponse<Cart>> RemoveProductFromCart(int customerId, int productId);
        Task<BaseServiceResponse<bool>> SetProductStockToRedis(CachedProduct product);
    }
}
