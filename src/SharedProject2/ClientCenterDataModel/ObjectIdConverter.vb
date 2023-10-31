Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Imports Newtonsoft.Json

''' <summary>
''' Handles Json De/Serialization for MongoDb ObjectId properties
''' </summary>
''' <remarks></remarks>
Public Class ObjectIdConverter
    Inherits JsonConverter

    Public Overrides Function CanConvert(objectType As Type) As Boolean
        Return GetType(ObjectId).IsAssignableFrom(objectType)
    End Function

    Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
        Dim val As String = reader.Value
        Dim oid As New ObjectId(val)
        Dim z As Integer = 0
        Return oid
    End Function

    Public Overrides Sub WriteJson(writer As JsonWriter, value As Object, serializer As JsonSerializer)
        serializer.Serialize(writer, value.ToString())
    End Sub
End Class