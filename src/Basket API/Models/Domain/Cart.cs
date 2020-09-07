using Basket.API.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Basket.API.Models.Domain
{
    public class Cart
    {
        public Cart()
        {
        }

        public Cart(int customerId)
        {
            CustomerId = customerId;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRequired]
        [BsonElement("CustomerId")]
        public int CustomerId { get; set; }

        [BsonElement("Status")]
        public CartStatus Status { get; set; }

        [BsonDateTimeOptions]
        [BsonElement("UpdatedDate")]
        public DateTime UpdatedDate { get; set; }

        [BsonDateTimeOptions]
        [BsonElement("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
