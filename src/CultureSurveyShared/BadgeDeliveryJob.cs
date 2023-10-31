using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using System.Numerics;

namespace CultureSurveyShared
{
    public enum BadgeDeliveryJobType { Unknown = -1, DownloadAll = 0, Share = 1, ShareAll = 2 }
    public enum BadgeDeliveryJobStatus { Created = 0, Processing = 1, Success = 3, Failed = 4 }
    public class BadgeDeliveryJob
    {
        public ObjectId Id { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ProcessedDate { get; set; } = DateTime.MinValue;
        public BadgeDeliveryJobType JobType { get; set; } = BadgeDeliveryJobType.Unknown;
        public BadgeDeliveryJobStatus JobStatus { get; set; } = BadgeDeliveryJobStatus.Created;
        public int ClientId { get; set; }
        public List<ECRCountryCode> ECRCountryCodes { get; set; } = new List<ECRCountryCode>();
        public string EmailAddressOfRecipient { get; set; } = "";
        public string EmailAddressOfOriginator { get; set; } = "";
        public string FirstNameOfOriginator { get; set; } = "";
        public string LastNameOfOriginator { get; set; } = "";
        public string BlobFileName { get; set; } = "";
        public string SASToken { get; set; } = "";
        public Boolean DownloadLinkClicked { get; set; } = false;
        public List<String> AuditHistory { get; set; } = new List<String>();
        public BadgeDeliveryJob()
        {
        }
        public void AuditHistoryAdd(String itemToLog)
        {
            if (this.AuditHistory == null)
                this.AuditHistory = new List<string>();

            this.AuditHistory.Add(DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss.fff tt") + ": " + itemToLog);
        }

    }

    public class ECRCountryCode
    {
        public int EngagementId { get; set; }
        public string CountryCode { get; set; }
    }

}


