using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharedProject2;
using Aspose.Words;

namespace CultureSurveyShared
{
    public class CultureSurveyOutputDoc
    {

        private string _daloMongoConnectionString;
        private string _mongoLockAzureStorageConnectionString;
        private string _clientAssetsBlobStorageConnectionString;

        public CultureSurveyOutputDoc(string daloMongoConnectionString = null, string mongoLockAzureStorageConnectionString = null, string clientAssetsBlobStorageConnectionString = null)
        {
            _daloMongoConnectionString = daloMongoConnectionString;
            _mongoLockAzureStorageConnectionString = mongoLockAzureStorageConnectionString;
            _clientAssetsBlobStorageConnectionString = clientAssetsBlobStorageConnectionString;
        }

        public string HelloWorld()
        {
            return "Hello, world";
        }

        //public Stream BuildDoc(AtlasOperationsDataRepository atlasOperationsRepo, CultureSurveyDataRepository csdr, ClientDocumentsDataRepository docRepo, int clientId, int engagementId, string surveyType, string countryCode)
        public Stream BuildDoc(int clientId, int engagementId, string surveyType, string countryCode)
        {

            CultureSurveyDataRepository csdr = new CultureSurveyDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);

            // Get company name
            AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);
            ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);
            string clientName = ecrv2.ClientName;

            // Get country name
            List<string> countryCodeList = new List<string>();
            List<Country> countryNameList = new List<Country>();
            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                //countryCodeList = countryCode.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
                countryCodeList = countryCode.Split(',').ToList();
            }

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
                docRepo = new ClientDocumentsDataRepository(_daloMongoConnectionString);
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

            // Global section
            foreach (CultureSurveyQuestion question in questions)
            {
                try
                {
                    string displayText = question.DisplayText;

                    if (displayText == "About [CompanyName]: [Country]")
                        // Reached the first country section.
                        break;

                    displayText = displayText.Replace(@"[CompanyName]", clientName);

                    // Replace entities with actual symbols
                    displayText = displayText.Replace(@"&trade;", "™");
                    displayText = displayText.Replace(@"&reg;", "®");
                    displayText = displayText.Replace(@"&copy;", "©");
                    displayText = displayText.Replace(@"&nbsp;", " ");
                    displayText = displayText.Replace(@"<b>", "");
                    displayText = displayText.Replace(@"</b>", "");
                    displayText = displayText.Replace(@"<p>", "");
                    displayText = displayText.Replace(@"</p>", "");
                    displayText = displayText.Replace(@"<br />", " ");

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
                        docBuilder.Writeln(displayText + @": ");
                        docBuilder.Font.Color = System.Drawing.Color.Black;
                        docBuilder.Font.Bold = false;

                        // Response text
                        docBuilder.ParagraphFormat.SpaceAfter = 6;
                        string variableName = question.VariableName;
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
                                    else
                                    {
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
                catch (Exception ex)
                {
                    Exception e = new Exception(String.Format("Fail on global question '{0}', variable name '{1}', for engagement {2}, {3}", question.DisplayText, question.VariableName, engagementId, ex.Message));
                    throw e;
                }
            }

            // Country sections
            foreach (var singleCountryCode in countryCodeList)
            {
                var currentCountryCode = singleCountryCode.Replace("*", "").Trim();
                if (currentCountryCode != "")
                {
                    var currentCountryName = atlasOperationsRepo.GetCountryByCountryCode(currentCountryCode).CountryName;
                    var isCountrySection = false;

                    foreach (CultureSurveyQuestion question in questions)
                    {
                        try
                        {
                            string displayText = question.DisplayText;

                            // Replace entities with actual symbols
                            displayText = displayText.Replace(@"&trade;", "™");
                            displayText = displayText.Replace(@"&reg;", "®");
                            displayText = displayText.Replace(@"&copy;", "©");
                            displayText = displayText.Replace(@"&nbsp;", " ");
                            displayText = displayText.Replace(@"<b>", "");
                            displayText = displayText.Replace(@"</b>", "");
                            displayText = displayText.Replace(@"<p>", "");
                            displayText = displayText.Replace(@"</p>", "");
                            displayText = displayText.Replace(@"<br />", " ");

                            if (displayText == "About [CompanyName]: [Country]")
                                isCountrySection = true;

                            //// #US-746
                            if (displayText.StartsWith("PEOPLE Companies That Care")
                                && countryCode != "US")
                            {
                                //No further questions for the non-US country.
                                break;
                            }

                            if (isCountrySection)
                            {
                                displayText = displayText.Replace(@"[CompanyName]", clientName);
                                displayText = displayText.Replace(@"[Country]", currentCountryName);

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
                                    docBuilder.Writeln(displayText + @": ");
                                    docBuilder.Font.Color = System.Drawing.Color.Black;
                                    docBuilder.Font.Bold = false;

                                    // Response text
                                    docBuilder.ParagraphFormat.SpaceAfter = 6;
                                    string variableName = question.VariableName.Replace(@"[?]", @"[" + currentCountryCode + @"]");
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
                                                else
                                                {
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
                        }
                        catch (Exception ex)
                        {
                            Exception e = new Exception(String.Format("Fail on country question '{0}', variable name '{1}', for engagement {2}, {3}", question.DisplayText, question.VariableName, engagementId, ex.Message));
                            throw e;
                        }
                    }
                }
            }

            Stream wordDocStream = new MemoryStream();
            docBuilder.Document.Save(wordDocStream, SaveFormat.Pdf);
            //docBuilder.Document.Save(wordDocStream, SaveFormat.Docx);
            wordDocStream.Position = 0;
            return wordDocStream;
        }

    }
}
