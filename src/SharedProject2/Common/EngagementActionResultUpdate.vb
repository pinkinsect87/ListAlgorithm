Imports MongoDB.Bson.Serialization.Attributes

''' <summary>
''' Defines an update that is necessary (to a Salesforce ECR record) due to actions taken (or events received) by the Orchestrator
''' </summary>
''' <remarks></remarks>
Public Class EngagementActionResultUpdate
    Public Property FieldToUpdate As EngagementUpdateField
    Public Property NewData As String = ""
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property DateCreated As DateTime = DateTime.UtcNow.ToUniversalTime
    Public Property IsUpdateCompleted As Boolean = False
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property DateUpdateCompleted As DateTime = Nothing
End Class