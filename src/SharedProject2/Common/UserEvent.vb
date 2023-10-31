Imports System.ComponentModel
Imports System.Reflection
Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Public Class UserEventEnums
    Public Enum Name
        <Description("Toolkit:Page View")> Toolkit_Page_View = 0
        <Description("Toolkit:Download Badge")> Toolkit_Download_Badge = 1 'Deprecated
        <Description("Toolkit:Email a Friend")> Toolkit_Email_Friend = 2
        <Description("Toolkit:View Usage Guidlines")> View_Usage_Guidlines = 3
        <Description("Toolkit:View Press Release")> View_Press_Release = 4
        <Description("Toolkit:Visit Store")> Toolkit_Visit_Store = 5
        <Description("Toolkit:Download Shareable Image 1")> Toolkit_Download_ShareableImage1 = 6
        <Description("Toolkit:Download Shareable Image 2")> Toolkit_Download_ShareableImage2 = 7
        <Description("Toolkit:Download Shareable Image 3")> Toolkit_Download_ShareableImage3 = 8
        <Description("Toolkit:Download Shareable Image 4")> Toolkit_Download_ShareableImage4 = 9
        <Description("Toolkit:Download Badge (SVG)")> Toolkit_Download_BadgeSVG = 10
        <Description("Toolkit:Download Badge (JPG)")> Toolkit_Download_BadgeJPG = 11
        <Description("Toolkit:Download Badge (PNG)")> Toolkit_Download_BadgePNG = 12
        <Description("Toolkit:Download Badge (ZIP)")> Toolkit_Download_BadgeZIP = 13
        <Description("Toolkit:Request Celebration Kit")> Request_Celebration_Kit = 14
        <Description("Toolkit:Share Toolkit")> Share_Toolkit = 15
        <Description("Engagement:Go To TI Survey")> Go_To_TI = 16
        <Description("Engagement:Go To CB Survey")> Go_To_CB = 17
        <Description("Engagement:Go To CA Survey")> Go_To_CA = 18
        <Description("Engagement:Report Download")> Report_Download = 19
        <Description("")> LAST_ITEM = Report_Download
    End Enum

    Public Shared Function ValidateName(Name As String) As Boolean
        For index As Integer = 0 To UserEventEnums.Name.LAST_ITEM
            Dim myEnum As UserEventEnums.Name = CType(index, UserEventEnums.Name)
            If UserEventEnums.GetEnumDescription(myEnum) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function NameEnumFromString(Name As String, ByRef ReturnedEnum As UserEventEnums.Name) As Boolean
        For index As Integer = 0 To UserEventEnums.Name.LAST_ITEM
            Dim myEnum As UserEventEnums.Name = CType(index, UserEventEnums.Name)
            If UserEventEnums.GetEnumDescription(myEnum) Then
                ReturnedEnum = myEnum
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function SourceEnumFromString(Name As String, ByRef ReturnedEnum As UserEventEnums.Source) As Boolean
        For index As Integer = 0 To UserEventEnums.Source.LAST_ITEM
            Dim myEnum As UserEventEnums.Source = CType(index, UserEventEnums.Source)
            If UserEventEnums.GetEnumDescription(myEnum) Then
                ReturnedEnum = myEnum
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function UserTypeEnumFromString(UserType As String, ByRef ReturnedEnum As UserEventEnums.UserType) As Boolean
        For index As Integer = 0 To UserEventEnums.UserType.LAST_ITEM
            Dim myEnum As UserEventEnums.UserType = CType(index, UserEventEnums.UserType)
            If UserEventEnums.GetEnumDescription(myEnum) Then
                ReturnedEnum = myEnum
                Return True
            End If
        Next
        Return False
    End Function

    Public Enum Source
        <Description("Portal")> Portal = 0
        <Description("Culture Survey")> Culture_Survey = 1
        <Description("")> LAST_ITEM = Culture_Survey
    End Enum

    Public Enum UserType
        <Description("Employee")> Employee = 0
        <Description("End User")> End_User = 1
        <Description("")> LAST_ITEM = End_User
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


End Class

Public Class UserEvent

    Public Sub New(EventSource As UserEventEnums.Source, EventName As UserEventEnums.Name,
                   ClientId As Integer, EngagementId As Integer, UserType As UserEventEnums.UserType,
                   UserEmail As String, UserSessionId As String, Optional AdditionalInfo As String = "")
        Me.EventSource = UserEventEnums.GetEnumDescription(EventSource)
        Me.EventName = UserEventEnums.GetEnumDescription(EventName)
        Me.ClientId = ClientId
        Me.EngagementId = EngagementId
        Me.UserType = UserEventEnums.GetEnumDescription(UserType)
        Me.UserEmail = UserEmail
        Me.UserSessionId = UserSessionId
        Me.AdditionalInfo = AdditionalInfo
    End Sub

    Public Property Id As ObjectId
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)> Public Property DateEventOccurred As Date = DateTime.Now
    Public Property EventName As String = ""
    Public Property EventSource As String = ""
    Public Property ClientId As Integer = 0
    Public Property EngagementId As Integer = 0
    Public Property UserType As String = ""
    Public Property UserEmail As String = ""
    Public Property UserSessionId As String = ""
    Public Property AdditionalInfo As String = ""
End Class
