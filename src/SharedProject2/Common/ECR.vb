
Option Explicit On
Option Strict On

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Public Class ECR

    Public Property Name As String = ""
    <Obsolete("Property has been dropped.")>
    Public Property AccountId As String = "" ' Salesforce Account Id
    <Obsolete("Property has been dropped.")>
    Public Property Account__c As String = "" ' Master/Detail reference to connect ECR to Account

    <Obsolete("Property has been dropped.")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property ReviewCenterPreviewLinkDate As Date? = Nothing
    <Obsolete("Property has been dropped.")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property ReviewCenterExpiryDate As Date? = Nothing
    <Obsolete("Property has been dropped.")>
    Public Property OpsTrackingNotes As String = ""
    <Obsolete("Property has been dropped.")>
    Public Property OpsCeaseCertificationFollowUp As Boolean = False ' Not nullable- will default to FALSE
    <Obsolete("Property has been dropped.")>
    Public Property DoNotContact As Boolean = False ' Not nullable- will default to FALSE
    <Obsolete("Property has been dropped.")>
    Public Property RenewalNotes As String = "" ' Old ReasonForWithdrawal + "-" + ProcessingComments

    <Obsolete("Property has been dropped.")>
    Public Property OverallSurveyResponse As Double? ' Percent(16,2)
    <Obsolete("Property has been dropped.")>
    Public Property ReviewCenterPreviewLink As String

    <Obsolete("Property has been moved to Countries")>
    Public Property CertificationStatus As String = ""
    <Obsolete("Property has been moved to Countries")>
    Public Property TIAverageScore As Double? ' Number(16.2)

    <Obsolete("Property has been dropped.")>
    Public Property ClientType As ECRClientType

    <Obsolete("Property has been dropped.")>
    Public Property CompanyNamePublishing As String   ' Talk to @Todd - when we publish badge do we use the same companynamepublishing for all countries?

    <Obsolete("Property has been dropped.")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CultureAuditDueDate As Date? = Nothing

    Public Property CultureAuditSourceSystemSurveyId As String
    Public Property CultureAuditSSOLink As String
    Public Property CultureAuditStatus As String = ""
    <Obsolete("Property has been dropped.")>
    Public Property CultureAuditType As ECRCultureAuditType
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CACompletionDate As Date? = Nothing
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CBCompletionDate As Date? = Nothing

    Public Property CultureBriefSourceSystemSurveyId As String
    Public Property CultureBriefSSOLink As String
    Public Property CultureBriefStatus As String = ""
    <BsonIgnore> Public ReadOnly Property Days_Survey_Open As Integer?
        Get
            If Me.SurveyOpenDate.HasValue And Me.SurveyCloseDate.HasValue Then
                ' TODO - Fix date diff calculation
                Dim closeDate As DateTime = CDate(Me.SurveyCloseDate)
                Dim openDate As DateTime = CDate(Me.SurveyOpenDate)
                Dim time As TimeSpan = (closeDate.Subtract(openDate))
                Return CInt(time.TotalDays)
                'Return CInt(DateDiff(DateInterval.Day, CDate(Me.SurveyOpenDate), CDate(Me.SurveyCloseDate)))
            End If
        End Get
    End Property
    ''Public Property ECRVersion As Double? = 3.1 ' 3.0 Migrated to new Country object format. 3.1 created with country object
    Public Property ECRVersion As Double? = 4.0 ' 4.0 represents new ECRV2's created after adding the new Customer Journey code
    <Obsolete("Property has been dropped.")>
    Public Property EECountDifferences As Double? ' Formula (Number)
    <Obsolete("Property has been dropped.")>
    Public Property EnableGreatRated As Boolean = False
    <Obsolete("Property has been dropped.")>
    Public Property EngagementType As ECREngagementType
    <Obsolete("Property has been dropped.")>
    Public Property ListType As ECRListType
    <Obsolete("Property has been dropped.")>
    Public Property ListYear As Integer?
    <Obsolete("Property has been dropped.")>
    Public Property NumberEmployeesSurveyed As Integer?
    <Obsolete("Property has been dropped.")>
    Public Property NumberEmployeesSurveyedRevised As Integer?
    <Obsolete("Property has been dropped.")>
    Public Property ReportDeliveryCenterProductName As String

    Public Property ReportDelivery As String
    Public Property PortalSSOLink As String
    <Obsolete("Property has been moved to the Audit History.")>
    Public Property ReportsDeliveredNotes As String = ""
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property ReportDeliveryCenterDeliveryDate As Date? = Nothing

    <Obsolete("Property has been dropped.")>
    Public Property Status As String
    <Obsolete("Property has been dropped.")>
    Public Property SurveyAffiliateId As String

    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property SurveyCloseDate As Date? = Nothing
    <Obsolete("Property has been dropped.")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property TrustIndexDueDate As Date? = Nothing

    Public Property TrustIndexSourceSystemSurveyId As String
    Public Property TrustIndexSSOLink As String
    Public Property NumberOfRespondents As Integer?

    <Obsolete("Property has been dropped.")>
    Public Property SurveyMethod As String

    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property SurveyOpenDate As Date? = Nothing

    <Obsolete("Property has been dropped.")>
    Public Property SurveyResponseRate As Integer?

    Public Property TrustIndexStatus As String = ""
    Public Property SurveyVersionId As Integer?

    <Obsolete("Property has been dropped.")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property SurveyTasksDueDate As Date? = Nothing

    Public Property TrustIndexSurveyType As ECRTrustIndexSurveyType

    <Obsolete("Property has moved to CountriesList/NumberOfEmployeesInCountry.")>
    Public Property TotalEmployees As Integer?
    <Obsolete("Property has been dropped")>
    Public Property TotalEmployeesAtSurveyTime As Integer?
    <Obsolete("Property has moved to CountriesList/MarginOfErrorAt90Percent.")>
    Public Property MarginOfErrorAt90Percent As Decimal?
    <Obsolete("Property has moved to CountriesList/MarginOfErrorAt95Percent.")>
    Public Property MarginOfErrorAt95Percent As Decimal?
    <Obsolete("Property has moved to CountriesList/ListEligibilityStatus.")>
    Public Property ListEligibilityStatus As String = ""
    <Obsolete("Property has moved to CountriesList/ProfilepackageURL.")>
    Public Property ReviewPackageURL As String
    <Obsolete("Property has moved to CountriesList/ProfilePublishStatus.")>
    Public Property ReviewPublishStatus As String = "" ' "Requested", "Success", "Failed"
    <Obsolete("Property has been moved to Countries/ProfilePublishedLink")>
    Public Property ReviewCenterPublishedLink As String
    <Obsolete("Property has been moved to Countries/ProfileRequestedDate")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property ReviewRequestedDate As Date? = Nothing
    <Obsolete("Property has been moved to Countries/ProfilePublishedDate")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property ReviewCenterPublishDate As Date? = Nothing
    <Obsolete("Property has been moved to Countries/CertificationDate")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CertificationDate As Date? = Nothing
    <Obsolete("Property has been moved to Countries/CertificationExpiryDate")>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CertificationExpiryDate As Date? = Nothing

    Public Property Countries As New List(Of CountryData)
End Class

Public Class CountryData
    Public Property CountryCode As String = Nothing
    Public Property NumberOfEmployeesInCountry As Integer? = Nothing
    Public Property NumberOfRespondentsInCountry As Integer? = Nothing
    Public Property TIAverageScore As Decimal? = Nothing
    Public Property MarginOfErrorAt90Percent As Decimal? = Nothing
    Public Property MarginOfErrorAt95Percent As Decimal? = Nothing
    Public Property CertificationStatus As String = ""
    Public Property CertificationDate As Date? = Nothing
    Public Property CertificationExpiryDate As Date? = Nothing
    Public Property ListEligibilityStatus As String = ""
    Public Property ProfilePackageURL As String = Nothing
    Public Property ProfileRequestedDate As Date? = Nothing
    Public Property ProfilePublishStatus As String = ""
    Public Property ProfilePublishedLink As String = Nothing
    Public Property ProfilePublishedDate As Date? = Nothing
    Public Property IsApplyingForCertification As String = "Unknown"
    Public Property DataSliceId As Integer = 0
    Public Property DataSliceFilter As String = ""
End Class
