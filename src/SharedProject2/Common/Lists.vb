Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Public Class Lists
    Public Property _id As ObjectId ' Mongo Id for this Job
    Public Property ListID As Integer
    Public Property ListID18 As String
    Public Property ListName As String = ""
    Public Property Active As Boolean = True
End Class
