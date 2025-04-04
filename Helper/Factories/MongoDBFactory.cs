// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Helper.Factories
{
    public interface IMongoDBFactory
    {
        IMongoClient GetClient();

        IMongoDatabase GetDataBase(string databasename);

        IMongoCollection<T> GetCollection<T>(string databasename, string collectionname);

        T GetDocumentById<T>(string databasename, string collectionname, string documentId);
    }

    public class MongoDBFactory : IMongoDBFactory, IDisposable
    {
        public MongoDBFactory(ISettings settings) //, ILogger<QueryFactory> logger)
        {
            if (!string.IsNullOrEmpty(settings.MongoDBConnectionString))
                mongoDBClient = new MongoClient(settings.MongoDBConnectionString);
            else
                mongoDBClient = null;
            //Logger = info => logger.LogDebug("SQL: {sql} {@parameters}", info.RawSql, info.NamedBindings);
        }

        private MongoClient mongoDBClient;

        public void Dispose()
        {
            if (mongoDBClient != null)
            {
                mongoDBClient.Cluster.Dispose();
                mongoDBClient = null;
            }
        }

        public IMongoClient GetClient()
        {
            return mongoDBClient;
        }

        public IMongoDatabase GetDataBase(string databasename)
        {
            return mongoDBClient.GetDatabase(databasename);
        }

        public IMongoCollection<T> GetCollection<T>(string databasename, string collectionname)
        {
            return mongoDBClient.GetDatabase(databasename).GetCollection<T>(collectionname);
        }

        public T GetDocumentById<T>(string databasename, string collectionname, string documentId)
        {
            var collection = mongoDBClient
                .GetDatabase(databasename)
                .GetCollection<T>(collectionname);
            var document = collection
                .Find(Builders<T>.Filter.Eq("_id", ObjectId.Parse(documentId)))
                .FirstOrDefault();

            return document;
        }
    }

    public class MongoDBDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
    }
}
