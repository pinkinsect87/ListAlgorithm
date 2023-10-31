Imports System.Net
Imports System.Web.HttpUtility

Public Class AtlasDataRequestReportStoreFileStreamer
    Implements IFileStreamer

    Private _ReportStoreUrl As String = ""
    Private _AffiliateId As String = ""
    Private _Username As String = ""
    Private _token As String = ""

    Public Sub New(ReportStoreUrl As String,
                   AffiliateId As String,
                   Username As String,
                   Token As String)

        ' catch simple problems
        If ReportStoreUrl Is Nothing Or
            AffiliateId Is Nothing Or
            Username Is Nothing Or
            Token Is Nothing Then

            Throw New Exception("An input parameter is empty.")
        End If

        If ReportStoreUrl = "" Or
            AffiliateId = "" Or
            Username = "" Or
            Token = "" Then

            Throw New Exception("An input parameter is empty.")
        End If

        If Not Username.Contains("@") Then
            Throw New Exception("Username must be a valid e-mail address.")
        End If

        If Not AffiliateId.Length = 3 Then
            Throw New Exception("AffiliateId must be 3 characters.")
        End If

        ' save inputs to global variables
        _ReportStoreUrl = ReportStoreUrl
        _AffiliateId = AffiliateId
        _Username = Username
        _token = Token

    End Sub ' New


    Public Function GetStream(FileURI As GptwUri) As IO.Stream Implements IFileStreamer.GetStream
        ' use the template to create the request URL

        Dim url As String = _ReportStoreUrl
        Dim RequestUrlTemplate As String = _ReportStoreUrl & "/api/reportstore/retrievedatarequestfile?token={0}&username={1}&affiliate={2}&uri={3}"
        Dim RequestUrl As String = String.Format(RequestUrlTemplate,
                                                 UrlEncode(_token),
                                                 UrlEncode(_Username),
                                                 UrlEncode(_AffiliateId),
                                                 UrlEncode(FileURI.Uri))

        ' retrieve the response and stream it
        Dim WebReq As HttpWebRequest = CType(WebRequest.Create(RequestUrl), HttpWebRequest)
        WebReq.Timeout = 600000
        WebReq.KeepAlive = False
        Dim WebResp As WebResponse = WebReq.GetResponse()
        Return WebResp.GetResponseStream
    End Function

End Class
