using Basket.API.Data.Interfaces;
using Basket.API.Models.Domain;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System.Linq;

namespace Basket.API.Data
{
    public class MongoDbDataContext : IMongoDbDataContext
    {
        internal readonly IMongoDatabase _mongoDatabase;

        public MongoDbDataContext(IMongoClient mongoClient, IConfiguration config)
        {
            // Set up MongoDB conventions
            var pack = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };
            ConventionRegistry.Register("EnumStringConvention", pack, t => true);

            var dbName = config.GetValue<string>("MongoDbName");
            if (!mongoClient.ListDatabaseNames().ToList().Contains(dbName)) throw new System.Exception($"{dbName} database does not exist!");

            _mongoDatabase = mongoClient.GetDatabase(dbName);
        }

        public IMongoCollection<Cart> BasketDbModel => _mongoDatabase.GetCollection<Cart>("Basket");

    }
}
