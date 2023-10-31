using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Net;
//using System.Web.Http;
using SharedProject2;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Model;
using Portal.Controllers;
using System.Runtime.InteropServices;


namespace Reports.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : PortalControllerBase
    {

        public ReportsController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpGet("[action]")]
        public ReturnTree GetClientReportTree(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetClientReportTree");
            gptwContext.ClientId = clientId;

            ReturnTree returnTree = new ReturnTree { IsSuccess = false, ErrorMessage = "", ClientReportTree = new List<ClientReportTreeNode>() };

            try
            {

                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    returnTree.ErrorMessage = "Not Authorized";
                    return returnTree;
                }
                // retrieve the client report bundle from the repository
                ClientCenterRepository repo = new ClientCenterRepository(appSettings.MongoDBConnectionString);
                var clientBundle = repo.RetrieveClientReportBundle(clientId);

                // if the bundle is nothing or has no reports, just return an empty list of reports
                if (clientBundle == null)
                {
                    AtlasLog.LogWarning("Returned no client reports", gptwContext);
                    returnTree.IsSuccess = true;
                    return returnTree;
                }
                else if (clientBundle.ReportList.Count == 0)
                {
                    AtlasLog.LogWarning(String.Format("No reports returned for client:{0}", clientId), gptwContext);
                    returnTree.IsSuccess = true;
                    return returnTree;
                }

                // otherwise process the tree
                ClientReportTreeNode root = new ClientReportTreeNode();
                //root.text = "Reports";
                //root.nodeType = "Folder";
                //root.expanded = true;
                //root.hasChildren = true;

                // make some dictionaries to keep track of folders and years
                // key = year // value = reference to node
                Dictionary<int, ClientReportTreeNode> yearDictionary = new Dictionary<int, ClientReportTreeNode>();
                // key = year & foldername.tolower // value = reference to node
                Dictionary<string, ClientReportTreeNode> folderDictionary = new Dictionary<string, ClientReportTreeNode>();

                var wrkReportList = (from a in clientBundle.ReportList
                                     orderby a.PublishFolderYear descending,
                                             a.PublishFolderName ascending,
                                             a.ReportFriendlyNameFullNoExtension ascending
                                     select a).ToList();

                foreach (var wrkReport in wrkReportList)
                {
                    if (wrkReport.IsReportPublished)
                    {

                        // determine whether we need to add a node for the year
                        bool yearFound = false;
                        if (yearDictionary.ContainsKey(wrkReport.PublishFolderYear))
                            yearFound = true;

                        // if we need to make a node for the year
                        if (!yearFound)
                        {
                            ClientReportTreeNode yearNode = new ClientReportTreeNode();
                            yearNode.text = System.Convert.ToString(wrkReport.PublishFolderYear);
                            yearNode.nodeType = "Folder";
                            yearNode.expanded = true;
                            yearNode.hasChildren = true;

                            if (root.items.Count == 0)
                                root = yearNode;
                            else
                                root.items.Add(yearNode); // add to tree
                            yearDictionary.Add(wrkReport.PublishFolderYear, yearNode); // record to dictionary
                        } // end making year node

                        // determine whether we need to add a node for the folder
                        bool folderFound = false;
                        string foldername = wrkReport.PublishFolderYear.ToString() + wrkReport.PublishFolderName.ToLower();
                        if (folderDictionary.ContainsKey(foldername))
                            folderFound = true;

                        // if we need to make a node for the folder
                        if (!folderFound)
                        {
                            ClientReportTreeNode folderNode = new ClientReportTreeNode();
                            folderNode.text = wrkReport.PublishFolderName;
                            folderNode.nodeType = "Folder";
                            folderNode.expanded = true;
                            folderNode.hasChildren = true;
                            yearDictionary[wrkReport.PublishFolderYear].items.Add(folderNode); // add to tree
                            folderDictionary.Add(foldername, folderNode); // record in dictionary
                        } // end making folder node

                        // finally make the actual file download node and add it to the tree
                        ClientReportTreeNode fileNode = new ClientReportTreeNode();
                        fileNode.text = wrkReport.get_ReportFriendlyNameWithoutClientName(false); // TODO actually want this to be the shortened name
                        fileNode.nodeType = "File";
                        fileNode.imageUrl = "/zImages/TreeIcons/tree-file.png";
                        fileNode.nodeFileId = wrkReport.ReportFileId;
                        fileNode.nodeFileUri = wrkReport.FileUri;
                        fileNode.fullFileName = wrkReport.get_ReportFriendlyNameFull(true);
                        fileNode.PublishingGroupId = wrkReport.PublishingGroupId.ToString();
                        fileNode.PubGroupReportid = wrkReport.Id.ToString();
                        fileNode.AffiliateId = wrkReport.AffiliateId;


                        folderDictionary[foldername].items.Add(fileNode);
                    } // ensure report is published
                } // report in list

                List<ClientReportTreeNode> treeList = new List<ClientReportTreeNode>();
                treeList.Add(root);

                returnTree.IsSuccess = true;
                returnTree.ErrorMessage = "";
                returnTree.ClientReportTree = treeList;
                return returnTree;

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnTree.ErrorMessage = "GetClientReportTree Exception caught.";
            }

            return returnTree;
        }


        [HttpGet("[action]")]
        public IActionResult DownloadClientReport(int clientId, string fileUri, string affiliateId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("DownloadClientReport");
            gptwContext.ClientId = clientId;

            // retrieve the file from report store
            // and stream to user
            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    return NotFound();
                }

                // retrieve the file
                AtlasReportStoreFileStreamer fileStreamer = new AtlasReportStoreFileStreamer(appSettings.ReportStoreUrl, affiliateId, "api@greatplacetowork.com", appSettings.ReportStoreServicesToken);
                var reportFile = fileStreamer.GetStream(new GptwUri() { Uri = fileUri });

                // Mark that a report has been downloaded
                setCustomerActivationReportDownloadField(clientId, IsGPTWEmployee(gptwContext));

                return File(reportFile, "application/octet-stream");
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                var reportFile = "Sorry, you cannot download the file. If you continue to receive this message, please contact our support team.";
                return File(reportFile, "application/octet-stream");
            }
        }


        // This will find your local Downloads folder. Saved just in case I ever want it again....
        //public static class KnownFolder
        //{
        //    public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        //}

        //[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        //static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

        /// <summary>
        /// Find the latest completed, non-abandoned engagement for the client, if there is one
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <returns>EngagementId of the desired engagement</returns>
        /// <remarks>If no engagement was found, the returned engagementId will be 0</remarks>
        [NonAction]
        private int FindLatestCompletedEngagementId(int clientId)
        {
            int engagementId = 0;

            try
            {
                // Get a list of all engagements for this client    
                List<ECRV2> AllECRV2sForClient = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                // Choose the ones that are completed and not abandoned, sorted from latest to earliest
                List<ECRV2> ECRV2sCompleted = (from e in AllECRV2sForClient.AsQueryable() where (e.IsAbandoned == false && e.EngagementStatus == EngagementStatus.COMPLETE) orderby e.CreatedDate descending select e).ToList();

                // TODO do we want to log an empty list as an error?
                if (ECRV2sCompleted.Count > 0)
                {
                    engagementId = ECRV2sCompleted[0].EngagementId;
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e);
            }

            return engagementId;
        }

        /// <summary>
        /// Set the CustomerActivationReportDownload status
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <remarks>Returns engagementId = 0 if there are no completed emgagements for this client</remarks>
        private void setCustomerActivationReportDownloadField(int clientId, bool isEmployee)
        {
            int engagementId = FindLatestCompletedEngagementId(clientId);
            if (engagementId > 0)
            {
                if (!isEmployee)
                    this.SetCustomerActivationStatus(engagementId, EngagementUpdateField.CustomerActivationReportDownload);
            }
        }
    }
}


public class DownloadMessage
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }

    public FileStream file { get; set; }

    public string url { get; set; }
}
public class ReturnTree
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }

    public List<ClientReportTreeNode> ClientReportTree { get; set; }
}

public class ClientReportTreeNode
{
    public string text { get; set; } = "";
    public List<ClientReportTreeNode> items { get; set; } = new List<ClientReportTreeNode>();
    public bool expanded { get; set; } = false;
    public bool hasChildren { get; set; } = false;
    public string imageUrl { get; set; } = "/zImages/TreeIcons/tree-folder.png";
    public string nodeType { get; set; } = "";
    public int nodeFileId { get; set; } = 0;
    public string nodeFileUri { get; set; } = "";
    public string fullFileName { get; set; } = "";
    public string PublishingGroupId { get; set; }
    public string PubGroupReportid { get; set; }
    public string AffiliateId { get; set; }

}
