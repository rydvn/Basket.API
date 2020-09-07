using Basket.API.Data.Interfaces;
using Basket.API.Models.Domain;
using Basket.API.Models.Enums;
using Basket.API.Models.Requests;
using Basket.API.Models.Responses;
using Basket.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Basket.API.Services
{
    public class BasketService : IBasketService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BasketService> _logger;
        private readonly IHttpClientDataContext _httpClientDataContext;
        private readonly IMongoDbDataContext _mongoDbDataContext;
        private readonly IRedisCacheDataContext _redisCacheDataContext;

        public BasketService(IConfiguration configuration, ILogger<BasketService> logger, IHttpClientDataContext httpClientDataContext, IMongoDbDataContext mongoDbDataContext, IRedisCacheDataContext redisCacheDataContext)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientDataContext = httpClientDataContext;
            _mongoDbDataContext = mongoDbDataContext;
            _redisCacheDataContext = redisCacheDataContext;
        }

        public async Task<BaseServiceResponse<Cart>> AddItemToBasketAsync(AddItemToBasketRequest request)
        {
            var response = new BaseServiceResponse<Cart>();

            try
            {
                var customerBasket = await GetCustomerOpenCartAsync(request.CustomerId);
                if(customerBasket.HasError)
                {
                    response.Errors.AddRange(customerBasket.Errors);
                    return response;
                }

                var product = await _redisCacheDataContext.GetProductAsync(request.ProductId);
                if (request.Quantity > product.Stock)
                {
                    response.Errors.Add($"There is no enough stock you want, only {product.Stock} left");
                    return response;
                }

                if (customerBasket.Data == null)
                {
                    var basket = new Cart
                    {
                        CustomerId = request.CustomerId,
                        Status = CartStatus.Open,
                        CreatedDate = DateTime.UtcNow
                    };

                    var basketItem = new CartItem
                    {
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        CreatedDate = DateTime.UtcNow
                    };
                    basket.Items.Add(basketItem);

                    await _mongoDbDataContext.BasketDbModel.InsertOneAsync(basket, new InsertOneOptions { BypassDocumentValidation = true });

                    response.Data = await _mongoDbDataContext.BasketDbModel.FindSync(x => x.CustomerId == basket.CustomerId && x.Status == CartStatus.Open).SingleAsync();
                }
                else
                {
                    var existingProduct = customerBasket.Data.Items.FirstOrDefault(i=>i.ProductId == request.ProductId);
                    if (existingProduct != null)
                    {
                        existingProduct.Quantity = request.Quantity;
                    }
                    else
                    {
                        var basketItem = new CartItem
                        {
                            ProductId = request.ProductId,
                            Quantity = request.Quantity,
                            CreatedDate = DateTime.UtcNow
                        };
                        customerBasket.Data.Items.Add(basketItem);
                    }
                    
                    var filterBuilder = Builders<Cart>.Filter;
                    FilterDefinition<Cart> filter = filterBuilder.Eq(x => x.CustomerId, customerBasket.Data.CustomerId) & filterBuilder.Eq(x=>x.Status, CartStatus.Open);

                    var updateBuilder = Builders<Cart>.Update;
                    UpdateDefinition<Cart> update = updateBuilder.Set(x => x.UpdatedDate, DateTime.Now)
                                                                  .Set(x => x.Items, customerBasket.Data.Items);

                    response.Data = await _mongoDbDataContext.BasketDbModel.FindOneAndUpdateAsync(filter, update
                                                                , new FindOneAndUpdateOptions<Cart, Cart> { IsUpsert = false, ReturnDocument = ReturnDocument.After });
                }
            }
            catch (Exception ex)
            {
                response.Errors.Add(ex.Message);
                _logger.LogError(ex, $"Something bad happened!!! CustomerId: {request.CustomerId} ProductId: {request.ProductId}");
            }

            return response;
        }

        public async Task<BaseServiceResponse<Cart>> GetCustomerOpenCartAsync(int customerId)
        {
            var response = new BaseServiceResponse<Cart>();

            try
            {
                var basketStatus = new List<CartStatus> { CartStatus.Open };

                var filterBuilder = Builders<Cart>.Filter;
                FilterDefinition<Cart> filter = filterBuilder.Empty;

                filter &= filterBuilder.Eq(x => x.CustomerId, customerId);
                filter &= filterBuilder.In(x => x.Status, basketStatus);

                var customerBasket = await _mongoDbDataContext.BasketDbModel.FindAsync<Cart>(filter);

                response.Data = customerBasket.FirstOrDefault();
            }
            catch (Exception ex)
            {
                var log = $"GetCustomerOpenBasketAsync error occured. Error: {ex.Message}";
                _logger.LogError(log);
                response.Errors.Add(log);
            }

            return response;
        }

        public async Task<BaseServiceResponse<Cart>> RemoveProductFromCart(int customerId, int productId)
        {
            var response = new BaseServiceResponse<Cart>();

            try
            {
                var customerBasket = await GetCustomerOpenCartAsync(customerId);
                if(customerBasket.HasError)
                {
                    response.Errors.AddRange(customerBasket.Errors);
                    return response;
                }

                var removedItem = customerBasket.Data.Items.FirstOrDefault(i=>i.ProductId == productId);
                if (removedItem == null)
                {
                    response.Data = customerBasket.Data;
                    return customerBasket;
                }

                customerBasket.Data.Items.Remove(removedItem);

                var filterBuilder = Builders<Cart>.Filter;
                    FilterDefinition<Cart> filter = filterBuilder.Eq(x => x.CustomerId, customerBasket.Data.CustomerId) & filterBuilder.Eq(x=>x.Status, CartStatus.Open);

                    var updateBuilder = Builders<Cart>.Update;
                    UpdateDefinition<Cart> update = updateBuilder.Set(x => x.UpdatedDate, DateTime.Now)
                                                                  .Set(x => x.Items, customerBasket.Data.Items);

                    response.Data = await _mongoDbDataContext.BasketDbModel.FindOneAndUpdateAsync(filter, update
                                                                , new FindOneAndUpdateOptions<Cart, Cart> { IsUpsert = false, ReturnDocument = ReturnDocument.After });

            }
            catch (Exception ex)
            {
                var log = $"GetCustomerOpenBasketAsync error occured. Error: {ex.Message}";
                _logger.LogError(log);
                response.Errors.Add(log);
            }

            return response;
        }

        public async Task<BaseServiceResponse<bool>> SetProductStockToRedis(CachedProduct product)
        {
            var response = new BaseServiceResponse<bool>();

            try
            {
                await _redisCacheDataContext.SetProductAsync(product);
                response.Data = true;
            }
            catch (Exception ex)
            {
                var log = $"GetCustomerOpenBasketAsync error occured. Error: {ex.Message}";
                _logger.LogError(log);
                response.Errors.Add(log);
                response.Data = false;
            }

            return response;
        }
    }
}
