
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

    public class ClientDocuments
    {
        public ObjectId Id { get; set; }
        public int ClientId { get; set; }
        public int EngagementId { get; set; }
        public List<Document> Documents { get; set; } = new List<Document>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        public ClientDocuments()
        {
        }

        public ClientDocuments(int _ClientId, int _EngagementId)
        {
            ClientId = _ClientId;
            EngagementId = _EngagementId;
        }

        public ClientDocuments(int _ClientId, int _EngagementId, List<Document> _Documents)
        {
            ClientId = _ClientId;
            EngagementId = _EngagementId;
            Documents = _Documents;
        }

        public void SetLastModifiedDate()
        {
            LastModifiedDate = DateTime.UtcNow;
        }
    }

    public class Document
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public string VariableName { get; set; }

        public Document()
        {
        }

        public Document(string _FileName, string _Name, string _VariableName, string _Type, int _Size)
        {
            FileName = _FileName;
            Name = _Name;
            VariableName = _VariableName;
            Size = _Size;
            Type = _Type;
        }
    }


