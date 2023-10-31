Option Explicit On
Option Strict On

Imports System.ComponentModel
Imports System.Reflection
Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Public Enum GenericStatus
    <Description("Invalid")> INVALID = -1
End Enum

Public Enum ActivationStatus
    <Description("Invalid")> INVALID = -1
    <Description("No")> NO = 0
    <Description("Yes")> YES = 1
    <Description("NA")> NA = 2
End Enum

Public Enum RenewalStatus
    <Description("Invalid")> INVALID = -1
    <Description("Too Early")> TOOEARLY = 0
    <Description("Eligible")> ELIGIBLE = 1
    <Description("Renewed")> RENEWED = 2
    <Description("Churned")> CHURNED = 3
    <Description("NA")> NA = 4
End Enum

Public Enum Health
    <Description("Invalid")> INVALID = -1
    <Description("White")> WHITE = 0
    <Description("Green")> GREEN = 1
    <Description("Yellow")> YELLOW = 2
    <Description("Red")> RED = 3
End Enum

Public Enum EngagementStatus
    <Description("Invalid")> INVALID = -1
    <Description("Created")> CREATED = 0
    <Description("Logged In")> LOGGEDIN = 1
    <Description("Dates Selected")> DATESSELECTED = 2
    <Description("Launch Approved")> LAUNCHAPPROVED = 3
    <Description("Survey Live")> SURVEYLIVE = 4
    <Description("Survey Closed")> SURVEYCLOSED = 5
    <Description("Complete")> COMPLETE = 6
    <Description("Incomplete")> INCOMPLETE = 7
End Enum

Public Enum JourneyStatus
    <Description("Invalid")> INVALID = -1
    <Description("Created")> CREATED = 0
    <Description("Logged In")> LOGGEDIN = 1
    <Description("Dates Selected")> DATESSELECTED = 2
    <Description("Launch Approved")> LAUNCHAPPROVED = 3
    <Description("Survey Live")> SURVEYLIVE = 4
    <Description("Survey Closed")> SURVEYCLOSED = 5
    <Description("Complete")> COMPLETE = 6
    <Description("Incomplete")> INCOMPLETE = 7
    <Description("Activated")> ACTIVATED = 8
    <Description("Eligible")> ELIGIBLE = 9
    <Description("Renewed")> RENEWED = 10
    <Description("Churned")> CHURNED = 11
End Enum

' Legacy Enums
Public Enum CERTIFICATION_STATUS
    <Description("")> NOT_SET = 0
    <Description("CERTIFIED")> CERTIFIED = 1
    <Description("NOT CERTIFIED")> NOT_CERTIFIED = 2
End Enum

Public Class ECRV2
    Public Property Id As ObjectId ' Mongo Id for this Job
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CreatedDate As Date = DateTime.Now
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property LastModifiedDate As Date = DateTime.Now
    Public Property AffiliateId As String = ""
    Public Property EngagementId As Integer = 0
    Public Property ClientId As Integer = 0
    Public Property ClientName As String = ""
    Public Property Failure As Boolean = False 'A property update or event failed
    Public Property LockLeaseId As String = ""
    Public Property ECR As New ECR
    <Obsolete("Property has been dropped.")>
    Public Property IsLatestECR As Boolean = True ' newest created date ecr of all the non abandoned ECR's
    Public Property IsAbandoned As Boolean = False
    <Obsolete("Property has been dropped.")>
    Public Property IsArchived As Boolean = False
    Public Property ActionStepList As New List(Of EngagementActionStep)
    Public Property ActionResultUpdateList As New List(Of EngagementActionResultUpdate)
    Public Property AuditHistory As New List(Of String)
    Public Property EmailHistory As New List(Of EmailsSent)
    Public Property ContactEmailAddresses As New List(Of String)
    Public Property ECRViewSalesforceId As String = ""

    Public Sub New()

    End Sub

    Public Property Tier As String
        Get
            Dim result = ""
            Select Case Me.ECR.TrustIndexSurveyType
                Case ECRTrustIndexSurveyType.None
                    result = "No TI"
                Case ECRTrustIndexSurveyType.Standard
                    result = "Assess"
                Case ECRTrustIndexSurveyType.Tailored
                    result = "Analyze"
                Case ECRTrustIndexSurveyType.UltraTailored
                    result = "Analyze"
                Case ECRTrustIndexSurveyType.Unlimited
                    result = "Accelerate"
            End Select
            Return result
        End Get
        Set(value As String)
        End Set
    End Property

    Public Property _EngagementStatus As String = GetEnumDescription(EngagementStatus.INVALID)
    <BsonIgnore> Public Property EngagementStatus As EngagementStatus
        Get
            Return CType(ConvertStringToEnum(Of EngagementStatus)(_EngagementStatus, False), EngagementStatus)
        End Get
        Set(ByVal value As EngagementStatus)
            If (_EngagementStatus <> GetEnumDescription(value)) Then
                _EngagementStatus = GetEnumDescription(value)
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                        {.FieldToUpdate = EngagementUpdateField.EngagementStatus, .NewData = _EngagementStatus})
                Me.EngagementStatusChangeDate = Date.UtcNow
            End If
        End Set
    End Property

    Public Property _EngagementStatusChangeDate As DateTime = DateTime.MinValue
    <BsonIgnore> Public Property EngagementStatusChangeDate As DateTime
        Get
            Return _EngagementStatusChangeDate
        End Get
        Set(value As Date)
            If (_EngagementStatusChangeDate <> value) Then
                _EngagementStatusChangeDate = value
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                        {.FieldToUpdate = EngagementUpdateField.EngagementStatusChangeDate, .NewData = _EngagementStatusChangeDate.ToString})
            End If
        End Set
    End Property

    <BsonIgnore> Public ReadOnly Property EngagementStatusDuration As Integer
        Get
            Return CInt((DateTime.UtcNow - EngagementStatusChangeDate).Days) '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day
        End Get
    End Property

    Public Property _EngagementHealth As String = GetEnumDescription(Health.INVALID)
    <BsonIgnore> Public Property EngagementHealth As Health
        Get
            Return CType(ConvertStringToEnum(Of Health)(_EngagementHealth, False), Health)
        End Get
        Set(value As Health)
            If (_EngagementHealth <> GetEnumDescription(value)) Then
                _EngagementHealth = GetEnumDescription(value)
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                         {.FieldToUpdate = EngagementUpdateField.EngagementHealth, .NewData = _EngagementHealth})
            End If
        End Set

    End Property

    Public Property _RenewalStatus As String = GetEnumDescription(RenewalStatus.INVALID)
    <BsonIgnore> Public Property RenewalStatus As RenewalStatus
        Get
            Return CType(ConvertStringToEnum(Of RenewalStatus)(_RenewalStatus, False), RenewalStatus)
        End Get
        Set(value As RenewalStatus)
            If (_RenewalStatus <> GetEnumDescription(value)) Then
                _RenewalStatus = GetEnumDescription(value)
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                         {.FieldToUpdate = EngagementUpdateField.RenewalStatus, .NewData = _RenewalStatus})
                Me.RenewalStatusChangeDate = Date.UtcNow
            End If
        End Set
    End Property

    Public Property _RenewalStatusChangeDate As DateTime = DateTime.MinValue
    <BsonIgnore> Public Property RenewalStatusChangeDate As DateTime
        Get
            Return _RenewalStatusChangeDate
        End Get
        Set(value As Date)
            If (_RenewalStatusChangeDate <> value) Then
                _RenewalStatusChangeDate = value
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                        {.FieldToUpdate = EngagementUpdateField.RenewalStatusChangeDate, .NewData = _RenewalStatusChangeDate.ToString})
            End If
        End Set
    End Property

    <BsonIgnore> Public ReadOnly Property RenewalStatusDuration As Integer
        Get
            Return CInt((DateTime.UtcNow - RenewalStatusChangeDate).Days) '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day
        End Get
    End Property

    Public Property _RenewalHealth As String = GetEnumDescription(Health.INVALID)
    <BsonIgnore> Public Property RenewalHealth As Health
        Get
            Return CType(ConvertStringToEnum(Of Health)(_RenewalHealth, False), Health)
        End Get
        Set(value As Health)
            If (_RenewalHealth <> GetEnumDescription(value)) Then
                _RenewalHealth = GetEnumDescription(value)
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                            {.FieldToUpdate = EngagementUpdateField.RenewalHealth, .NewData = _RenewalHealth})
            End If
        End Set
    End Property

    Public Property _JourneyStatus As String = GetEnumDescription(JourneyStatus.INVALID)
    <BsonIgnore> Public Property JourneyStatus As JourneyStatus
        Get
            Return CType(ConvertStringToEnum(Of JourneyStatus)(_JourneyStatus, False), JourneyStatus)
        End Get
        Set(value As JourneyStatus)
            If (_JourneyStatus <> GetEnumDescription(value)) Then
                _JourneyStatus = GetEnumDescription(value)
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                        {.FieldToUpdate = EngagementUpdateField.JourneyStatus, .NewData = _JourneyStatus})
                Select Case Me.JourneyStatus
                    Case JourneyStatus.CREATED, JourneyStatus.LOGGEDIN, JourneyStatus.DATESSELECTED, JourneyStatus.LAUNCHAPPROVED,
                         JourneyStatus.SURVEYLIVE, JourneyStatus.SURVEYCLOSED, JourneyStatus.COMPLETE, JourneyStatus.INCOMPLETE,
                         JourneyStatus.ACTIVATED ' Activated doesn't have a change date so we'll use EngagementStatusChangeDate
                        Me.JourneyStatusChangeDate = Me.EngagementStatusChangeDate
                    Case JourneyStatus.ELIGIBLE, JourneyStatus.RENEWED, JourneyStatus.CHURNED
                        Me.JourneyStatusChangeDate = Me.RenewalStatusChangeDate
                    Case JourneyStatus.INVALID
                        Me.JourneyStatusChangeDate = Date.MinValue
                    Case Else
                        Me.JourneyStatusChangeDate = Date.MinValue
                End Select
            End If
        End Set
    End Property

    <BsonIgnore> Public ReadOnly Property JourneyStatusDuration As Integer
        Get
            Return CInt((DateTime.UtcNow - JourneyStatusChangeDate).Days) '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day
        End Get
    End Property

    Public Property _JourneyStatusChangeDate As DateTime = DateTime.MinValue
    <BsonIgnore> Public Property JourneyStatusChangeDate As DateTime
        Get
            Return _JourneyStatusChangeDate
        End Get
        Set(value As Date)
            If (_JourneyStatusChangeDate <> value) Then
                _JourneyStatusChangeDate = value
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                        {.FieldToUpdate = EngagementUpdateField.JourneyStatusChangeDate, .NewData = _JourneyStatusChangeDate.ToString})
            End If
        End Set
    End Property

    Public Property _JourneyHealth As String = GetEnumDescription(Health.INVALID)
    <BsonIgnore> Public Property JourneyHealth As Health
        Get
            Return CType(ConvertStringToEnum(Of Health)(_JourneyHealth, False), Health)
        End Get
        Set(value As Health)
            If (_JourneyHealth <> GetEnumDescription(value)) Then
                _JourneyHealth = GetEnumDescription(value)
                Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With {.FieldToUpdate = EngagementUpdateField.JourneyHealth,
                                              .NewData = _JourneyHealth})
            End If
        End Set
    End Property

    ' When a customer is marked as "NOT CERTIFIED" we should mark
    ' BadgeDownload // ShareToolkit // ShareableImages
    ' as NA to make it obvious that these activation states no longer apply.

    'The ActivationStatus fields have the following values:
    'Initially (blank).

    'We want To track events For:
    '(1) Customer downloaded the badge.
    '(2) Customer shared the toolkit
    '(3) Customer downloaded one of the sharable images
    '(4) Customer clicked link to access Emprising from the portal.
    '(5) Customer downloaded the report (investigate whether there Is a snag here because reports are per-client vs per-engagement).

    'We should only track these events For customer users.  I think we may already be tracking these events And might need To Do a
    'slight expansion.  We need To ensure that the tracking includes user / Date / time / cid / eid.

    'In the ECR we should add fields for CustomerActivationEmprisingAccess // CustomerActivationBadgeDownload //
    'CustomerActivationShareToolkit // CustomerActivationSharableImages // CustomerActivationReportDownload.  Each of these fields
    'should be blank initially.

    'There Is also a field for IsCustomerActivated which should be set to "Y" when one Or more activation activities are completed.
    'Initially this field should be blank.

    'When a customer Is marked as "NOT CERTIFIED" we should mark BadgeDownload // ShareToolkit // ShareableImages as NA to make it
    'obvious that these activation states no longer apply.

    'When EngagementStatus = COMPLETE And the customer takes one of the above 5 actions we should set the appropriate field to "Y"
    'to indicate that the customer Is activated for this item.  Note that we can record actions for GPTW employees but we should
    'Not set the activation status to "Y" for actions taken by employees.

    'Need to see if there Is a wrinkle related to downloading reports because report page Is by CID.  In this case the best approach
    'Is probably to notify the server that the customer just downloaded a report for CID = 123.  At this point the server needs to
    'look up the most recent engagement where EngagementStatus = COMPLETE because it won't directly have the EID.  If an EID cannot
    'be found the action can be recorded but there will be no EID to set the ReportDownload = True flag (which is fine).

    Public Property _IsCustomerActivated As Boolean = False
    <BsonIgnore>
    Public Property IsCustomerActivated As Boolean
        Get
            Return _IsCustomerActivated
        End Get
        Set(value As Boolean)
            _IsCustomerActivated = value
            Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
                {.FieldToUpdate = EngagementUpdateField.IsCustomerActivated, .NewData = value.ToString})
        End Set
    End Property

    Public Property _CustomerActivationEmprisingAccess As String = GetEnumDescription(ActivationStatus.NO)
    <BsonIgnore>
    Public Property CustomerActivationEmprisingAccess As ActivationStatus
        Get
            Return CType(ConvertStringToEnum(Of ActivationStatus)(_CustomerActivationEmprisingAccess, False), ActivationStatus)
        End Get
        Set(value As ActivationStatus)
            If (value = ActivationStatus.YES And Me.EngagementStatus <> EngagementStatus.COMPLETE) Then
                Return
            End If
            _CustomerActivationEmprisingAccess = GetEnumDescription(value)
            Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
               {.FieldToUpdate = EngagementUpdateField.CustomerActivationEmprisingAccess, .NewData = GetEnumDescription(value)})
            UpdateActivationStatus()
        End Set
    End Property

    Public Property _CustomerActivationBadgeDownload As String = GetEnumDescription(ActivationStatus.NO)
    <BsonIgnore>
    Public Property CustomerActivationBadgeDownload As ActivationStatus
        Get
            Return CType(ConvertStringToEnum(Of ActivationStatus)(_CustomerActivationBadgeDownload, False), ActivationStatus)
        End Get
        Set(value As ActivationStatus)
            If (value = ActivationStatus.YES And Me.EngagementStatus <> EngagementStatus.COMPLETE) Then
                Return
            End If
            _CustomerActivationBadgeDownload = GetEnumDescription(value)
            Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With
            {.FieldToUpdate = EngagementUpdateField.CustomerActivationBadgeDownload, .NewData = GetEnumDescription(value)})
            UpdateActivationStatus()
        End Set
    End Property

    Public Property _CustomerActivationShareToolkit As String = GetEnumDescription(ActivationStatus.NO)
    <BsonIgnore>
    Public Property CustomerActivationShareToolkit As ActivationStatus
        Get
            Return CType(ConvertStringToEnum(Of ActivationStatus)(_CustomerActivationShareToolkit, False), ActivationStatus)
        End Get
        Set(value As ActivationStatus)
            If (value = ActivationStatus.YES And Me.EngagementStatus <> EngagementStatus.COMPLETE) Then
                Return
            End If
            _CustomerActivationShareToolkit = GetEnumDescription(value)
            Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With {.FieldToUpdate = EngagementUpdateField.CustomerActivationShareToolkit,
                                                  .NewData = GetEnumDescription(value)})
            UpdateActivationStatus()
        End Set
    End Property

    Public Property _CustomerActivationSharableImages As String = GetEnumDescription(ActivationStatus.NO)
    <BsonIgnore>
    Public Property CustomerActivationSharableImages As ActivationStatus
        Get
            Return CType(ConvertStringToEnum(Of ActivationStatus)(_CustomerActivationSharableImages, False), ActivationStatus)
        End Get
        Set(value As ActivationStatus)
            If (value = ActivationStatus.YES And Me.EngagementStatus <> EngagementStatus.COMPLETE) Then
                Return
            End If
            _CustomerActivationSharableImages = GetEnumDescription(value)
            Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With {.FieldToUpdate = EngagementUpdateField.CustomerActivationSharableImages,
                                                  .NewData = GetEnumDescription(value)})
            UpdateActivationStatus()
        End Set
    End Property

    Public Property _CustomerActivationReportDownload As String = GetEnumDescription(ActivationStatus.NO)
    <BsonIgnore>
    Public Property CustomerActivationReportDownload As ActivationStatus
        Get
            Return CType(ConvertStringToEnum(Of ActivationStatus)(_CustomerActivationReportDownload, False), ActivationStatus)
        End Get
        Set(value As ActivationStatus)
            If (value = ActivationStatus.YES And Me.EngagementStatus <> EngagementStatus.COMPLETE) Then
                Return
            End If
            _CustomerActivationReportDownload = GetEnumDescription(value)
            Me.ActionResultUpdateList.Add(New EngagementActionResultUpdate With {.FieldToUpdate = EngagementUpdateField.CustomerActivationReportDownload,
                                                  .NewData = GetEnumDescription(value)})
            UpdateActivationStatus()
        End Set
    End Property

    Public Sub UpdateActivationStatus()
        Me.IsCustomerActivated = (Me.CustomerActivationEmprisingAccess = ActivationStatus.YES Or Me.CustomerActivationBadgeDownload = ActivationStatus.YES Or Me.CustomerActivationShareToolkit = ActivationStatus.YES Or
            Me.CustomerActivationSharableImages = ActivationStatus.YES Or Me.CustomerActivationReportDownload = ActivationStatus.YES)
    End Sub

    Public Sub UpdateStatusOnSave()

        UpdateEngagementHealth()
        UpdateRenewalHealth()
        UpdateJourneyStatus()
        UpdateJourneyHealth()

    End Sub

    Public Sub UpdateEngagementHealth()

        If (EngagementStatus = EngagementStatus.INVALID) Then
            Return
        End If

        'The EngagementHealth field has the following values:
        'We will apply engagement health (red, yellow, green) To Each Of the engagement statuses (CREATED, LOGGED In, DATES SELECTED...)
        'And will show both the engagement state And the engagement health On the engagement dashboard.

        Select Case EngagementStatus
            Case EngagementStatus.CREATED
                'If the ECR was created
                '<= 7 days ago --> GREEN
                'Between 7 And 14.999 days ago --> YELLOW
                '>= 15 days ago --> RED
                If (Me.EngagementStatusDuration <= 7) Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If
                If (Me.EngagementStatusDuration <= 15) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.RED
            Case EngagementStatus.LOGGEDIN
                'If duration In LOGGED In stage
                '<= 14 days --> GREEN
                'Between 14 And 20.999 days --> YELLOW
                '>= 21 days --> RED
                If (Me.EngagementStatusDuration <= 14) Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If
                If (Me.EngagementStatusDuration <= 21) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.RED
            Case EngagementStatus.DATESSELECTED
                'If (SURVEY START/OPEN DATE - TODAY)
                '<= 3 --> RED
                '<= 10 --> YELLOW
                'Else GREEN
                Dim dateOfSurveyOpen As Date

                If Me.ECR.SurveyOpenDate Is Nothing Then
                    Me.AuditHistory.Add(String.Format("EngagementHealth/EngagementStatus:DatesSelected- Logic failed. Can't calculate health because Me.ECR.SurveyOpenDate is Nothing."))
                    Me.EngagementHealth = Health.INVALID
                    Return
                End If

                dateOfSurveyOpen = Date.Parse(Me.ECR.SurveyOpenDate.ToString)

                Dim daysUntilSurveyOpens = (dateOfSurveyOpen - DateTime.UtcNow).Days '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day

                If (daysUntilSurveyOpens <= 3) Then
                    Me.EngagementHealth = Health.RED
                    Return
                End If
                If (daysUntilSurveyOpens <= 10) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.GREEN
            Case EngagementStatus.LAUNCHAPPROVED
                'If (SURVEY START/OPEN DATE - TODAY)
                '>= 4 --> GREEN
                '< 4 --> YELLOW
                If Me.ECR.SurveyOpenDate Is Nothing Then
                    Me.AuditHistory.Add(String.Format("EngagementHealth/EngagementStatus:LaunchApproved- Logic failed. Can't calculate health because Me.ECR.SurveyOpenDate is Nothing."))
                    Me.EngagementHealth = Health.INVALID
                    Return
                End If
                Dim dateOfSurveyOpen = Date.Parse(Me.ECR.SurveyOpenDate.ToString)
                Dim daysUntilSurveyOpens = (dateOfSurveyOpen - DateTime.UtcNow).Days '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day

                If (daysUntilSurveyOpens < 4) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.GREEN
            Case EngagementStatus.SURVEYLIVE
                '    If list / cert eligibility = met In all countries --> GREEN
                'Else (If Not list/cert eligible In all countries) And num_days_survey_open
                ' <= 10 --> GREEN
                '  Between 10 And 18.999 --> YELLOW
                '  >= 19 --> RED

                Dim eligibility = HasListAndCertEligibilityBeenMetInAllCountries()

                If eligibility Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If

                If Me.ECR.SurveyOpenDate Is Nothing Then
                    Me.AuditHistory.Add(String.Format("EngagementHealth/EngagementStatus:LaunchApproved- Logic failed. Can't calculate health because Me.ECR.SurveyOpenDate is Nothing."))
                    Me.EngagementHealth = Health.INVALID
                    Return
                End If

                Dim dateOfSurveyOpen = Date.Parse(Me.ECR.SurveyOpenDate.ToString)
                Dim daysSinceSurveyOpened = (DateTime.UtcNow - dateOfSurveyOpen).Days '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day

                If (daysSinceSurveyOpened <= 10) Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If
                If (daysSinceSurveyOpened < 19) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.RED
            Case EngagementStatus.SURVEYCLOSED
                '    If duration In SURVEY CLOSED stage
                '<= 14 days --> GREEN
                'Between 14 And 27.999 days --> YELLOW
                '>= 28 days --> RED
                If (Me.EngagementStatusDuration <= 14) Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If
                If (Me.EngagementStatusDuration < 28) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.RED
            Case EngagementStatus.COMPLETE
                'If (count of completed activation activities > 0) --> WHITE
                'ElseIf (duration in EngagementStatus = COMPLETE stage)
                '<= 7 days --> GREEN
                'Between 7 And 20.999 days --> RED
                'Between 21 And 59.999 days --> YELLOW
                '>= 60 days --> WHITE

                Dim countOfCompletedActivationActivities = GetCountOfCompletedActivationActivities()

                If (countOfCompletedActivationActivities > 0) Then
                    Me.EngagementHealth = Health.WHITE
                    Return
                End If

                If (Me.EngagementStatusDuration <= 7) Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If
                If (Me.EngagementStatusDuration < 21) Then
                    Me.EngagementHealth = Health.RED
                    Return
                End If
                If (Me.EngagementStatusDuration < 60) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.WHITE
            Case EngagementStatus.INCOMPLETE 'INCOMPLETE
                '(same as above for complete)
                '[CS will have the ability to filter for INCOMPLETE customers for follow-up using the dashboard And it won't
                'be a surprise that customers end up on the incomplete board because we took that action]

                Dim countOfCompletedActivationActivities = GetCountOfCompletedActivationActivities()

                If (countOfCompletedActivationActivities > 0) Then
                    Me.EngagementHealth = Health.WHITE
                    Return
                End If

                If (Me.EngagementStatusDuration <= 7) Then
                    Me.EngagementHealth = Health.GREEN
                    Return
                End If
                If (Me.EngagementStatusDuration < 21) Then
                    Me.EngagementHealth = Health.RED
                    Return
                End If
                If (Me.EngagementStatusDuration < 60) Then
                    Me.EngagementHealth = Health.YELLOW
                    Return
                End If
                Me.EngagementHealth = Health.WHITE
            Case Else
                Me.AuditHistory.Add(String.Format("UpdateEngagementHealth:Found unexpected _EngagementStatus:{0}", _EngagementStatus))
        End Select

    End Sub

    Public Sub UpdateRenewalHealth()

        'Initially renewal health Is Invalid.
        If (RenewalStatus = RenewalStatus.INVALID) Then
            Me.RenewalHealth = Health.INVALID
            Return
        End If

        ' Renewed Or Churned: White
        If (RenewalStatus = RenewalStatus.CHURNED Or RenewalStatus = RenewalStatus.RENEWED) Then
            Me.RenewalHealth = Health.WHITE
            Return
        End If

        'PENDING:
        'Green until it Is 335 days old (eg. until 335 days after creation).
        'Yellow between 335 And 390 days
        'Red when it Is older than 390 days

        Dim date335DaysAgo As DateTime = DateTime.UtcNow.AddDays(-335)
        Dim date390DaysAgo As DateTime = DateTime.UtcNow.AddDays(-390)

        If (Me.CreatedDate > date335DaysAgo) Then
            Me.RenewalHealth = Health.GREEN
            Return
        End If

        If (Me.CreatedDate > date390DaysAgo) Then
            Me.RenewalHealth = Health.YELLOW
            Return
        End If

        Me.RenewalHealth = Health.RED

    End Sub

    Public Sub UpdateJourneyStatus()

        'JourneyStatus needs to be a New field that aggregate several of the underlying statuses into a single field for the ECR.
        'It Is Eligible / Renewed / Churned if those statuses are set (RenewalStatus)
        'Else it Is Activated If an activation activity has occurred (IsCustomerActivated = Y)
        'Else it Is EngagementStatus

        If (Me.RenewalStatus = RenewalStatus.ELIGIBLE) Then
            Me.JourneyStatus = JourneyStatus.ELIGIBLE
            Return
        End If
        If (Me.RenewalStatus = RenewalStatus.RENEWED) Then
            Me.JourneyStatus = JourneyStatus.RENEWED
            Return
        End If
        If (Me.RenewalStatus = RenewalStatus.CHURNED) Then
            Me.JourneyStatus = JourneyStatus.CHURNED
            Return
        End If
        If (Me.IsCustomerActivated) Then
            Me.JourneyStatus = JourneyStatus.ACTIVATED '(White)
            Return
        End If

        ' if were here we want to basically return the EngagementStatus as a JourneyStatus
        ' Since the JourneyStatus enums have a text for text match of the EngagementStatus enums
        ' we can match on text and get the JourneyStatus enum by matching on the text description
        'Me.JourneyStatus = [Enum].Parse(GetType(JourneyStatus), GetEnumDescription(Me.EngagementStatus))

        Select Case Me.EngagementStatus
            Case EngagementStatus.INVALID
                Me.JourneyStatus = JourneyStatus.INVALID
            Case EngagementStatus.CREATED
                Me.JourneyStatus = JourneyStatus.CREATED
            Case EngagementStatus.LOGGEDIN
                Me.JourneyStatus = JourneyStatus.LOGGEDIN
            Case EngagementStatus.DATESSELECTED
                Me.JourneyStatus = JourneyStatus.DATESSELECTED
            Case EngagementStatus.LAUNCHAPPROVED
                Me.JourneyStatus = JourneyStatus.LAUNCHAPPROVED
            Case EngagementStatus.SURVEYLIVE
                Me.JourneyStatus = JourneyStatus.SURVEYLIVE
            Case EngagementStatus.SURVEYCLOSED
                Me.JourneyStatus = JourneyStatus.SURVEYCLOSED
            Case EngagementStatus.COMPLETE
                Me.JourneyStatus = JourneyStatus.COMPLETE
            Case EngagementStatus.INCOMPLETE
                Me.JourneyStatus = JourneyStatus.INCOMPLETE
        End Select

    End Sub

    Public Sub UpdateJourneyHealth()

        ' use health of associated system. so for RenewalStatus use the RenewalHealth logic

        If (Me.RenewalStatus = RenewalStatus.ELIGIBLE) Then
            Me.JourneyHealth = RenewalHealth
            Return
        End If
        If (Me.RenewalStatus = RenewalStatus.RENEWED) Then
            Me.JourneyHealth = RenewalHealth
            Return
        End If
        If (Me.RenewalStatus = RenewalStatus.CHURNED) Then
            Me.JourneyHealth = RenewalHealth
            Return
        End If
        If (Me.IsCustomerActivated) Then
            Me.JourneyHealth = Health.GREEN
            Return
        End If
        Me.JourneyHealth = Me.EngagementHealth

    End Sub

    ' Called from NotifySurvey when rolling back status from "ready to launch" (Scheduled) to "setup in progress" (Draft) so set engagment status back to DatesSelected
    Public Sub RollingBackTIStatus(newStatus As String)
        If (Me.ECR.TrustIndexStatus.ToLower = "ready to launch" And newStatus.ToLower = "setup in progress") Then
            Me.EngagementStatus = EngagementStatus.DATESSELECTED
        End If
    End Sub

    ' Called from NotifySurvey when we are notified that a date (SurveyOpen/SurveyClosed) has been Selected
    ' TBD- This should be removed and done within a (SurveyOpen/SurveyClosed) setter method 
    Public Sub DateSelected()
        If (Me.EngagementStatus = EngagementStatus.CREATED Or
            Me.EngagementStatus = EngagementStatus.LOGGEDIN) Then
            Me.EngagementStatus = EngagementStatus.DATESSELECTED
        End If
    End Sub

    ' Called from NotifySurvey when LaunchApproved has occurred
    ' TBD- This should be removed and done within a TrustIndexStatus setter method 
    Public Sub LaunchApproved()
        If (Me.EngagementStatus = EngagementStatus.CREATED Or
            Me.EngagementStatus = EngagementStatus.LOGGEDIN Or
            Me.EngagementStatus = EngagementStatus.DATESSELECTED) Then
            Me.EngagementStatus = EngagementStatus.LAUNCHAPPROVED
        End If
    End Sub

    ' Called from NotifySurvey when the survey has gone live
    ' TBD- This should be removed and done within a TrustIndexStatus setter method 
    Public Sub SurveyLive()
        If (Me.EngagementStatus = EngagementStatus.CREATED Or
            Me.EngagementStatus = EngagementStatus.LOGGEDIN Or
            Me.EngagementStatus = EngagementStatus.DATESSELECTED Or
            Me.EngagementStatus = EngagementStatus.LAUNCHAPPROVED) Then
            Me.EngagementStatus = EngagementStatus.SURVEYLIVE
        End If
    End Sub

    ' Called from NotifySurvey when the survey has closed  AND TIDataNotify in case we didn't receive the CloseSurvey status from Emprising
    ' TBD- This should be removed and done within a TrustIndexStatus setter method 
    Public Sub SurveyClosed()

        'SURVEY CLOSED - If the current status Is 
        '            (CREATED // LOGGED IN // DATES SELECTED // LAUNCH APPROVED // SURVEY LIVE) And 
        'CB Status = (CREATED // IN PROGRESS) And TI Status Is changed to SURVEY CLOSED then we should
        '            set the engagement status to SURVEY CLOSED.

        'COMPLETE -There are TWO ways that we can enter this state.  FIRST WAY: A notification from 
        '            Emprising can trigger this status if the current state Is (CREATED // LOGGED IN
        '            // DATES SELECTED // LAUNCH APPROVED // SURVEY LIVE) And CB Status = (COMPLETED)
        '            And we receive a notification from Emprising that TI Status = SURVEY CLOSED then
        ' we should set the engagement status to COMPLETE.  SECOND WAY: A notification from the CB can
        '            trigger this status if the current state Is SURVEY CLOSED And we receive a notification
        '            From the CB that CB Status = COMPLETED then we should set the engagement status to
        '                COMPLETE. (We need to talk about ECR logic because ideally this Is in the core
        '            And order of operations doesn't matter when it comes to entering the COMPLETE state).

        'INCOMPLETE -Similarly to COMPLETE there are several patterns which trigger this status.
        'TI = Complete // CB = Opt Out (Or Abandon)
        'TI = Opt Out (Or Abandon) // CB = Complete
        'TI = Opt Out (Or Abandon) // CB = Opt Out (Or Abandon)

        If (Me.EngagementStatus = EngagementStatus.CREATED Or
            Me.EngagementStatus = EngagementStatus.LOGGEDIN Or
            Me.EngagementStatus = EngagementStatus.DATESSELECTED Or
            Me.EngagementStatus = EngagementStatus.LAUNCHAPPROVED Or
            Me.EngagementStatus = EngagementStatus.SURVEYLIVE) Then

            'CACBStatusValues = {"", "Created", "In Progress", "Completed", "Abandoned", "Opted-Out"}
            Select Case Me.ECR.CultureBriefStatus.ToUpper
                Case "CREATED"
                    Me.EngagementStatus = EngagementStatus.SURVEYCLOSED
                Case "IN PROGRESS"
                    Me.EngagementStatus = EngagementStatus.SURVEYCLOSED
                Case "COMPLETED"
                    Me.EngagementStatus = EngagementStatus.COMPLETE
                Case "ABANDONED"
                    Me.EngagementStatus = EngagementStatus.INCOMPLETE
                Case "OPTED-OUT"
                    Me.EngagementStatus = EngagementStatus.INCOMPLETE
                Case Else
                    Me.EngagementStatus = EngagementStatus.INVALID
            End Select
        End If
    End Sub

    ' NO TI case only supposed to be called for Manual Dalo
    Public Sub ManualDaloSurveyComplete()
        If Me.ECR.TrustIndexSurveyType = ECRTrustIndexSurveyType.None Then
            If (Me.EngagementStatus = EngagementStatus.CREATED Or
             Me.EngagementStatus = EngagementStatus.LOGGEDIN Or
             Me.EngagementStatus = EngagementStatus.DATESSELECTED Or
             Me.EngagementStatus = EngagementStatus.LAUNCHAPPROVED Or
             Me.EngagementStatus = EngagementStatus.SURVEYLIVE) Then

                'CACBStatusValues = {"Completed", "Abandoned", "Opted-Out"}
                Select Case Me.ECR.CultureBriefStatus.ToUpper
                    Case "COMPLETED"
                        Me.EngagementStatus = EngagementStatus.COMPLETE
                    Case "ABANDONED"
                        Me.EngagementStatus = EngagementStatus.INCOMPLETE
                    Case "OPTED-OUT"
                        Me.EngagementStatus = EngagementStatus.INCOMPLETE
                End Select
            End If
        End If
    End Sub

    ' Called from the CultureSurveyController's SaveCultureSurveyData method
    ' TBD- This should be removed and done within a CertificationStatus setter method 
    Public Sub CultureBriefStatusChangingToCompleted()
        '   A notification from the CB can
        '   trigger this status if the current state Is SURVEY CLOSED And we receive a notification
        '   From the CB that CB Status = COMPLETED then we should set the engagement status to COMPLETE. 
        '   No TI case will only have either logged in or created status since this goes thru Manual Dalo process
        If Me.ECR.TrustIndexSurveyType = ECRTrustIndexSurveyType.None Then
            If (Me.EngagementStatus = EngagementStatus.CREATED Or Me.EngagementStatus = EngagementStatus.LOGGEDIN) And Me.ECR.TrustIndexStatus.ToUpper = "DATA LOADED" Then
                Me.EngagementStatus = EngagementStatus.COMPLETE
            End If
        Else
            If (Me.EngagementStatus = EngagementStatus.SURVEYCLOSED) And Me.ECR.CultureBriefStatus.ToUpper = "COMPLETED" Then
                Me.EngagementStatus = EngagementStatus.COMPLETE
            End If
        End If
    End Sub

    ' Called from the PortalController's GetEngagementInfo method
    Public Sub PortalEngagementPageViewed()
        'NOTE: This was originally a login event however since there is no eid known at that point we changed this to an engagement page view event
        'essentially because the eid is known at that point.
        'LOGGED IN - There Is existing functionality that records/updates the last sign-in date for a customer user as they sign in to the portal.
        ' Since this additional functionality doesn't have access to the eid we decided that when the Engagement Page is shown (and we have the eid)
        ' the call to the controller to get the data to show the Engagement Page will call this method.
        'We will extend this functionality to call a method on the ECR to indicate that a client user has signed in.  If the current status of the
        'ECR Is CREATED And a customer user signs in, we should move the status to LOGGED IN.  GPTW employees signing in to the portal should Not
        'trigger this change.  This logic should be encapsulated in the ECR.

        If (Me.EngagementStatus = EngagementStatus.CREATED) Then
            Me.EngagementStatus = EngagementStatus.LOGGEDIN
        End If
    End Sub

    ' Called from the PortalController's OptOutAbandonSurvey method
    Public Sub TICACBOptedOutOrAbandoned()
        'INCOMPLETE -Similarly to COMPLETE there are several patterns which trigger this status.
        'TI = Complete ("Survey closed", "data transfered", "data loaded") And CB = Opt Out (Or Abandon)
        'TI = Opt Out (Or Abandon) And CB = Complete
        'TI = Opt Out (Or Abandon) And CB = Opt Out (Or Abandon)
        'For "NO TI", CB = opt-out (Or Abandon)
        If Me.ECR.TrustIndexSurveyType = ECRTrustIndexSurveyType.None Then
            If (Me.ECR.CultureBriefStatus.ToUpper = "OPTED-OUT" Or Me.ECR.CultureBriefStatus.ToUpper = "ABANDONED") Then
                Me.EngagementStatus = EngagementStatus.INCOMPLETE
            End If
        Else
            If (Me.ECR.TrustIndexStatus.ToUpper = "SURVEY CLOSED" Or Me.ECR.TrustIndexStatus.ToUpper = "DATA TRANSFERED" Or Me.ECR.TrustIndexStatus.ToUpper = "DATA LOADED") And
            (Me.ECR.CultureBriefStatus.ToUpper = "OPTED-OUT" Or Me.ECR.CultureBriefStatus.ToUpper = "ABANDONED") Then
                Me.EngagementStatus = EngagementStatus.INCOMPLETE
            End If
            If (Me.ECR.TrustIndexStatus.ToUpper = "OPTED-OUT" Or Me.ECR.TrustIndexStatus.ToUpper = "ABANDONED") And
            Me.ECR.CultureBriefStatus.ToUpper = "COMPLETED" Then
                Me.EngagementStatus = EngagementStatus.INCOMPLETE
            End If
            If (Me.ECR.TrustIndexStatus.ToUpper = "OPTED-OUT" Or Me.ECR.TrustIndexStatus.ToUpper = "ABANDONED") And
                (Me.ECR.CultureBriefStatus.ToUpper = "OPTED-OUT" Or Me.ECR.CultureBriefStatus.ToUpper = "ABANDONED") Then
                Me.EngagementStatus = EngagementStatus.INCOMPLETE
            End If
        End If
    End Sub

    ' Called from   TBD!
    ' TBD- This should be removed and done within a Certification setter method 
    Public Sub TakeNotCertifedActions()
        'When a ECR Is marked as "NOT CERTIFIED" we should mark BadgeDownload // ShareToolkit // ShareableImages as NA
        'to make it obvious that these activation states no longer apply.
        Me.CustomerActivationBadgeDownload = ActivationStatus.NA
        Me.CustomerActivationShareToolkit = ActivationStatus.NA
        Me.CustomerActivationSharableImages = ActivationStatus.NA
    End Sub

    Public Function IsRenewalStatusReadyForEligibleStatus() As Boolean
        Return (Me.EngagementStatus = EngagementStatus.COMPLETE Or Me.EngagementStatus = EngagementStatus.INCOMPLETE) And
                            Me.RenewalStatus = RenewalStatus.TOOEARLY And
                            Me.CreatedDate < DateTime.UtcNow.AddDays(-245)
    End Function
    Public Function IsRenewalStatusReadyForChurnedStatus() As Boolean
        Return Me.RenewalStatus = RenewalStatus.ELIGIBLE And
                Me.CreatedDate < DateTime.UtcNow.AddDays(-450)
    End Function

    Private Function HasListAndCertEligibilityBeenMetInAllCountries() As Boolean
        ' This method supports the EngagementHealth/EngagementStatus.SURVEYLIVE case 
        ' If list / cert eligibility = met In all countries --> GREEN
        'Else (If Not list/cert eligible In all countries) And num_days_survey_open
        ' <= 10 --> GREEN
        '  Between 10 And 18.999 --> YELLOW
        '  >= 19 --> RED

        For Each country As CountryData In Me.ECR.Countries
            If (country.NumberOfEmployeesInCountry IsNot Nothing AndAlso country.NumberOfEmployeesInCountry > 0) Then
                Dim ListCertThresholdResult = ListCertThresholds.GetMOECertList(CInt(country.NumberOfEmployeesInCountry))
                If country.NumberOfRespondentsInCountry < ListCertThresholdResult.CertThreshold Then
                    Return False
                End If
                If country.NumberOfRespondentsInCountry < ListCertThresholdResult.ListThreshold Then
                    Return False
                End If
            Else
                Return False
            End If
        Next
        Return True ' If we get here we know that all the MOE calcs worked for each country and that they were above the Thresholds in ALL countries
    End Function

    Private Function GetCountOfCompletedActivationActivities() As Integer
        Dim count = 0
        If Me.CustomerActivationEmprisingAccess = ActivationStatus.YES Then
            count += 1
        End If
        If Me.CustomerActivationShareToolkit = ActivationStatus.YES Then
            count += 1
        End If
        If Me.CustomerActivationBadgeDownload = ActivationStatus.YES Then
            count += 1
        End If
        If Me.CustomerActivationSharableImages = ActivationStatus.YES Then
            count += 1
        End If
        If Me.CustomerActivationReportDownload = ActivationStatus.YES Then
            count += 1
        End If
        Return count
    End Function

    Public Sub AuditHistoryAdd(itemToLog As String)
        Me.AuditHistory.Add(DateTime.UtcNow & ": " & itemToLog)
    End Sub

    Public Sub SetLastModifiedDate()
        LastModifiedDate = DateTime.UtcNow
    End Sub

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

    ''' <summary>
    ''' Get an enum value that matches the passed description
    ''' </summary>
    ''' <typeparam name="T">Generic type, the caller will pass the specific enum to be examined</typeparam>
    ''' <param name="description">Description string to use to match on an enum value</param>
    ''' <returns></returns>
    Public Shared Function GetEnumFromDescription(Of T)(description As String) As [Enum]
        Dim type = GetType(T)
        For Each fi As FieldInfo In type.GetFields()
            Dim attr() As System.ComponentModel.DescriptionAttribute =
                      DirectCast(fi.GetCustomAttributes(GetType(System.ComponentModel.DescriptionAttribute),
                      False), System.ComponentModel.DescriptionAttribute())
            For i As Integer = 0 To attr.Length - 1
                If attr(i).Description.ToLower = description.ToLower Then
                    Return DirectCast(fi.GetValue(Nothing), [Enum])
                End If
            Next
        Next
        Return Nothing
    End Function

    ''' <summary>
    ''' Get an enum value that matches the passed description
    ''' </summary>
    ''' <typeparam name="T">Generic type, the caller will pass the specific enum to be examined</typeparam>
    ''' <param name="description">Description string to use to match on an enum value</param>
    ''' <returns></returns>
    Public Shared Function ConvertStringToEnum(Of T)(description As String, caseInsensative As Boolean) As [Enum]
        Dim type = GetType(T)
        For Each fi As FieldInfo In type.GetFields()
            Dim attr() As System.ComponentModel.DescriptionAttribute =
                      DirectCast(fi.GetCustomAttributes(GetType(System.ComponentModel.DescriptionAttribute),
                      False), System.ComponentModel.DescriptionAttribute())
            For i As Integer = 0 To attr.Length - 1
                If caseInsensative Then
                    If attr(i).Description.ToLower = description.ToLower Then
                        Return DirectCast(fi.GetValue(Nothing), [Enum])
                    End If
                Else
                    If attr(i).Description = description Then
                        Return DirectCast(fi.GetValue(Nothing), [Enum])
                    End If
                End If
            Next
        Next
        Return DirectCast(GenericStatus.INVALID, [Enum])
    End Function

    Public Function GetECRV2Snippet() As ECRV2Snippet
        Dim country As CountryData
        Dim countries As List(Of CountryData)
        Dim countriesCertifying = (From c In Me.ECR.Countries Where c.IsApplyingForCertification = "Yes").ToList()

        Dim ecrv2Snippet = New ECRV2Snippet() With {.Id = Me.Id, .ClientId = Me.ClientId,
            .EngagementId = Me.EngagementId, .AffiliateId = Me.AffiliateId,
            .CreatedDate = CDate(Me.CreatedDate.ToShortDateString()),
            .ClientName = Me.ClientName, .CultureAuditSSOLink = Me.ECR.CultureAuditSSOLink,
            .CultureAuditStatus = Me.ECR.CultureAuditStatus,
            .CultureBriefSSOLink = Me.ECR.CultureBriefSSOLink,
            .CultureBriefStatus = Me.ECR.CultureBriefStatus,
            .TrustIndexSSOLink = Me.ECR.TrustIndexSSOLink,
            .TrustIndexStatus = Me.ECR.TrustIndexStatus,
            .Tier = Me.Tier,
            .EngagementStatus = Me._EngagementStatus,
            .JourneyStatusChangeDate = Me.JourneyStatusChangeDate,
            .EngagementHealth = Me._EngagementHealth,
            .JourneyStatus = Me._JourneyStatus,
            .JourneyHealth = Me._JourneyHealth,
            .RenewalStatus = Me._RenewalStatus,
            .RenewalHealth = Me._RenewalHealth,
            .Abandoned = Me.IsAbandoned,
            .SurveyOpenDate = Me.ECR.SurveyOpenDate,
            .SurveyCloseDate = Me.ECR.SurveyCloseDate
            }

        If (Me.ECR.NumberOfRespondents IsNot Nothing) Then
            ecrv2Snippet.NumberOfSurveyRespondents = CInt(Me.ECR.NumberOfRespondents)
        End If

        ' Certification and List Eligibility columns would show a check icon if ANY of the countries applying for certification were Certified or List Eligible.
        ' And show an X icon if ANY of the countries applying for certification were Not Certified. The rollover popup shows all countries
        ' in three categories Certified-AO,PT,BR Not Certified-HK, UK, US Note: this list will ONLY include country codes of those countries applying for certification.  
        country = (From c In countriesCertifying Where c.CertificationStatus = "Certified").FirstOrDefault
        If (country IsNot Nothing) Then
            ecrv2Snippet.CertificationStatus = country.CertificationStatus
        Else
            country = (From c In countriesCertifying Where c.CertificationStatus = "Not Certified").FirstOrDefault
            If (country IsNot Nothing) Then
                ecrv2Snippet.CertificationStatus = country.CertificationStatus
            Else
                countries = (From c In countriesCertifying Where c.CertificationStatus = "Pending").ToList()
                If (countries.Count = 1) Then
                    ecrv2Snippet.CertificationStatus = "Pending"
                End If
                If (countries.Count > 1) Then
                    ecrv2Snippet.CertificationStatus = "Not Certified"
                End If
            End If
        End If

        country = (From c In countriesCertifying Where c.ListEligibilityStatus = "Eligible").FirstOrDefault
        If (country IsNot Nothing) Then
            ecrv2Snippet.ListEligibilityStatus = country.ListEligibilityStatus
        Else
            country = (From c In countriesCertifying Where c.ListEligibilityStatus = "Not Eligible").FirstOrDefault
            If (country IsNot Nothing) Then
                ecrv2Snippet.ListEligibilityStatus = country.ListEligibilityStatus
            Else
                countries = (From c In countriesCertifying Where c.ListEligibilityStatus = "Pending").ToList()
                If (countries.Count = 1) Then
                    ecrv2Snippet.ListEligibilityStatus = "Pending"
                End If
                If (countries.Count > 1) Then
                    ecrv2Snippet.ListEligibilityStatus = "Not Eligible"
                End If
            End If
        End If

        ' Only set the Profile Columns if there is a US country and IsApplyingForCertification = "Yes"
        country = (From c In countriesCertifying Where c.CountryCode = "US").FirstOrDefault
        If (country IsNot Nothing) Then
            ecrv2Snippet.ProfilePublishStatus = country.ProfilePublishStatus
            ecrv2Snippet.ProfilePublishedLink = country.ProfilePublishedLink
        End If

        ' Only set the CertificationExpiryDate if there is a date specified for any country
        country = (From c In countriesCertifying Where c.CertificationExpiryDate IsNot Nothing).FirstOrDefault
        If (country IsNot Nothing) Then
            ecrv2Snippet.CertificationExpiryDate = country.CertificationExpiryDate
        End If

        For Each c In Me.ECR.Countries
            If ecrv2Snippet.Country.Count > 0 Then
                ecrv2Snippet.Country &= ","
            End If
            ecrv2Snippet.Country &= c.CountryCode
            If (c.IsApplyingForCertification <> "Yes") Then
                ecrv2Snippet.Country &= "*"
            End If
        Next

        If (countriesCertifying.Count > 1) Then ' only set these properties if there is more than one country certifying
            ' This column would show a coma delimited list of country codes for all the countries within the ECR. The column itself would show a ellipsis
            ' if there were too many to fit however the filtering and sorting should work correctly.  
            Dim certifiedCountries As New List(Of String)
            Dim PendingCertificationCountries As New List(Of String)
            Dim NotCertificationCountries As New List(Of String)
            Dim ListEligibleCountries As New List(Of String)
            Dim PendingListEligibleCountries As New List(Of String)
            Dim NotListEligibleCountries As New List(Of String)
            For Each c In countriesCertifying
                If (c.CertificationStatus = "Certified") Then
                    certifiedCountries.Add(c.CountryCode)
                End If
                If (c.CertificationStatus = "Pending") Then
                    PendingCertificationCountries.Add(c.CountryCode)
                End If
                If (c.CertificationStatus = "Not Certified") Then
                    NotCertificationCountries.Add(c.CountryCode)
                End If
                If (c.ListEligibilityStatus = "Eligible") Then
                    ListEligibleCountries.Add(c.CountryCode)
                End If
                If (c.ListEligibilityStatus = "Pending") Then
                    PendingListEligibleCountries.Add(c.CountryCode)
                End If
                If (c.ListEligibilityStatus = "Not Eligible") Then
                    NotListEligibleCountries.Add(c.CountryCode)
                End If
            Next
            ecrv2Snippet.AllCountryCertification = "Certified:" & String.Join(",", certifiedCountries) & ", Not Certified:" & String.Join(",", NotCertificationCountries) & ", Pending:" & String.Join(",", PendingCertificationCountries)
            ecrv2Snippet.AllCountryListEligiblity = "Eligible:" & String.Join(",", ListEligibleCountries) & ", Not Eligible:" & String.Join(",", NotListEligibleCountries) & ", Pending:" & String.Join(",", PendingListEligibleCountries)
            ' Disable Pending Menu when countriesCertifying.Count > 1 by setting to empty string
            If ecrv2Snippet.CertificationStatus = "Pending" Then
                ecrv2Snippet.CertificationStatus = ""
            End If
            If ecrv2Snippet.ListEligibilityStatus = "Pending" Then
                ecrv2Snippet.ListEligibilityStatus = ""
            End If
        End If

        Return ecrv2Snippet
    End Function


End Class

Public Class EmailsSent
    Public Property EmailType As OperationsEmailType
    Public Property DateTimeSent As DateTime = DateTime.Now
    Public Property EmailedTo As List(Of String)
    Public Property IsSuccess As Boolean
End Class

Public Class GenericResult
    Public Property IsError As Boolean
    Public Property ErrStr As String = ""
End Class

Public Class ECRV2Snippet
    Public Property Id As ObjectId
    Public Property ClientId As Integer = 0
    Public Property EngagementId As Integer = 0
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CreatedDate As Date = DateTime.Now
    Public Property AffiliateId As String = ""
    Public Property ClientName As String = ""
    Public Property TrustIndexStatus As String = ""
    Public Property CultureBriefStatus As String = ""
    Public Property CultureAuditStatus As String = ""
    Public Property CertificationStatus As String = ""
    Public Property TrustIndexSSOLink As String = ""
    Public Property CultureBriefSSOLink As String = ""
    Public Property CultureAuditSSOLink As String = ""
    Public Property ProfilePublishStatus As String = ""
    Public Property ProfilePublishedLink As String = ""
    Public Property ListEligibilityStatus As String = ""
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property CertificationExpiryDate As Date? = Nothing
    Public Property Country As String = ""
    Public Property AllCountryCertification As String = ""
    Public Property AllCountryListEligiblity As String = ""
    Public Property Tier As String = ""
    Public Property EngagementStatus As String = ""
    <BsonIgnore> Public ReadOnly Property Duration As Integer
        Get
            Return CInt((DateTime.UtcNow - Me.JourneyStatusChangeDate).Days) '' Days appears to do a floor whereas TotalDays counts > 12 hours as a day
        End Get
    End Property
    Public Property EngagementHealth As String = ""
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property JourneyStatusChangeDate As Date = DateTime.Now
    Public Property JourneyStatus As String = ""
    Public Property JourneyHealth As String = ""
    Public Property RenewalStatus As String = ""
    Public Property RenewalHealth As String = ""
    Public Property NumberOfSurveyRespondents As Integer
    Public Property Abandoned As Boolean = False
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property SurveyOpenDate As Date? = Nothing
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property SurveyCloseDate As Date? = Nothing
End Class