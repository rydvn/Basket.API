using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Basket.API.Models.Domain
{
    public class CachedProduct
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Stock { get; set; }
    }
}