using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;

    public class ClientDocumentsDataRepository
    {
        private const int MAX_SAVE_ATTEMPTS = 3;
        private const int MAX_RETRIEVE_ATTEMPTS = 3;
        private const int SLEEP_INTERVAL = 250;

        private string _mongoConnectionString { get; set; } = "";
        private const string _mongoCultureSurveyDb = "clientdocuments";
        //private const string _mongoClientImagesTemplateCollection = "templates";
        private const string _mongoClientDocumentsCollection = "documents";

        public ClientDocumentsDataRepository(string mongoConnectionString)
        {
            _mongoConnectionString = mongoConnectionString;
        }

        public IMongoDatabase GetMongoDatabase(string databaseName)
        {
            MongoClient mongoClient = new MongoClient(_mongoConnectionString);
            IMongoDatabase mongoDaLoDb = mongoClient.GetDatabase(databaseName);
            return mongoDaLoDb;
        }

        public List<ClientDocuments> GetAllClientDocuments()
        {
            IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
            IMongoCollection<ClientDocuments> clientDocumentsColl = CSDb.GetCollection<ClientDocuments>(_mongoClientDocumentsCollection);
            List<ClientDocuments> clientDocuments = (from ci in clientDocumentsColl.AsQueryable() select ci).ToList();
            return clientDocuments;
        }

    /// <summary>
    ///     ''' Get the client documents for a given engagement
    ///     ''' </summary>
    ///     ''' <param name="clientId">Client id</param>
    ///     ''' <param name="engagementId">Engagement id</param>
    ///     ''' <returns>Client documents</returns>
    public ClientDocuments GetClientDocuments(int clientId, int engagementId)
        {
            IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
            IMongoCollection<ClientDocuments> clientDocumentsColl = CSDb.GetCollection<ClientDocuments>(_mongoClientDocumentsCollection);

            ClientDocuments clientDocuments = (from ci in clientDocumentsColl.AsQueryable()
                                         where ci.EngagementId == engagementId && ci.ClientId == clientId
                                         select ci).FirstOrDefault();

            return clientDocuments;
        }

        public void SaveClientDocuments(ClientDocuments documents)
        {
            int attempts = 0;

            ClientDocuments cd = GetClientDocuments(documents.ClientId, documents.EngagementId);

            if (cd != null)
            {
                documents.Id = cd.Id;
            }

            while ((attempts < MAX_SAVE_ATTEMPTS))
            {
                try
                {
                    IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
                    IMongoCollection<ClientDocuments> clientDocumentsColl = CSDb.GetCollection<ClientDocuments>(_mongoClientDocumentsCollection);
                    // Generate an object id if this is a new document.
                    if (documents.Id == ObjectId.Empty)
                        documents.Id = ObjectId.GenerateNewId();
                    FindOneAndReplaceOptions<ClientDocuments> replaceOptions = new FindOneAndReplaceOptions<ClientDocuments>();
                    replaceOptions.IsUpsert = true;
                    FilterDefinition<ClientDocuments> filter = Builders<ClientDocuments>.Filter.Eq<ObjectId>("_id", documents.Id);
                    clientDocumentsColl.FindOneAndReplace(filter, documents, replaceOptions);
                    // Successful save.
                    break;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error("CultureSurvey",ex);
                }
                attempts = attempts + 1;
                if (attempts < MAX_SAVE_ATTEMPTS)
                    System.Threading.Thread.Sleep(SLEEP_INTERVAL);
            }
        }
    }
