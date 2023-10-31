using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Portal.Model;

namespace Portal.Misc
{
    public class ClientImagesDataRepository
    {
        private const int MAX_SAVE_ATTEMPTS = 3;
        private const int MAX_RETRIEVE_ATTEMPTS = 3;
        private const int SLEEP_INTERVAL = 250;

        private string _mongoConnectionString { get; set; } = "";
        private const string _mongoCultureSurveyDb = "clientimages";
        private const string _mongoClientImagesTemplateCollection = "templates";
        private const string _mongoClientImagesCollection = "images";

        public ClientImagesDataRepository(string mongoConnectionString)
        {
            _mongoConnectionString = mongoConnectionString;
        }

        public IMongoDatabase GetMongoDatabase(string databaseName)
        {
            MongoClient mongoClient = new MongoClient(_mongoConnectionString);
            IMongoDatabase mongoDaLoDb = mongoClient.GetDatabase(databaseName);
            return mongoDaLoDb;
        }

        /// <summary>
        ///     ''' Get the client images for a given engagement
        ///     ''' </summary>
        ///     ''' <param name="clientId">Client id</param>
        ///     ''' <param name="engagementId">Engagement id</param>
        ///     ''' <returns>Client images document</returns>
        public ClientImages GetClientImages(int clientId, int engagementId)
        {
            IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
            IMongoCollection<ClientImages> clientImagesColl = CSDb.GetCollection<ClientImages>(_mongoClientImagesCollection);

            ClientImages ClientImages = (from ci in clientImagesColl.AsQueryable()
                                         where ci.EngagementId == engagementId && ci.ClientId == clientId
                                         select ci).FirstOrDefault();

            return ClientImages;
        }

        public void SaveClientImages(ClientImages images)
        {
            int attempts = 0;

            ClientImages ci = GetClientImages(images.ClientId, images.EngagementId);

            if (ci != null)
            {
                images.Id = ci.Id;
            }

            while ((attempts < MAX_SAVE_ATTEMPTS))
            {
                try
                {
                    IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
                    IMongoCollection<ClientImages> clientImages = CSDb.GetCollection<ClientImages>(_mongoClientImagesCollection);
                    // Generate an object id if this is a new document.
                    if (images.Id == ObjectId.Empty)
                        images.Id = ObjectId.GenerateNewId();
                    FindOneAndReplaceOptions<ClientImages> replaceOptions = new FindOneAndReplaceOptions<ClientImages>();
                    replaceOptions.IsUpsert = true;
                    FilterDefinition<ClientImages> filter = Builders<ClientImages>.Filter.Eq<ObjectId>("_id", images.Id);
                    clientImages.FindOneAndReplace(filter, images, replaceOptions);
                    // Successful save.
                    break;
                }
                catch (Exception ex)
                {
                }
                attempts = attempts + 1;
                if (attempts < MAX_SAVE_ATTEMPTS)
                    System.Threading.Thread.Sleep(SLEEP_INTERVAL);
            }
        }
    }

}

