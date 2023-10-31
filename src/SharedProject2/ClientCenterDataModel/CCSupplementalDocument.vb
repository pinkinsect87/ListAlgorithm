Imports Newtonsoft.Json
Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes


Public Class CCSupplementalDocument
    ''' <summary>
    ''' The Id of the object (automatically generated)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property Id As ObjectId = ObjectId.GenerateNewId

    ''' <summary>
    ''' Controls whether the supplemental document is being actively published or has been deactivated.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property IsActive As Boolean = True

    ''' <summary>
    ''' The AffiliateId for which the supplemental document was uploaded
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property AffiliateId As String = "US1"

    ''' <summary>
    ''' The friendly name of the document that should be shown to the user (i.e. How To Interpret Your Scores)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property DocumentFriendlyName As String = "How To Interpret Your Scores"

    ''' <summary>
    ''' The URI of the document in the report store
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property FileUri As String = "myfile.pdf"

    ''' <summary>
    ''' The report type that the additional document is affiliated with.
    ''' A given additional document will only be published if the client has a report of this type.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReportType As String = "ReportType"

    ''' <summary>
    ''' The time the object was created
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property CreatedDate As Date = DateTime.UtcNow

    ''' <summary>
    ''' The time the object was modified
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property ModifiedDate As Date = DateTime.UtcNow

    Public Sub New(zAffiliateId As String,
                   zDocumentFriendlyName As String,
                   zFileUri As String,
                   zReportType As String)

        AffiliateId = zAffiliateId
        DocumentFriendlyName = zDocumentFriendlyName
        FileUri = zFileUri
        ReportType = zReportType

    End Sub

    ''' <summary>
    ''' zero argument constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

End Class
