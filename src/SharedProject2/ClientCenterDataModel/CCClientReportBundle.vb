Imports Newtonsoft.Json
Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Public Class CCClientReportBundle

    ''' <summary>
    ''' The Id of the object (automatically generated)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonConverter(GetType(ObjectIdConverter))>
    Public Property Id As ObjectId = ObjectId.GenerateNewId

    ''' <summary>
    ''' The client id that the report bundle is for
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ClientId As Integer = 0

    ''' <summary>
    ''' The list of reports in the publishing group.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ReportList As List(Of CCClientReport) = New List(Of CCClientReport)

    ''' <summary>
    ''' The time the object was created
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property CreatedDate As Date = DateTime.UtcNow

    ''' <summary>
    ''' The time the object was modified
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <BsonDateTimeOptions(Kind:=DateTimeKind.Utc)>
    Public Property ModifiedDate As Date = DateTime.UtcNow

    Public Sub New(zClientId As Integer)
        ClientId = zClientId
    End Sub

    ''' <summary>
    ''' Adds new report to the list or updates existing info for a report
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub AddReport(wrkClientReport As CCClientReport, activeSupplementalDocsList As List(Of CCSupplementalDocument))

        ' ADD REPORT TO BUNDLE
        ReportList.Add(wrkClientReport)

        ' PUBLISH APPLICABLE SUPPLEMENTAL DOCUMENTS

        ' determine which supplemental documents apply to the report we are adding
        Dim applicableSupplementalDocumentList = (From a In activeSupplementalDocsList
                                                  Where a.ReportType = wrkClientReport.ReportType
                                                  Select a).ToList

        ' get the list of supplemental document uris that have already been published
        ' (we don't want to publish the same supplemental document twice to the same folder)
        Dim alreadyPublishedDocumentUris = (From a In ReportList
                                            Where a.PublishFolderName = wrkClientReport.PublishFolderName And a.PublishFolderYear = wrkClientReport.PublishFolderYear
                                            Select a.FileUri
                                            Distinct).ToList

        ' publish each required doc to the folder
        For Each requiredSuppDoc In applicableSupplementalDocumentList
            If Not alreadyPublishedDocumentUris.Contains(requiredSuppDoc.FileUri) Then

                ' create the "report" for the supplemental document
                Dim clientSuppDoc As New CCClientReport(wrkClientReport.PublishingGroupId, requiredSuppDoc.AffiliateId, ClientId, wrkClientReport.PublishFolderYear & "01", 0, 0,
                                                        wrkClientReport.CompanyName, "Supplemental", 0,
                                                        requiredSuppDoc.DocumentFriendlyName, requiredSuppDoc.FileUri,
                                                        wrkClientReport.PublishFolderName, wrkClientReport.PublishFolderYear, wrkClientReport.ReportPublishOnDate)

                ' need to auto-approve the report
                clientSuppDoc.IsReportApproved = True
                ' override the auto-genned id
                clientSuppDoc.Id = requiredSuppDoc.Id

                ' add the report to the list
                ReportList.Add(clientSuppDoc)
            End If ' check whether the supplemental doc has already been published
        Next ' required supplemental document

    End Sub

End Class
