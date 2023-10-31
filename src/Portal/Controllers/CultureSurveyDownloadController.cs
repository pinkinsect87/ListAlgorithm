using System;
using Microsoft.AspNetCore.Mvc;
using Portal.Model;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;
using System.IO;
using System.Net;
using System.Net.Http;
using SharedProject2;
using System.IO.Compression;
using System.Collections.Generic;
using Aspose.Words;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Azure.Amqp.Framing;
using SharpCompress.Archives;

namespace Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CultureSurveyDownloadController : PortalControllerBase
    {
        public CultureSurveyDownloadController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpGet("[Action]")]
        public IActionResult CreatePackage(int clientId, int engagementId, string surveyType, string countryCode, string token)
        {
            this.SetTokenFromExternalSource(token);
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateSurveyDownloadPackage", null);

            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = engagementId;
            }
            catch (Exception) { }

            if (!IsUserAuthorized(gptwContext, clientId))
            {
                AtlasLog.LogError(String.Format("Not Authorized.token:{0}", token), gptwContext);
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                this.ClearTokenFromExternalSource(token);
                return Unauthorized();
            }

            // The token is no longer needed to be overidden at this point so clear it.
            this.ClearTokenFromExternalSource(token);

            try
            {
                // Create the culture survey package as a stream
                Stream outputStream = BuildCultureSurveyZipPackage(clientId, engagementId, surveyType, countryCode);

                // Name to give the delivered file
                string downloadFilename = makeDownloadableFilename(engagementId, surveyType);

                // Return the result
                // TODO is this the best content type?
                string contentType = "application/octet-stream";
                FileStreamResult fs = File(outputStream, contentType, downloadFilename);

                AtlasLog.LogInformation(String.Format("Returning a {0} culture survey package", surveyType), gptwContext);

                return fs;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to build a {0} culture survey package", surveyType), e, gptwContext);
                return StatusCode(500);
            }
        }

        public Stream BuildCultureSurveyZipPackage(int clientId, int engagementId, string surveyType, string countryCode)
        {

            // Create Zip archive
            Stream archiveStream = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {

                // Add the culture survey document
                AddCultureSurveyDoc(archive, engagementId, surveyType, countryCode, clientId);

                // Add photos (if a Culture Brief)
                if (surveyType == "CB")
                {
                    AddPhotos(archive, clientId, engagementId);
                    AddLogo(archive, clientId, engagementId);
                }

                // Add supplemental documents (if a Culture Audit)
                if (surveyType == "CA")
                {
                    AddSupplementalDocs(archive, clientId, engagementId);
                }
            }

            archiveStream.Position = 0;
            return archiveStream;
        }

        public Stream BuildMultipleCultureSurveyZipPackage(MultiplePackageDownload[] multiplePackageDownloads)
        {
            // Create Zip archive
            Stream archiveStream = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {
                foreach(var package in multiplePackageDownloads)
                {
                    if (package.IsDownloadCA)
                        // Add the culture survey document
                        AddCultureSurveyDoc(archive, package.EngagementId, "CA", package.CountryCode, package.ClientId, true);
                    if (package.IsDownloadCB)
                        // Add the culture survey document
                        AddCultureSurveyDoc(archive, package.EngagementId, "CB", package.CountryCode, package.ClientId, true);

                    // Add photos (if a Culture Brief)
                    if (package.IsDownloadCB)
                    {
                        AddPhotos(archive, package.ClientId, package.EngagementId, true);
                        AddLogo(archive, package.ClientId, package.EngagementId, true);
                    }

                    // Add supplemental documents (if a Culture Audit)
                    if (package.IsDownloadCA)
                    {
                        AddSupplementalDocs(archive, package.ClientId, package.EngagementId, true);
                    }
                }
            }

            archiveStream.Position = 0;
            return archiveStream;
        }

        public void AddCultureSurveyDoc(ZipArchive archive, int engagementId, string surveyType, string countryCode, int clientId = 0, bool isMultiple = false)
        {
            string wordDocFileName = "";
            if (surveyType == "CB")
            {
                wordDocFileName = "CultureBrief.pdf";
            }
            if (surveyType == "CA")
            {
                wordDocFileName = "CultureAudit.pdf";
            }
            Stream docStream = BuildCultureSurveyDoc(clientId, engagementId, surveyType, countryCode);
            if (isMultiple)
                AddZipEntry(archive, docStream, wordDocFileName, clientId.ToString(), engagementId.ToString());
            else
                AddZipEntry(archive, docStream, wordDocFileName);
        }

        public Stream BuildCultureSurveyDoc(int clientId, int engagementId, string surveyType, string countryCode)
        {

            CultureSurveyDataRepository csdr = new CultureSurveyDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);

            // Get company name
            AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);
            ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);
            string clientName = ecrv2.ClientName;

            // Get country name
            Country country = atlasOperationsRepo.GetCountryByCountryCode(countryCode);
            string countryName = country.CountryName;

            //Create an empty Word doc
            Aspose.Words.License l = new Aspose.Words.License();
            l.SetLicense("Aspose.Total.lic");
            DocumentBuilder docBuilder = new DocumentBuilder();
            docBuilder.Font.Name = "Calibri";
            docBuilder.Font.Size = 12;

            // Write the client name
            docBuilder.Font.Bold = true;
            docBuilder.Writeln(clientName);
            docBuilder.Font.Bold = false;
            docBuilder.Writeln("");

            // Write the type of survey
            string surveyTypeFull = "";
            if (surveyType == "CA")
            {
                surveyTypeFull = "CULTURE AUDIT";
            }
            if (surveyType == "CB")
            {
                surveyTypeFull = "CULTURE BRIEF";
            }
            docBuilder.Writeln(surveyTypeFull);

            ClientDocumentsDataRepository docRepo = null;
            ClientDocuments documents = null;

            // Write the completion date
            DateTime? surveyCompletedDate = null;
            if (surveyType == "CA")
            {
                surveyCompletedDate = ecrv2.ECR.CACompletionDate;
                docRepo = new ClientDocumentsDataRepository(appSettings.MongoDBConnectionString);
                documents = docRepo.GetClientDocuments(clientId, engagementId);
            }
            if (surveyType == "CB")
            {
                surveyCompletedDate = ecrv2.ECR.CBCompletionDate;
            }
            if (surveyCompletedDate != null)
            {
                docBuilder.Writeln("Submitted: " + ((DateTime)surveyCompletedDate).ToString("MMMM dd, yyyy"));
            }

            docBuilder.InsertHorizontalRule();
            docBuilder.Writeln("");

            // Get survey with responses
            CultureSurveyDTO cultureSurvey = csdr.GetCultureSurvey(engagementId, surveyType);

            // Get flattened list of template questions
            CultureSurveyTemplate cst = csdr.GetCultureSurveyTemplate(surveyType, cultureSurvey.TemplateVersion);
            List<CultureSurveyQuestion> questions = cst.FlattenQuestions();

            foreach (CultureSurveyQuestion question in questions)
            {
                string displayText = question.DisplayText;
                displayText = displayText.Replace(@"[CompanyName]", clientName);
                displayText = displayText.Replace(@"[Country]", countryName);

                // Replace entities with actual symbols
                displayText = displayText.Replace(@"&trade;", "™");
                displayText = displayText.Replace(@"&reg;", "®");
                displayText = displayText.Replace(@"&copy;", "©");

                // #US-746
                // Remove question if OnlyAskInCountry has value and OnlyAskInCountry list do not contain the countrycode
                if (question.OnlyAskInCountry.Count > 0)
                {
                    if (!question.OnlyAskInCountry.Contains(countryCode))
                    { continue; }
                }

                //if (displayText.StartsWith("PEOPLE Companies That Care")
                //    && countryCode != "US")
                //{
                //    //No further questions for the non-US country.
                //    break;
                //}

                if (question.VariableName == "")
                {
                    // Section header
                    docBuilder.Font.Bold = true;
                    docBuilder.ParagraphFormat.SpaceBefore = 18;
                    docBuilder.ParagraphFormat.SpaceAfter = 6;
                    docBuilder.Writeln(displayText);
                    docBuilder.ParagraphFormat.SpaceBefore = 0;
                    docBuilder.ParagraphFormat.SpaceAfter = 0;
                    docBuilder.Font.Bold = false;
                }
                else
                {
                    // Indent CB questions and responses slightly to offset from section headers
                    if (surveyType == "CB")
                    {
                        docBuilder.ParagraphFormat.LeftIndent = 24;
                    }

                    // Question text
                    if (question.FieldType.ToLower() == "textarea" && surveyType == "CA")
                    {
                        // Add some extra spacing before the response for CA essay questions
                        docBuilder.ParagraphFormat.SpaceBefore = 6;
                        docBuilder.ParagraphFormat.SpaceAfter = 6;
                    }
                    docBuilder.Font.Bold = true;
                    docBuilder.Font.Color = System.Drawing.Color.DimGray;
                    docBuilder.Writeln(question.DisplayText + @": ");
                    docBuilder.Font.Color = System.Drawing.Color.Black;
                    docBuilder.Font.Bold = false;

                    // Response text
                    docBuilder.ParagraphFormat.SpaceAfter = 6;
                    string variableName = question.VariableName.Replace(@"[?]", @"[" + countryCode + @"]");
                    if (cultureSurvey.GetCultureSurveyResponse(variableName) != null)
                    {
                        CultureSurveyResponse response = cultureSurvey.GetCultureSurveyResponse(variableName);
                        docBuilder.Font.Color = System.Drawing.Color.SteelBlue;
                        docBuilder.Write(response.Response);
                        if (response.IsConfidential)
                        {
                            docBuilder.Font.Italic = true;
                            docBuilder.Font.Color = System.Drawing.Color.DarkRed;
                            docBuilder.Write(" (confidential)");
                            docBuilder.Font.Italic = false;
                        }
                        docBuilder.Font.Color = System.Drawing.Color.Black;
                        docBuilder.Writeln("");
                    }
                    else
                    {
                        // No response
                        docBuilder.Font.Color = System.Drawing.Color.DarkGray;
                        if (surveyType == "CA" && question.FieldType.ToLower() == "file")
                        {
                            if (documents != null)
                            {
                                Document document = (from doc in documents.Documents.AsQueryable()
                                 where (String.Equals(doc.VariableName, question.VariableName, StringComparison.OrdinalIgnoreCase))
                                 select doc).FirstOrDefault();

                                if (document == null)
                                    docBuilder.Writeln("No File was Submitted");
                                else {
                                    docBuilder.Writeln(document.Name);
                                    docBuilder.Writeln("(File Included within this Download)");
                                }
                            }
                        }
                        else
                            docBuilder.Writeln("No response");
                        docBuilder.Font.Color = System.Drawing.Color.Black;
                    }
                    docBuilder.ParagraphFormat.SpaceAfter = 0;
                    docBuilder.ParagraphFormat.LeftIndent = 0;

                    // For the CA, end each essay question with a page break due to their multi-page length
                    if (question.FieldType.ToLower() == "textarea" && surveyType == "CA")
                    {
                        docBuilder.InsertBreak(BreakType.PageBreak);
                    }
                }
            }

            Stream wordDocStream = new MemoryStream();
            docBuilder.Document.Save(wordDocStream, SaveFormat.Pdf);
            //docBuilder.Document.Save(wordDocStream, SaveFormat.Docx);
            wordDocStream.Position = 0;
            return wordDocStream;
        }

        private void AddPhotos(ZipArchive archive, int clientId, int engagementId, bool isMultiple = false)
        {
            ClientImagesDataRepository repo = new ClientImagesDataRepository(appSettings.MongoDBConnectionString);
            ClientImages images = repo.GetClientImages(clientId, engagementId);

            if (images != null)
            {
                // TODO collect the image captions and output into a separate file
                foreach (ClientPhoto image in images.Photos)
                {
                    // TODO add the order number to the filename
                    string filename = image.FileName;
                    string clientFilename = image.FileName;
                    string containerName = "images";
                    Stream imageStream = GetClientAsset(clientId, engagementId, containerName, filename);
                    if (imageStream != null)
                    {
                        if (isMultiple)
                            AddZipEntry(archive, imageStream, filename, clientId.ToString(), engagementId.ToString());
                        else
                            AddZipEntry(archive, imageStream, filename);
                    }
                }
            }
        }

        private void AddLogo(ZipArchive archive, int clientId, int engagementId, bool isMultiple = false)
        {
            ClientImagesDataRepository repo = new ClientImagesDataRepository(appSettings.MongoDBConnectionString);

            ClientImages images = repo.GetClientImages(clientId, engagementId);

            if (images != null)
            {
                string logoFilename = images.LogoFileName;
                string containerName = "images";
                Stream imageStream = GetClientAsset(clientId, engagementId, containerName, logoFilename);
                if (imageStream != null)
                {
                    if (isMultiple)
                        AddZipEntry(archive, imageStream, logoFilename, clientId.ToString(), engagementId.ToString());
                    else
                        AddZipEntry(archive, imageStream, logoFilename);
                }
            }
        }

        private void AddSupplementalDocs(ZipArchive archive, int clientId, int engagementId, bool isMultiple = false)
        {
            ClientDocumentsDataRepository repo = new ClientDocumentsDataRepository(appSettings.MongoDBConnectionString);

            ClientDocuments documents = repo.GetClientDocuments(clientId, engagementId);

            if (documents != null)
            {
                foreach (Document document in documents.Documents)
                {
                    string filename = document.FileName;
                    string containerName = "documents";
                    Stream documentStream = GetClientAsset(clientId, engagementId, containerName, filename);
                    if (documentStream != null)
                    {
                        if (isMultiple)
                            AddZipEntry(archive, documentStream, filename, clientId.ToString(), engagementId.ToString());
                        else
                            AddZipEntry(archive, documentStream, filename);
                    }
                }
            }
        }

        private void AddZipEntry(ZipArchive archive, Stream fileStream, string filename, string clientId = "", string engagementId = "")
        {
            ZipArchiveEntry fileEntry = archive.CreateEntry(string.Format("{0}{1}{2}{3}{4}",
                clientId,
                !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(engagementId) ? "_" : string.Empty,
                engagementId,
                !string.IsNullOrWhiteSpace(clientId) || !string.IsNullOrWhiteSpace(engagementId) ? "/" : string.Empty,
                filename));
            using (Stream entryStream = fileEntry.Open())
            {
                using (Stream fileToCompressStream = fileStream)
                {
                    fileToCompressStream.CopyTo(entryStream);
                }
            }
        }

        private Stream GetClientAsset(int clientId, int engagementId, string containerName, string fileName)
        {

            try
            {
                MemoryStream ms = new MemoryStream();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(appSettings.ClientAssetsBlobStorageConnectionString);

                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = BlobClient.GetContainerReference(containerName);

                CloudBlob file = container.GetBlobReference(fileName);

                file.DownloadToStream(ms);
                Stream imageStream = file.OpenRead();
                imageStream.Position = 0;
                return imageStream;
            }

            catch (Exception e)
            {
                // TODO
                //return Content("GetClientAsset Failed.");
                return null;
            }
        }

        private string makeDownloadableFilename(int engagementId, string surveyType)
        {

            string filename = getCompanyName(engagementId);
            string downloadFilename = sanitizeFilename(filename + "_" + surveyType + ".zip");
            return downloadFilename;
        }

        private string getCompanyName(int engagementId)
        {
            // Get company name
            AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);
            ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);
            string companyName = prepareCompanyName(ecrv2.ClientName);
            return companyName;
        }

        private string prepareCompanyName(string companyName)
        {

            if (companyName.StartsWith("Activated Insights - "))
            {
                companyName = companyName.Replace("Activated Insights - ", "");
            }
            return companyName;
        }

        private string sanitizeFilename(string filename)
        {
            // Remove any invalid characters from a string intended to be used as a filename
            string sanitizedFilename = string.Join("", filename.Split(Path.GetInvalidFileNameChars()));
            // Also replace spaces with '_'
            sanitizedFilename = sanitizedFilename.Replace(" ", "_");
            return sanitizedFilename;
        }
    }
}
