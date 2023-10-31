using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static System.Collections.Specialized.BitVector32;

public class ClientImages
{
    public ObjectId Id { get; set; }
    public int ClientId { get; set; }
    public int EngagementId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public List<ClientPhoto> Photos { get; set; } = new List<ClientPhoto>();
    public string LogoFileName { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastModifiedDate { get; set; } = DateTime.Now;

    public ClientImages()
    {
    }

    public ClientImages(int _ClientId, int _EngagementId)
    {
        ClientId = _ClientId;
        EngagementId = _EngagementId;
    }

    public ClientImages(int _ClientId, int _EngagementId, List<ClientPhoto> _Photos, string _LogoFileName)
    {
        ClientId = _ClientId;
        EngagementId = _EngagementId;
        Photos = _Photos;
        LogoFileName = _LogoFileName;
    }

    public void SetLastModifiedDate()
    {
        LastModifiedDate = DateTime.UtcNow;
    }
}

public class ClientPhoto
{
    public string FileName { get; set; }
    public string Caption { get; set; }
    public bool Primary { get; set; }

    public ClientPhoto()
    {
    }

    public ClientPhoto(string _FileName, string _Caption, bool _Primary)
    {
        FileName = _FileName;
        Caption = _Caption;
        Primary = _Primary;
    }
}

