Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes
Public Class OperationsEmail
    Public Property Id As ObjectId ' Mongo Id for this Job
    Public Property EmailType As Integer
    Public Property HtmlForEmail As String
    Public Property SubjectForEmail As String

End Class
