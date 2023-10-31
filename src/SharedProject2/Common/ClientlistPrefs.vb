Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Public Class ClientListPrefs
    Public Property _id As ObjectId
    Public Property ClientId As Integer
    Public Property HasChanged As Boolean = False
    Public Property ListPrefs As List(Of Integer)
End Class