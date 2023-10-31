Imports MongoDB.Bson
Imports Newtonsoft.Json

''' <summary>
''' Confirms that a given publishing group contains reports for a given client
''' Used to determine which pub groups should be queried when constructing a client bundle
''' </summary>
''' <remarks></remarks>
Public Class CCPublishingGroupClientLink

    ''' <summary>
    ''' The Id of the object (automatically generated)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property Id As ObjectId = ObjectId.GenerateNewId

    ''' <summary>
    ''' The publishing group that is being described by the linkage
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property PublishingGroupId As ObjectId = New ObjectId

    ''' <summary>
    ''' The client id that the pub group contains reports for
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ClientId As Integer = 0

    Public Sub New(aPublishingGroupId As ObjectId,
                   aClientId As Integer)

        PublishingGroupId = aPublishingGroupId
        ClientId = aClientId

    End Sub

    ''' <summary>
    ''' zero argument constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
    End Sub

End Class
