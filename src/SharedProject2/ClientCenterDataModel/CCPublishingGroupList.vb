Imports Newtonsoft.Json
Imports MongoDB.Bson

Public Class CCPublishingGroupList

    ''' <summary>
    ''' The Id of the list of publishing groups
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property Id As ObjectId

    ''' <summary>
    ''' The list of information about each publishing group
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property InfoList As List(Of CCPublishingGroupListInfo) = New List(Of CCPublishingGroupListInfo)

    ''' <summary>
    ''' zero argument constructor for mongo re-hydration
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
        Id = ObjectId.GenerateNewId
    End Sub

    ''' <summary>
    ''' Adds new info to the list or updates existing info for a publishing group
    ''' </summary>
    ''' <param name="newOrUpdatedInfo"></param>
    ''' <remarks></remarks>
    Public Sub AddOrUpdateListInfo(newOrUpdatedInfo As CCPublishingGroupListInfo)
        ' find the id of the object we are looking for in the list
        Dim newPubGroupId As ObjectId = newOrUpdatedInfo.PublishingGroupId

        ' see whether there is a matching object in the list
        Dim matching = (From a In InfoList
                        Where a.PublishingGroupId = newPubGroupId
                        Select a).SingleOrDefault

        If matching Is Nothing Then
            ' if there is no match, we need to add this item
            InfoList.Add(newOrUpdatedInfo)
        Else
            ' if a match is found we need to replace with the possibly updated data
            InfoList.Remove(matching)
            InfoList.Add(newOrUpdatedInfo)
        End If
    End Sub


End Class
