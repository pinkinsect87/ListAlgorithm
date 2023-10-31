using System;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using CultureSurveyShared;
using System.Runtime.ConstrainedExecution;

public class BadgeDeliveryDataRepository
{
    private const int MAX_SAVE_ATTEMPTS = 3;
    private const int MAX_RETRIEVE_ATTEMPTS = 3;
    private const int SLEEP_INTERVAL = 250;

    private string _mongoConnectionString { get; set; } = "";
    private string _mongoCultureSurveyDb = "badgedelivery";
    private string _mongoBadgeDeliveryJobsCollection = "jobs";

    public BadgeDeliveryDataRepository(string mongoConnectionString)
    {
        _mongoConnectionString = mongoConnectionString;
    }

    public IMongoDatabase GetMongoDatabase(string databaseName)
    {
        MongoClient mongoClient = new MongoClient(_mongoConnectionString);
        IMongoDatabase mongoDaLoDb = mongoClient.GetDatabase(databaseName);
        return mongoDaLoDb;
    }
    public BadgeDeliveryJob AddJob(int clientId, List<ECRCountryCode> ECRCountryCodes, string emailAddressOfRecipient,string emailAddressOfOriginator, BadgeDeliveryJobType jobType)
    {
        BadgeDeliveryJob job = new BadgeDeliveryJob { ClientId = clientId, EmailAddressOfOriginator = emailAddressOfOriginator , EmailAddressOfRecipient = emailAddressOfRecipient , ECRCountryCodes = ECRCountryCodes , JobType = jobType };
        this.SaveJob(job);
        return job;
    }
    public BadgeDeliveryJob GetNextJob()
    {
        try
        {
            IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
            IMongoCollection<BadgeDeliveryJob> badgeDeliveryJobCollection = CSDb.GetCollection<BadgeDeliveryJob>(_mongoBadgeDeliveryJobsCollection);

            BadgeDeliveryJob nextJob = (from job in badgeDeliveryJobCollection.AsQueryable() where job.JobStatus == BadgeDeliveryJobStatus.Created 
                                        select job).OrderByDescending(d => d.CreatedDate).FirstOrDefault();
            return nextJob;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error("BadgeDeliveryJob GetNextJob failed with exception.", ex);
        }
        return null;
    }

    public BadgeDeliveryJob GetJob(string id)
    {
        try
        {
            IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
            IMongoCollection<BadgeDeliveryJob> badgeDeliveryJobCollection = CSDb.GetCollection<BadgeDeliveryJob>(_mongoBadgeDeliveryJobsCollection);

            BadgeDeliveryJob returnedJob = (from job in badgeDeliveryJobCollection.AsQueryable()
                                        where job.Id == ObjectId.Parse(id)
                                        select job).FirstOrDefault();
            return returnedJob;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error("BadgeDeliveryJob GetJob failed with exception.", ex);
        }
        return null;
    }

    public void SaveJob(BadgeDeliveryJob job)
    {
        try
        {
            IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
            IMongoCollection<BadgeDeliveryJob> badgeDeliveryJobCollection = CSDb.GetCollection<BadgeDeliveryJob>(_mongoBadgeDeliveryJobsCollection);
            if (job.Id == ObjectId.Empty)
                job.Id = ObjectId.GenerateNewId();
            FindOneAndReplaceOptions<BadgeDeliveryJob> replaceOptions = new FindOneAndReplaceOptions<BadgeDeliveryJob>();
            replaceOptions.IsUpsert = true;
            FilterDefinition<BadgeDeliveryJob> filter = Builders<BadgeDeliveryJob>.Filter.Eq<ObjectId>("_id", job.Id);
            badgeDeliveryJobCollection.FindOneAndReplace(filter, job, replaceOptions);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error("BadgeDeliveryJob SaveJob failed with exception.", ex);
        }
    }

}
