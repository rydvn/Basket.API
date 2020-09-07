using Basket.API.Models.Domain;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Data.Interfaces
{
    public interface IMongoDbDataContext
    {
        IMongoCollection<Cart> BasketDbModel { get; }
    }
}
