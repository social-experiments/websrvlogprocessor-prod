using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using ParserLib;

namespace ParserFn
{
    public static class LogToJson
    {
        private static string connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static string containerName = System.Environment.GetEnvironmentVariable("WebSrvLogContainerName");

        [FunctionName("LogToJson")]
        public static void Run([BlobTrigger("webserverlog/{name}", Connection = "AzureWebJobsStorage")]Stream accessLogBlob, string name, ILogger log)
        {
            LogParser parserObj = new LogParser(connectionString, containerName, true);

            log.LogInformation("Connection String: " + connectionString);
            log.LogInformation("Container Name: " + containerName);

            //Parse the Access Log and Store summary data in JSON - One per day.
            IList<string> resultList = parserObj.Parse(name, accessLogBlob);
            if (resultList == null)
            {
                log.LogError("Error Parsing the Access Log");
                return;
            }
            log.LogInformation("Successfully Parsed the Access Log");

            //Upload the JSON Files to azure blob container
            bool uploadStatus = parserObj.Upload(resultList);
            if (uploadStatus == false)
            {
                log.LogError("Error Parsing the Access Log");
                return;
            }
            log.LogInformation("Successfully Uploaded");
        }
    }
}
