Imports MongoDB.Driver
Imports MongoDB.Bson
Imports MongoDB.Driver.Linq

''' <summary>
''' Repository that holds objects for the ClientCenter (ReportCenter)
''' These objects include things like PublishingGroupLists, PublishingGroups. and ClientReportBundles
''' </summary>
''' <remarks></remarks>
Public Class ClientCenterRepository

    Const CONST_ATLAS_CLIENT_CENTER_DB As String = "atlasclientcenter"
    Const PUBLISHING_GROUPS_COLLECTION As String = "publishinggroups"
    Const PUBLISHING_GROUP_LIST_COLLECTION As String = "publishinggrouplists"
    Const SUPPLEMENTAL_DOCUMENTS_COLLECTION As String = "supplementaldocuments"
    Const PUBLISHING_GROUP_CLIENT_LINK_COLLECTION As String = "publishinggroupclientlinks"

    Private Property MongoConnectionString As String = ""
    Private Property MongoDb As IMongoDatabase = Nothing

    Public Sub New(mongoConnectionString As String)
        Me.MongoConnectionString = mongoConnectionString
    End Sub

    ' --- SAVE FUNCTIONS ---

    ''' <summary>
    ''' Saves a publishing group to the repository
    ''' </summary>
    ''' <param name="pg">The publishing group to be saved</param>
    ''' <remarks></remarks>
    Public Sub SavePublishingGroup(ByRef pg As CCPublishingGroup)
        Dim commonModificationTime As DateTime = DateTime.UtcNow

        ' set the modified date to the present
        pg.ModifiedDate = commonModificationTime

        ' clear and refresh the list of supplemental documents
        Dim allSupplementalDocuments = RetrieveSupplementalDocuments()

        Dim activeSupplementalDocuments = (From a In allSupplementalDocuments
                                           Where a.IsActive = True
                                           Select a).ToList

        pg.SupplementalDocumentList.Clear()
        pg.SupplementalDocumentList = activeSupplementalDocuments

        ' now save the publishing group
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupsCollection As IMongoCollection(Of CCPublishingGroup) =
            clientCenterDb.GetCollection(Of CCPublishingGroup)(PUBLISHING_GROUPS_COLLECTION)

        ' ensure all reports in the report list inherit the properties from the publishing group
        For Each rept In pg.ReportList
            rept.PublishFolderName = pg.PublishFolderName
            rept.PublishFolderYear = pg.PublishFolderYear
            rept.ReportPublishOnDate = pg.PublishOnDate
        Next

        Dim replaceOptions As New FindOneAndReplaceOptions(Of CCPublishingGroup)
        replaceOptions.IsUpsert = True
        Dim filter As FilterDefinition(Of CCPublishingGroup) = Builders(Of CCPublishingGroup).Filter.Eq(Of ObjectId)("_id", pg.Id)
        mongoPublishingGroupsCollection.FindOneAndReplace(filter, pg, replaceOptions)

        ' update the linkages list that tracks which client_ids are in which pub group
        Dim clientIdList = (From a In pg.ReportList
                            Select a.ClientId
                            Distinct).ToList

        SaveClientIdsForPublishingGroup(pg.Id, clientIdList)

        ' next retrieve and modify the master list of publishing groups (used by the grid)
        Dim pgl = RetrievePublishingGroupList()
        If (pgl Is Nothing) Then
            pgl = New CCPublishingGroupList
        End If
        pgl.AddOrUpdateListInfo(pg.GetPublishingGroupListInfo)
        SavePublishingGroupList(pgl)

    End Sub

    ''' <summary>
    ''' Refreshes the list of cid/pubgroupid that determines where reports for a given client can be found
    ''' </summary>  
    ''' <remarks></remarks>
    Public Sub SaveClientIdsForPublishingGroup(pubGroupId As ObjectId, clientIdList As List(Of Integer))
        If clientIdList.Count > 0 Then
            Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

            Dim mongoPubGroupLinkagesCollection As IMongoCollection(Of CCPublishingGroupClientLink) =
                clientCenterDb.GetCollection(Of CCPublishingGroupClientLink)(PUBLISHING_GROUP_CLIENT_LINK_COLLECTION)

            Dim filter As FilterDefinition(Of CCPublishingGroupClientLink) = Builders(Of CCPublishingGroupClientLink).Filter.Eq(Of ObjectId)("PublishingGroupId", pubGroupId)

            ' clear any previous linkages related to this publishing group
            mongoPubGroupLinkagesCollection.DeleteMany(filter)

            ' make the set of new linkages to be inserted
            Dim linkagesList As New List(Of CCPublishingGroupClientLink)
            For Each cid In clientIdList
                Dim wrkLinkage As New CCPublishingGroupClientLink(pubGroupId, cid)
                linkagesList.Add(wrkLinkage)
            Next

            ' batch insert the linkages
            mongoPubGroupLinkagesCollection.InsertMany(linkagesList)
        End If ' end ensuring there is at least one client id in the list
    End Sub

    ''' <summary>
    ''' Saves a supplemental document to the repository
    ''' </summary>
    ''' <param name="sd">The supplemental document to be saved</param>
    ''' <remarks></remarks>
    Public Sub SaveSupplementalDocument(sd As CCSupplementalDocument)
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoSupplementalDocumentsCollection As IMongoCollection(Of CCSupplementalDocument) =
            clientCenterDb.GetCollection(Of CCSupplementalDocument)(SUPPLEMENTAL_DOCUMENTS_COLLECTION)
        If sd.Id = ObjectId.Empty Then
            sd.Id = ObjectId.GenerateNewId
        End If
        Dim replaceOptions As New FindOneAndReplaceOptions(Of CCSupplementalDocument)
        replaceOptions.IsUpsert = True
        Dim filter As FilterDefinition(Of CCSupplementalDocument) = Builders(Of CCSupplementalDocument).Filter.Eq(Of ObjectId)("Id", sd.Id)
        mongoSupplementalDocumentsCollection.FindOneAndReplace(filter, sd, replaceOptions)

    End Sub

    ''' <summary>
    ''' Saves the publishing group list (for the grid that shows all publishing groups) to the repository
    ''' </summary>
    ''' <param name="pgl">The publishing group list</param>
    ''' <remarks></remarks>
    Public Sub SavePublishingGroupList(pgl As CCPublishingGroupList)
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupListsCollection As IMongoCollection(Of CCPublishingGroupList) =
            clientCenterDb.GetCollection(Of CCPublishingGroupList)(PUBLISHING_GROUP_LIST_COLLECTION)
        Dim replaceOptions As New FindOneAndReplaceOptions(Of CCPublishingGroupList)
        replaceOptions.IsUpsert = True

        Dim filter As FilterDefinition(Of CCPublishingGroupList) = Builders(Of CCPublishingGroupList).Filter.Eq(Of ObjectId)("Id", pgl.Id)
        mongoPublishingGroupListsCollection.FindOneAndReplace(filter, pgl, replaceOptions)

    End Sub

    ' --- RETRIEVAL FUNCTIONS ---

    ''' <summary>
    ''' Retrieves a publishing group from the repository
    ''' </summary>
    ''' <param name="oid">The id of the publishing group being retrieved</param>
    ''' <remarks></remarks>
    Public Function RetrievePublishingGroup(oid As ObjectId) As CCPublishingGroup
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupsCollection As IMongoCollection(Of CCPublishingGroup) =
            clientCenterDb.GetCollection(Of CCPublishingGroup)(PUBLISHING_GROUPS_COLLECTION)

        Dim filter As FilterDefinition(Of CCPublishingGroup) = Builders(Of CCPublishingGroup).Filter.Eq(Of ObjectId)("Id", oid)
        Dim pg As CCPublishingGroup = mongoPublishingGroupsCollection.Find(filter).FirstOrDefault

        Return pg
    End Function

    Public Function RetrieveSupplementalDocuments() As List(Of CCSupplementalDocument)
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoSupplementalDocumentsCollection As IMongoCollection(Of CCSupplementalDocument) =
            clientCenterDb.GetCollection(Of CCSupplementalDocument)(SUPPLEMENTAL_DOCUMENTS_COLLECTION)

        Dim sdlist As List(Of CCSupplementalDocument) = mongoSupplementalDocumentsCollection.Find(FilterDefinition(Of CCSupplementalDocument).Empty).ToList

        Return sdlist
    End Function

    ''' <summary>
    ''' Retrieves a client report bundle from the repository by client id
    ''' </summary>
    ''' <param name="cid">The client id of the report bundle being retrieved</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RetrieveClientReportBundle(cid As Integer) As CCClientReportBundle
        ' retrieve the group ids that contain reports for this client
        Dim publishingGroupIdList = RetrievePublishingGroupIdsForClient(cid)

        Dim clientBundle As New CCClientReportBundle(cid)

        ' no publishing groups at all
        If publishingGroupIdList.Count = 0 Then
            ' just return an empty bundle
            Return clientBundle
        End If

        ' retrieve the applicable publishing groups (for this client) from the repository
        Dim applicablePublishingGroupList = New List(Of CCPublishingGroup)
        For Each pgid In publishingGroupIdList
            Dim pg = RetrievePublishingGroup(pgid)
            applicablePublishingGroupList.Add(pg)
        Next ' publishing groups for this client

        ' now work with the pub groups that were discovered
        For Each pubGroup In applicablePublishingGroupList
            ' retrieve the published reports for this client
            Dim applicableReports = (From a In pubGroup.ReportList
                                     Where a.IsReportPublished = True And a.ClientId = cid
                                     Select a).ToList

            ' add the reports to the bundle and publish the supplemental documents
            For Each clientReport In applicableReports
                clientBundle.AddReport(clientReport, pubGroup.SupplementalDocumentList.ToList)
            Next ' report
        Next ' publishing group

        Return clientBundle

    End Function

    ''' <summary>
    ''' Retrieves the list of pub group ids that contain reports for the given client
    ''' </summary>
    ''' <param name="clientId"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RetrievePublishingGroupIdsForClient(clientId As Integer) As List(Of ObjectId)
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPubGroupLinkagesCollection As IMongoCollection(Of CCPublishingGroupClientLink) =
            clientCenterDb.GetCollection(Of CCPublishingGroupClientLink)(PUBLISHING_GROUP_CLIENT_LINK_COLLECTION)

        Dim filter As FilterDefinition(Of CCPublishingGroupClientLink) = Builders(Of CCPublishingGroupClientLink).Filter.Eq(Of Integer)("ClientId", clientId)
        Dim requestedLinkages = mongoPubGroupLinkagesCollection.Find(filter).ToList

        Dim pubGroupIdList = (From a In requestedLinkages.AsQueryable
                              Select a.PublishingGroupId
                              Distinct).ToList

        Return pubGroupIdList
    End Function

    ''' <summary>
    ''' Retrieves the latest list of publishing groups
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Used to populate the grid showing all publishing groups</remarks>
    Public Function RetrievePublishingGroupList() As CCPublishingGroupList
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupListsCollection As IMongoCollection(Of CCPublishingGroupList) =
            clientCenterDb.GetCollection(Of CCPublishingGroupList)(PUBLISHING_GROUP_LIST_COLLECTION)

        Dim pgl = mongoPublishingGroupListsCollection.Find(FilterDefinition(Of CCPublishingGroupList).Empty).SingleOrDefault

        Return pgl
    End Function

    ' --- DELETE FUNCTIONS

    ''' <summary>
    ''' Removes all items from the collection of publishing group lists
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DeleteAllPublishingGroupLists()
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupListsCollection As IMongoCollection(Of CCPublishingGroupList) =
            clientCenterDb.GetCollection(Of CCPublishingGroupList)(PUBLISHING_GROUP_LIST_COLLECTION)

        mongoPublishingGroupListsCollection.DeleteMany(FilterDefinition(Of CCPublishingGroupList).Empty)
    End Sub

    ''' <summary>
    ''' Removes all items from the collection of publishing group lists
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DeleteAllPublishingGroups()
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupsCollection As IMongoCollection(Of CCPublishingGroup) =
            clientCenterDb.GetCollection(Of CCPublishingGroup)(PUBLISHING_GROUPS_COLLECTION)

        mongoPublishingGroupsCollection.DeleteMany(FilterDefinition(Of CCPublishingGroup).Empty)
    End Sub

    ''' <summary>
    ''' Removes all items from the collection of pub group client id links
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DeleteAllPublishingGroupClientLinks()
        Dim clientCenterDb As IMongoDatabase = GetMongoDatabase(CONST_ATLAS_CLIENT_CENTER_DB)

        Dim mongoPublishingGroupsCollection As IMongoCollection(Of CCPublishingGroupClientLink) =
            clientCenterDb.GetCollection(Of CCPublishingGroupClientLink)(PUBLISHING_GROUP_CLIENT_LINK_COLLECTION)

        mongoPublishingGroupsCollection.DeleteMany(FilterDefinition(Of CCPublishingGroupClientLink).Empty)
    End Sub

    ''' <summary>
    ''' Gets the reference to the requested Mongo database
    ''' </summary>
    ''' <param name="databaseName">The name of the database to connect to</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetMongoDatabase(databaseName As String) As IMongoDatabase

        If Me.MongoDb Is Nothing Then
            Dim mongoClient As New MongoClient(MongoConnectionString)
            Dim mongoDb As IMongoDatabase = mongoClient.GetDatabase(databaseName)
            Me.MongoDb = mongoDb
        End If

        Return MongoDb

    End Function

End Class
