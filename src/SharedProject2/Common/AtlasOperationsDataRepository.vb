Imports MongoDB.Bson
Imports MongoDB.Driver
Imports MongoDB.Bson.Serialization.Attributes
Imports System.Text
Imports System.IO
Imports MongoDB.Driver.Linq
Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports MongoDB.Driver.Core.Events
Imports System.Threading
Imports Microsoft.ApplicationInsights
Imports System.Reflection
Imports Azure.Storage.Blobs
Imports System.Security.Cryptography
Imports Azure.Storage.Blobs.Specialized
Imports Azure

Public Class AtlasOperationsDataRepository

    Public CertificationStatusValues = New List(Of String) From {"", "Certified", "Not Certified", "Pending"}
    Public ListEligibilityStatusValues = New List(Of String) From {"", "Eligible", "Not Eligible", "Pending"}
    Public TrustIndexStatusValues = New List(Of String) From {"", "Created", "Setup in Progress", "Ready to Launch", "Survey in Progress", "Survey Closed", "Data Transferred", "Data Loaded", "Abandoned", "Opted-Out"}
    Public CACBStatusValues = New List(Of String) From {"", "Created", "In Progress", "Completed", "Abandoned", "Opted-Out"}
    Public ProfilePublishStatusValues = New List(Of String) From {"", "Requested", "Success", "Failure"}
    Public IsApplyingForCertificationStatusValues = New List(Of String) From {"Yes", "No", "Unknown"}

    Private Const LEASE_LENGTH_SECONDS As Integer = 59
    Private Const MAX_SAVE_ATTEMPTS As Integer = 3
    Private Const MAX_RETRIEVE_ATTEMPTS As Integer = 3
    Private Const SLEEP_INTERVAL As Integer = 250

    Private Property _mongoConnectionString As String = ""
    Private Property _mongoLockAzureStorageConnectionString As String = ""
    Private Property _mongoLockAzureStorageContainer As String = "orchestratorlock"

    Private Property _mongoAtlasOperationsDb As String = "atlasoperations"
    Private Property _mongoAffiliatesDb As String = "affiliateinfo"

    Private Property _mongoAtlasOperationsECRV2Collection As String = "ecrv2"
    Public Property MongoAtlasOperationsECRV2Collection As String
        Get
            Return _mongoAtlasOperationsECRV2Collection
        End Get
        Set(value As String)
            _mongoAtlasOperationsECRV2Collection = value
        End Set
    End Property

    Private Property _mongoAtlasOperationsECRV2SnippetCollection As String = "ecrv2snippet"
    Public Property MongoAtlasOperationsECRV2SnippetCollection As String
        Get
            Return _mongoAtlasOperationsECRV2SnippetCollection
        End Get
        Set(value As String)
            _mongoAtlasOperationsECRV2SnippetCollection = value
        End Set
    End Property

    Private Property _mongoAtlasOperationsSyncLogCollection As String = "synclog"
    Private Property _mongoAtlasOperationsContactsCollection As String = "contacts"
    Private Property _mongoAtlasOperationsEmailsCollection As String = "emailtemplates"
    Private Property _mongoAtlasOperationsListsCollection As String = "lists"
    Private Property _mongoAtlasOperationsClientListPrefsCollection As String = "clientlistprefs"
    Private Property _mongoAtlasOperationsUserEventsCollection As String = "userevents"
    Private Property _mongoAtlasOperationsFavoriteClientsCollection As String = "favoriteclients"
    Private Property _mongoAffiliatesCollection As String = "affiliates"
    Private Property _mongoCountriesCollection As String = "countries"
    Private Property _mongoLanguagesCollection As String = "languages"
    Private Property _mongoCurrenciesCollection As String = "currencies"
    Private Property _mongoMNCCollection As String = "mnc_cids"
    Private Property _mongoEmailTrackingcollection As String = "emailtracking"
    Private Property _mongoDataExtractRequestQueueCollection As String = "dataextractrequestqueue"
    Private Property _mongoDataExtractRequestQueueV2Collection As String = "dataextractrequestqueuev2"

    Private Property _log As GptwLog = Nothing

    Public Sub New(mongoConnectionString As String, mongoLockAzureStorageConnectionString As String)
        _mongoConnectionString = mongoConnectionString
        _mongoLockAzureStorageConnectionString = mongoLockAzureStorageConnectionString
    End Sub

    Public Sub New(mongoConnectionString As String, mongoLockAzureStorageConnectionString As String, LogConfigInfo As GptwLogConfigInfo)
        _mongoConnectionString = mongoConnectionString
        _mongoLockAzureStorageConnectionString = mongoLockAzureStorageConnectionString
        _log = New GptwLog(LogConfigInfo)
    End Sub

    Public Function GetOperationsDb() As IMongoDatabase
        Dim mongoClient As New MongoClient(_mongoConnectionString)
        Dim mongoDaLoDb As IMongoDatabase = mongoClient.GetDatabase(_mongoAtlasOperationsDb)
        Return mongoDaLoDb
    End Function

    Public Function GetAffiliatesDb() As IMongoDatabase
        Dim mongoClient As New MongoClient(_mongoConnectionString)
        Dim mongoDaLoDb As IMongoDatabase = mongoClient.GetDatabase(_mongoAffiliatesDb)
        Return mongoDaLoDb
    End Function

    ' True for VALID. False for INVALID
    Public Function TestValidityOfCertainFields(fieldToUpdate As EngagementUpdateField, ByRef value As String) As Boolean
        Dim validValues As List(Of String) = Nothing

        If (fieldToUpdate = EngagementUpdateField.ProfileRequestedDate Or fieldToUpdate = EngagementUpdateField.ProfilePublishedDate Or fieldToUpdate = EngagementUpdateField.ReportCenterDeliveryDate) Then
            Return Date.TryParse(value, Nothing)
        End If

        If (fieldToUpdate = EngagementUpdateField.ProfilePublishedLink Or fieldToUpdate = EngagementUpdateField.ProfilePackageURL) Then
            If value.Trim <> "" Then
                'Test if a valid url/uri.
                Return Uri.IsWellFormedUriString(value.Trim.ToLower, UriKind.RelativeOrAbsolute)
            End If
            Return True
        End If

        Select Case fieldToUpdate
            Case EngagementUpdateField.CultureAuditStatus
                validValues = CACBStatusValues
            Case EngagementUpdateField.CultureBriefStatus
                validValues = CACBStatusValues
            Case EngagementUpdateField.TrustIndexStatus
                validValues = TrustIndexStatusValues
            Case EngagementUpdateField.CertificationStatus
                validValues = CertificationStatusValues
            Case EngagementUpdateField.ListEligibilityStatus
                validValues = ListEligibilityStatusValues
            Case EngagementUpdateField.ProfilePublishStatus
                validValues = ProfilePublishStatusValues
            Case EngagementUpdateField.IsApplyingForCertification
                validValues = IsApplyingForCertificationStatusValues
            Case Else
                Return False ' If it's not one of these specfic fields we need to return a false. It didn't validate!
        End Select
        For Each v As String In validValues
            If (String.Compare(v, value, True) = 0) Then
                value = v
                Return True
            End If
        Next
        Return False
    End Function

    Public Function GetFavoriteClients(email As String) As FavoriteClients
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim FavoriteClientsCollection As IMongoCollection(Of FavoriteClients) =
            atlasOperationsDb.GetCollection(Of FavoriteClients)(_mongoAtlasOperationsFavoriteClientsCollection)
        Dim filter As FilterDefinition(Of FavoriteClients) = Builders(Of FavoriteClients).Filter.Eq(Of String)("EmailAddress", email)
        Dim myFavoriteClients As FavoriteClients = FavoriteClientsCollection.Find(filter).SingleOrDefault
        Return myFavoriteClients
    End Function

    Public Sub SaveFavoriteClients(favorateClients As FavoriteClients)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim FavoriteClientsCollection As IMongoCollection(Of FavoriteClients) =
            atlasOperationsDb.GetCollection(Of FavoriteClients)(_mongoAtlasOperationsFavoriteClientsCollection)
        'Upsert requires an id.
        If favorateClients.Id = ObjectId.Empty Then
            favorateClients.Id = ObjectId.GenerateNewId
        End If
        Dim filter As FilterDefinition(Of FavoriteClients) = Builders(Of FavoriteClients).Filter.Eq(Of ObjectId)("Id", favorateClients.Id)

        Dim replaceOptions As New FindOneAndReplaceOptions(Of FavoriteClients)
        replaceOptions.IsUpsert = True
        FavoriteClientsCollection.FindOneAndReplace(filter, favorateClients, replaceOptions)
    End Sub

    ' returns true if a failure occurred
    Public Function ToggleFavoriteClient(email As String, clientId As Integer) As Boolean
        Dim result As Boolean = True
        Try
            Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
            Dim FavoriteClientsCollection As IMongoCollection(Of FavoriteClients) =
            atlasOperationsDb.GetCollection(Of FavoriteClients)(_mongoAtlasOperationsFavoriteClientsCollection)
            Dim myFavoriteClients As FavoriteClients = GetFavoriteClients(email)
            ' if we don't find a FavoriteClients object then create one and this cid to it
            If myFavoriteClients Is Nothing Then
                myFavoriteClients = New FavoriteClients() With {.EmailAddress = email, .Clients = New List(Of Integer)}
                myFavoriteClients.Clients.Add(clientId)
            Else
                If myFavoriteClients.Clients.Contains(clientId) Then
                    myFavoriteClients.Clients.Remove(clientId)
                Else
                    myFavoriteClients.Clients.Add(clientId)
                End If
            End If
            SaveFavoriteClients(myFavoriteClients)
            result = False
        Catch ex As Exception
            'AtlasWebUILog.LogError(String.Format("/api/client/AddClientToFavoriteClients failed for {0}, error:{1}", email, ExceptionHelper.GetDetailedExceptionString(ex)))
        End Try
        Return result
    End Function

    Public Function CreateNewEmailTracking(clientId As Integer, engagementId As Integer,
                            subject As String, emailType As EmailType, dateTimeSent As DateTime, email As String) As EmailTracking
        Return New EmailTracking With {.Id = ObjectId.GenerateNewId, .ClientId = clientId, .EngagementId = engagementId,
            .Subject = subject, .EmailType = emailType, .DateTimeSent = dateTimeSent, .Address = email}
    End Function

    Public Sub SaveEmailTracking(emailTracking As EmailTracking)
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim filter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of ObjectId)("Id", emailTracking.Id)
        Dim replaceOptions As New FindOneAndReplaceOptions(Of EmailTracking)
        replaceOptions.IsUpsert = True
        EmailTrackingCollection.FindOneAndReplace(filter, emailTracking, replaceOptions)
        Return
    End Sub

    Public Function GetEmailTracking(id As String) As EmailTracking
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim filter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of ObjectId)("Id", New ObjectId(id))
        Dim EmailTracking = EmailTrackingCollection.Find(filter).SingleOrDefault
        Return EmailTracking
    End Function

    Public Function GetEmailTrackingByClientId(clientId As Integer) As List(Of EmailTracking)
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim filter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of Integer)("clientId", clientId)
        Dim EmailTracking = EmailTrackingCollection.Find(FilterDefinition(Of EmailTracking).Empty).ToList
        Return EmailTracking
    End Function

    Public Function GetEmailTrackingByEngagementId(engagmentId As Integer) As List(Of EmailTracking)
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim filter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of Integer)("EngagementId", engagmentId)
        Dim EmailTracking = EmailTrackingCollection.Find(filter).ToList
        Return EmailTracking
    End Function

    Public Function GetAllEmailTracking() As List(Of EmailTracking)
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim AllEmailTracking = EmailTrackingCollection.Find(FilterDefinition(Of EmailTracking).Empty).ToList
        Return AllEmailTracking
    End Function

    Public Function HasThisEmailTypeBeenSent(engagmentId As Integer, emailType As EmailType) As Boolean
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim emailTypefilter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of Integer)("EmailType", emailType)
        Dim engagementIdfilter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of Integer)("EngagementId", engagmentId)
        Dim overallFilter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.And(emailTypefilter, engagementIdfilter)
        Dim EmailTrackingList = EmailTrackingCollection.Find(overallFilter).ToList
        Return EmailTrackingList.Count > 0
    End Function

    Public Function GetSortedListOfEmailTracking(engagmentId As Integer, emailType As EmailType) As List(Of EmailTracking)
        Dim DB As IMongoDatabase = GetOperationsDb()
        Dim EmailTrackingCollection As IMongoCollection(Of EmailTracking) =
            DB.GetCollection(Of EmailTracking)(_mongoEmailTrackingcollection)
        Dim emailTypefilter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of Integer)("EmailType", emailType)
        Dim engagementIdfilter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.Eq(Of Integer)("EngagementId", engagmentId)
        Dim overallFilter As FilterDefinition(Of EmailTracking) = Builders(Of EmailTracking).Filter.And(emailTypefilter, engagementIdfilter)
        Dim sortOrder As SortDefinition(Of EmailTracking) = Builders(Of EmailTracking).Sort.Descending("DateTimeSent")
        Dim EmailTrackingList = EmailTrackingCollection.Find(overallFilter).Sort(sortOrder).ToList
        Return EmailTrackingList
    End Function
    Public Function IsCIDAnMNC(clientId As Integer) As Boolean
        Dim mncs As List(Of MNC) = GetAllMNCS()
        Return (From mnc As MNC In mncs Where mnc.CID = clientId).SingleOrDefault IsNot Nothing
    End Function

    Public Function GetAllMNCCIDS() As List(Of Integer)
        Dim mncs As List(Of MNC) = GetAllMNCS()
        Return (From mnc As MNC In mncs Select mnc.CID).ToList
    End Function

    Public Function GetAllMNCS() As List(Of MNC)
        Dim AffiliateInfoDB As IMongoDatabase = GetOperationsDb()
        Dim MNCCollection As IMongoCollection(Of MNC) =
            AffiliateInfoDB.GetCollection(Of MNC)(_mongoMNCCollection)
        Dim MNCCollectionReturn = MNCCollection.Find(FilterDefinition(Of MNC).Empty).ToList
        Return MNCCollectionReturn
    End Function

    Public Function GetAllCurrencies() As List(Of Currency)
        Dim AffiliateInfoDB As IMongoDatabase = GetAffiliatesDb()
        Dim CurrenciesCollection As IMongoCollection(Of Currency) =
            AffiliateInfoDB.GetCollection(Of Currency)(_mongoCurrenciesCollection)
        Dim sortOrder As SortDefinition(Of Currency) = Builders(Of Currency).Sort.Ascending("Code")
        Return CurrenciesCollection.Find(FilterDefinition(Of Currency).Empty).Sort(sortOrder).ToList
    End Function

    Public Function GetAllAffiliates() As List(Of Affiliate)
        Dim AffiliateDB As IMongoDatabase = GetAffiliatesDb()
        Dim AffiliatesCollection As IMongoCollection(Of Affiliate) =
            AffiliateDB.GetCollection(Of Affiliate)(_mongoAffiliatesCollection)
        Dim AffiliatesCollectionReturn = AffiliatesCollection.Find(FilterDefinition(Of Affiliate).Empty).ToList
        Return AffiliatesCollectionReturn
    End Function

    Public Function GetAffiliatebyAffiliateId(AffiliateId As String) As Affiliate
        Dim AffiliateDB As IMongoDatabase = GetAffiliatesDb()
        Dim AffiliatesCollection As IMongoCollection(Of Affiliate) =
            AffiliateDB.GetCollection(Of Affiliate)(_mongoAffiliatesCollection)
        Dim filter As FilterDefinition(Of Affiliate) = Builders(Of Affiliate).Filter.Eq(Of String)("AffiliateId", AffiliateId)
        Dim myAffiliate = AffiliatesCollection.Find(filter).SingleOrDefault
        Return myAffiliate
    End Function

    Public Function GetAssociatedAffiliateByCountryCode(countryCode As String) As Affiliate
        Dim affiliatesDb As IMongoDatabase = GetAffiliatesDb()
        Dim affiliateCollection As IMongoCollection(Of Affiliate) =
            affiliatesDb.GetCollection(Of Affiliate)(_mongoAffiliatesCollection)

        Dim filter As FilterDefinition(Of Affiliate) = Builders(Of Affiliate).Filter.Eq(Of String)("AllowableCountryCodes", countryCode)
        Dim affilate = affiliateCollection.Find(filter).FirstOrDefault()

        Return affilate
    End Function

    Public Function GetAllCountries() As List(Of Country)
        Dim CountryDB As IMongoDatabase = GetAffiliatesDb()
        Dim CountriesCollection As IMongoCollection(Of Country) =
            CountryDB.GetCollection(Of Country)(_mongoCountriesCollection)
        Dim sortOrder As SortDefinition(Of Country) = Builders(Of Country).Sort.Ascending("CountryName")
        Return CountriesCollection.Find(FilterDefinition(Of Country).Empty).Sort(sortOrder).ToList
    End Function

    Public Sub SaveAffiliate(ByRef affiliate As Affiliate)
        Dim affiatesDB As IMongoDatabase = GetAffiliatesDb()
        Dim affiatesCollection As IMongoCollection(Of Affiliate) =
            affiatesDB.GetCollection(Of Affiliate)(_mongoAffiliatesCollection)
        'Upsert requires an id.
        If affiliate.id = ObjectId.Empty Then
            affiliate.id = ObjectId.GenerateNewId
        End If
        Dim filter As FilterDefinition(Of Affiliate) = Builders(Of Affiliate).Filter.Eq(Of ObjectId)("Id", affiliate.id)

        Dim replaceOptions As New FindOneAndReplaceOptions(Of Affiliate)
        replaceOptions.IsUpsert = True
        affiatesCollection.FindOneAndReplace(filter, affiliate, replaceOptions)
        Return
    End Sub

    Public Sub SaveCountry(ByRef country As Country)
        Dim CountryDB As IMongoDatabase = GetAffiliatesDb()
        Dim CountriesCollection As IMongoCollection(Of Country) =
            CountryDB.GetCollection(Of Country)(_mongoCountriesCollection)
        'Upsert requires an id.
        If country.Id = ObjectId.Empty Then
            country.Id = ObjectId.GenerateNewId
        End If
        Dim filter As FilterDefinition(Of Country) = Builders(Of Country).Filter.Eq(Of ObjectId)("Id", country.Id)
        'Insert as a new contact if it doesn't already exist.
        Dim replaceOptions As New FindOneAndReplaceOptions(Of Country)
        replaceOptions.IsUpsert = True
        CountriesCollection.FindOneAndReplace(filter, country, replaceOptions)
        Return
    End Sub

    Public Function GetCountryByCountryCode(CountryCode As String) As Country
        Dim CountryDB As IMongoDatabase = GetAffiliatesDb()
        Dim CountriesCollection As IMongoCollection(Of Country) =
            CountryDB.GetCollection(Of Country)(_mongoCountriesCollection)
        Dim filter As FilterDefinition(Of Country) = Builders(Of Country).Filter.Eq(Of String)("CountryCode", CountryCode)
        Dim country = CountriesCollection.Find(filter).SingleOrDefault
        Return country
    End Function

    ' Adding function for MNC work Jan 24, 2023
    Public Function GetCountryByCountryCodeList(CountryCodeList As List(Of String)) As List(Of Country)
        Dim CountryDB As IMongoDatabase = GetAffiliatesDb()
        Dim CountriesCollection As IMongoCollection(Of Country) =
            CountryDB.GetCollection(Of Country)(_mongoCountriesCollection)
        Dim filter As FilterDefinition(Of Country) = Builders(Of Country).Filter.In(Of String)("CountryCode", CountryCodeList)
        Dim country = CountriesCollection.Find(filter).ToList()
        If (country IsNot Nothing) Then
            country.OrderBy(Function(k) k.CountryName)
        End If
        Return country
    End Function

    Public Function GetAllLanguages() As List(Of Language)
        Dim LanguageDB As IMongoDatabase = GetAffiliatesDb()
        Dim LanguageCollection As IMongoCollection(Of Language) =
            LanguageDB.GetCollection(Of Language)(_mongoLanguagesCollection)
        Dim LanguageCollectionReturn = LanguageCollection.Find(FilterDefinition(Of Language).Empty).ToList
        Return LanguageCollectionReturn
    End Function
    ''' <summary>
    ''' Set an ECRV2 to the failure state by engagement id (integer)
    ''' </summary>
    ''' <param name="engagementId">The engagement id of the ecrv2 to fail</param>
    ''' <remarks></remarks>
    Public Sub FailECRV2(engagementId As Integer, auditHistoryItems As List(Of String))
        Try
            Dim ecrv2 As ECRV2 = RetrieveReadWriteECRV2(engagementId)

            If ecrv2 Is Nothing Then
                Throw New Exception(String.Format("FailECRV2/RetrieveReadWriteECRV2 failed for eid:{0}", engagementId))
            End If

            ecrv2.AuditHistoryAdd(String.Format("ECRV2 set to failed state at:{0}", DateTime.UtcNow))
            For Each str As String In auditHistoryItems
                ecrv2.AuditHistoryAdd(str)
            Next

            ecrv2.Failure = True
            SaveECRV2(ecrv2)
        Catch ex As Exception
            'AtlasWebUILog.LogError(String.Format("/api/client/AODRestartFailedECR failed for {0}, error:{1}", req.EngagementId, ExceptionHelper.GetDetailedExceptionString(ex)))
        End Try
    End Sub

    ''' <summary>
    ''' Retrieve a specific ecrv2 by engagement id (integer)
    ''' </summary>
    ''' <param name="engagementId">The engagement id of the ecrv2 to retrieve</param>
    ''' <returns>The requested ecrv2</returns>
    ''' <remarks></remarks>
    Public Function RetrieveReadOnlyECRV2(engagementId As Integer) As ECRV2
        Dim attempts As Integer = 0
        Dim ecrv2 As ECRV2 = Nothing
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                    atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("EngagementId", engagementId)
                ecrv2 = AtlasOperationsCollection.Find(filter).FirstOrDefault
                If ecrv2 IsNot Nothing Then
                    Exit While
                End If
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        If ecrv2 IsNot Nothing Then
            ecrv2.LockLeaseId = ""
        End If
        Return ecrv2
    End Function

    'Public Function RetrieveLatestECR(clientId As Integer, includeAbandoned As Boolean) As ECRV2
    '    Dim ecrv2s As List(Of ECRV2) = RetrieveReadOnlyECRV2sByClientId(clientId)
    '    ' This will return ecrs sorted by created
    '    ecrv2s = (From ecrv2 As ECRV2 In ecrv2s Where ecrv2.IsAbandoned = includeAbandoned).ToList()
    '    If ecrv2s.Count = 0 Then
    '        Return Nothing
    '    End If
    '    Return ecrv2s.First
    'End Function

    'Public Function IsLatestECR(ecrv2 As ECRV2, includeAbandoned As Boolean) As IsLatestECRResult
    '    Dim result = New IsLatestECRResult() With {
    '            .ErrorString = "General error occured",
    '            .IsError = True,
    '            .IsLatestECR = False}
    '    Try
    '        Dim latestEcrv2 As ECRV2 = RetrieveLatestECR(ecrv2.ClientId, includeAbandoned)
    '        If latestEcrv2 IsNot Nothing Then
    '            result.IsLatestECR = ecrv2.EngagementId = latestEcrv2.EngagementId
    '        End If
    '        result.IsError = False
    '        result.ErrorString = ""
    '    Catch ex As Exception
    '        result.ErrorString = ex.Message
    '    End Try
    'End Function


    Public Function RetrieveLatestECR(clientId As Integer) As ECRV2
        ' This will return ecrs sorted by created
        Dim ecrv2s As List(Of ECRV2) = RetrieveReadOnlyECRV2sByClientId(clientId)
        ' This will remove abandoned ECR's
        ecrv2s = (From ecrv2 As ECRV2 In ecrv2s Where ecrv2.IsAbandoned = False).ToList()
        If ecrv2s.Count = 0 Then
            Return Nothing
        End If
        Return ecrv2s.First
    End Function

    Public Function IsLatestECR(ecrv2 As ECRV2) As Boolean
        Return RetrieveLatestECR(ecrv2.ClientId) Is ecrv2
    End Function

    ''' <summary>
    ''' Retrieve a list of ecrv2s associated with a company
    ''' </summary>
    ''' <returns>The requested ecrv2s</returns>
    ''' <remarks></remarks>
    Public Function RetrieveReadOnlyECRV2sByClientId(ClientId As Integer) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim ecrv2s = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                    atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("ClientId", ClientId)
                Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("CreatedDate")
                ecrv2s = AtlasOperationsCollection.Find(filter).Sort(sortOrder).ToList
                If ecrv2s IsNot Nothing Then
                    Exit While
                End If
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While

        For Each ecrv2 In ecrv2s
            ecrv2.LockLeaseId = ""
        Next

        Return ecrv2s
    End Function

    ''' <summary>
    ''' Retrieve the most recently created ECR's Affiliate Id for a given clientId
    ''' </summary>
    ''' <returns>The requested affiliateId</returns>
    ''' <remarks></remarks>
    Public Function RetrieveAffiliateIdByClientId(ClientId As Integer) As String
        Dim affiliateId As String = ""
        Try
            Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
            Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
            atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
            Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("ClientId", ClientId)
            Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("CreatedDate")
            Dim p = Builders(Of ECRV2).Projection.Include("AffiliateId")
            Dim ecrv2 As BsonDocument = AtlasOperationsCollection.Find(filter).Project(p).Sort(sortOrder).Limit(1).SingleOrDefault()
            If ecrv2 IsNot Nothing Then
                affiliateId = ecrv2.Item("AffiliateId")
            End If
        Catch ex As Exception
        End Try
        Return affiliateId
    End Function

    ''' <summary>
    ''' Retrieve Affiliate Id associated with an engagement
    ''' </summary>
    ''' <returns>The requested affiliateId</returns>
    ''' <remarks></remarks>
    Public Function RetrieveAffiliateIdByEngagementId(EngagementId As Integer) As String
        Dim affiliateId As String = ""
        Try
            Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
            Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
            atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
            Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("EngagementId", EngagementId)
            Dim p = Builders(Of ECRV2).Projection.Include("AffiliateId")
            Dim ecrv2 As BsonDocument = AtlasOperationsCollection.Find(filter).Project(p).SingleOrDefault()
            If ecrv2 IsNot Nothing Then
                affiliateId = ecrv2.Item("AffiliateId")
            End If
        Catch ex As Exception
        End Try
        Return affiliateId
    End Function

    ''' <summary>
    ''' Retrieves a read-write engagement control bundle.
    ''' Subject to a mutex lock.
    ''' You may be forced to wait.
    ''' </summary>
    ''' <param name="engagementId"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RetrieveReadWriteECRV2(engagementId As Integer) As ECRV2
        Return RetrieveReadWriteECRV2(engagementId, LEASE_LENGTH_SECONDS)
    End Function

    'Public Function RetrieveReadWriteECRV2(engagementId As Integer, customLeaseTime As Integer) As ECRV2

    '    ' spin until we can get a lock
    '    Dim leaseId As String = ""
    '    While leaseId = ""
    '        leaseId = AcquireBlobLease(engagementId, customLeaseTime)
    '        If leaseId = "" Then
    '            ' if we didn't get the lease, slight pause, then try again
    '            System.Threading.Thread.Sleep(250)
    '        Else
    '            ' we got a lock, just continue
    '        End If
    '    End While ' global wait until we can get a lease

    '    ' retrieve the bundle from the repository
    '    Dim bund = RetrieveReadOnlyECRV2(engagementId)
    '    If bund IsNot Nothing Then
    '        bund.LockLeaseId = leaseId
    '    End If

    '    Return bund
    'End Function   

    Public Function RetrieveReadWriteECRV2(engagementId As Integer, customLeaseTime As Integer) As ECRV2
        ' spin until we can get a lock
        Dim attempts As Integer = 0
        Dim leaseId As String = ""
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                leaseId = AcquireBlobLeaseAsync(engagementId, customLeaseTime)
                attempts = attempts + 1
                If leaseId Is "" Then
                    If attempts < MAX_RETRIEVE_ATTEMPTS Then
                        Threading.Thread.Sleep(SLEEP_INTERVAL)
                    End If
                Else
                    Exit While
                End If
            Catch ex As Exception
            End Try
        End While

        ' retrieve the bundle from the repository
        Dim bund = RetrieveReadOnlyECRV2(engagementId)
        If bund IsNot Nothing Then
            bund.LockLeaseId = leaseId
        End If
        Return bund
    End Function

    Public Function RetrieveReadOnlyECRV2ByClientIdAffiliateId(ClientId As Integer, AffiliateId As String) As ECRV2
        Dim attempts As Integer = 0
        Dim ecrv2 As ECRV2 = Nothing
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                    atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("ClientId", ClientId)
                Dim filter2 As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of String)("AffiliateId", AffiliateId)
                ecrv2 = AtlasOperationsCollection.Find(filter And filter2).FirstOrDefault
                If ecrv2 IsNot Nothing Then
                    Exit While
                End If
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        If ecrv2 IsNot Nothing Then
            ecrv2.LockLeaseId = ""
        End If
        Return ecrv2

    End Function

    Public Function SaveECRV2(ByRef ecrv2 As ECRV2, Optional setLastModifiedDate As Boolean = True) As ObjectId
        ' make sure this is a readwrite object
        Dim isNewBundle As Boolean = False

        If ecrv2.LockLeaseId = "" And ecrv2.Id.ToString <> "000000000000000000000000" Then
            ' has no lease, and has objectid
            Throw New Exception("This Is a read-only bundle And cannot be saved In the repository. Use RetrieveReadWriteEngagementControlRecordBundle instead.")
        ElseIf ecrv2.LockLeaseId = "" And ecrv2.Id.ToString = "000000000000000000000000" Then
            ' has no lease, and has no objectid
            isNewBundle = True
        ElseIf ecrv2.LockLeaseId <> "" And ecrv2.Id.ToString <> "000000000000000000000000" Then
            ' has a lease, and has an objectid
            isNewBundle = False
        Else
            Throw New Exception("Unexpected combination Of lease And objectid found")
        End If

        If setLastModifiedDate Then
            ecrv2.LastModifiedDate = DateTime.UtcNow
        End If

        Try
            ecrv2.UpdateStatusOnSave() ' Update various health and status values
        Catch ex As Exception
            If (_log IsNot Nothing) Then
                _log.LogError(String.Format("UpdateStatusOnSave failed for EID:{0}, error:{1}", ecrv2.EngagementId, ex.Message))
            End If
            If Not isNewBundle Then ' release the mutex write lock if this is an existing ecrv2
                Try
                    ReleaseBlobLease(ecrv2.EngagementId, ecrv2.LockLeaseId)
                Catch ex1 As Exception
                End Try
            End If
            Throw New Exception(String.Format("AtlasOperationsRepository/SaveECRV2/UpdateStatusOnSave failed for eid:{0}", ecrv2.EngagementId))
        End Try

        Dim attempts As Integer = 0

        While (attempts < MAX_SAVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim atlasOperationsCollection As IMongoCollection(Of ECRV2) =
                    atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                If ecrv2.Id = ObjectId.Empty Then
                    ecrv2.Id = ObjectId.GenerateNewId
                End If
                Dim replaceOptions As New FindOneAndReplaceOptions(Of ECRV2)
                replaceOptions.IsUpsert = True
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of ObjectId)("_id", ecrv2.Id)
                atlasOperationsCollection.FindOneAndReplace(filter, ecrv2, replaceOptions)
                Dim ecrv2Snippet As ECRV2Snippet = ecrv2.GetECRV2Snippet()
                SaveECRV2Snippet(ecrv2Snippet)
                Exit While
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_SAVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While

        If Not isNewBundle Then ' release the mutex write lock if this is an existing ecrv2
            Try
                ReleaseBlobLease(ecrv2.EngagementId, ecrv2.LockLeaseId)
            Catch ex As Exception
                ' for now just ignore this
                ' either the lease couldn't be released or it was already released
                ' most common is 409 already released
            End Try
        End If

        Return ecrv2.Id

    End Function

    ''' <summary>
    ''' Used to manually release the lock on an ECR bundle when saving is not necessary
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub ManuallyClearWriteLock(ecrv2 As ECRV2)
        ReleaseBlobLease(ecrv2.EngagementId, ecrv2.LockLeaseId)
    End Sub

    Public Function SaveECRV2Snippet(ByRef ecrv2Snippet As ECRV2Snippet) As ObjectId
        Try
            Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
            Dim atlasOperationsCollection As IMongoCollection(Of ECRV2Snippet) =
            atlasOperationsDb.GetCollection(Of ECRV2Snippet)(_mongoAtlasOperationsECRV2SnippetCollection)
            If ecrv2Snippet.Id = ObjectId.Empty Then
                ecrv2Snippet.Id = ObjectId.GenerateNewId
            End If
            Dim replaceOptions As New FindOneAndReplaceOptions(Of ECRV2Snippet)
            replaceOptions.IsUpsert = True
            Dim filter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Eq(Of ObjectId)("_id", ecrv2Snippet.Id)
            atlasOperationsCollection.FindOneAndReplace(filter, ecrv2Snippet, replaceOptions)
        Catch ex As Exception
        End Try

        Return ecrv2Snippet.Id

    End Function

    ''' <summary>
    ''' Updates EngagementActionResult and EngagementActionStep lists in an ECR
    ''' </summary>
    ''' <param name="engagementId">Engagement id of thre ECR to update</param>
    ''' <param name="updates">EngagementActionResult list of updates to add to the ECR</param>
    ''' <returns>update result</returns>
    ''' <remarks>This is an overloaded version of the call to include action steps</remarks>
    Public Function UpdateECRV2Properties(engagementId As Integer, updates As List(Of EngagementActionResultUpdate), Optional countryCode As String = Nothing) As UpdateECRV2PropertiesResult
        Dim result = New UpdateECRV2PropertiesResult() With {.Success = False, .ErrorMessage = ""}
        Dim ecrv2 As ECRV2 = Nothing
        Try
            ecrv2 = RetrieveReadWriteECRV2(engagementId)

            If ecrv2 Is Nothing Then
                result.ErrorMessage = String.Format("UpdateECRV2Properties-Couldn't get a lock on engagmentId:{0}", engagementId)
                Return result
            End If

            Dim failed As Boolean = UpdateECRV2Properties(ecrv2, updates, countryCode)

            SaveECRV2(ecrv2)

            result.Success = Not failed
        Catch ex As Exception
            Dim exceptionString = String.Format("UpdateECRV2Properties-failed error:{0}", ex.ToString)
            ecrv2.Failure = True
            ecrv2.AuditHistoryAdd(exceptionString)
            SaveECRV2(ecrv2)
            result.ErrorMessage = exceptionString
            Try
                ReleaseBlobLease(ecrv2.EngagementId, ecrv2.LockLeaseId)
            Catch ex2 As Exception
            End Try
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Updates EngagementActionResult and EngagementActionStep lists in an ECR
    ''' </summary>
    ''' <param name="engagementId">Engagement id of thre ECR to update</param>
    ''' <param name="updates">EngagementActionResult list of updates to add to the ECR</param>
    ''' <returns>True if any of the updates failed. False if all were successful</returns>
    Public Function UpdateECRV2Properties(ecrv2 As ECRV2, updates As List(Of EngagementActionResultUpdate), Optional countryCode As String = Nothing) As Boolean
        Return _ProcessECRV2Properties(ecrv2, updates, countryCode)
    End Function

    Public Function UpdateECRV2Property(ecrv2 As ECRV2, FieldToUpdate As EngagementUpdateField, NewData As String, failureMessage As String, Optional countryCode As String = Nothing) As Boolean
        If (_ProcessECRV2Properties(ecrv2, New List(Of EngagementActionResultUpdate) From {New EngagementActionResultUpdate With {.FieldToUpdate = FieldToUpdate, .NewData = NewData}}, countryCode)) Then
            failureMessage = String.Format("Property:{0} being set to invalid value:{1}", FieldToUpdate.ToString(), NewData)
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Updates EngagementActionResult and EngagementActionStep lists in an ECR
    ''' </summary>
    ''' <param name="engagementId">Engagement id of thre ECR to update</param>
    ''' <param name="updates">EngagementActionResult list of updates to add to the ECR</param>
    ''' <returns>True if any of the updates failed. False if all were successful</returns>
    Private Function _ProcessECRV2Properties(ecrv2 As ECRV2, updates As List(Of EngagementActionResultUpdate), countryCode As String) As Boolean
        Dim failed As Boolean = False
        Dim countryCodeIndex As Integer = -1
        If (countryCode IsNot Nothing) Then
            Dim index As Integer = 0
            For Each c As CountryData In ecrv2.ECR.Countries
                If c.CountryCode = countryCode Then
                    countryCodeIndex = index
                    Exit For
                End If
                index += 1
            Next
            If (countryCodeIndex < 0) Then
                Return True
            End If
        End If

        For Each pendingUpdate In updates
            pendingUpdate.IsUpdateCompleted = False
            pendingUpdate.DateUpdateCompleted = DateTime.UtcNow.ToUniversalTime
            Try
                Select Case pendingUpdate.FieldToUpdate

                    Case EngagementUpdateField.TrustIndexSurveyType
                        Dim surveyType As ECRTrustIndexSurveyType = ECRTrustIndexSurveyType.None
                        Select Case pendingUpdate.NewData.ToUpper
                            Case "NONE"
                                surveyType = ECRTrustIndexSurveyType.None
                            Case "STANDARD"
                                surveyType = ECRTrustIndexSurveyType.Standard
                            Case "TAILORED"
                                surveyType = ECRTrustIndexSurveyType.Tailored
                            Case "ULTRATAILORED"
                                surveyType = ECRTrustIndexSurveyType.UltraTailored
                            Case "UNLIMITED"
                                surveyType = ECRTrustIndexSurveyType.Unlimited
                            Case Else
                                failed = True
                                pendingUpdate.IsUpdateCompleted = False
                        End Select

                        If failed <> True Then
                            If (ecrv2.ECR.TrustIndexSurveyType <> surveyType) Then
                                ecrv2.ECR.TrustIndexSurveyType = surveyType
                                pendingUpdate.IsUpdateCompleted = True
                            Else
                                pendingUpdate.IsUpdateCompleted = False
                            End If
                        End If
                    Case EngagementUpdateField.ProfilePublishedLink
                        If (TestValidityOfCertainFields(EngagementUpdateField.ProfilePublishedLink, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).ProfilePublishedLink, pendingUpdate.NewData)
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.ReportCenterDeliveryDate
                        If (TestValidityOfCertainFields(EngagementUpdateField.ReportCenterDeliveryDate, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.ReportDeliveryCenterDeliveryDate, CDate(pendingUpdate.NewData))
                        Else failed = True
                        End If
                    Case EngagementUpdateField.TrustIndexSourceSystemSurveyId
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.TrustIndexSourceSystemSurveyId, pendingUpdate.NewData)
                    Case EngagementUpdateField.TrustIndexSSOLink
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.TrustIndexSSOLink, pendingUpdate.NewData)
                    Case EngagementUpdateField.TrustIndexStatus
                        If (TestValidityOfCertainFields(EngagementUpdateField.TrustIndexStatus, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.TrustIndexStatus, pendingUpdate.NewData)
                        Else failed = True
                        End If
                    Case EngagementUpdateField.TrustIndexSurveyVersionId
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.SurveyVersionId, CInt(pendingUpdate.NewData))
                    Case EngagementUpdateField.TrustIndexSurveyOpenDate
                        Dim newDataDate As Date? = Nothing
                        If (pendingUpdate.NewData <> "") Then
                            newDataDate = CDate(pendingUpdate.NewData)
                        End If
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.SurveyOpenDate, newDataDate)
                    Case EngagementUpdateField.TrustIndexSurveyCloseDate
                        Dim newDataDate As Date? = Nothing
                        If (pendingUpdate.NewData <> "") Then
                            newDataDate = CDate(pendingUpdate.NewData)
                        End If
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.SurveyCloseDate, newDataDate)
                    Case EngagementUpdateField.CultureAuditSourceSystemSurveyId
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CultureAuditSourceSystemSurveyId, pendingUpdate.NewData)
                    Case EngagementUpdateField.CultureAuditSSOLink
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CultureAuditSSOLink, pendingUpdate.NewData)
                    Case EngagementUpdateField.CultureAuditStatus
                        If (TestValidityOfCertainFields(EngagementUpdateField.CultureAuditStatus, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CultureAuditStatus, pendingUpdate.NewData)
                        Else failed = True
                        End If
                    Case EngagementUpdateField.CultureBriefSourceSystemSurveyId
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CultureBriefSourceSystemSurveyId, pendingUpdate.NewData)
                    Case EngagementUpdateField.CultureBriefSSOLink
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CultureBriefSSOLink, pendingUpdate.NewData)
                    Case EngagementUpdateField.CultureBriefStatus
                        If (TestValidityOfCertainFields(EngagementUpdateField.CultureBriefStatus, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CultureBriefStatus, pendingUpdate.NewData)
                        Else failed = True
                        End If
                    Case EngagementUpdateField.ECRVersion
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.ECRVersion, CDbl(pendingUpdate.NewData))
                    Case EngagementUpdateField.ClientName
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ClientName, pendingUpdate.NewData)
                    Case EngagementUpdateField.Name
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Name, pendingUpdate.NewData)
                    Case EngagementUpdateField.AffiliateId
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.AffiliateId, pendingUpdate.NewData)
                    Case EngagementUpdateField.NumberOfRespondents
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.NumberOfRespondents, CInt(pendingUpdate.NewData))
                    Case EngagementUpdateField.CertificationStatus
                        If (TestValidityOfCertainFields(EngagementUpdateField.CertificationStatus, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).CertificationStatus, pendingUpdate.NewData)
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.ListEligibilityStatus
                        If (TestValidityOfCertainFields(EngagementUpdateField.ListEligibilityStatus, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).ListEligibilityStatus, pendingUpdate.NewData)
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.MarginOfErrorAt90Percent
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).MarginOfErrorAt90Percent, CDec(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.MarginOfErrorAt95Percent
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).MarginOfErrorAt95Percent, CDec(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.ProfilePackageURL
                        If (TestValidityOfCertainFields(EngagementUpdateField.ProfilePackageURL, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).ProfilePackageURL, pendingUpdate.NewData)
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.ProfileRequestedDate
                        If (TestValidityOfCertainFields(EngagementUpdateField.ProfileRequestedDate, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).ProfileRequestedDate, CDate(pendingUpdate.NewData))
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.ProfilePublishedDate
                        If (TestValidityOfCertainFields(EngagementUpdateField.ProfilePublishedDate, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).ProfilePublishedDate, CDate(pendingUpdate.NewData))
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.ProfilePublishStatus
                        If (TestValidityOfCertainFields(EngagementUpdateField.ProfilePublishStatus, pendingUpdate.NewData)) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).ProfilePublishStatus, pendingUpdate.NewData)
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else failed = True
                        End If
                    Case EngagementUpdateField.IsApplyingForCertification
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).IsApplyingForCertification, pendingUpdate.NewData)
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.CertificationDate
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).CertificationDate, CDate(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.CertificationExpiryDate
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).CertificationExpiryDate, CDate(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.CACompletionDate
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CACompletionDate, CDate(pendingUpdate.NewData))
                    Case EngagementUpdateField.CBCompletionDate
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.CBCompletionDate, CDate(pendingUpdate.NewData))
                    Case EngagementUpdateField.NumberOfRespondentsInCountry
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).NumberOfRespondentsInCountry, CInt(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.NumberOfEmployeesInCountry
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).NumberOfEmployeesInCountry, CInt(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.DataSliceId
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).DataSliceId, CInt(pendingUpdate.NewData))
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.DataSliceFilter
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).DataSliceFilter, pendingUpdate.NewData)
                        pendingUpdate.NewData += " (Country:" + countryCode + ")"
                    Case EngagementUpdateField.TIAverageScore
                        Dim tmpvalue As Decimal
                        If Decimal.TryParse(pendingUpdate.NewData, tmpvalue) Then
                            pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.ECR.Countries.Item(countryCodeIndex).TIAverageScore, tmpvalue)
                            pendingUpdate.NewData += " (Country:" + countryCode + ")"
                        Else
                            Throw New Exception(String.Format("TIAverageScore exception: Cannot parse {0} as decimal?", pendingUpdate.NewData))
                        End If
                    Case EngagementUpdateField.IsAbandoned
                        pendingUpdate.IsUpdateCompleted = UpdateIfNecessary(ecrv2.IsAbandoned, CBool(pendingUpdate.NewData))
                    Case EngagementUpdateField.None
                    Case EngagementUpdateField.ModifiedDate
                        ' do nothing
                    Case Else
                        'TODO - where to log this?
                        'Throw New Exception("Could Not apply updated data " & pendingUpdate.NewData & " to field " & pendingUpdate.FieldToUpdate & ", no such field.")
                End Select
                ' Only add items which were updated successfully to the ActionResultUpdateList
                ecrv2.ActionResultUpdateList.Add(pendingUpdate)
            Catch ex As Exception
                failed = True
                Try
                    ecrv2.AuditHistoryAdd(String.Format("ECRV2 Prop update failed. {0} was being set {1}, ex:", pendingUpdate.FieldToUpdate, pendingUpdate.NewData, ex.Message))
                Catch ex2 As Exception
                End Try
            End Try
        Next

        Return failed
    End Function

    Private Function UpdateIfNecessary(ByRef valueBefore As String, valueAfter As String) As Boolean
        If valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    Private Function UpdateIfNecessary(ByRef valueBefore As Boolean, valueAfter As Boolean) As Boolean
        If valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    Private Function UpdateIfNecessary(ByRef valueBefore As ECRTrustIndexSurveyType, valueAfter As String) As Boolean
        If valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    ' If valueBefore can be null and is null then don't compare just set
    Private Function UpdateIfNecessary(ByRef valueBefore As Integer?, valueAfter As Integer) As Boolean
        If valueBefore Is Nothing Or valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function
    Private Function UpdateIfNecessary(ByRef valueBefore As Integer, valueAfter As Integer) As Boolean
        If valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    ' If valueBefore can be null and is null then don't compare just set
    Private Function UpdateIfNecessary(ByRef valueBefore As Double?, valueAfter As Double) As Boolean
        If valueBefore Is Nothing Or valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    ' If valueBefore can be null and is null then don't compare just set
    Private Function UpdateIfNecessary(ByRef valueBefore As Decimal?, valueAfter As Decimal) As Boolean
        If valueBefore Is Nothing Or valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    ' compare two nullable dates
    Private Function UpdateIfNecessary(ByRef valueBefore As Date?, valueAfter As Date?) As Boolean
        If Not Object.Equals(valueBefore, valueAfter) Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    ' If valueBefore can be null and is null then don't compare just set
    Private Function UpdateIfNecessary(ByRef valueBefore As Date?, valueAfter As Date) As Boolean
        If valueBefore Is Nothing Or valueBefore <> valueAfter Then
            valueBefore = valueAfter
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Acquires a lease on a blob in windows azure storage to effectively lock writes to individual ECR bundles.
    ''' </summary>
    ''' <param name="engagementId"></param>
    ''' <param name="leaseLengthSeconds"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AcquireBlobLeaseAsync(engagementId As Integer, leaseLengthSeconds As Integer) As String
        Try
            Dim blobContainerClient As BlobContainerClient = New BlobContainerClient(_mongoLockAzureStorageConnectionString, _mongoLockAzureStorageContainer)
            Dim leaseId = ""
            Dim blobName = engagementId & ".lck"
            Dim azureLockBlob = blobContainerClient.GetBlobClient(blobName)

            If Not blobContainerClient.Exists.Value Then
                blobContainerClient.CreateIfNotExists()
            End If

            If azureLockBlob IsNot Nothing Then
                If azureLockBlob.Exists.Value = False Then
                    'create the blob
                    Dim bytesToUpload = Encoding.UTF8.GetBytes("lock-" & engagementId)
                    Using ms As New MemoryStream(bytesToUpload)
                        azureLockBlob.Upload(ms)
                    End Using
                    azureLockBlob = blobContainerClient.GetBlobClient(blobName)
                End If

                Dim blobLeaseClient = azureLockBlob.GetBlobLeaseClient()
                'acquire lease 
                Dim leaseTimeSpan As New TimeSpan(0, 0, leaseLengthSeconds)

                blobLeaseClient.Acquire(leaseTimeSpan)

                leaseId = blobLeaseClient.LeaseId

            End If

            Return leaseId

        Catch ex As Exception
            Return ""
        End Try
        'Try
        '    ' get a reference to the container where the locks are stored
        '    Dim localRetryPolicy = New ExponentialRetry(New TimeSpan(0, 0, 0, 500), 10)
        '    Dim azureStorageMongoLockAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse(_mongoLockAzureStorageConnectionString)
        '    Dim azureStorageMongoLockClient = azureStorageMongoLockAccount.CreateCloudBlobClient()
        '    azureStorageMongoLockClient.DefaultRequestOptions.RetryPolicy = localRetryPolicy
        '    Dim azureStorageMongoLockOrchestratorContainer = azureStorageMongoLockClient.GetContainerReference(_mongoLockAzureStorageContainer)

        '    'If Not azureStorageMongoLockOrchestratorContainer.Exists Then
        '    '    azureStorageMongoLockOrchestratorContainer.CreateIfNotExists()
        '    'End If

        '    ' get a reference to the lock blob for this particular engagement
        '    Dim azureLockBlob = azureStorageMongoLockOrchestratorContainer.GetBlockBlobReference(engagementId & ".lck")
        '    If Not azureLockBlob.Exists Then
        '        ' create the blob
        '        Dim bytesToUpload = Encoding.UTF8.GetBytes("lock-" & engagementId)
        '        Using ms As New MemoryStream(bytesToUpload)
        '            azureLockBlob.UploadFromStream(ms)
        '        End Using
        '    End If

        '    ' acquire a lease/lock on the blob
        '    Dim leaseTimeSpan As New TimeSpan(0, 0, leaseLengthSeconds)

        '    Dim leaseId = azureLockBlob.AcquireLease(leaseTimeSpan, Nothing, Nothing, Nothing, Nothing)
        '    Return leaseId
        'Catch ex As Exception
        '    ' sometimes we cannot get a lease right now - try back later
        '    _log.LogError("AcquireBlobRelease Exception: ", ex.ToString())
        '    Return ""
        'End Try

    End Function

    ''' <summary>
    ''' Releases the lease on a blob in windows azure storage when an ECR bundle is successfully saved
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub ReleaseBlobLease(engagementId As Integer, leaseId As String)
        'Dim blobServiceClient As BlobServiceClient = New BlobServiceClient(_mongoLockAzureStorageConnectionString)
        'Dim blobContainerClient As BlobContainerClient = blobServiceClient.GetBlobContainerClient(_mongoLockAzureStorageContainer)
        Dim blobContainerClient As BlobContainerClient = New BlobContainerClient(_mongoLockAzureStorageConnectionString, _mongoLockAzureStorageContainer)

        Dim azureLockBlob = blobContainerClient.GetBlobClient(engagementId & ".lck")
        Dim blobLeaseClient = azureLockBlob.GetBlobLeaseClient(leaseId)
        blobLeaseClient.Release()

        '' get a reference to the container where the locks are stored
        'Dim azureStorageMongoLockAccount = CloudStorageAccount.Parse(_mongoLockAzureStorageConnectionString)
        'Dim azureStorageMongoLockClient = azureStorageMongoLockAccount.CreateCloudBlobClient()
        'Dim azureStorageMongoLockOrchestratorContainer = azureStorageMongoLockClient.GetContainerReference(_mongoLockAzureStorageContainer)

        ''If (Not azureStorageMongoLockOrchestratorContainer.Exists) Then
        ''    azureStorageMongoLockOrchestratorContainer.CreateIfNotExists()
        ''End If

        '' get a reference to the lock blob for this particular engagement
        'Dim azureLockBlob = azureStorageMongoLockOrchestratorContainer.GetBlockBlobReference(engagementId & ".lck")

        '' release the lease
        'Dim ac As New AccessCondition
        'ac.LeaseId = leaseId
        'azureLockBlob.ReleaseLease(ac, Nothing, Nothing)
    End Sub
    Public Function RetrieveReadOnlyECRV2ByAffiliateId(AffiliateId As String) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim ecrv2 As List(Of ECRV2) = Nothing
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                    atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                ' Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("ClientId", ClientId)
                Dim filter2 As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of String)("AffiliateId", AffiliateId)
                ecrv2 = AtlasOperationsCollection.Find(filter2).ToList()
                If ecrv2 IsNot Nothing Then
                    Exit While
                End If
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While

        Return ecrv2

    End Function

    ''' <summary>
    ''' Retrieve a list of ecrv2s associated with a company
    ''' </summary>
    ''' <returns>The requested ecrv2s</returns>
    ''' <remarks></remarks>
    Public Function RetrieveECRV2Snippets(earliestCreationDate As DateTime, affiliateId As String) As List(Of ECRV2Snippet)
        Dim attempts As Integer = 0
        Dim ecrv2s = New List(Of ECRV2)

        Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2Snippet) =
                    AtlasOperationsDb.GetCollection(Of ECRV2Snippet)(_mongoAtlasOperationsECRV2SnippetCollection)

        Dim createdDateFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate)
        Dim affiliateIdFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Eq(Of String)("AffiliateId", affiliateId)

        Dim overallFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.And(createdDateFilter, affiliateIdFilter)

        Dim ecrv2Snippets As List(Of ECRV2Snippet) = AtlasOperationsCollection.Find(overallFilter).ToList()

        Return ecrv2Snippets
    End Function

    Public Function RetrieveECRV2SnippetsNotAbandoned(earliestCreationDate As DateTime, affiliateId As String, faveClientIds As List(Of Integer)) As List(Of ECRV2Snippet)
        Dim attempts As Integer = 0
        Dim ecrv2s = New List(Of ECRV2)

        Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2Snippet) =
                    AtlasOperationsDb.GetCollection(Of ECRV2Snippet)(_mongoAtlasOperationsECRV2SnippetCollection)

        Dim overallFilter As FilterDefinition(Of ECRV2Snippet) = Nothing
        Dim createdDateFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate)
        Dim affiliateIdFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Eq(Of String)("AffiliateId", affiliateId)
        Dim abandonedFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Eq(Of Boolean)("Abandoned", False)

        If faveClientIds IsNot Nothing AndAlso faveClientIds.Count > 0 Then
            Dim faveClientsFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.In(Of String)("ClientId", faveClientIds)
            overallFilter = Builders(Of ECRV2Snippet).Filter.And(createdDateFilter, affiliateIdFilter, abandonedFilter, faveClientsFilter)
        Else
            overallFilter = Builders(Of ECRV2Snippet).Filter.And(createdDateFilter, affiliateIdFilter, abandonedFilter)
        End If

        Dim ecrv2Snippets As List(Of ECRV2Snippet) = AtlasOperationsCollection.Find(overallFilter).ToList()

        Return ecrv2Snippets
    End Function

    ' All we are doing here is seeing what kind of matches we have. Since we will be setting up our own filters
    ' we don't need to return the match data. Just what affiliates are involved in the match
    Public Function SearchECRs(searchValue As String, affiliates As List(Of Affiliate)) As EcrSearchResult
        Dim result = New EcrSearchResult() With {.ErrorStr = "", .IsError = False}
        Try
            Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
            Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2Snippet) =
                    AtlasOperationsDb.GetCollection(Of ECRV2Snippet)(_mongoAtlasOperationsECRV2SnippetCollection)
            Dim number As Integer = 0
            If Int32.TryParse(searchValue, number) Then
                Dim ecrv2s As List(Of ECRV2) = RetrieveReadOnlyECRV2sByClientId(number)
                Dim ecrv2 As ECRV2 = Nothing
                If ecrv2s IsNot Nothing And ecrv2s.Count > 0 Then
                    result.NumCIDMatches = 1
                    ecrv2 = ecrv2s.Item(0)
                Else
                    ecrv2 = RetrieveReadOnlyECRV2(number)
                    If ecrv2 IsNot Nothing Then
                        result.NumEIDMatches = 1
                    End If
                End If

                If ecrv2 IsNot Nothing Then
                    For Each affiliate As Affiliate In affiliates
                        If affiliate.AffiliateId = ecrv2.AffiliateId Then
                            If (Not result.Affiliates.Contains(affiliate)) Then
                                result.Affiliates.Add(affiliate)
                            End If
                        End If
                    Next
                    Return result
                End If
            End If

            'Dim filter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Regex("ClientName", New BsonRegularExpression("^" + searchValue + ".*", RegexOptions.IgnoreCase)) '  Filter.Eq(Of String)("ClientName", searchValue)
            'Dim filter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Regex("ClientName", New BsonRegularExpression("/.*" & searchValue & ".*/i", RegexOptions.IgnoreCase)) '  Filter.Eq(Of String)("ClientName", searchValue)
            Dim filter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Regex("ClientName", New BsonRegularExpression(searchValue, "i")) '  Filter.Eq(Of String)("ClientName", searchValue)
            Dim snippets As List(Of ECRV2Snippet) = AtlasOperationsCollection.Find(filter).ToList

            If snippets IsNot Nothing And snippets.Count > 0 Then
                result.NumCNameMatches = snippets.Count
                For i As Integer = snippets.Count - 1 To 0 Step -1
                    Dim s As ECRV2Snippet = snippets.Item(i)
                    For Each affiliate As Affiliate In affiliates
                        If affiliate.AffiliateId = s.AffiliateId Then
                            If (Not result.Affiliates.Contains(affiliate)) Then
                                result.Affiliates.Add(affiliate)
                            End If
                        End If
                    Next
                Next i
            End If
        Catch ex As Exception
            result.ErrorStr = "And exception was thrown. ex:" & ex.Message
            result.IsError = True
        End Try
        Return result
    End Function

    Public Function isInteger(searchValue As String) As Boolean
        Try
            Dim number As Integer = 0
            Return Int32.TryParse(searchValue, number)
        Catch ex As Exception
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Save the sync log
    ''' </summary>
    ''' <param name="log">SyncLog object</param>
    Public Sub SaveSynchLog(ByRef log As SyncLog)

        'Only write the sync log if there are actual log entries.
        If log.Log.Count > 0 Then
            Dim attempts As Integer = 0
            While (attempts < MAX_SAVE_ATTEMPTS)
                Try
                    Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                    Dim atlasOperationsCollection As IMongoCollection(Of SyncLog) =
                    atlasOperationsDb.GetCollection(Of SyncLog)(_mongoAtlasOperationsSyncLogCollection & "_" & DateTime.Now().ToString("yyyyMM"))
                    ' update total run time each time it's saved
                    Dim span As System.TimeSpan = DateTime.UtcNow - log.CreatedDate
                    log.TotalRunTime = String.Format("{0}:{1}:{2}", span.Hours, span.Minutes, span.Seconds)
                    atlasOperationsCollection.InsertOne(log)
                    Return
                Catch ex As Exception
                End Try
                attempts = attempts + 1
                If attempts < MAX_SAVE_ATTEMPTS Then
                    Threading.Thread.Sleep(SLEEP_INTERVAL)
                End If
            End While
        End If

    End Sub


    'Public Sub ResetIsLatestECR(clientId As Integer)
    '    Dim ecrv2s As List(Of ECRV2) = Me.RetrieveReadOnlyECRV2sByClientId(clientId)
    '    Dim firstNonAbandonedECRId As String = ""
    '    Dim ecrV2ToChange As ECRV2 = Nothing
    '    For Each ecrv2 As ECRV2 In ecrv2s

    '        If ecrv2.IsAbandoned Then 'If this ECR is abandoned AND marked isLatestECR then it was just abandoned and needs to have it's IsLatestECR flag cleared.
    '            If ecrv2.IsLatestECR Then
    '                ecrV2ToChange = Me.RetrieveReadWriteECRV2(ecrv2.EngagementId)
    '                If ecrV2ToChange IsNot Nothing Then
    '                    ecrV2ToChange.IsLatestECR = False
    '                    Me.SaveECRV2(ecrV2ToChange)
    '                End If
    '            End If
    '            Continue For
    '        End If

    '        If firstNonAbandonedECRId = "" Then
    '            firstNonAbandonedECRId = ecrv2.Id.ToString
    '        End If

    '        If ecrv2.Id.ToString = firstNonAbandonedECRId Then ' if this ecrv2 is the firstNonAbandonedECRId then it's at the top after sorting by createdDate so set it to the latest
    '            If Not ecrv2.IsLatestECR Then
    '                ecrV2ToChange = Me.RetrieveReadWriteECRV2(ecrv2.EngagementId)
    '                If ecrV2ToChange IsNot Nothing Then
    '                    ecrV2ToChange.IsLatestECR = True
    '                    Me.SaveECRV2(ecrV2ToChange)
    '                End If
    '            End If
    '        Else
    '            If ecrv2.IsLatestECR Then
    '                ecrV2ToChange = Me.RetrieveReadWriteECRV2(ecrv2.EngagementId)
    '                If ecrV2ToChange IsNot Nothing Then
    '                    ecrV2ToChange.IsLatestECR = False
    '                    Me.SaveECRV2(ecrV2ToChange)
    '                End If
    '            End If
    '        End If
    '    Next

    'End Sub

    Public Function RetrieveAllECRV2s() As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) = atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("CreatedDate")
                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(FilterDefinition(Of ECRV2).Empty).Sort(sortOrder).ToList
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return Nothing
    End Function
    Public Function retrieveAllIdsModifiedDates() As List(Of ECRIdsModifiedDates)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRIdsModifiedDates)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) = atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
        Dim p As ProjectionDefinition(Of ECRV2)
        p = Builders(Of ECRV2).Projection.Include("EngagementId")
        p = p.Include("Id")
        p = p.Include("LastModifiedDate")

        Dim bdocs As List(Of BsonDocument) = AtlasOperationsCollection.Find(FilterDefinition(Of ECRV2).Empty).Project(p).ToList
        For Each bdoc In bdocs
            Dim ecrIdModDate As New ECRIdsModifiedDates
            ecrIdModDate.EngagementId = bdoc.Item("EngagementId")
            ecrIdModDate.id = bdoc.Item("_id").ToString
            ecrIdModDate.LastModifiedDate = bdoc.Item("LastModifiedDate")
            result.Add(ecrIdModDate)
        Next

        Return result

    End Function

    Public Function RetrieveAllECRV2EngagementIds() As List(Of Integer)
        Dim result As New List(Of Integer)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) = atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
        Dim p = Builders(Of ECRV2).Projection.Include("EngagementId")
        'Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("CreatedDate")
        ' over sort limit
        Dim eids As List(Of BsonDocument) = AtlasOperationsCollection.Find(FilterDefinition(Of ECRV2).Empty).Project(p).ToList
        For Each eid In eids
            result.Add(eid.Item("EngagementId"))
        Next
        Return result
    End Function

    Public Function RetrieveAllEIDsGroupedByCID() As List(Of Integer)
        Dim result As New List(Of Integer)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) = atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
        Dim p = Builders(Of ECRV2).Projection.Include("EngagementId").Include("ClientId").Include("CreatedDate")
        'Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("CreatedDate")
        ' over sort limit
        Dim results As List(Of BsonDocument) = AtlasOperationsCollection.Find(FilterDefinition(Of ECRV2).Empty).Project(p).ToList
        Dim sortedResult = results.OrderBy(Function(s) s.Item("ClientId")).ThenBy(Function(s) s.Item("CreatedDate"))
        For Each ecr In sortedResult
            result.Add(ecr.Item("EngagementId"))
        Next
        Return result
    End Function

    Public Function GetDistinctClientIdsWithinECRs() As List(Of Integer)
        Dim result As New List(Of Integer)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) = atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
        Dim p = Builders(Of ECRV2).Projection.Include("ClientId")
        Dim cids As List(Of BsonDocument) = AtlasOperationsCollection.Find(FilterDefinition(Of ECRV2).Empty).Project(p).ToList

        For Each cid In cids
            Try
                Dim clientId As Integer = cid.Item("ClientId")
                If (Not result.Contains(clientId)) Then
                    result.Add(clientId)
                End If
            Catch ex As Exception
                Dim i = 1
            End Try
        Next

        Return result
    End Function

    Public Function RetrieveECRV2MostRecentlyModified(numRecordsToTake) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Ascending("LastModifiedDate")
                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(FilterDefinition(Of ECRV2).Empty).Sort(sortOrder).Limit(numRecordsToTake).ToList()
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return Nothing
    End Function

    Public Function RetrieveAllRecentECRsFromDate(dateTimeFrom) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)

                'Last 7 days ones
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Gte(Of Date)("CreatedDate", dateTimeFrom)

                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filter).ToList
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    Public Function RetrieveAllFailedReviewECRV2s() As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim dateTime30MinutesAgo = DateTime.UtcNow.AddMinutes(-30)
                ' Look for Review Requests made more than 30 minutes ago where ReviewPublishStatus is still 'Requested'
                ' which would indicate that it hasn't finished on time. Also return known failures.

                'Filter for ECRs that have failed to publish.
                Dim reviewPublishStatusFailedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.ReviewPublishStatus", New BsonRegularExpression("Failed", "i"))
                'Filter for ECRs requested more than 30 minutes ago.
                Dim reviewRequestedDateFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Lt(Of Date)("ECR.ReviewRequestedDate", dateTime30MinutesAgo)
                'Filter for ECRs that still have a 'requested' publication status.
                Dim reviewPublishStatusRequestedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.ReviewPublishStatus", New BsonRegularExpression("Requested", "i"))

                'Combine the filters for stuck ECRs: those with a 'requested' status AND which was requested more than 30 minutes ago.
                Dim stuckECRFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.And(reviewRequestedDateFilter, reviewPublishStatusRequestedFilter)

                'Build the overall filter: get ECRs that are stuck OR that have failed.
                Dim filterOverall As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Or(reviewPublishStatusFailedFilter, stuckECRFilter)

                Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("ECR.ReviewRequestedDate")

                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filterOverall).Sort(sortOrder).ToList
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    Public Function RetrieveAllEligibilityECRV2s() As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)

                'Filter for ECRs with 'data loaded' status.
                Dim dataLoadedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.TrustIndexStatus", New BsonRegularExpression("Data Loaded", "i"))
                'Filter for ECRs with CA completed status.
                Dim caCompletedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.CultureAuditStatus", New BsonRegularExpression("Completed", "i"))
                'Filters for ECRs with CB completed status.
                Dim cbCompletedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.CultureBriefStatus", New BsonRegularExpression("Completed", "i"))
                'Filter for pending certification status.
                Dim certificationPendingFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.CertificationStatus", New BsonRegularExpression("Pending", "i"))
                'Filter for pending list eligibility status.
                Dim listEligibilityPendingFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Regex("ECR.ListEligibilityStatus", New BsonRegularExpression("Pending", "i"))

                'Filter for either CA or CB status completed.
                Dim caOrCbCompletedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Or(caCompletedFilter, cbCompletedFilter)

                'Filter for either certification pending or list eligibility pending status.
                Dim overallPendingFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Or(certificationPendingFilter, listEligibilityPendingFilter)

                'Build the overall filter for those ECRs with status 'data loaded' AND (either CA OR CB are completed) AND (either certification or list eligibility status is 'pending')
                Dim overallFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.And(dataLoadedFilter, caOrCbCompletedFilter, overallPendingFilter)

                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(overallFilter).ToList
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    Public Function RetrieveAllFailedECRV2s() As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Boolean)("Failure", True)
                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filter).ToList()
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    Public Function GetECRV2sChangedSince(lastModifiedDate As DateTime) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Gt(Of Date)("LastModifiedDate", lastModifiedDate)
                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filter).ToList()
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    ''' <summary>
    ''' Get a list of ECRV2 that was created on or after the specified date and optionally, by affiliate
    ''' </summary>
    ''' <param name="earliestCreationDate">Earliest creation date that will be accepted for inclusion in the returned list</param>
    ''' <param name="affiliateId">AffiliateId to optionally filter on</param>
    ''' <returns>List of ECRV2</returns>
    Public Function GetECRV2sCreatedAfter(earliestCreationDate As DateTime, Optional affiliateId As String = "") As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2)
                If affiliateId <> "" Then
                    filter = Builders(Of ECRV2).Filter.And(Builders(Of ECRV2).Filter.Eq(Of String)("AffiliateId", affiliateId), Builders(Of ECRV2).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate))
                Else
                    filter = Builders(Of ECRV2).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate)
                End If
                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filter).ToList()
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    ''' <summary>
    ''' Get a list of ECRV2 that was created on or after the specified date and that includes a specified country
    ''' </summary>
    ''' <param name="earliestCreationDate">Earliest creation date that will be accepted for inclusion in the returned list</param>
    ''' <param name="countryCode">Country code to filter on</param>
    ''' <returns>List of ECRV2</returns>
    Public Function GetECRV2sCreatedAfterDateWithCountry(earliestCreationDate As DateTime, countryCode As String) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim result = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim filter As FilterDefinition(Of ECRV2)
                filter = Builders(Of ECRV2).Filter.And(Builders(Of ECRV2).Filter.Eq(Of String)("ECR.Countries.CountryCode", countryCode), Builders(Of ECRV2).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate))
                Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filter).ToList()
                Return ecrv2s
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Return result
    End Function

    Public Function FindECRV2sByAccountId(AccountId As String) As List(Of ECRV2)

        Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
        AtlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
        Dim filter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of String)("ECR.AccountId", AccountId)
        Dim ecrv2s As List(Of ECRV2) = AtlasOperationsCollection.Find(filter).ToList()
        Return ecrv2s

    End Function
    ' NOTE THIS WILL NEED TO CHANGE  WE DON"T KNOW WHAT COUNTRY THIS IS
    Public Function FindUSCountryData(ECRV2 As ECRV2) As CountryData
        Dim USA As CountryData = Nothing
        For Each country In ECRV2.ECR.Countries
            If (country.CountryCode = "US") Then
                USA = country
            End If
        Next
        Return USA

    End Function
    Public Function FindCountryData(ECRV2 As ECRV2, CountryCode As String) As CountryData
        Dim myCountry As CountryData = Nothing
        For Each country In ECRV2.ECR.Countries
            If (country.CountryCode = CountryCode) Then
                myCountry = country
            End If
        Next
        Return myCountry

    End Function

    Public Function FindCertificationCountry(ECRV2 As ECRV2) As CountryData
        Return (From c As CountryData In ECRV2.ECR.Countries.AsQueryable() Select c Where String.Compare(c.IsApplyingForCertification, "Yes", True) = 0).SingleOrDefault()
    End Function


    Public Function FindCertificationCountries(ECRV2 As ECRV2) As List(Of CountryData)
        Dim certificationCountries As List(Of CountryData) = New List(Of CountryData)
        For Each country In ECRV2.ECR.Countries
            If (country.IsApplyingForCertification = "Yes") Then
                certificationCountries.Add(country)
            End If
        Next
        Return certificationCountries

    End Function


    ' Add every new contacts email address to the ECRV2 ContactEmailAddresses list.
    Public Sub AddContactEmailToCurrentECRV2(clientId As Integer, emailAddress As String)
        Dim ecrv2s As List(Of ECRV2) = Me.RetrieveReadOnlyECRV2sByClientId(clientId)
        If ecrv2s.Count > 0 Then
            Dim latestEcrv2 As ECRV2 = ecrv2s(0)
            AddContactEmailToECRV2(latestEcrv2, emailAddress)
        End If
    End Sub

    ' Add every new contacts email address to the ECRV2 ContactEmailAddresses list.
    Private Sub AddContactEmailToECRV2(ecrv2 As ECRV2, emailAddress As String)
        If ecrv2 IsNot Nothing Then
            If Not ecrv2.ContactEmailAddresses.Contains(emailAddress) Then
                Dim ecrv2ToChange As ECRV2 = Me.RetrieveReadWriteECRV2(ecrv2.EngagementId)
                ecrv2ToChange.ContactEmailAddresses.Add(emailAddress)
                Me.SaveECRV2(ecrv2ToChange)
            End If
        End If
    End Sub

    Public Function GetChangedListPrefs() As List(Of ClientListPrefs)
        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim listPrefsDB As IMongoCollection(Of ClientListPrefs) =
                AOD.GetCollection(Of ClientListPrefs)(_mongoAtlasOperationsClientListPrefsCollection)

        Dim newListPrefs = (From lp In listPrefsDB.AsQueryable() Select lp Where lp.HasChanged = True).ToList()
        Return newListPrefs
    End Function
    Public Sub SaveListPrefs(listPref As ClientListPrefs)
        Try
            Dim AOD As IMongoDatabase = GetOperationsDb()
            Dim listPrefsDB As IMongoCollection(Of ClientListPrefs) =
                AOD.GetCollection(Of ClientListPrefs)(_mongoAtlasOperationsClientListPrefsCollection)

            listPrefsDB.InsertOne(listPref)
        Catch ex As Exception
            Throw ex
        End Try

    End Sub
    Public Function GetClientListPrefs(clientid As Integer) As ClientListPrefs

        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim listPrefs As IMongoCollection(Of ClientListPrefs) =
            AOD.GetCollection(Of ClientListPrefs)(_mongoAtlasOperationsClientListPrefsCollection)
        Dim myClient As ClientListPrefs = (From l In listPrefs.AsQueryable()
                                           Where l.ClientId = clientid).FirstOrDefault
        Return myClient

    End Function
    Public Function UpdateListPrefs(clientId As Integer, listPrefs As List(Of Integer)) As String

        Dim errorMessage As String = String.Empty
        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim listDB As IMongoCollection(Of ClientListPrefs) =
                AOD.GetCollection(Of ClientListPrefs)(_mongoAtlasOperationsClientListPrefsCollection)
        Dim filter As FilterDefinition(Of ClientListPrefs) = Builders(Of ClientListPrefs).Filter.Eq(Of Integer)("ClientId", clientId)

        Dim workingClient As ClientListPrefs = listDB.Find(filter).FirstOrDefault
        If workingClient Is Nothing Then
            workingClient = New ClientListPrefs
            workingClient._id = ObjectId.GenerateNewId
            workingClient.ClientId = clientId
        End If

        Dim ActiveLists As List(Of Lists) = GetLists()
        Dim ValidLists As New List(Of Integer)

        For Each listid In listPrefs
            Dim found As Boolean = False
            For Each activeListid In ActiveLists
                If listid = activeListid.ListID Then
                    found = True
                    ValidLists.Add(listid)
                End If
            Next
            If found = False Then
                errorMessage = errorMessage + "Could not find an active list with Id " + listid.ToString + ".<br/>"
            End If
        Next
        workingClient.ListPrefs = ValidLists
        workingClient.HasChanged = True

        'Insert a new document if one is not already present.
        Dim replaceOptions As New FindOneAndReplaceOptions(Of ClientListPrefs)
        replaceOptions.IsUpsert = True
        listDB.FindOneAndReplace(filter, workingClient, replaceOptions)
        Return errorMessage

    End Function

    ''' <summary>
    ''' Return a count of all List Prefs
    ''' </summary>
    ''' <returns>Count</returns>
    Public Function RetrieveTotalListPrefsCount() As Integer
        Dim attempts As Integer = 0
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of Lists) =
                atlasOperationsDb.GetCollection(Of Lists)(_mongoAtlasOperationsListsCollection)
                Return AtlasOperationsCollection.CountDocuments(FilterDefinition(Of Lists).Empty)
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While
        Throw New Exception("AtlasOperationsDataRepository:RetrieveTotalListPrefsCount-Failed to get list pref count after " & MAX_RETRIEVE_ATTEMPTS & " attempts.")
    End Function

    Public Function GetLists() As List(Of Lists)
        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim listDB As IMongoCollection(Of Lists) =
                AOD.GetCollection(Of Lists)(_mongoAtlasOperationsListsCollection)
        Dim result = (From list In listDB.AsQueryable() Where list.Active = True).ToList
        Return result
    End Function

    Public Sub RefreshLists(lists As List(Of Lists))
        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim listDB As IMongoCollection(Of Lists) =
                AOD.GetCollection(Of Lists)(_mongoAtlasOperationsListsCollection)

        listDB.DeleteMany(FilterDefinition(Of Lists).Empty)
        listDB.InsertMany(lists)

    End Sub
    Public Function RetrieveECRsWithNoEmailHistory() As List(Of ECRV2)
        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim ecrv2s As List(Of ECRV2)
        Dim ECRCol As IMongoCollection(Of ECRV2) =
            AOD.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)

        ecrv2s = (From selecr In ECRCol.AsQueryable Where selecr.EmailHistory.Count = 0).ToList

        Return ecrv2s

    End Function

    Public Function GetLatestNotAbandonedECR(clientId As Integer) As ECRV2
        Dim ecrv2s As List(Of ECRV2) = RetrieveReadOnlyECRV2sByClientId(clientId)
        Dim orderByDescendingECRs As List(Of ECRV2) = (From ecrv2 As ECRV2 In ecrv2s Where ecrv2.IsAbandoned = False Order By ecrv2.CreatedDate Descending Select ecrv2).ToList()
        If (orderByDescendingECRs.Count = 0) Then
            Return Nothing
        End If
        Return orderByDescendingECRs(0)

    End Function

    Public Function RetrieveOperationsEmails(EmailType As OperationsEmailType) As OperationsEmail
        Dim AOD As IMongoDatabase = GetOperationsDb()
        Dim email As OperationsEmail
        Dim emailCol As IMongoCollection(Of OperationsEmail) =
            AOD.GetCollection(Of OperationsEmail)(_mongoAtlasOperationsEmailsCollection)

        Dim filter As FilterDefinition(Of OperationsEmail) = Builders(Of OperationsEmail).Filter.Eq(Of Integer)("EmailType", CInt(EmailType))
        email = emailCol.Find(filter).FirstOrDefault

        Return email
    End Function

    'Public Shared Function NormalizeStatus(name As String, status As String) As String
    '    Dim log As String = ""
    '    Dim statusIn As String = status
    '    Dim statusOut As String = ""

    '    If status Is Nothing Or status = "" Then
    '        statusOut = ""
    '    Else
    '        Select Case name
    '            Case "tistatus"
    '                Select Case status.ToLower()
    '                    Case "data loaded"
    '                        statusOut = "Data Loaded"
    '                    Case "Data Transferred - CAT"
    '                        statusOut = "Data Loaded"
    '                    Case "survey closed"
    '                        statusOut = "Survey Closed"
    '                    Case "data transferred"
    '                        statusOut = "Data Loaded"
    '                    Case "created"
    '                        statusOut = "Created"
    '                    Case "ready to launch"
    '                        statusOut = "Ready to Launch"
    '                    Case "abandoned - hidden"
    '                        statusOut = "Abandoned"
    '                    Case "survey in progress"
    '                        statusOut = "Survey in Progress"
    '                    Case "setup in progress"
    '                        statusOut = "Setup In Progress"
    '                    Case "opted-out"
    '                        statusOut = "Opted-Out"
    '                    Case "abandoned"
    '                        statusOut = "Abandoned"
    '                    Case "transfer failed - cat"
    '                        statusOut = "Data Loaded"
    '                    Case Else
    '                        statusOut = "ERROR"
    '                End Select
    '            Case "cbstatus"
    '                Select Case status.ToLower()
    '                    Case "in progress"
    '                        statusOut = "In Progress"
    '                    Case "completed"
    '                        statusOut = "Completed"
    '                    Case "created"
    '                        statusOut = "Created"
    '                    Case "opted-out"
    '                        statusOut = "Opted-Out"
    '                    Case "abandoned"
    '                        statusOut = "Abandoned"
    '                    Case "abandoned - hidden"
    '                        statusOut = "Abandoned"
    '                    Case Else
    '                        statusOut = "ERROR"
    '                End Select
    '            Case "castatus"
    '                Select Case status.ToLower()
    '                    Case "in progress"
    '                        statusOut = "In Progress"
    '                    Case "completed"
    '                        statusOut = "Completed"
    '                    Case "created"
    '                        statusOut = "Created"
    '                    Case "opted-out"
    '                        statusOut = "Opted-Out"
    '                    Case "abandoned"
    '                        statusOut = "Abandoned"
    '                    Case "abandoned - hidden"
    '                        statusOut = "Abandoned"
    '                    Case Else
    '                        statusOut = "ERROR"
    '                End Select

    '            Case "cstatus"
    '                Select Case status.ToLower()
    '                    Case "pending"
    '                        statusOut = "Pending"
    '                    Case "certified"
    '                        statusOut = "Certified"
    '                    Case "not certified"
    '                        statusOut = "Not Certified"
    '                    Case "in progress"
    '                        statusOut = ""
    '                    Case Else
    '                        statusOut = "ERROR"
    '                End Select

    '            Case "rstatus"
    '                Select Case status.ToLower()
    '                    Case "requested"
    '                        statusOut = "Requested"
    '                    Case "success"
    '                        statusOut = "Success"
    '                    Case "failure"
    '                        statusOut = "Failure"
    '                    Case Else
    '                        statusOut = "ERROR"
    '                End Select

    '            Case "lstatus"
    '                Select Case status.ToLower()
    '                    Case "pending"
    '                        statusOut = "Pending"
    '                    Case "eligible"
    '                        statusOut = "Eligible"
    '                    Case "not eligible"
    '                        statusOut = "Not Eligible"
    '                    Case Else
    '                        statusOut = "ERROR"
    '                End Select
    '        End Select
    '    End If

    '    Return statusOut

    'End Function


    Public Sub SaveUserEvent(EventSource As UserEventEnums.Source, EventName As UserEventEnums.Name,
               ClientId As Integer, EngagementId As Integer, UserType As UserEventEnums.UserType,
               UserEmail As String, UserSessionId As String, Optional AdditionalInfo As String = "")
        Dim UserEvent = New UserEvent(EventSource, EventName, ClientId, EngagementId, UserType,
               UserEmail, UserSessionId, AdditionalInfo)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim atlasOperationsUserEventsCollection As IMongoCollection(Of UserEvent) =
                atlasOperationsDb.GetCollection(Of UserEvent)(_mongoAtlasOperationsUserEventsCollection)
        If UserEvent.Id = ObjectId.Empty Then
            UserEvent.Id = ObjectId.GenerateNewId
        End If
        atlasOperationsUserEventsCollection.InsertOne(UserEvent)
    End Sub

    ''' <summary>
    ''' Retrieve user events for a given client id
    ''' </summary>
    ''' <param name="clientId">Client Id</param>
    ''' <returns>List of user events</returns>
    Public Function GetUserEventsByClientId(clientId As Integer) As List(Of UserEvent)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim atlasOperationsUserEventsCollection As IMongoCollection(Of UserEvent) =
        atlasOperationsDb.GetCollection(Of UserEvent)(_mongoAtlasOperationsUserEventsCollection)
        Dim clientIdFilter As FilterDefinition(Of UserEvent) = Builders(Of UserEvent).Filter.Eq(Of String)("ClientId", clientId)
        Dim result As List(Of UserEvent) = atlasOperationsUserEventsCollection.Find(clientIdFilter).ToList()
        Return result
    End Function

    ''' <summary>
    ''' Retrieve user events for a given user
    ''' </summary>
    ''' <param name="clientId">Client Id</param>
    ''' <returns>List of user events</returns>
    Public Function GetUserEventByUser(userEmail As String) As List(Of UserEvent)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of UserEvent) =
        atlasOperationsDb.GetCollection(Of UserEvent)(_mongoAtlasOperationsUserEventsCollection)
        Dim userEmailFilter As FilterDefinition(Of UserEvent) = Builders(Of UserEvent).Filter.Eq(Of String)("UserEmail", userEmail)
        Dim result As List(Of UserEvent) = AtlasOperationsCollection.Find(userEmailFilter).ToList()
        Return result
    End Function

    Public Sub SaveDataExtractRequestQueue(ByRef request As DataExtractRequestQueue)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim atlasOperationsDataExtractRequestQueueCollection As IMongoCollection(Of DataExtractRequestQueue) =
                atlasOperationsDb.GetCollection(Of DataExtractRequestQueue)(_mongoDataExtractRequestQueueCollection)
        If request.Id = ObjectId.Empty Then
            request.Id = ObjectId.GenerateNewId
        End If
        'atlasOperationsDataExtractRequestQueueCollection.InsertOne(request)
        Dim filter As FilterDefinition(Of DataExtractRequestQueue) = Builders(Of DataExtractRequestQueue).Filter.Eq(Of ObjectId)("Id", request.Id)

        Dim replaceOptions As New FindOneAndReplaceOptions(Of DataExtractRequestQueue)
        replaceOptions.IsUpsert = True
        atlasOperationsDataExtractRequestQueueCollection.FindOneAndReplace(filter, request, replaceOptions)
        Return
    End Sub

    Public Sub SaveDataExtractRequestQueueV2(ByRef request As DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim requestQueueCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
                atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)
        If request.Id = ObjectId.Empty Then
            request.Id = ObjectId.GenerateNewId
        End If

        Dim filter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Eq(Of ObjectId)("Id", request.Id)

        Dim replaceOptions As New FindOneAndReplaceOptions(Of DataExtractRequestQueueV2)
        replaceOptions.IsUpsert = True
        requestQueueCollection.FindOneAndReplace(filter, request, replaceOptions)
        Return
    End Sub

    Public Function GetAllDataExtractRequestQueue() As List(Of DataExtractRequestQueue)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueue) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueue)(_mongoDataExtractRequestQueueCollection)
        Dim result As List(Of DataExtractRequestQueue) = AtlasOperationsCollection.Find(FilterDefinition(Of DataExtractRequestQueue).Empty).ToList()
        Return result
    End Function

    Public Function GetDataExtractRequestQueueByAffiliates(affiliates As IEnumerable(Of String)) As List(Of DataExtractRequestQueue)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueue) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueue)(_mongoDataExtractRequestQueueCollection)
        Dim affiliatesFilter As FilterDefinition(Of DataExtractRequestQueue) = Builders(Of DataExtractRequestQueue).Filter.In(Of String)("AffiliateId", affiliates)
        Dim result As List(Of DataExtractRequestQueue) = AtlasOperationsCollection.Find(affiliatesFilter).ToList()
        Return result
    End Function

    Public Function GetDataExtractRequestQueueByEmail(email As String) As List(Of DataExtractRequestQueue)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueue) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueue)(_mongoDataExtractRequestQueueCollection)
        Dim emailFilter As FilterDefinition(Of DataExtractRequestQueue) = Builders(Of DataExtractRequestQueue).Filter.Eq(Of String)("RequestorEmail", email)
        Dim result As List(Of DataExtractRequestQueue) = AtlasOperationsCollection.Find(emailFilter).ToList()
        Return result
    End Function

    Public Function GetDataExtractRequestQueueByStatus(status As String) As List(Of DataExtractRequestQueue)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueue) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueue)(_mongoDataExtractRequestQueueCollection)
        Dim statusFilter As FilterDefinition(Of DataExtractRequestQueue) = Builders(Of DataExtractRequestQueue).Filter.Eq(Of String)("Status", status)
        Dim result As List(Of DataExtractRequestQueue) = AtlasOperationsCollection.Find(statusFilter).ToList()
        Return result
    End Function

    Public Function GetDataExtractRequestQueueById(id As String) As DataExtractRequestQueue
        Dim oid As ObjectId = ObjectId.Parse(id)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueue) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueue)(_mongoDataExtractRequestQueueCollection)
        Dim filter As FilterDefinition(Of DataExtractRequestQueue) = Builders(Of DataExtractRequestQueue).Filter.Eq(Of ObjectId)("_id", oid)
        Dim result As DataExtractRequestQueue = AtlasOperationsCollection.Find(filter).SingleOrDefault
        Return result
    End Function

    Public Sub SaveDataExtractRequest(ByRef request As DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim atlasOperationsDataExtractRequestQueueCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
                atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)

        If request.Id = ObjectId.Empty Then
            request.Id = ObjectId.GenerateNewId
        End If

        Dim filter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Eq(Of ObjectId)("Id", request.Id)

        Dim replaceOptions As New FindOneAndReplaceOptions(Of DataExtractRequestQueueV2)
        replaceOptions.IsUpsert = True
        atlasOperationsDataExtractRequestQueueCollection.FindOneAndReplace(filter, request, replaceOptions)
        Return
    End Sub

    Public Function GetDataExtractRequestQueueV2ById(id As String) As DataExtractRequestQueueV2
        Dim oid As ObjectId = ObjectId.Parse(id)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)
        Dim filter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Eq(Of ObjectId)("_id", oid)
        Dim result As DataExtractRequestQueueV2 = AtlasOperationsCollection.Find(filter).SingleOrDefault
        Return result
    End Function

    Public Function GetDataExtractRequestQueueV2ByCountryCodes(countryCodes As IEnumerable(Of String)) As List(Of DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)
        Dim affiliatesFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.In(Of String)("DataRequestParameters.CountryCode", countryCodes)
        Dim result As List(Of DataExtractRequestQueueV2) = AtlasOperationsCollection.Find(affiliatesFilter).ToList()
        Return result
    End Function
    Public Function GetDataExtractRequestQueueV2ByAffiliates(affiliates As IEnumerable(Of String)) As List(Of DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)
        Dim affiliatesFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.In(Of String)("AffiliateId", affiliates)
        Dim sortOrder As SortDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Sort.Descending("RequestDate")
        Dim result As List(Of DataExtractRequestQueueV2) = AtlasOperationsCollection.Find(affiliatesFilter).Sort(sortOrder).ToList()
        Return result
    End Function
    Public Function GetAllDataExtractRequestQueueV2() As List(Of DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)
        Dim result As List(Of DataExtractRequestQueueV2) = AtlasOperationsCollection.Find(FilterDefinition(Of DataExtractRequestQueueV2).Empty).ToList()
        Return result
    End Function

    Public Function GetCreatedOrInProgressDataRequestQueue() As List(Of DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()

        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)

        Dim createdStatusFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Eq(Of Integer)("Status", DataRequestEnums.JobStatus.CREATED)

        Dim inProgressStatusFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Eq(Of Integer)("Status", DataRequestEnums.JobStatus.IN_PROGRESS)

        Dim overallFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Or(createdStatusFilter, inProgressStatusFilter)

        Dim sortOrder As SortDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Sort.Ascending("RequestDate")

        Dim result As List(Of DataExtractRequestQueueV2) = AtlasOperationsCollection.Find(overallFilter).Sort(sortOrder).ToList()

        Return result
    End Function


    Public Function GetLongRunningDataRequestQueue() As List(Of DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()

        Dim AtlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)

        Dim inProgressStatusFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Eq(Of Integer)("Status", DataRequestEnums.JobStatus.IN_PROGRESS)
        Dim requestDateFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Lte(Of Date)("HeartbeatTime", Date.UtcNow.AddHours(-3))

        Dim overallFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.And(inProgressStatusFilter, requestDateFilter)

        Dim sortOrder As SortDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Sort.Ascending("HeartbeatTime")

        Dim result As List(Of DataExtractRequestQueueV2) = AtlasOperationsCollection.Find(overallFilter).Sort(sortOrder).ToList()

        Return result
    End Function

    Public Function GetDataExtractRequestQueueV2ByDateandNotExpired(status As Integer) As List(Of DataExtractRequestQueueV2)
        Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim atlasOperationsCollection As IMongoCollection(Of DataExtractRequestQueueV2) =
        atlasOperationsDb.GetCollection(Of DataExtractRequestQueueV2)(_mongoDataExtractRequestQueueV2Collection)
        Dim expiryTime = New Date
        expiryTime = Date.Now.AddDays(-30)
        Dim requestDateFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Lte(Of Date)("RequestDate", expiryTime)
        Dim statusFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.Ne(Of Integer)("Status", status)
        Dim overallFilter As FilterDefinition(Of DataExtractRequestQueueV2) = Builders(Of DataExtractRequestQueueV2).Filter.And(requestDateFilter, statusFilter)
        Dim result As List(Of DataExtractRequestQueueV2) = atlasOperationsCollection.Find(overallFilter).ToList()
        Return result
    End Function

    Public Function RetrieveECRV2SnippetsByClientId(earliestCreationDate As DateTime, clientId As Integer) As List(Of ECRV2Snippet)
        Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2Snippet) =
                    AtlasOperationsDb.GetCollection(Of ECRV2Snippet)(_mongoAtlasOperationsECRV2SnippetCollection)

        Dim createdDateFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate)
        Dim clientIdFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Eq(Of Integer)("ClientId", clientId)
        Dim overallFilter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.And(createdDateFilter, clientIdFilter)
        Dim sortOrder As SortDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Sort.Descending("CreatedDate")
        Dim ecrv2Snippets As List(Of ECRV2Snippet) = AtlasOperationsCollection.Find(overallFilter).Sort(sortOrder).ToList()

        Return ecrv2Snippets
    End Function

    Public Function RetrieveECRV2SnippetsByClientId(clientId As Integer) As List(Of ECRV2Snippet)
        Dim AtlasOperationsDb As IMongoDatabase = GetOperationsDb()
        Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2Snippet) =
                    AtlasOperationsDb.GetCollection(Of ECRV2Snippet)(_mongoAtlasOperationsECRV2SnippetCollection)

        Dim filter As FilterDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Filter.Eq(Of Integer)("ClientId", clientId)
        Dim sortOrder As SortDefinition(Of ECRV2Snippet) = Builders(Of ECRV2Snippet).Sort.Descending("CreatedDate")
        Dim ecrv2Snippets As List(Of ECRV2Snippet) = AtlasOperationsCollection.Find(filter).Sort(sortOrder).ToList()
        Return ecrv2Snippets
    End Function

    Public Function RetrieveReadOnlyECRV2sByClientId(earliestCreationDate As DateTime, ClientId As Integer, IsAbandoned As Boolean) As List(Of ECRV2)
        Dim attempts As Integer = 0
        Dim ecrv2s = New List(Of ECRV2)
        While (attempts < MAX_RETRIEVE_ATTEMPTS)
            Try
                Dim atlasOperationsDb As IMongoDatabase = GetOperationsDb()
                Dim AtlasOperationsCollection As IMongoCollection(Of ECRV2) =
                    atlasOperationsDb.GetCollection(Of ECRV2)(_mongoAtlasOperationsECRV2Collection)
                Dim createdDateFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Gte(Of Date)("CreatedDate", earliestCreationDate)
                Dim clientIdFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("ClientId", ClientId)
                Dim isAbandonedFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.Eq(Of Integer)("IsAbandoned", IsAbandoned)
                Dim overallFilter As FilterDefinition(Of ECRV2) = Builders(Of ECRV2).Filter.And(createdDateFilter, clientIdFilter, isAbandonedFilter)
                Dim sortOrder As SortDefinition(Of ECRV2) = Builders(Of ECRV2).Sort.Descending("CreatedDate")
                ecrv2s = AtlasOperationsCollection.Find(overallFilter).Sort(sortOrder).ToList
                If ecrv2s IsNot Nothing Then
                    Exit While
                End If
            Catch ex As Exception
            End Try
            attempts = attempts + 1
            If attempts < MAX_RETRIEVE_ATTEMPTS Then
                Threading.Thread.Sleep(SLEEP_INTERVAL)
            End If
        End While

        For Each ecrv2 In ecrv2s
            ecrv2.LockLeaseId = ""
        Next

        Return ecrv2s
    End Function

End Class

Public Enum OperationsEmailType
    <Description("Welcome Sent")> Welcome_Sent = 0 ' Special email type to support Legacy data(Salesforce ECR) and prevent resending 
    <Description("Welcome TICA")> Welcome_TICA = 1 ' TI and Culture Audit
    <Description("Welcome TICB")> Welcome_TICB = 2
    <Description("Welcome CA")> Welcome_CA = 3
    <Description("Welcome CB")> Welcome_CB = 4
    <Description("ReceivedCertification")> ReceivedCertification = 5 ' Notification to client that they have become certified
    <Description("FailedCertification")> FailedCertification = 6 ' Notification to client that they failed certification
    <Description("TIResultsReady")> TIResultsReady = 7 ' Notification to client that TI results are ready to review
    <Description("WelcomeNew")> WelcomeNew = 8 ' Notification to client that TI results are ready to review
    <Description("SendToolkitEmail")> SendToolkitEmail = 9 ' Email badge and useful information on how to use it
    <Description("Template")> Template = 99 ' Master Template that the other templates all use to make our message consistent and pretty
End Enum

Public Class UpdateECRV2PropertiesResult
    Public Property Success As Boolean = False
    Public Property ErrorMessage As String = ""
End Class

Public Class SyncLog
    Public Sub Add(log As String)
        Me.Log.Add(DateTime.UtcNow & ": " & log)
    End Sub
    Public Property Id As ObjectId ' Mongo Id for this Job
    Public Property Type As String = ""
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CreatedDate As Date = DateTime.UtcNow
    Public Property TotalRunTime As String = ""
    Public Property Log As New List(Of String)
End Class

Public Class Affiliate
    Public Property id As ObjectId
    Public Property AffiliateId As String
    Public Property AffiliateName As String
    Public Property StartClientId As Integer
    Public Property EndClientId As Integer
    Public Property DefaultCountryCode As String
    Public Property AllowableCountryCodes As New List(Of String)
End Class

Public Class Country
    Public Property Id As ObjectId
    Public Property CountryCode As String
    Public Property CountryName As String
    Public Property CurrencySymbol As String = ""
    Public Property CurrencyCode As String = ""
    Public Property MinScoreForCertification As Decimal
    Public Property MinEmployeesToLaunchCA As Integer
    Public Property MinEmployeesForListEligibility As Integer
    Public Property FacebookLink As String
    Public Property InstagramLink As String
    Public Property LinkedInLink As String
    Public Property TwitterLink As String
    Public Property BadgePreferences As List(Of BadgePreference)

End Class

Public Class BadgePreference
    Public Property ShortNameForBadge As String
    Public Property BadgeVersion As String
    Public Property BadgeTemplate As String
    Public Property CultureId As String
    Public Property BadgeDefault As String

End Class

Public Class Language
    Public Property id As ObjectId
    Public Property CultureId As String
    Public Property Language As String
    Public Property CountryName As String
End Class

Public Class Currency
    Public Property id As ObjectId
    Public Property Code As String
    Public Property Currency As String
End Class

Public Class ECRIdsModifiedDates
    Public Property id As String
    Public Property LastModifiedDate As DateTime
    Public Property EngagementId As Integer
End Class

Public Class FavoriteClients
    Public Property Id As ObjectId
    Public Property EmailAddress As String
    Public Property Clients As New List(Of Integer)
End Class

Public Class MNC
    Public Property Id As ObjectId
    Public Property CID As Integer
End Class

Public Enum EmailType
    <Description("Invalid")> Invalid = 0
    <Description("Welcome")> Welcome = 1
    <Description("CompleteCB")> CompleteCB = 2 ' Notification to client that they need to finish the CB
    <Description("SetSurveyDates")> SetSurveyDates = 3 ' Notification to client that they need to set their survey dates
    <Description("SurveyLaunch")> SurveyLaunch = 4 ' Notification to client that the survey has launched
    <Description("SurveyResults")> SurveyResults = 5 ' Notification to client that TI results are ready to review
    <Description("ReceivedCertification")> ReceivedCertification = 6 ' Notification to client that they have become certified
    <Description("FailedCertification")> FailedCertification = 7 ' Notification to client that they failed certification
    <Description("SendToolkitEmail")> SendToolkitEmail = 8 ' Email badge and useful information on how to use it
    <Description("AutomatedWelcome")> AutomatedWelcome = 9 ' Automated Welcome Email. Needed to create a new email type to distinguish the triggered welcome from the Automated welcom emails.
    <Description("LicenseeNotification")> LicenseeNotification = 10 ' Licesee Notification for Data Extract.
    <Description("BadgeDelivery")> BadgeDelivery = 11 'BadgeDelivery Email.
End Enum

Public Class EmailTracking
    Public Property Id As ObjectId = Nothing
    Public Property EmailType As EmailType = EmailType.Invalid
    Public Property ClientId As Integer
    Public Property EngagementId As Integer
    Public Property DateTimeSent As DateTime = Nothing
    Public Property Subject As String = ""
    Public Property Body As String = ""
    Public Property Address As String = ""
    Public Property Opened As Boolean = False
    Public Property DateTimeOpened As New List(Of DateTime)
    Public Property IsError As Boolean = False
    Public Property ErrorMessage As String = ""
End Class

Public Class EcrSearchResult
    Public Property ErrorStr As String = ""
    Public Property IsError As Boolean = False
    Public Property Affiliates As New List(Of Affiliate)
    Public Property NumCNameMatches As Integer = 0
    Public Property NumCIDMatches As Integer = 0
    Public Property NumEIDMatches As Integer = 0
End Class


Public Class DataRequestInfo
    Public Property RequestorEmail As String = ""
    Public Property AffiliateId As String = ""
    Public Property UploadedFileName As String = ""
    Public Property RequestorFirstName As String = ""
    Public Property RequestorLastName As String = ""
    Public Property Status As String = ""
    Public Property RequestDate As DateTime
    Public Property ID As Object = Nothing
    Public Property ReportLink As String = ""
    Public Property HeartbeatTime As DateTime
    Public Property EventLog As List(Of String)
End Class

Public Class DataExtractRequestQueue
    Public Sub New()
    End Sub
    Public Sub New(requestInfo As DataRequestInfo)
        Me.RequestorFirstName = requestInfo.RequestorFirstName
        Me.RequestorFirstName = requestInfo.RequestorFirstName
        Me.RequestorEmail = requestInfo.RequestorEmail
        Me.AffiliateId = requestInfo.AffiliateId
        Me.UploadedFileName = requestInfo.UploadedFileName
    End Sub

    Public Property Id As ObjectId
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property RequestDate As Date = DateTime.Now
    Public Property Status As DataRequestEnums.JobStatus = DataRequestEnums.JobStatus.CREATED
    Public Property RequestorFirstName As String = ""
    Public Property RequestorLastName As String = ""
    Public Property RequestorEmail As String = ""
    Public Property AffiliateId As String = ""
    Public Property ReportLink As String = ""
    Public Property UploadedFileName As String = ""
    Public Property HeartbeatTime As New DateTime ' Keep alive written to often so subsequent background threads won't think this job is abandoned
    Public Property EventLog As New List(Of String) ' log of events

    Public Sub SetHeartbeatTime()
        HeartbeatTime = DateTime.UtcNow
    End Sub

    Public Function Log(str As String) As DataExtractRequestQueue
        EventLog.Add(String.Format("{0}:{1}", DateTime.UtcNow.ToString, str))
        HeartbeatTime = DateTime.UtcNow
        Return Me
    End Function
End Class

Public Class DataRequestParameters
    Public Property CountryCode As String = ""
    Public Property RequestReason As String = ""
    Public Property TrustIndexData As String = ""
    Public Property TrustIndexTbq As String = ""
    Public Property CultureBriefDatapoints As String = ""
    Public Property CultureBriefWord As String = ""
    Public Property CultureAuditEssays As String = ""
    Public Property PhotosAndCaptions As String = ""
    Public Property CertificationExpiry As String = ""
    Public Property CertificationDateFrom As String = ""
    Public Property CertificationDateTo As String = ""
    Public Property CompletedCultureAudit As String = ""
    Public Property Industry As String = ""
    Public Property IndustryVertical As String = ""
    Public Property Industry2 As String = ""
    Public Property IndustryVertical2 As String = ""
    Public Property MinimumNumberEmployees As String = ""
    Public Property MaximumNumberEmployees As String = ""
    Public Property DataRequestCompanies As New List(Of DataRequestCompany)
    Public Property Eligibility As String = ""
End Class

Public Class DataRequestCompany
    Public Property ClientId As Integer = 0
    Public Property EngagementId As Integer = 0
    Public Property CompanyName As String = ""
End Class

Public Class DataExtractRequestQueueV2
    Public Sub New()
    End Sub
    Public Sub New(requestInfo As DataRequestInfo, dataRequestParameters As DataRequestParameters)
        Me.RequestorFirstName = requestInfo.RequestorFirstName
        Me.RequestorLastName = requestInfo.RequestorLastName
        Me.RequestorEmail = requestInfo.RequestorEmail
        Me.AffiliateId = requestInfo.AffiliateId
        Me.UploadedFileName = requestInfo.UploadedFileName
        Me.DataRequestParameters = dataRequestParameters
    End Sub

    Public Property Id As ObjectId
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property RequestDate As Date = DateTime.Now
    Public Property Status As DataRequestEnums.JobStatus = DataRequestEnums.JobStatus.CREATED
    Public Property RequestorEmail As String = ""
    Public Property AffiliateId As String = ""
    Public Property ReportLink As String = ""
    Public Property UploadedFileName As String = ""
    Public Property DataRequestParameters As New DataRequestParameters
    Public Property DataOutput As String = ""
    Public Property HeartbeatTime As New DateTime ' Keep alive written to often so subsequent background threads won't think this job is abandoned
    Public Property EventLog As New List(Of String) ' log of events
    Public Property RequestorFirstName As String = ""
    Public Property RequestorLastName As String = ""
    Public Property RecordOfExtract As New List(Of String)

    Public Sub SetHeartbeatTime()
        HeartbeatTime = DateTime.UtcNow
    End Sub

    Public Function Log(str As String) As DataExtractRequestQueueV2
        EventLog.Add(String.Format("{0}:{1}", DateTime.UtcNow.ToString, str))
        HeartbeatTime = DateTime.UtcNow
        Return Me
    End Function
End Class

Public Class DataRequestEnums

    Public Enum JobStatus
        <Description("Created")> CREATED = 0
        <Description("In Progress")> IN_PROGRESS = 1
        <Description("Complete")> COMPLETE = 2
        <Description("Failed")> FAILED = 3
        <Description("Cancelled")> CANCELLED = 4
        <Description("Expired")> Expired = 5
    End Enum

    Public Shared Function GetEnumDescription(ByVal EnumConstant As [Enum]) As String
        Dim fi As FieldInfo = EnumConstant.GetType().GetField(EnumConstant.ToString())
        Dim attr() As DescriptionAttribute =
                      DirectCast(fi.GetCustomAttributes(GetType(DescriptionAttribute),
                      False), DescriptionAttribute())

        If attr.Length > 0 Then
            Return attr(0).Description
        Else
            Return EnumConstant.ToString()
        End If
    End Function

    Public Shared Function GetEnumDescriptions() As List(Of String)
        Dim statusList As List(Of String) = New List(Of String)

        For Each status In [Enum].GetValues(GetType(JobStatus))
            statusList.Add(GetEnumDescription(status))
        Next

        Return statusList
    End Function

    Public Shared Function GetEnumFromDescription(ByVal description As String) As Integer
        For Each field In GetType(JobStatus).GetFields()
            Dim attribute As DescriptionAttribute = TryCast(attribute.GetCustomAttribute(field, GetType(DescriptionAttribute)), DescriptionAttribute)
            If attribute Is Nothing Then Continue For

            If attribute.Description = description Then
                Return CInt(field.GetValue(Nothing))
            End If
        Next

        Return -1
    End Function

End Class

''' <summary>
''' Class to Log data
''' </summary>
''' <remarks></remarks>
Public Class GptwLog
    Public Property _GptwLogConfigInfo As GptwLogConfigInfo

    Public Sub New(lci As GptwLogConfigInfo)
        _GptwLogConfigInfo = lci
    End Sub

    Public Sub LogInformation(EntryDetail As String, Optional SessionId As String = "", Optional ClientId As String = "", Optional EngagementId As String = "", Optional Email As String = "")
        GptwLog("Information", EntryDetail, SessionId, ClientId, EngagementId, Email)
    End Sub

    Public Sub LogWarning(EntryDetail As String, Optional SessionId As String = "", Optional ClientId As String = "", Optional EngagementId As String = "", Optional Email As String = "")
        GptwLog("Warning", EntryDetail, SessionId, ClientId, EngagementId, Email)
    End Sub

    ' TODO - Eventually want to completely remove this method in favor of LogErrorStringOnly and LogErrorWithException
    Public Sub LogError(EntryDetail As String, Optional SessionId As String = "", Optional ClientId As String = "", Optional EngagementId As String = "", Optional Email As String = "")
        LogErrorStringOnly(EntryDetail, SessionId, ClientId, EngagementId, Email)
    End Sub

    Public Sub LogErrorStringOnly(EntryDetail As String, Optional SessionId As String = "", Optional ClientId As String = "", Optional EngagementId As String = "", Optional Email As String = "")
        GptwLog("Error", EntryDetail, SessionId, ClientId, EngagementId, Email)
    End Sub

    Public Sub LogErrorWithException(EntryDetail As String, ex As Exception, Optional SessionId As String = "", Optional ClientId As String = "", Optional EngagementId As String = "", Optional Email As String = "")
        If ex IsNot Nothing And _GptwLogConfigInfo.AppInsightsTelemetryClient IsNot Nothing Then
            ' we can only log exceptions to AppInsights if calling environments passes and exception AND a telemetry client
            _GptwLogConfigInfo.AppInsightsTelemetryClient.TrackException(ex)
        End If
        Dim ExceptionString As String = ""

        Dim WorkingEx As Exception = ex
        While Not (WorkingEx Is Nothing)
            ExceptionString &= WorkingEx.Message & " | " & WorkingEx.StackTrace
            WorkingEx = WorkingEx.InnerException
        End While

        GptwLog("Error", EntryDetail & " ,ex: " & ExceptionString, SessionId, ClientId, EngagementId, Email)
    End Sub

    Private Sub GptwLog(EntryType As String, EntryDetail As String, Optional SessionId As String = "", Optional ClientId As String = "", Optional EngagementId As String = "", Optional Email As String = "")

        Dim alteredEntryDetail As String = EntryDetail
        If EntryDetail = "" Then alteredEntryDetail = "-"


        Dim alteredSessionId As String = SessionId
        If SessionId = "" Then alteredSessionId = "-"

        Dim alteredClientId As String = ClientId
        If ClientId = "" Then alteredClientId = "-1"

        Dim alteredEngagementId As String = EngagementId
        If EngagementId = "" Then alteredEngagementId = "-1"

        Dim alteredEmail As String = Email
        If Email = "" Then alteredEmail = "-"

        If EntryType = "Information" Then
            Serilog.Log.Information("{GPTWMsg} {GPTWcid} {GPTWeid} {GPTWEmail} {GPTWSessId}",
                                    alteredEntryDetail, alteredClientId, alteredEngagementId, alteredEmail, alteredSessionId)
        ElseIf EntryType = "Warning" Then
            Serilog.Log.Warning("{GPTWMsg} {GPTWcid} {GPTWeid} {GPTWEmail} {GPTWSessId}",
                                alteredEntryDetail, alteredClientId, alteredEngagementId, alteredEmail, alteredSessionId)
        ElseIf EntryType = "Error" Then
            Serilog.Log.Error("{GPTWMsg} {GPTWcid} {GPTWeid} {GPTWEmail} {GPTWSessId}",
                              alteredEntryDetail, alteredClientId, alteredEngagementId, alteredEmail, alteredSessionId)
        End If

    End Sub
End Class

Public Class GptwLogDetail
    Public Property EntryType As String
    Public Property Application As String
    Public Property Environment As String
    Public Property EntryDetail As String
    Public Property EntryDateTime As Date = Date.UtcNow
    Public Property ClientId As String = ""
    Public Property EngagementId As String = ""
    Public Property Email As String = ""
    Public Property SessionId As String = ""
End Class

Public Class GptwLogConfigInfo
    Public Property ApplicationName As String
    Public Property ApplicationEnvironment As String
    Public Property MongoConnectionString As String
    Public Property AppInsightsTelemetryClient As TelemetryClient

    ' FOR NEW APP INSIGHTS LOGGING (with telemetry client)
    Public Sub New(AppName As String,
                   AppEnv As String,
                   MongoConnStr As String,
                   ServBusConnStr As String,
                   TelemClient As TelemetryClient)

        ApplicationName = AppName
        ApplicationEnvironment = AppEnv
        MongoConnectionString = MongoConnStr
        AppInsightsTelemetryClient = TelemClient
    End Sub

    ' TODO - Eventually want to completely remove this method in favor of LogErrorStringOnly and LogErrorWithException
    ' OLD WAY OF LOGGING (no telemetry client)
    Public Sub New(AppName As String,
                   AppEnv As String,
                   MongoConnStr As String,
                   ServBusConnStr As String)

        ApplicationName = AppName
        ApplicationEnvironment = AppEnv
        MongoConnectionString = MongoConnStr

    End Sub
End Class
