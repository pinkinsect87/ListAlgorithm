Imports Newtonsoft.Json
Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Public Class CCPublishingGroupListInfo

    ''' <summary>
    ''' The id of the publishing group
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property PublishingGroupId As ObjectId

    ''' <summary>
    ''' The name of the publishing group
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupName As String = "GroupName"

    ''' <summary>
    ''' The date the publishing group was last modified
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property PublishingGroupModifiedDate As DateTime

    ''' <summary>
    ''' The date on which the publishing group will publish its reports
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupPublishOnDate As DateTime

    ''' <summary>
    ''' The total number of reports in the publishing group
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupNumReportsTotal As Integer = 0

    ''' <summary>
    ''' The number of approved reports in the publishing group
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupNumReportsApproved As Integer = 0

    ''' <summary>
    ''' The number of retracted reports in the publishing group
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupNumReportsRetracted As Integer = 0

    ''' <summary>
    ''' Indicates whether the group has been published
    ''' (i.e. whether the publication date has passed)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property IsPublished As Boolean
        Get
            If PublishingGroupPublishOnDate < DateTime.UtcNow Then
                ' if publication date is in the past
                Return True
            Else
                ' otherwise not published yet
                Return False
            End If
        End Get
    End Property

    ''' <summary>
    ''' Indicates the health of the group
    ''' </summary>
    ''' <value>either healthy or unhealthy</value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PublishingGroupHealth As String
        Get
            If IsPublished And PublishingGroupNumReportsApproved < (PublishingGroupNumReportsTotal - PublishingGroupNumReportsRetracted) Then
                ' if publish date passed and some reports are not approved
                Return "unhealthy"
            Else
                Return "healthy"
            End If
        End Get
    End Property

    Public Sub New(zPublishingGroupId As ObjectId,
                   zPublishingGroupName As String,
                   zPublishingGroupModifiedDate As DateTime,
                   zPublishingGroupPublishOnDate As DateTime,
                   zPublishingGroupNumReportsTotal As Integer,
                   zPublishingGroupNumReportsApproved As Integer,
                   zPublishingGroupNumReportsRetracted As Integer)

        PublishingGroupId = zPublishingGroupId
        PublishingGroupName = zPublishingGroupName
        PublishingGroupModifiedDate = zPublishingGroupModifiedDate
        PublishingGroupPublishOnDate = zPublishingGroupPublishOnDate
        PublishingGroupNumReportsTotal = zPublishingGroupNumReportsTotal
        PublishingGroupNumReportsApproved = zPublishingGroupNumReportsApproved
        PublishingGroupNumReportsRetracted = zPublishingGroupNumReportsRetracted

    End Sub

    ''' <summary>
    ''' zero argument constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

End Class
