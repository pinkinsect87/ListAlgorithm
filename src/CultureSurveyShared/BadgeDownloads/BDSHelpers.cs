using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace CultureSurveyShared.BadgeDownloads
{
    public static class BDSHelpers
    {

        public static Dictionary<string, string> getStatementScoresFromBDS(int clientId, int surveyVersionId, string filter, string businessDataServicesURL, string businessDataServicesToken, int httpClientTimeoutSeconds)
        {

            Dictionary<string, string> statementScores = new Dictionary<string, string>();

            try
            {
                HttpResponseMessage result;

                using (HttpClient hc = new HttpClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, httpClientTimeoutSeconds);
                    string url = businessDataServicesURL + "/api/trustindex/getdatacolumn";

                    DataColRequestParams pst = new DataColRequestParams
                    {
                        token = businessDataServicesToken,
                        cid = clientId,
                        svid = surveyVersionId,
                        filter = filter
                    };

                    string content = JsonConvert.SerializeObject(pst);

                    if (content == null)
                    {
                        //AtlasLog.LogError(String.Format("ERROR:GetStatementScoresFromBDS content = null."), gptwContext);
                        return new Dictionary<string, string>();
                    }

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    result = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                }

                if (result.IsSuccessStatusCode)
                {
                    string apiResponse = result.Content.ReadAsStringAsync().Result;
                    var statements = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiResponse);

                    // Build a dictionary of statement scores
                    for (int i = 0; i < statements.Count; i++)
                    {
                        string str;
                        string stmt_core_id = "";
                        string statementScore = "N/A";

                        if (statements[i].TryGetValue("stmt_core_id", out str))
                        {
                            stmt_core_id = str;
                            if (statements[i].TryGetValue("PercentPos", out str))
                                statementScore = str;
                            statementScores.Add(stmt_core_id, statementScore);
                        }
                    }

                }
                else
                {
                    //AtlasLog.LogError(String.Format("ERROR:RetrieveFromBDS failed with an http status code of {0}.", result.StatusCode), gptwContext);
                    statementScores = new Dictionary<string, string>();
                }
            }
            catch (Exception e)
            {
                //AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                statementScores = new Dictionary<string, string>();
            }

            return statementScores;
        }

    }
}
