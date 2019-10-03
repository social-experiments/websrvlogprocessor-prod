using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace parserlib
{
    //Utility Class to Upload to Blob
    public class AzureBlobStorage : IStorage
    {
        public async Task Create(Stream stream, string path, string containerName, string storageConnectionString)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(storageConnectionString);

            var blobClient = account.CreateCloudBlobClient();

            var blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateIfNotExistsAsync();

            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(path);
            await blockBlob.UploadFromStreamAsync(stream);

        }
    }
}
