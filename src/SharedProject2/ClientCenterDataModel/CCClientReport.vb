Imports Newtonsoft.Json
Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports System.Security.Cryptography
Imports System.Text
Imports Microsoft.VisualBasic

Public Class CCClientReport

    ''' <summary>
    ''' The Id of the object (automatically generated)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property Id As ObjectId = ObjectId.GenerateNewId

    ''' <summary>
    ''' The id of the publishing group where the report can be found
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupId As ObjectId = Nothing

    ''' <summary>
    ''' The affiliate id of the published report
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property AffiliateId As String = "US1"

    ''' <summary>
    ''' The client id of the published report
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ClientId As Integer = 0

    ''' <summary>
    ''' The (most recent) survey version id of the published report
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SurveyVersionId As Integer = 0

    ''' <summary>
    ''' The reference to the report work item id when the report was run in Atlas
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReportWorkItemId As Integer = 0

    ''' <summary>
    ''' The reference to the report file id when the report was run in Atlas
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReportFileId As Integer = 0

    ''' <summary>
    ''' The company name in effect when the report was published
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property CompanyName As String = "CompanyName"

    ''' <summary>
    ''' The slice name that was used to run the report
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SliceName As String = "SliceName"

    ''' <summary>
    ''' The id of the slice that was used to run the report
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property SliceId As Integer = 0

    ''' <summary>
    ''' The type of the report (i.e. Ungrouped Comment Report)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReportType As String = "ReportType"

    ''' <summary>
    ''' The uri of the file in the report store represented by this report
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property FileUri As String = "myfile.xlsx"

    ''' <summary>
    ''' The name of the folder in which the reports should be published.
    ''' i.e. Fortune 100 Best
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishFolderName As String = "New Folder"

    ''' <summary>
    ''' The year of the folder in which the reports should be published.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishFolderYear As Integer = 2014

    ''' <summary>
    ''' Determines whether the report was approved by the user
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property IsReportApproved As Boolean = False

    ''' <summary>
    ''' Determines whether the report was retracted by the user
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property IsReportRetracted As Boolean = False

    ''' <summary>
    ''' The reason entered by the user for retracting the report.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReasonForRetraction As String = ""

    ''' <summary>
    ''' The reason entered by the user for approving the report.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReasonForApproval As String = ""

    ''' <summary>
    ''' Determines whether a report in a bundle is a piece of supplemental material or not
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property IsSupplementalDocument As Boolean = False

    ''' <summary>
    ''' Determines whether a report is published based on the publish date and approval status
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property IsReportPublished As Boolean
        Get
            ' if report is not approved it is definitely not published
            If Not IsReportApproved Then
                Return False
            End If

            ' if report is retracted it is definitely not published
            If IsReportRetracted Then
                Return False
            End If

            ' then check whether the publication date has passed
            If DateTime.UtcNow < ReportPublishOnDate Then
                Return False
            Else
                Return True
            End If

        End Get
    End Property


    ''' <summary>
    ''' The date on which the report should be published
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property ReportPublishOnDate As DateTime = DateTime.UtcNow

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

    <BsonIgnore>
    Public Property ClientSsoUrl As String = ""

    ''' <summary>
    ''' Returns the full friendly report name including company, slice, year, type
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonIgnore>
    Public ReadOnly Property ReportFriendlyNameFull(includeExtension As Boolean) As String
        Get

            Dim friendlyName As String = String.Format("{0} - {1} - {2} - {3}",
                                                       Me.CompanyName,
                                                       Me.SliceName,
                                                       Me.SurveyVersionId.ToString.Substring(0, 4),
                                                       Me.ReportType)

            If includeExtension Then
                friendlyName = friendlyName & "." & FileUri.Split(".").Last
            End If

            friendlyName = friendlyName.Replace("""", "")

            Return friendlyName
        End Get
    End Property

    ''' <summary>
    ''' Returns the full friendly report name including company, slice, year, type
    ''' (does not include extension)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ReportFriendlyNameFullNoExtension() As String
        Get
            Return ReportFriendlyNameFull(False)
        End Get
    End Property

    ''' <summary>
    ''' Returns the full friendly report name including company, slice, year, type
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonIgnore>
    Public ReadOnly Property ReportFriendlyNameWithoutClientName(includeExtension As Boolean) As String
        Get
            Dim friendlyName As String = String.Format("{0} - {1} - {2}",
                                                       Me.SliceName,
                                                       Me.SurveyVersionId.ToString.Substring(0, 4),
                                                       Me.ReportType)

            If includeExtension Then
                friendlyName = friendlyName & "." & FileUri.Split(".").Last
            End If

            Return friendlyName
        End Get
    End Property

    ''' <summary>
    ''' Returns the friendly text status of the report (i.e. Awaiting Approval, Approved, Published, or Retracted)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonIgnore>
    Public ReadOnly Property ReportStatusText As String
        Get
            If IsReportRetracted Then
                Return "Retracted"
            ElseIf (ReportPublishOnDate < DateTime.UtcNow) And IsReportApproved Then
                Return "Published"
            ElseIf IsReportApproved Then
                Return "Approved"
            Else
                Return "Awaiting Approval"
            End If
        End Get
    End Property

    ''' <summary>
    ''' Determines whether a report is approvable.
    ''' </summary>
    ''' <value></value>
    ''' <returns>either "true" (A string) or a text string indicating why the report cannot be approved</returns>
    ''' <remarks></remarks>
    <BsonIgnore>
    Public ReadOnly Property IsReportApprovable As String
        Get
            If IsReportRetracted Then
                Return "A report cannot be re-approved once it has been retracted."
            ElseIf IsReportApproved Then
                Return "This report has already been approved."
            ElseIf ReportPublishOnDate < DateTime.UtcNow Then
                Return "A report cannot be approved once the publication date has passed, you must create a new group."
            Else
                Return "true"
            End If
        End Get
    End Property

    ''' <summary>
    ''' Determines whether a report is retractable.
    ''' </summary>
    ''' <value></value>
    ''' <returns>either "true" (A string) or a text string indicating why the report cannot be retracted</returns>
    ''' <remarks></remarks>
    <BsonIgnore>
    Public ReadOnly Property IsReportRetractable As String
        Get
            If IsReportRetracted Then
                Return "This report has already been retracted."
            ElseIf ReportPublishOnDate >= DateTime.UtcNow Then
                Return "A report cannot be retracted until it has been published (try removing it instead)."
            ElseIf Not IsReportApproved Then
                Return "A report cannot be retracted until it has been approved."
            Else
                Return "true"
            End If
        End Get
    End Property

    ''' <summary>
    ''' Determines whether a report is removable.
    ''' </summary>
    ''' <value></value>
    ''' <returns>either "true" (A string) or a text string indicating why the report cannot be removed</returns>
    ''' <remarks></remarks>
    <BsonIgnore>
    Public ReadOnly Property IsReportRemovable As String
        Get
            If IsReportRetracted Then
                Return "A report that has been retracted cannot be removed."
            ElseIf IsReportPublished And IsReportApproved Then
                Return "A report that has been approved and published cannot be removed."
            Else
                Return "true"
            End If
        End Get
    End Property

    Public Sub New(zPublishingGroupId As ObjectId,
                   zAffiliateId As String,
                   zClientId As Integer,
                   zSurveyVersionId As Integer,
                   zReportWorkItemId As Integer,
                   zReportFileId As Integer,
                   zCompanyName As String,
                   zSliceName As String,
                   zSliceId As Integer,
                   zReportType As String,
                   zFileUri As String,
                   zPublishFolderName As String,
                   zPublishFolderYear As Integer,
                   zReportPublishOnDate As DateTime)

        PublishingGroupId = zPublishingGroupId
        AffiliateId = zAffiliateId
        ClientId = zClientId
        SurveyVersionId = zSurveyVersionId
        ReportWorkItemId = zReportWorkItemId
        ReportFileId = zReportFileId
        CompanyName = zCompanyName
        SliceName = zSliceName
        SliceId = zSliceId
        ReportType = zReportType
        FileUri = zFileUri
        PublishFolderName = zPublishFolderName
        PublishFolderYear = zPublishFolderYear
        ReportPublishOnDate = zReportPublishOnDate

    End Sub
    Public Function GetReportFriendlyName(includeExt As Boolean) As String
        Return ReportFriendlyNameFull(includeExt)
    End Function

    ''' <summary>
    ''' zero argument constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

End Class