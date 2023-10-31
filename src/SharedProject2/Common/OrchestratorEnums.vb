Imports System.ComponentModel

''' <summary>
''' Enumerates the types of clients available for an engagement.
''' For example Advisory and Non-Advisory
''' </summary>
''' <remarks></remarks>
Public Enum ECRClientType
    <Description("NONE")> None = 0
    <Description("NON ADVISORY")> NonAdvisory = 1
    <Description("ADVISORY")> Advisory = 2
End Enum

''' <summary>
''' Enumerates the types of engagements that are available.
''' For example a strictly List or Great Rated engagement or a Consulting Only engagement.
''' </summary>
''' <remarks></remarks>
Public Enum ECREngagementType
    <Description("NONE")> None = 0
    <Description("LIST")> List = 1
    <Description("GREAT RATED")> GreatRated = 2
    <Description("ADVISORY")> Advisory = 3
End Enum

''' <summary>
''' Enumerates the types of lists that are available.
''' For example FORTUNE or SME or NONE.
''' </summary>
''' <remarks></remarks>
Public Enum ECRListType
    <Description("NONE")> None = 0
    <Description("FORTUNE")> Fortune = 1
    <Description("SME")> SME = 2
    <Description("SUBSCRIPTION")> Subscription = 3
End Enum

''' <summary>
''' Enumerates the types of trust index surveys that are available for an orchestrated engagement.
''' For example STANDARD or TAILORED or NONE.
''' </summary>
''' <remarks></remarks>
Public Enum ECRTrustIndexSurveyType
    <Description("NONE")> None = 0
    <Description("STANDARD")> Standard = 1
    <Description("TAILORED")> Tailored = 2
    <Description("ULTRATAILORED")> UltraTailored = 3
    <Description("UNLIMITED")> Unlimited = 4
End Enum

''' <summary>
''' Enumerates the types of culture surveys that are available for an orchestrated engagement.
''' For example CULTURE AUDIT or CULTURE BRIEF or NONE.
''' </summary>
''' <remarks></remarks>
Public Enum ECRCultureAuditType
    <Description("NONE")> None = 0
    <Description("CULTURE BRIEF")> CultureBrief = 1
    <Description("CULTURE AUDIT")> CultureAudit = 2
End Enum

''' <summary>
''' Enumerates the available applications that can be controlled by the orchestrator.
''' For example SAM, GreatRated, ...
''' </summary>
''' <remarks></remarks>
Public Enum EngagementActionSourceSystem
    <Description("NONE")> None = 0
    <Description("SAM")> SAM = 1
    <Description("GREAT RATED")> GreatRated = 2
    <Description("EMPRISING")> Emprising = 3
End Enum

''' <summary>
''' Enumerates the types of actions that can be taken using the orchestration APIs
''' For example create, abandon, ...
''' </summary>
''' <remarks></remarks>
Public Enum EngagementActionType
    <Description("NONE")> None = 0
    <Description("CREATE")> CreateSurvey = 1
    <Description("ABANDON - HIDDEN")> AbandonSurveyHidden = 2
    <Description("ABANDON - READ-ONLY")> AbandonSurveyReadOnly = 3
    <Description("NOTIFY CAT - DATA READY")> NotifyCatDataReadyInWarehouse = 4
    <Description("UPDATE CONTACTS")> UpdateContacts = 5
    <Description("ENABLE GREAT RATED")> EnableGreatRated = 6
End Enum

''' <summary>
''' Enumerates the types of surveys for an engagement.
''' For example trust index, culture audit, culture brief, ...
''' </summary>
''' <remarks></remarks>
Public Enum EngagementActionSurveyType
    <Description("NONE")> None = 0
    <Description("CULTURE BRIEF")> CultureBrief = 1
    <Description("CULTURE AUDIT")> CultureAudit = 2
    <Description("TRUST INDEX")> TrustIndex = 3
End Enum


Public Class ObsoletedEngagementUpdateFields

    Public Shared mapping As New Dictionary(Of Integer, String) From {
        {11, "OBS-OrchestratorStatus"},
        {13, "OBS-TrustIndexAffiliateId"},
        {28, "OBS-ReviewCenterPreviewLinkSentDate"},
        {29, "OBS-ReviewCenterPublishDate"},
        {30, "OBS-ReviewCenterExpiryDate"},
        {31, "OBS-ReviewCenterPreviewLink"},
        {32, "OBS-ReviewCenterPublishedLiveLink"},
        {33, "OBS-ReportCenterReportName"},
        {36, "OBS-TotalEmployeesAtSurveyTime"},
        {41, "OBS-TrustIndexSurveyType"},
        {42, "OBS-YearsInBusiness"},
        {43, "OBS-TotalEmployees"},
        {44, "OBS-DoNotContact"},
        {45, "OBS-OpsCeaseCertificationFollowUp"},
        {46, "OBS-OpsTrackingNotes"},
        {47, "OBS-RenewalNotes"},
        {48, "OBS-CompanyNamePublishing"},
        {52, "OBS-OverallSurveyResponse"},
        {53, "UNKNOWN"},
        {58, "OBS(Moved)-ReviewPublishStatus"},
        {59, "OBS-ProfileUrl"},
        {60, "OBS-ReviewPackageURL"},
        {61, "OBS-ReviewRequestedDate"},
        {62, "OBS-ReportsDeliveredNotes"},
        {67, "UNKNOWN"},
        {68, "UNKNOWN"},
        {69, "UNKNOWN"}
        }

End Class



''' <summary>
''' Defines the fields of a Salesforce ECR that can be updated by the Orchestrator.
''' </summary>
''' <remarks></remarks>
Public Enum EngagementUpdateField
    <Description("NONE")> None = 0
    <Description("ModifiedDate")> ModifiedDate = 1
    <Description("TrustIndexSourceSystemSurveyId")> TrustIndexSourceSystemSurveyId = 2
    <Description("TrustIndexSSOLink")> TrustIndexSSOLink = 3
    <Description("TrustIndexStatus")> TrustIndexStatus = 4
    <Description("CultureAuditSourceSystemSurveyId")> CultureAuditSourceSystemSurveyId = 5
    <Description("CultureAuditSSOLink")> CultureAuditSSOLink = 6
    <Description("CultureAuditStatus")> CultureAuditStatus = 7
    <Description("CultureBriefSourceSystemSurveyId")> CultureBriefSourceSystemSurveyId = 8
    <Description("CultureBriefSSOLink")> CultureBriefSSOLink = 9
    <Description("CultureBriefStatus")> CultureBriefStatus = 10
    '<Description("OrchestratorStatus")> OrchestratorStatus = 11
    <Description("ReportCenterSSOLink")> ReportCenterSSOLink = 12
    '<Description("TrustIndexAffiliateId")> TrustIndexAffiliateId = 13
    <Description("TrustIndexSurveyVersionId")> TrustIndexSurveyVersionId = 14
    <Description("TrustIndexSurveyOpenDate")> TrustIndexSurveyOpenDate = 15
    <Description("TrustIndexSurveyCloseDate")> TrustIndexSurveyCloseDate = 16
    <Description("TrustIndexSurveyTasksDueDate")> TrustIndexSurveyTasksDueDate = 17
    <Description("TrustIndexSurveyResponseRate")> TrustIndexSurveyResponseRate = 18
    <Description("TrustIndexNumberEmployeesSurveyed")> TrustIndexNumberEmployeesSurveyed = 19
    <Description("TrustIndexRevisedNumberEmployeesSurveyed")> TrustIndexRevisedNumberEmployeesSurveyed = 20
    <Description("TrustIndexSurveyMethod")> TrustIndexSurveyMethod = 21
    <Description("CultureAuditDueDate")> CultureAuditDueDate = 22
    <Description("CultureAuditExtensionDate")> CultureAuditExtensionDate = 23
    <Description("CultureAuditLink")> CultureAuditLink = 24
    <Description("CultureBriefLink")> CultureBriefLink = 27
    '<Description("ReviewCenterPreviewLinkSentDate")> ReviewCenterPreviewLinkSentDate = 28
    '<Description("ReviewCenterPublishDate")> ReviewCenterPublishDate = 29
    '<Description("ReviewCenterExpiryDate")> ReviewCenterExpiryDate = 30
    '<Description("ReviewCenterPreviewLink")> ReviewCenterPreviewLink = 31
    '<Description("ReviewCenterPublishedLiveLink")> ReviewCenterPublishedLiveLink = 32
    '<Description("ReportCenterReportName")> ReportCenterReportName = 33
    <Description("ReportCenterDeliveryDate")> ReportCenterDeliveryDate = 34
    <Description("ECRVersion")> ECRVersion = 35
    '<Description("TotalEmployeesAtSurveyTime")> TotalEmployeesAtSurveyTime = 36
    <Description("CultureAuditType")> CultureAuditType = 37
    ' for UpdateECRController
    <Description("ClientType")> ClientType = 38
    <Description("EngagementType")> EngagementType = 39
    <Description("ListType")> ListType = 40
    <Description("TrustIndexSurveyType")> TrustIndexSurveyType = 41
    '<Description("YearsInBusiness")> YearsInBusiness = 42
    '<Description("TotalEmployees")> TotalEmployees = 43
    '<Description("DoNotContact")> DoNotContact = 44
    '<Description("OpsCeaseCertificationFollowUp")> OpsCeaseCertificationFollowUp = 45
    '<Description("OpsTrackingNotes")> OpsTrackingNotes = 46
    '<Description("RenewalNotes")> RenewalNotes = 47
    '<Description("CompanyNamePublishing")> CompanyNamePublishing = 48
    <Description("Name")> Name = 49
    <Description("ClientName")> ClientName = 50
    <Description("TIAverageScore")> TIAverageScore = 51
    '<Description("OverallSurveyResponse")> OverallSurveyResponse = 52
    <Description("CertificationStatus")> CertificationStatus = 54
    <Description("ListEligibilityStatus")> ListEligibilityStatus = 55
    <Description("MarginOfErrorAt90Percent")> MarginOfErrorAt90Percent = 56
    <Description("MarginOfErrorAt95Percent")> MarginOfErrorAt95Percent = 57
    '<Description("ReviewPublishStatus")> ReviewPublishStatus = 58
    '<Description("ProfileUrl")> ProfileUrl = 59
    '<Description("ReviewPackageURL")> ReviewPackageURL = 60
    '<Description("ReviewRequestedDate")> ReviewRequestedDate = 61
    '<Description("ReportsDeliveredNotes")> ReportsDeliveredNotes = 62
    <Description("CBCompletionDate")> CBCompletionDate = 63
    <Description("CACompletionDate")> CACompletionDate = 64
    <Description("CertificationDate")> CertificationDate = 65
    <Description("CertificationExpiryDate")> CertificationExpiryDate = 66
    <Description("AffiliateId")> AffiliateId = 70
    <Description("NumberOfRespondents")> NumberOfRespondents = 71

    <Description("ProfilePublishStatus")> ProfilePublishStatus = 72
    <Description("ProfileRequestedDate")> ProfileRequestedDate = 73
    <Description("ProfilePackageURL")> ProfilePackageURL = 74
    <Description("ProfilePublishedLink")> ProfilePublishedLink = 75
    <Description("ProfilePublishedDate")> ProfilePublishedDate = 76
    <Description("IsApplyingForCertification")> IsApplyingForCertification = 77
    <Description("NumberOfRespondentsInCountry")> NumberOfRespondentsInCountry = 78
    <Description("NumberOfEmployeesInCountry")> NumberOfEmployeesInCountry = 79
    <Description("CountryCode")> CountryCode = 80
    <Description("DataSliceId")> DataSliceId = 81
    <Description("DataSliceFilter")> DataSliceFilter = 82
    <Description("IsAbandoned")> IsAbandoned = 83

    <Description("EngagementStatus")> EngagementStatus = 100
    <Description("EngagementHealth")> EngagementHealth = 101
    <Description("RenewalStatus")> RenewalStatus = 102
    <Description("RenewalHealth")> RenewalHealth = 103
    <Description("JourneyStatus")> JourneyStatus = 104
    <Description("JourneyHealth")> JourneyHealth = 105

    <Description("IsCustomerActivated")> IsCustomerActivated = 106
    <Description("CustomerActivationEmprisingAccess")> CustomerActivationEmprisingAccess = 107
    <Description("CustomerActivationBadgeDownload")> CustomerActivationBadgeDownload = 108
    <Description("CustomerActivationShareToolkit")> CustomerActivationShareToolkit = 109
    <Description("CustomerActivationSharableImages")> CustomerActivationSharableImages = 110
    <Description("CustomerActivationReportDownload")> CustomerActivationReportDownload = 111

    <Description("EngagementStatusChangeDate")> EngagementStatusChangeDate = 112
    <Description("RenewalStatusChangeDate")> RenewalStatusChangeDate = 113
    <Description("JourneyStatusChangeDate")> JourneyStatusChangeDate = 114

End Enum

''' <summary>
''' Defines the event sources for an orchestrator notify event.
''' </summary>
''' <remarks></remarks>
Public Enum EngagementNotifySource
    <Description("NONE")> None = 0
    <Description("Trust Index")> TrustIndex = 1
    <Description("Culture Audit")> CultureAudit = 2
    <Description("Culture Brief")> CultureBrief = 3
    <Description("Review Center")> ReviewCenter = 4
    <Description("Report Center")> ReportCenter = 5
    <Description("CAT")> CAT = 6
End Enum

Public Enum NotifySurveyQueueStatus
    <Description("Completed")> Completed = 1
    <Description("Failed")> Failed = 2
    <Description("Skipped")> Skipped = 3
    <Description("Queued")> Queued = 4
End Enum