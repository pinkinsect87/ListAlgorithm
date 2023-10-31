using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Microsoft.WindowsAzure.Storage;

public class CultureSurveyDataRepository
{
    private const int MAX_SAVE_ATTEMPTS = 3;
    private const int MAX_RETRIEVE_ATTEMPTS = 3;
    private const int SLEEP_INTERVAL = 250;
    private const int LEASE_LENGTH_SECONDS = 59;

    private string _mongoConnectionString { get; set; } = "";
    private string _mongoLockAzureStorageConnectionString { get; set; } = "";
    private string _mongoCultureSurveyDb = "culturesurveys";
    private string _mongoCultureSurveyTemplateCollection = "templates";
    private string _mongoCultureSurveyCollection = "surveys";
    private string _mongoCultureSurveyAuditCollection = "surveys_audit ";
    public string _mongoLockAzureStorageContainer = "orchestratorlock";

    public CultureSurveyDataRepository(string mongoConnectionString, string mongoLockAzureStorageConnectionString)
    {
        _mongoConnectionString = mongoConnectionString;
        _mongoLockAzureStorageConnectionString = mongoLockAzureStorageConnectionString;
    }

    public CultureSurveyDataRepository(string mongoConnectionString)
    {
        _mongoConnectionString = mongoConnectionString;
    }

    public CultureSurveyDataRepository(string mongoConnectionString, string databaseName, string templateCollectionName, string surveyCollectionName)
    {
        _mongoConnectionString = mongoConnectionString;
        _mongoCultureSurveyDb = databaseName;
        _mongoCultureSurveyTemplateCollection = templateCollectionName;
        _mongoCultureSurveyCollection = surveyCollectionName;
    }

    public IMongoDatabase GetMongoDatabase(string databaseName)
    {
        MongoClient mongoClient = new MongoClient(_mongoConnectionString);
        IMongoDatabase mongoDaLoDb = mongoClient.GetDatabase(databaseName);
        return mongoDaLoDb;
    }

    /// <summary>
    ///     ''' Get a culture survey template of the given type and version
    ///     ''' </summary>
    ///     ''' <param name="surveyType">Type of template (CA, CB, or...)</param>
    ///     ''' <param name="templateVersion">Version number of template</param>
    ///     ''' <returns></returns>
    public CultureSurveyTemplate GetCultureSurveyTemplate(string surveyType, string templateVersion)
    {
        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyTemplate> cultureSurveyTemplates = CSDb.GetCollection<CultureSurveyTemplate>(_mongoCultureSurveyTemplateCollection);

        CultureSurveyTemplate cultureSurveyTemplate = (from cst in cultureSurveyTemplates.AsQueryable()
                                                       where cst.SurveyType == surveyType & cst.TemplateVersion == templateVersion
                                                       select cst).FirstOrDefault();

        return cultureSurveyTemplate;
    }

    /// <summary>
    ///     ''' Update a new or existing culture survey template
    ///     ''' </summary>
    ///     ''' <param name="cst">Culture survey template to be written into Mongo</param>
    public void UpdateCultureSurveyTemplate(CultureSurveyTemplate cst)
    {
        int attempts = 0;

        cst.SetLastModifiedDate();

        while ((attempts < MAX_SAVE_ATTEMPTS))
        {
            try
            {
                IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
                IMongoCollection<CultureSurveyTemplate> cultureSurveyTemplates = CSDb.GetCollection<CultureSurveyTemplate>(_mongoCultureSurveyTemplateCollection);
                // Generate an object id if this is a new document.
                if (cst.Id == ObjectId.Empty)
                    cst.Id = ObjectId.GenerateNewId();
                FindOneAndReplaceOptions<CultureSurveyTemplate> replaceOptions = new FindOneAndReplaceOptions<CultureSurveyTemplate>();
                replaceOptions.IsUpsert = true;
                FilterDefinition<CultureSurveyTemplate> filter = Builders<CultureSurveyTemplate>.Filter.Eq<ObjectId>("_id", cst.Id);
                cultureSurveyTemplates.FindOneAndReplace(filter, cst, replaceOptions);
                // Successful save.
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            attempts = attempts + 1;
            if (attempts < MAX_SAVE_ATTEMPTS)
                System.Threading.Thread.Sleep(SLEEP_INTERVAL);
        }
    }



    /// <summary>
    ///     ''' Get all CB culture surveys associated with a single EID
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetAllCBCultureSurveysEx(int engagementId)
    {
        IMongoDatabase CSDb;
        List<CultureSurveyDTO> cultureSurveys = null;

        try
        {
            CSDb = GetMongoDatabase(_mongoCultureSurveyDb);

            IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

            cultureSurveys = (from csr in cultureSurveyCollection.AsQueryable()
                              where csr.EngagementId == engagementId &&
                                            csr.SurveyType == "CB"
                              select csr).ToList();
        }
        catch (Exception ex)
        {
            string error = ex.Message;
        }

        return cultureSurveys;
    }

    /// <summary>
    /// <summary>
    ///     ''' Get the culture survey with responses for a given id
    ///     ''' </summary>
    ///     ''' <param name="Id"> id</param>
    ///     ''' <param name="surveyType">Survey type (CA, CB)</param>
    ///     ''' <returns>Culture survey document with responses</returns>
    public CultureSurveyDTO GetCultureSurveyById(string id)
    {
        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurvey = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

        CultureSurveyDTO CultureSurvey = (from csr in cultureSurvey.AsQueryable()
                                          where csr.Id == ObjectId.Parse(id)
                                          select csr).FirstOrDefault();

        return CultureSurvey;
    }

    /// <summary>
    ///     ''' Create a new CultureSurvey bypassing validation checks
    ///     ''' </summary>
    ///     ''' <param name="_clientId">Client Id</param>
    ///     ''' <param name="_engagementId">Engagement id</param>
    ///     ''' <param name="_surveyType">SurveyType</param>
    ///     ''' <param name="_templateVersion">TempateVersion</param>
    ///     ''' <returns></returns>
    public CreateCultureSurveyResult CreateCultureSurveyEx(int clientId, int engagementId, string surveyType, string templateVersion, string cultureSurveyBaseURL, List<string> countries)
    {
        CultureSurveyDTO cultureSurvey = new CultureSurveyDTO(clientId, engagementId, surveyType, templateVersion, countries);
        CreateCultureSurveyResult createCultureSurveyResult = new CreateCultureSurveyResult();
        createCultureSurveyResult.ErrorOccurred = true;
        createCultureSurveyResult.ErrorMessage = "An general error occurred while creating the culture survey.";
        createCultureSurveyResult.SingleSignOnUrl = "";

        try
        {

            if (GetCultureSurveyTemplate(surveyType, templateVersion) == null)
            {
                createCultureSurveyResult.ErrorOccurred = true;
                createCultureSurveyResult.ErrorMessage = "The specified SurveyType and TemplateVersion could not be found.";
                return createCultureSurveyResult;
            }

            if (cultureSurvey.Id == ObjectId.Empty)
                cultureSurvey.Id = ObjectId.GenerateNewId();

            cultureSurvey.SetLastModifiedDate();

            int attempts = 0;
            bool success = false;

            while ((attempts < MAX_SAVE_ATTEMPTS))
            {
                try
                {
                    IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
                    IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

                    FindOneAndReplaceOptions<CultureSurveyDTO> replaceOptions = new FindOneAndReplaceOptions<CultureSurveyDTO>();
                    replaceOptions.IsUpsert = true;

                    FilterDefinition<CultureSurveyDTO> filter = Builders<CultureSurveyDTO>.Filter.Eq<ObjectId>("_id", cultureSurvey.Id);
                    cultureSurveyCollection.FindOneAndReplace(filter, cultureSurvey, replaceOptions);
                    // Successful save.
                    success = true;
                    break;
                }
                catch
                {
                }
                attempts = attempts + 1;
                if (attempts < MAX_SAVE_ATTEMPTS)
                    System.Threading.Thread.Sleep(SLEEP_INTERVAL);
            }

            if (success)
            {
                createCultureSurveyResult.ErrorOccurred = false;
                createCultureSurveyResult.ErrorMessage = "";
                createCultureSurveyResult.SourceSystemId = cultureSurvey.Id.ToString();
                DateTime oneYearFromNow = DateTime.UtcNow.AddDays(365);
                createCultureSurveyResult.CultureAuditDueDate = oneYearFromNow.ToShortDateString();
                createCultureSurveyResult.SingleSignOnUrl = cultureSurveyBaseURL + cultureSurvey.Id;
            }
            else
            {
                createCultureSurveyResult.ErrorOccurred = true;
                createCultureSurveyResult.ErrorMessage = "Failed to create the culture survey.";
            }
        }
        catch
        {
            createCultureSurveyResult.ErrorOccurred = true;
            createCultureSurveyResult.ErrorMessage = "An unhandled error occurred while creating the culture survey.";
        }

        return createCultureSurveyResult;
    }

    /// <summary>
    ///     ''' Create a new CultureSurvey
    ///     ''' </summary>
    ///     ''' <param name="_clientId">Client Id</param>
    ///     ''' <param name="_engagementId">Engagement id</param>
    ///     ''' <param name="_surveyType">SurveyType</param>
    ///     ''' <param name="_templateVersion">TempateVersion</param>
    ///     ''' <returns></returns>
    public CreateCultureSurveyResult CreateCultureSurvey(int clientId, int engagementId, string surveyType, string templateVersion, string cultureSurveyBaseURL, List<string> countries)
    {
        CultureSurveyDTO cultureSurvey = new CultureSurveyDTO(clientId, engagementId, surveyType, templateVersion, countries);
        CreateCultureSurveyResult createCultureSurveyResult = new CreateCultureSurveyResult();
        createCultureSurveyResult.ErrorOccurred = true;
        createCultureSurveyResult.ErrorMessage = "An general error occurred while creating the culture survey.";
        createCultureSurveyResult.SingleSignOnUrl = "";

        try
        {

            CultureSurveyDTO existingCultureSurvey = GetCultureSurvey(engagementId);

            if (existingCultureSurvey != null)
            {
                if (existingCultureSurvey.ClientId != clientId)
                {
                    createCultureSurveyResult.ErrorOccurred = true;
                    createCultureSurveyResult.ErrorMessage = "A culture survey already exists for this engagementId And it's associated with a different clientId.";
                    return createCultureSurveyResult;
                }
                if (existingCultureSurvey.SurveyType == surveyType)
                {
                    createCultureSurveyResult.ErrorOccurred = true;
                    createCultureSurveyResult.ErrorMessage = "A culture survey already exists for this clientId/engagementId/surveyType.";
                    return createCultureSurveyResult;
                }
            }

            if (GetCultureSurveyTemplate(surveyType, templateVersion) == null)
            {
                createCultureSurveyResult.ErrorOccurred = true;
                createCultureSurveyResult.ErrorMessage = "The specified SurveyType and TemplateVersion could not be found.";
                return createCultureSurveyResult;
            }

            if (cultureSurvey.Id == ObjectId.Empty)
                cultureSurvey.Id = ObjectId.GenerateNewId();

            cultureSurvey.SetLastModifiedDate();

            int attempts = 0;
            bool success = false;

            while ((attempts < MAX_SAVE_ATTEMPTS))
            {
                try
                {
                    IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
                    IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

                    FindOneAndReplaceOptions<CultureSurveyDTO> replaceOptions = new FindOneAndReplaceOptions<CultureSurveyDTO>();
                    replaceOptions.IsUpsert = true;

                    FilterDefinition<CultureSurveyDTO> filter = Builders<CultureSurveyDTO>.Filter.Eq<ObjectId>("_id", cultureSurvey.Id);
                    cultureSurveyCollection.FindOneAndReplace(filter, cultureSurvey, replaceOptions);
                    // Successful save.
                    success = true;
                    break;
                }
                catch
                {
                }
                attempts = attempts + 1;
                if (attempts < MAX_SAVE_ATTEMPTS)
                    System.Threading.Thread.Sleep(SLEEP_INTERVAL);
            }

            if (success)
            {
                createCultureSurveyResult.ErrorOccurred = false;
                createCultureSurveyResult.ErrorMessage = "";
                createCultureSurveyResult.SourceSystemId = cultureSurvey.Id.ToString();
                DateTime oneYearFromNow = DateTime.UtcNow.AddDays(365);
                createCultureSurveyResult.CultureAuditDueDate = oneYearFromNow.ToShortDateString();
                createCultureSurveyResult.SingleSignOnUrl = cultureSurveyBaseURL + cultureSurvey.Id;
            }
            else
            {
                createCultureSurveyResult.ErrorOccurred = true;
                createCultureSurveyResult.ErrorMessage = "Failed to create the culture survey.";
            }
        }
        catch
        {
            createCultureSurveyResult.ErrorOccurred = true;
            createCultureSurveyResult.ErrorMessage = "An unhandled error occurred while creating the culture survey.";
        }

        return createCultureSurveyResult;
    }

    /// <summary>
    ///     ''' Get all culture surveys
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetAllCultureSurveys()
    {
        IMongoDatabase CSDb;
        List<CultureSurveyDTO> cultureSurveys = null;

        try
        {
            CSDb = GetMongoDatabase(_mongoCultureSurveyDb);

            IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

            cultureSurveys = (from csr in cultureSurveyCollection.AsQueryable() select csr).ToList();
        }
        catch (Exception ex)
        {
            string error = ex.Message;
        }

        return cultureSurveys;
    }

    /// <summary>
    ///     ''' Get all culture surveys
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetCultureSurveysOfType(string cultureSurveyType)
    {
        IMongoDatabase CSDb;
        List<CultureSurveyDTO> cultureSurveys = null;

        try
        {
            CSDb = GetMongoDatabase(_mongoCultureSurveyDb);

            IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

            cultureSurveys = (from csr in cultureSurveyCollection.AsQueryable() where csr.SurveyType == cultureSurveyType select csr).ToList();
        }
        catch (Exception ex)
        {
            string error = ex.Message;
        }

        return cultureSurveys;
    }
    /// <summary>
    ///     ''' Get all culture surveys of a specific type and version
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetCultureSurveysOfTypeAndVersion(string cultureSurveyType, string cultureSurveyTemplateVersion)
    {
        IMongoDatabase CSDb;
        List<CultureSurveyDTO> cultureSurveys = null;

        try
        {
            CSDb = GetMongoDatabase(_mongoCultureSurveyDb);

            IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

            cultureSurveys = (from csr in cultureSurveyCollection.AsQueryable()
                              where csr.SurveyType == cultureSurveyType && csr.TemplateVersion == cultureSurveyTemplateVersion
                              select csr).ToList();
        }
        catch (Exception ex)
        {
            string error = ex.Message;
        }

        return cultureSurveys;
    }


    /// <summary>
    ///     ''' Get all Incomplete CB culture surveys
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetAllInCompleteCBCultureSurveys()
    {
        List<CultureSurveyDTO> surveys = GetCultureSurveysOfType("CB");
        return (from csr in surveys where (csr.SurveyState == SurveyStatus.Initial || csr.SurveyState == SurveyStatus.InProgress) select csr).ToList();
    }

    /// <summary>
    ///     ''' Get all Incomplete CB culture surveys
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetAllInCompleteCACultureSurveys()
    {
        List<CultureSurveyDTO> surveys = GetCultureSurveysOfType("CA");
        return (from csr in surveys where (csr.SurveyState == SurveyStatus.Initial || csr.SurveyState == SurveyStatus.InProgress) select csr).ToList();
    }

    /// <summary>
    ///     ''' Get all CB culture surveys
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetAllCBCultureSurveys()
    {
        return GetCultureSurveysOfType("CB");
    }

    /// <summary>
    ///     ''' Get all CA culture surveys
    ///     ''' </summary>
    ///     ''' <returns></returns>
    public List<CultureSurveyDTO> GetAllCACultureSurveys()
    {
        return GetCultureSurveysOfType("CA");
    }

    /// <summary>
    /// <summary>
    ///     ''' Get the culture survey with responses for a given engagement
    ///     ''' </summary>
    ///     ''' <param name="engagementId">Engagement id</param>
    ///     ''' <param name="surveyType">Survey type (CA, CB)</param>
    ///     ''' <returns>Culture survey document with responses</returns>
    public CultureSurveyDTO GetCultureSurvey(int engagementId, string surveyType)
    {
        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurvey = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

        CultureSurveyDTO CultureSurvey = (from csr in cultureSurvey.AsQueryable()
                                          where csr.EngagementId == engagementId && csr.SurveyType == surveyType
                                          select csr).FirstOrDefault();

        return CultureSurvey;
    }

    /// <summary>
    ///     ''' Get the culture survey with responses for a given CultureSurvey Id
    ///     ''' </summary>
    ///     ''' <param name="CultureSurveyId">Object Id</param>
    ///     ''' <returns></returns>
    public CultureSurveyDTO GetCultureSurvey(int engagementId)
    {

        IMongoDatabase CSDb;
        CultureSurveyDTO cultureSurvey = null;

        try
        {
            CSDb = GetMongoDatabase(_mongoCultureSurveyDb);

            IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

            cultureSurvey = (from csr in cultureSurveyCollection.AsQueryable()
                             where csr.EngagementId == engagementId
                             select csr).FirstOrDefault();

        }
        catch (Exception ex)
        {
            string error = ex.Message;
        }

        return cultureSurvey;
    }

    /// <summary>
    ///     ''' Get the culture survey for a given CultureSurvey Id
    ///     ''' </summary>
    ///     ''' <param name="engagementId">Engagement id</param>
    ///     ''' <returns></returns>
    public CultureSurveyDTO GetCultureSurvey(ObjectId CultureSurveyId)
    {
        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurvey = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

        CultureSurveyDTO CultureSurvey = (from csr in cultureSurvey.AsQueryable()
                                          where csr.Id == CultureSurveyId
                                          select csr).FirstOrDefault();

        return CultureSurvey;
    }

    /// <summary>
    ///     ''' Get the culture survey for a given CultureSurvey Id
    ///     ''' </summary>
    ///     ''' <param name="engagementId">Engagement id</param>
    ///     ''' <returns></returns>
    public CultureSurveyDTO GetCultureSurveyDTO(ObjectId CultureSurveyId)
    {
        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurvey = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

        CultureSurveyDTO CultureSurvey = (from csr in cultureSurvey.AsQueryable()
                                          where csr.Id == CultureSurveyId
                                          select csr).FirstOrDefault();

        return CultureSurvey;
    }

    /// <summary>
    ///     ''' Get the culture survey for a given CultureSurvey Id
    ///     ''' </summary>
    ///     ''' <param name="engagementId">Engagement id</param>
    ///     ''' <returns></returns>
    public async Task<CultureSurveyDTO> GetWriteableCultureSurvey(ObjectId CultureSurveyId)
    {
        // spin until we can get a lock
        string leaseId = "";
        while (leaseId == "")
        {
            leaseId = await AcquireBlobLease(CultureSurveyId, LEASE_LENGTH_SECONDS);
            if (leaseId == "")
                System.Threading.Thread.Sleep(250);  // if we didn't get the lease, slight pause, then try again
        } // global wait until we can get a lease


        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurvey = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

        CultureSurveyDTO CultureSurvey = GetCultureSurvey(CultureSurveyId);

        if (CultureSurvey != null)
            CultureSurvey.LockLeaseId = leaseId;

        return CultureSurvey;
    }

    /// <summary>
    ///     ''' Get the culture survey for a given CultureSurvey Id
    ///     ''' </summary>
    ///     ''' <param name="engagementId">Engagement id</param>
    ///     ''' <returns></returns>
    public void SaveCultureSurvey(CultureSurveyDTO cultureSurvey)
    {
        // make sure this is a readwrite object
        bool isNewBundle = false;

        if (cultureSurvey.LockLeaseId == "" & cultureSurvey.Id.ToString() != "000000000000000000000000")
            // has no lease, and has objectid
            throw new Exception("This Is a read-only bundle And cannot be saved In the repository. Use RetrieveReadWriteEngagementControlRecordBundle instead.");
        else if (cultureSurvey.LockLeaseId == "" & cultureSurvey.Id.ToString() == "000000000000000000000000")
            // has no lease, and has no objectid
            isNewBundle = true;
        else if (cultureSurvey.LockLeaseId != "" & cultureSurvey.Id.ToString() != "000000000000000000000000")
            // has a lease, and has an objectid
            isNewBundle = false;
        else
            throw new Exception("Unexpected combination Of lease And objectid found");

        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyCollection);

        cultureSurvey.SetLastModifiedDate();

        int attempts = 0;
        while ((attempts < MAX_SAVE_ATTEMPTS))
        {
            try
            {
                FilterDefinition<CultureSurveyDTO> filter = Builders<CultureSurveyDTO>.Filter.Eq<ObjectId>("_id", cultureSurvey.Id);
                cultureSurveyCollection.FindOneAndReplace(filter, cultureSurvey);
                // Successful save.
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            attempts = attempts + 1;
            if (attempts < MAX_SAVE_ATTEMPTS)
                System.Threading.Thread.Sleep(SLEEP_INTERVAL);
        }

        if (!isNewBundle)
        {
            try
            {
                ReleaseBlobLease(cultureSurvey.Id, cultureSurvey.LockLeaseId);
            }
            catch (Exception ex)
            {
            }
        }
    }

    public async Task<string> AcquireBlobLease(ObjectId CultureSurveyId, int leaseLengthSeconds)
    {
        try
        {
            // get a reference to the container where the locks are stored
            var azureStorageMongoLockAccount = CloudStorageAccount.Parse(_mongoLockAzureStorageConnectionString);
            var azureStorageMongoLockClient = azureStorageMongoLockAccount.CreateCloudBlobClient();
            var azureStorageMongoLockOrchestratorContainer = azureStorageMongoLockClient.GetContainerReference(_mongoLockAzureStorageContainer);

            //if (!azureStorageMongoLockOrchestratorContainer.Exists)
            //    azureStorageMongoLockOrchestratorContainer.CreateIfNotExists();

            // get a reference to the lock blob for this particular engagement
            var azureLockBlob = azureStorageMongoLockOrchestratorContainer.GetBlockBlobReference(CultureSurveyId + ".lck");
            //if (!azureLockBlob.Exists)
            if (azureLockBlob != null)
            {
                // create the blob
                var bytesToUpload = Encoding.UTF8.GetBytes("lock-" + CultureSurveyId);
                using (MemoryStream ms = new MemoryStream(bytesToUpload))
                {
                    //azureLockBlob.UploadFromStream(ms)
                    //await azureLockBlob.UploadFromStreamAsync(ms);
                    await azureLockBlob.UploadFromStreamAsync(ms);
                }
            }
            // acquire a lease/lock on the blob
            TimeSpan leaseTimeSpan = new TimeSpan(0, 0, leaseLengthSeconds);
            //Task<string> leaseId = return azureLockBlob.AcquireLeaseAsync(leaseTimeSpan, null, null, null, null)
            return await azureLockBlob.AcquireLeaseAsync(leaseTimeSpan, null, null, null, null);

            // var leaseId = azureLockBlob.AcquireLease(leaseTimeSpan, null/* TODO Change to default(_) if this is not a reference type */, null/* TODO Change to default(_) if this is not a reference type */, null/* TODO Change// to default(_) if this is not a reference type */, null/* TODO Change to default(_) if this is not a reference type */);
            //return leaseId;
        }
        catch (Exception ex)
        {
            // sometimes we cannot get a lease right now - try back later
            return "";
        }
    }

    public void ReleaseBlobLease(ObjectId CultureSurveyId, string leaseId)
    {
        // get a reference to the container where the locks are stored
        var azureStorageMongoLockAccount = CloudStorageAccount.Parse(_mongoLockAzureStorageConnectionString);
        var azureStorageMongoLockClient = azureStorageMongoLockAccount.CreateCloudBlobClient();
        var azureStorageMongoLockOrchestratorContainer = azureStorageMongoLockClient.GetContainerReference(_mongoLockAzureStorageContainer);

        // get a reference to the lock blob for this particular engagement
        var azureLockBlob = azureStorageMongoLockOrchestratorContainer.GetBlockBlobReference(CultureSurveyId + ".lck");

        // release the lease
        AccessCondition ac = new AccessCondition();
        ac.LeaseId = leaseId;
        azureLockBlob.ReleaseLeaseAsync(ac, null, null);
    }

    ///     ''' Saves the culture survey to the SurveyAuditCollection (To be called before reopening a CS for editing)
    public void SaveCultureSurveyToSurveyAuditCollection(CultureSurveyDTO cultureSurvey)
    {
        IMongoDatabase CSDb = GetMongoDatabase(_mongoCultureSurveyDb);
        IMongoCollection<CultureSurveyDTO> cultureSurveyCollection = CSDb.GetCollection<CultureSurveyDTO>(_mongoCultureSurveyAuditCollection);
        cultureSurvey.SetLastModifiedDate();
        cultureSurvey.Id = ObjectId.GenerateNewId();
        cultureSurveyCollection.InsertOne(cultureSurvey);
    }
}
