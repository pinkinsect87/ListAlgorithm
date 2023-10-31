Option Explicit On
Option Strict On

Imports System.ComponentModel
Imports System.Reflection

Public Class AtlasOperationsEnums

    ''' <summary>
    ''' Enumerates the SiteEventCategories  that are available.
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum SiteEventCategory
        <Description("NONE")> None = 0
        <Description("USER_EVENT")> UserEvent = 1
    End Enum

    'Public Enum Status
    '    <Description("InProgress")> IN_PROGRESS = 0
    '    <Description("Complete")> COMPLETE = 1
    '    <Description("Failed")> FAILED = 2
    'End Enum

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
