using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Authentication;

public enum SurveyStatus { Initial = 0, InProgress = 1,  Complete = 2, Abandoned = 3, OptedOut = 4 }
    //public class CultureSurvey
    //{
    //    public ObjectId Id;
    //    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    //    public DateTime CreatedDate { get; set; } = DateTime.Now;
    //    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    //    public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    //    public int ClientId { get; set; }
    //    public int EngagementId { get; set; }
    //    public string SurveyType { get; set; } = ""; // TODO - could also be an enum
    //    public string TemplateVersion { get; set; } = "";
    //    public List<CultureSurveyResponse> Responses { get; set; } = new List<CultureSurveyResponse>();
    //    public SurveyStatus SurveyState { get; set; } = SurveyStatus.Initial;

    //    public CultureSurvey(int _clientId, int _engagementId, string _surveyType, string _templateVersion)
    //    {
    //        ClientId = _clientId;
    //        EngagementId = _engagementId;
    //        SurveyType = _surveyType;
    //        TemplateVersion = _templateVersion;
    //    }



    //    public void SetLastModifiedDate()
    //    {
    //        LastModifiedDate = DateTime.UtcNow;
    //    }
    //}

    public class CultureSurveyDTO
    {
        public ObjectId Id;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        public int ClientId { get; set; }
        public int EngagementId { get; set; }
        public string SurveyType { get; set; } = ""; // TODO - could also be an enum
        public string TemplateVersion { get; set; } = "";
        public List<CultureSurveyResponse> Responses { get; set; } = new List<CultureSurveyResponse>();
        public SurveyStatus SurveyState { get; set; } = SurveyStatus.Initial;
        public List<String> Countries { get; set; }
        public string LastSavedClientGUIDId { get; set; }
        public List<String> AuditHistory { get; set; } = new List<String>();

        public string LockLeaseId = "";

        public CultureSurveyDTO(int _clientId, int _engagementId, string _surveyType, string _templateVersion, List<string> _countries)
        {
            ClientId = _clientId;
            EngagementId = _engagementId;
            SurveyType = _surveyType;
            TemplateVersion = _templateVersion;
            Countries = _countries;
        }

        public void SetLastModifiedDate()
        {
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        ///     ''' Get response which matches the variable name passed in (Case Insensative)
        ///     ''' </summary>
        ///     ''' <returns></returns>
        public CultureSurveyResponse GetCultureSurveyResponse(string variableName)
        {
            return (from CultureSurveyResponse res in this.Responses where res.VariableName == variableName select res).SingleOrDefault();
        }

        /// <summary>
        ///     ''' Get response which starts with the variable name passed in (Case Insensative)
        ///     ''' </summary>
        ///     ''' <returns></returns>
        public CultureSurveyResponse GetCultureSurveyResponseStartsWith(string variableName)
        {
            return (from CultureSurveyResponse res in this.Responses where res.VariableName.IndexOf(variableName) == 0 select res).SingleOrDefault();
        }

        public void AuditHistoryAdd(String itemToLog)
        {
            if (this.AuditHistory == null) {
                this.AuditHistory = new List<string>();
            }

            this.AuditHistory.Add(DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss.fff tt") + ": " + itemToLog);
        }
}

public class CreateCultureSurveyResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public string SourceSystemId = "";
        public string SingleSignOnUrl = "";
        public string CultureAuditDueDate = "";
    }

    public class CreateCultureSurveyRequest
    {
        public string Username = "";
        public string Password = "";
        public int EngagementId;
        public int ClientId;
        public string ClientName = "";
        public string SurveyType = "";
        public List<String> Countries;
    }
    public class SaveCultureDataRequest
    {
        public string CultureSurveyId;
        public bool isSubmit;
        public string userGUIDId;
        public int saveId;
        public bool verbosLogging;
        public List<CultureSurveyResponse> Responses;
    }
    public class SaveCultureDataResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public string SurveyState = "";
        public string lastSaveGuid;
        public List<CultureSurveyResponse> refreshResponses = null;
        public List<ClientImages> ClientImages = new List<ClientImages>();
        public List<Document> clientDocuments = new List<Document>();
    }
public class SaveClientImagesRequest
    {
        public string CountryCode;
        public string CultureSurveyId;
        public List<ClientPhoto> clientPhotos;
        public string LogoFileName;
        public string userGUIDId;
    }

    public class SaveClientImagesResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public SurveyStatus SurveyState;
        public string CountryCode;
        public List<ClientImages> ClientImages = new List<ClientImages>();
}

    public class SaveClientDocumentsRequest
    {
        public string CultureSurveyId;
        public List<Document> clientDocuments;
        public string LogoFileName;
        public string userGUIDId;
}

    public class SaveClientDocumentsResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
    }

    public class DeleteClientDocumentRequest
    {
        public string CultureSurveyId;
        public string FileName;
    }

    public class DeleteClientDocumentResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
    }

    public class DeleteClientImageRequest
    {
        public string CultureSurveyId;
        public string FileName;
    }

    public class DeleteClientImageResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
    }

