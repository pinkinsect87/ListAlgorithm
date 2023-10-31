Imports MongoDB.Bson.Serialization.Attributes

''' <summary>
''' Represents a specific action that will be taken by the orchestrator.
''' For example launching a Trust Index survey in SAM or a CA/CB in Great Rated
''' </summary>
''' <remarks></remarks>
Public Class EngagementActionStep

    ''' <summary>
    ''' Empty constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

    Public Property Application As EngagementActionSourceSystem = EngagementActionSourceSystem.None
    Public Property ActionType As EngagementActionType = EngagementActionType.None
    Public Property SurveyType As EngagementActionSurveyType = EngagementActionSurveyType.None

    ' unless otherwise specified actions are taken immediately
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property TakeActionOnDate As DateTime = DateTime.SpecifyKind(New DateTime(2000, 1, 1), DateTimeKind.Utc)

    Public Property SurveyTemplate As String = ""
    Public Property SourceSystemSurveyId As String = ""

    Public Property AffiliateId As String = ""
    Public Property SurveyVersionId As Integer = 0
    Public Property WarehouseLoadId As Integer = 0

    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CreatedDate As DateTime = DateTime.UtcNow.ToUniversalTime
    Public Property IsActionCompleted As Boolean = False
    Public Property IsActionFailed As Boolean = False
    Public Property ErrorCount As Integer = 0
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CompletedDate As DateTime = Nothing
End Class