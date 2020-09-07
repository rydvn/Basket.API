using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Basket.API.Models.Domain
{
    public class CartItem
    {
        [BsonRequired]
        [BsonElement("ProductId")]
        public int ProductId { get; set; }

        [BsonElement("Quantity")]
        public int Quantity { get; set; }

        [BsonDateTimeOptions]
        [BsonElement("CreatedDate")]  
        public DateTime CreatedDate { get; set; }
    }
}