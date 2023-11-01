using System;

namespace Portal.Model
{
    public class ListRequestItemModel
    {
        public int ListRequestId { get; set; }
        public string CountryCode { get; set; }
        public int PublicationYear { get; set; }
        public DateTime ModifiedDateTime { get; set; }
        public string ListName { get; set; }
        public string ListNameLocalLanguage { get; set; }
        public string LicenseId { get; set; }
        public int NumberOfWinners { get; set; }
        public string SegmentName { get; set; }
        public string UploadStatus { get; set; }
    }
}
