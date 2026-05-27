// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OdhApiCore.Swagger;
using Microsoft.Extensions.Logging;

namespace OdhApiCore.Controllers.api
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    //[Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment env;
        private readonly ISettings settings;
        protected ILogger<OdhController> Logger { get; }

        public FileUploadController(
            IWebHostEnvironment env,
            ISettings settings,
            ILogger<OdhController> logger
        )
        {
            this.env = env;
            this.settings = settings;
            this.Logger = logger;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        //[HttpPost, Route("v1/FileUpload/{type}/{directory}")]
        [HttpPost, Route("v1/FileUpload")]
        [HttpPost, Route("v1/FileUpload/Image")]
        public async Task<IActionResult> PostFormData(IFormCollection form)
        {
            var filenames = new List<string>();

            // read from settings
            var keyid = settings.S3ImageresizerConfig.AccessKey;
            var key = settings.S3ImageresizerConfig.SecretKey;
            var bucketName = settings.S3ImageresizerConfig.BucketAccessPoint;

            var creds = new BasicAWSCredentials(keyid, key);
            var config = new AmazonS3Config();
            config.RegionEndpoint = RegionEndpoint.EUWest1;
            var client = new AmazonS3Client(creds, config);

            foreach (var file in form.Files)
            {
                var filename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var request = new PutObjectRequest //UploadPartRequest
                {
                    BucketName = bucketName,
                    Key = filename,
                    InputStream = file.OpenReadStream(),
                    ContentType = file.ContentType,
                };
                var response = await client.PutObjectAsync(request); //UploadPartAsync(request);

                if (IsFileImage(file.ContentType))
                    filenames.Add(
                        String.Format("{0}{1}", settings.S3ImageresizerConfig.Url, filename)
                    );
                else
                    filenames.Add(
                        String.Format("{0}{1}", settings.S3ImageresizerConfig.DocUrl, filename)
                    );
            }
            if (filenames.Count == 1)
                return Ok(filenames.FirstOrDefault());
            else
                return Ok(filenames);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpPost, Route("v1/FileUpload/Doc")]
        public async Task<IActionResult> PostFormDataPDF(IFormCollection form)
        {
            var filenames = new List<string>();

            // read from settings
            var keyid = settings.S3ImageresizerConfig.AccessKey;
            var key = settings.S3ImageresizerConfig.SecretKey;
            var bucketName = settings.S3ImageresizerConfig.BucketAccessPoint;

            var creds = new BasicAWSCredentials(keyid, key);
            var config = new AmazonS3Config();
            config.RegionEndpoint = RegionEndpoint.EUWest1;
            var client = new AmazonS3Client(creds, config);

            foreach (var file in form.Files)
            {
                var filename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = filename,
                    InputStream = file.OpenReadStream(),
                    ContentType = file.ContentType,
                };
                var response = await client.PutObjectAsync(request);

                filenames.Add(
                    String.Format("{0}{1}", settings.S3ImageresizerConfig.DocUrl, filename)
                );
            }
            if (filenames.Count == 1)
                return Ok(filenames.FirstOrDefault());
            else
                return Ok(filenames);
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpDelete, Route("v1/FileDelete/{filepath}")]
        public async Task<IActionResult> Delete(string filepath)
        {
            string keyName = filepath.Replace("|", "\\").Replace("$", ".");

            // read from settings
            var keyid = settings.S3ImageresizerConfig.AccessKey;
            var key = settings.S3ImageresizerConfig.SecretKey;
            var bucketName = settings.S3ImageresizerConfig.BucketAccessPoint;

            var creds = new BasicAWSCredentials(keyid, key);
            var config = new AmazonS3Config();
            config.RegionEndpoint = RegionEndpoint.EUWest1;
            var client = new AmazonS3Client(creds, config);

            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                };

                Console.WriteLine("Deleting an object");
                var deleteresult = await client.DeleteObjectAsync(deleteObjectRequest);

                //AWS api is always returning nocontent
                //if(deleteresult != null )
                //{
                //    switch(deleteresult.HttpStatusCode)
                //    {
                //        case System.Net.HttpStatusCode.OK:
                //            return Ok(String.Format("Success: '{0}' deleted", filepath));
                //        case System.Net.HttpStatusCode.NotFound:
                //        case System.Net.HttpStatusCode.NoContent:
                //            return NotFound(String.Format("Not found: '{0}'", filepath));
                //        default:
                //            return BadRequest(String.Format("An error occured Http Status: '{0}'", deleteresult.HttpStatusCode.ToString()));

                //    }
                //}
                //else
                //{
                //    return BadRequest("Generic Error");
                //}

                return Ok(String.Format("Success: '{0}' deleted", filepath));
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(
                    String.Format(
                        "Error encountered on server.Message:'{0}' when deleting an object",
                        e.Message
                    )
                );
            }
            catch (Exception e)
            {
                return BadRequest(
                    String.Format(
                        "Unknown encountered on server. Message:'{0}' when deleting an object",
                        e.Message
                    )
                );
            }
        }

        //Hack check if contenttype begins with image
        private static bool IsFileImage(string contenttype)
        {
            return contenttype.ToLower().StartsWith("image");
        }
    }
}
