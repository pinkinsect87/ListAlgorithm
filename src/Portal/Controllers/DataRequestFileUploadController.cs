using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Portal.Model;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Net.Http;
using System.IO;

namespace Portal.Controllers
{
    [Route("api/[controller]")]
    public class DataRequestFileUploadController : PortalControllerBase
    {

        public DataRequestFileUploadController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpPost("[Action]")]
        async public Task<IActionResult> SaveFile(List<IFormFile> files)
        {
            string affiliateId = Request.Form["affiliateId"].ToString();

            IFormFile ff = files[0];
            Guid blobName = Guid.NewGuid();
            string extension = Path.GetExtension(ff.FileName);
            string fileName = blobName.ToString() + extension;

            var filestream = ff.OpenReadStream();
            
            filestream.Seek(0, SeekOrigin.Begin);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(appSettings.DataConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference("dataextract/" + affiliateId);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            await blockBlob.UploadFromStreamAsync(filestream);

            return Ok(new
            {
                blobName = blobName,
                fileName = fileName,
                size = blockBlob.Properties.Length
            });
        }
    }
}
