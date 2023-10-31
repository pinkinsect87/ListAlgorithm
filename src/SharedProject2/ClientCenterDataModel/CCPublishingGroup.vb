Imports MongoDB.Bson
Imports Newtonsoft.Json
Imports MongoDB.Bson.Serialization.Attributes

Public Class CCPublishingGroup

    ''' <summary>
    ''' The Id of the object that will be appended by Mongo when it is saved.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property Id As ObjectId

    ''' <summary>
    ''' The friendly name of the publishing group.
    ''' Will show up on grids for GPTW admin users.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Name As String = "New Publishing Group"

    ''' <summary>
    ''' The name of the folder in which the reports should be published.
    ''' i.e. Fortune 100 Best
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishFolderName As String = "New Folder"

    ''' <summary>
    ''' The year of the folder in which the report s should be published.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishFolderYear As Integer = 2014

    ''' <summary>
    ''' The date on which the reports in the publishing group will be published
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property PublishOnDate As DateTime = DateTime.UtcNow.AddYears(5)

    ''' <summary>
    ''' The list of reports in the publishing group.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReportList As List(Of CCClientReport) = New List(Of CCClientReport)

    ''' <summary>
    ''' The list of supplemental documents that should be applied to this publishing group.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SupplementalDocumentList As List(Of CCSupplementalDocument) = New List(Of CCSupplementalDocument)

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

    Public Sub New(zName As String,
                   zPublishFolderName As String,
                   zPublishFolderYear As Integer,
                   zPublishOnDate As DateTime)

        Id = ObjectId.GenerateNewId
        Name = zName
        PublishFolderName = zPublishFolderName
        PublishFolderYear = zPublishFolderYear
        PublishOnDate = zPublishOnDate

    End Sub

    ''' <summary>
    ''' zero argument constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

    ''' <summary>
    ''' Retrieves the number of approved reports in the group
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function numApprovedReports() As Integer
        Dim countApprovedReports As Integer = (From a In ReportList
                                               Where a.IsReportApproved = True
                                               Select a).Count

        Return countApprovedReports
    End Function

    ''' <summary>
    ''' Retrieves the number of approved retracted in the group
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function numRetractedReports() As Integer
        Dim countRetractedReports As Integer = (From a In ReportList
                                                Where a.IsReportRetracted = True
                                                Select a).Count

        Return countRetractedReports
    End Function

    ''' <summary>
    ''' Retrieves the info object representing the group
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetPublishingGroupListInfo() As CCPublishingGroupListInfo

        Dim pgli As New CCPublishingGroupListInfo(Id, Name, ModifiedDate, PublishOnDate, ReportList.Count, 0, 0)
        pgli.PublishingGroupNumReportsApproved = numApprovedReports()
        pgli.PublishingGroupNumReportsRetracted = numRetractedReports()
        Return pgli
    End Function

    ''' <summary>
    ''' Retrieves a report from the reportlist by its id
    ''' </summary>
    ''' <param name="id"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function getClientReport(id As String) As CCClientReport
        Dim report = (From a In ReportList
                      Where a.Id.ToString = id
                      Select a).FirstOrDefault

        Return report
    End Function

    Public Function containsReportFileId(fileId As Integer) As Boolean
        For Each wi In ReportList
            If wi.ReportFileId = fileId Then
                Return True
            End If
        Next

        Return False
    End Function

End Class
