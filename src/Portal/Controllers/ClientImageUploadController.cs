//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;

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
    public class ClientImageUploadController : PortalControllerBase
    {

        public ClientImageUploadController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpPost("[Action]")]
        async public Task<IActionResult> SaveFile(List<IFormFile> files)
        {
            string cid = Request.Form["cid"].ToString();
            string eid = Request.Form["eid"].ToString();

            IFormFile ff = files[0];
            Guid blobName = Guid.NewGuid();
            string extension = Path.GetExtension(ff.FileName);
            string fileName = blobName.ToString() + extension;

            var filestream = ff.OpenReadStream();

            if (!ValidateFile(fileName, filestream, cid))
            {
                AtlasLog.LogError(String.Format("Portal:ClientImageUploadController ValidateFile failed. cid:{0}, eid:{1}", cid, eid));

                return Ok(new
                {
                    blobName = "VALIDATION_ERROR",
                    fileName = "VALIDATION_ERROR",
                    size = 0
                });
            }

            // Set the stream position to the beginning of the file.
            filestream.Seek(0, SeekOrigin.Begin);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(appSettings.ClientAssetsBlobStorageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference("images");

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
