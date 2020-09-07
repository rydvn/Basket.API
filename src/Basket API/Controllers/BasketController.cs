using System;
using System.Threading.Tasks;
using Basket.API.Models.Domain;
using Basket.API.Models.Requests;
using Basket.API.Models.Responses;
using Basket.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
    public class BasketController : ControllerBase
    {
        private readonly ILogger<BasketController> _logger;
        private readonly IBasketService _basketService;

        public BasketController(ILogger<BasketController> logger, IBasketService basketService)
        {
            _logger = logger;
            _basketService = basketService;
        }

        [HttpGet()]
        public string GetThisThingInHere()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        [HttpPost()]
        public async Task<IActionResult> Basket([FromBody]AddItemToBasketRequest request)
        {
            var response = new BaseServiceResponse<Cart>();
            if (request.Quantity <= 0)
            {
                response = await _basketService.RemoveProductFromCart(request.CustomerId, request.ProductId);
            }
            else
            {
                response = await _basketService.AddItemToBasketAsync(request);
            }
            
            if (response.HasError)
            {
                return BadRequest(response.Errors);
            }

            return Ok(response.Data);
        }

        [HttpGet()]
        public async Task<IActionResult> Basket(int customerId)
        {
            if (customerId <= 0)
            {
                return BadRequest();
            }

            var response = await _basketService.GetCustomerOpenCartAsync(customerId);

            if (response.HasError)
            {
                return BadRequest(response.Errors);
            }

            return Ok(response.Data);
        }

        [HttpDelete()]
        public async Task<IActionResult> Basket(int customerId, int productId)
        {
            if (customerId <= 0 && productId <= 0)
            {
                return BadRequest();
            }

            var response = await _basketService.RemoveProductFromCart(customerId, productId);

            if (response.HasError)
            {
                return BadRequest(response.Errors);
            }

            return Ok(response.Data);
        }

        [HttpPost()]
        public async Task<IActionResult> Product([FromBody]CachedProduct product)
        {
            var response = await _basketService.SetProductStockToRedis(product);

            if (response.HasError)
            {
                return BadRequest(response.Errors);
            }

            return Ok(response.Data);
        }
    }
}
