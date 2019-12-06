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

namespace ParserLib
{
    public class LogParser
    {
        private IDictionary<string, IDictionary<string, AccessData>> accessDataList = new Dictionary<string, IDictionary<string, AccessData>>();
        private IDictionary<string, DateRange> dateRangeList = new Dictionary<string, DateRange>();
        private IDictionary<string, AccessDataDetail> existingRecords = new Dictionary<string, AccessDataDetail>();

        private string connectionString;
        private string containerName;
        private bool inCloud;
        private string localPath;

        public LogParser(string connectionString, string containerName, bool inCloud, string localPath = "")
        {
            this.connectionString = connectionString;
            this.containerName = containerName;
            this.inCloud = inCloud;
            this.localPath = localPath;
        }

        //Blob Name is of the following format
        //abc@def.ghi.com_AccessDate
        //Device Id is "def" - that is anything between @ and the first "." 
        private string GetDeviceId(string blobName)
        {
            string[] blobSubStrings = blobName.Split('@', '.');
            if (blobSubStrings.Length >= 2)
                return blobSubStrings[1];
            return "unknown";
        }

        private string ComputeJsonName(string deviceId, string accessDate)
        {
            string result = deviceId + "_" + accessDate + ".json";
            return result;
        }

        public AccessDataDetail DeserializeJsonDataInCloud(string deviceId, string accessDate)
        {
            string jsonName = ComputeJsonName(deviceId, accessDate);

            // connect to our storage account and create a blob client
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // get a reference to the container
            var blobcontainer = blobClient.GetContainerReference(containerName);
            blobcontainer.CreateIfNotExists();

            foreach (var blob in blobcontainer.ListBlobs())
            {
                string flName = blob.Uri.Segments.Last().ToString();
                Console.WriteLine(flName);

                if (jsonName == flName)
                {
                    CloudBlob cblob = blobcontainer.GetBlobReference(flName);
                    using (StreamReader reader = new StreamReader(cblob.OpenRead()))
                    {
                        string content = "";
                        StringBuilder strjson = new StringBuilder();
                        while ((content = reader.ReadLine()) != null)
                        {
                            strjson.Append(content);
                        }
                        AccessDataDetail overlapAddObj = JsonConvert.DeserializeObject<AccessDataDetail>(strjson.ToString());
                        return overlapAddObj;
                    }
                }
            }

            return null;
        }

        private AccessDataDetail DeserializeJsonDataInLocal(string deviceId, string accessDate)
        {
            string jsonName = ComputeJsonName(deviceId, accessDate);

            if (!Directory.Exists(localPath))
                return null;

            string fullPath = Path.Combine(localPath, jsonName);
            if (File.Exists(fullPath))
            {
                var content = File.ReadAllText(fullPath);
                AccessDataDetail deserializedObj = JsonConvert.DeserializeObject<AccessDataDetail>(content);
                return deserializedObj;
            }

            return null;
        }

        private AccessDataDetail DeserializeJsonData(string deviceId, string accessDate)
        {
            try
            {
                if (inCloud)
                    return DeserializeJsonDataInCloud(deviceId, accessDate);

                return DeserializeJsonDataInLocal(deviceId, accessDate);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Updates a bandwidth record for the given module
        // Returns true if this module was already in the dictionary,
        // Returns false otherwise
        private void IncrementModuleBandwidth(IDictionary<string, long> bandwidths, string module, long bandwidth)
        {
            if (!bandwidths.TryGetValue(module, out long current)) {
                bandwidths.Add(module, bandwidth);
            } else
            {
                bandwidths[module] = current + bandwidth;
            }
        }

        private void IncrementModuleBandwidth(IDictionary<string, IDictionary<string, long>> bandwidths, string date, string module, long bandwidth)
        {
            if (!bandwidths.TryGetValue(date, out IDictionary<string, long> dateBandwidths))
            {
                dateBandwidths = new Dictionary<string, long>();
                bandwidths.Add(date, dateBandwidths);
            }
            IncrementModuleBandwidth(dateBandwidths, module, bandwidth);
        }

        private LineDataDetail ExtractLineData(string lineEntry)
        {
            LineDataDetail info = new LineDataDetail();

            try
            {
                dynamic line = JsonConvert.DeserializeObject(lineEntry);
                info.ClientIP = line.remote_addr;
                //info.Date = line.time_local.Substring(0, line.time_local.Length - 6);
                info.Date = ((string)line.time_local).Substring(0, ((string)line.time_local).Length - 6);
                info.Module = line.request;
                info.Bandwidth = Convert.ToInt64(line.body_bytes_sent);
                info.LineType = "json";
                info.Skip = false;
            }
            catch (JsonReaderException)
            {
                string[] tokens = lineEntry.Split(' ');

                info.ClientIP = string.Empty;
                info.Date = string.Empty;
                info.Module = string.Empty;
                info.LineType = "tokenized";
                info.Bandwidth = 0;

                //If the first token (token[0]) is not of type IP address - skip processing - evaluate via Regex
                Regex ipRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                MatchCollection regexResult = ipRegex.Matches(tokens[0]);

                if (regexResult.Count == 0 || tokens.Length <= 6)
                {
                    //this line would be skipped anyway so no data is recorded because the line is probably broken
                    info.Skip = true;
                }
                else
                {
                    //token[0] - ip address
                    info.ClientIP = tokens[0];
                    //Ignore token[1] & token[2] - Irrelevant
                    //token[3] - Date information
                    info.Date = tokens[3].Substring(1, tokens[3].Length - 7);
                    //Ignore token[4] & token[5] - Irrelevant
                    //token[6] - Contains Module Information.
                    info.Module = tokens[6];
                    //token[7] and token[8] are not used for now
                    //token[9] - Contains bandwidth info
                    info.Bandwidth = Convert.ToInt64(tokens[9]);
                    info.Skip = false;
                }                    
            }

            return info;
        }

        public IList<string> Parse(string blobName, Stream accessLogBlob)
        {
            string deviceId = GetDeviceId(blobName);

            // Using another object that does its work before the skip logic. This should be refactored at some point.
            IDictionary<string, IDictionary<string, long>> bandwidths = new Dictionary<string, IDictionary<string, long>>();  // any way to approximate final size to reduce rehashes?

            long totalBandwidth = 0L;

            IList<string> result = new List<string>();
            string lineEntry = string.Empty;

            //Initialize the prev record to some random date
            DateTime prevRecord = DateTime.Parse("01/01/1900");

            string prevLineType = string.Empty;

            var file = new StreamReader(accessLogBlob);
            while ((lineEntry = file.ReadLine()) != null)
            {
                LineDataDetail lineInfo = ExtractLineData(lineEntry);

                //skip the next line if the next line is not of the same type as the previous type
                //deciding type is the type of the very first line
                //this assumes that the group of the lines of different type contain the same data so they can just be skipped without losing data
                if (!prevLineType.Equals(string.Empty)) 
                {
                    if (!lineInfo.LineType.Equals(prevLineType))
                    {
                        continue;
                    }
                }
                else
                {
                    prevLineType = lineInfo.LineType;
                }

                if (lineInfo.Skip)
                {
                    continue;
                }

                string dateValue = lineInfo.Date;
                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime dtObj = DateTime.ParseExact(dateValue, "dd/MMM/yyyy:HH:mm:ss", provider);

                //TODO return if date is not valid
                string formattedDateValue = dtObj.ToString("yyyyMMdd");

                string module = lineInfo.Module;
                
                long bandwidth = lineInfo.Bandwidth;

                string[] moduleTokens = module.Split('/', '?');

                //TODO: We skip this for now - need to check if this is an error and log appropriately
                if (moduleTokens.Length <= 2)
                {
                    //continue;
                    throw new Exception();
                }

                // Handle bandwidth before we skip
                // Cleans hour:minute:second from the datetime string
                IncrementModuleBandwidth(bandwidths, formattedDateValue, moduleTokens[2], bandwidth);
                Console.WriteLine("{0} {1}", moduleTokens[2], bandwidth);
                totalBandwidth += bandwidth;

                //Specification says, the module url should start with "modules" - if that's not the case skip
                if (!moduleTokens.Contains("modules"))
                {
                    continue;
                }

                //Ignore all the Image based URLs
                if (moduleTokens[moduleTokens.Length - 1].Contains(".png") || moduleTokens[moduleTokens.Length - 1].Contains(".jpg") ||
                        moduleTokens[moduleTokens.Length - 1].Contains(".bmp") || moduleTokens[moduleTokens.Length - 1].Contains(".gif") ||
                        moduleTokens[moduleTokens.Length - 1].Contains(".js") || moduleTokens[moduleTokens.Length - 1].Contains(".css"))
                {
                    continue;
                }

                //Ignore all the request that came for the same time - An html page has css, js, and lot of references - they dont count towards an article that was read - round off to 1 event for 1 second.
                if (prevRecord == dtObj)
                    continue;
                else
                    prevRecord = dtObj;

                //If we reached here - we are certainly processing the record - Save any metadata information about the record.
                if (!dateRangeList.ContainsKey(formattedDateValue))
                {
                    //If there is no record for a date - create a record and register start and end time as that of current log entry
                    dateRangeList.Add(formattedDateValue, new DateRange() { StartTime = DateTime.Parse(dtObj.ToString("HH:mm:ss")), EndTime = DateTime.Parse(dtObj.ToString("HH:mm:ss")) });
                }
                else
                {
                    //If there exist a record, update only the end time.
                    DateRange rangeObj = dateRangeList[formattedDateValue];
                    rangeObj.EndTime = DateTime.Parse(dtObj.ToString("HH:mm:ss"));
                }

                if (!existingRecords.ContainsKey(formattedDateValue))
                {
                    //Check if there is a JSON for this date - if exist load the JSON in memory
                    AccessDataDetail existingObj = DeserializeJsonData(deviceId, formattedDateValue);
                    if (existingObj != null)
                    {
                        existingRecords.Add(formattedDateValue, existingObj);
                    }
                    else
                    {
                        existingRecords.Add(formattedDateValue, null);
                    }
                }

                if (existingRecords[formattedDateValue] != null)
                {
                    DateTime startTime = existingRecords[formattedDateValue].StartTime;
                    DateTime endTime = existingRecords[formattedDateValue].EndTime;

                    DateTime currrentTime = DateTime.Parse(dtObj.ToString("HH:mm:ss"));

                    //TODO: need to ignore module count but still add to bandwidth count here
                    //If the current log entry is a overlapping with existing record - ignore
                    if (currrentTime >= startTime && currrentTime <= endTime)
                    {
                        //it seems that total bandwidth is still being added to totalBandwidth at this point
                        //because of continue, the module count below is not executed but the bandwidth count before the continue is still executed
                        //so far, it seems nothing needs to be done for this TODO
                        continue;
                    }
                }

                if (!accessDataList.ContainsKey(formattedDateValue))
                {
                    //Add new value object (Dictionary) for each date
                    IDictionary<string, AccessData> accessDataKVPair = new Dictionary<string, AccessData>();
                    accessDataList.Add(formattedDateValue, accessDataKVPair);
                }

                IDictionary<string, AccessData> dictionaryObj = accessDataList[formattedDateValue];

                string moduleName = moduleTokens[2];
                if (!dictionaryObj.ContainsKey(moduleName))
                {
                    //Add new value object (AccessData) for each record in access log
                    AccessData obj = new AccessData() { ModuleName = moduleName, MainModuleCount = 0, SubModuleCount = 0, UpLoadTime = dtObj };
                    dictionaryObj.Add(moduleName, obj);
                }

                AccessData accessDataObj = dictionaryObj[moduleName];
                //Check if the request is for Main Module
                if (moduleTokens[3].Contains(".htm") || moduleTokens[3].Contains("html"))
                {
                    accessDataObj.MainModuleCount += 1;
                }
                else
                {
                    accessDataObj.SubModuleCount += 1;
                }

            }

            //Iterate AccessDataDetails for each date
            foreach (string dateValue in accessDataList.Keys)
            {
                AccessDataDetail addObj = new AccessDataDetail();
                addObj.AccessDate = dateValue;
                addObj.DeviceId = deviceId;
                addObj.Bandwidth = totalBandwidth;

                if (dateRangeList.ContainsKey(dateValue))
                {
                    addObj.StartTime = dateRangeList[dateValue].StartTime;
                    addObj.EndTime = dateRangeList[dateValue].EndTime;
                }

                IDictionary<string, AccessData> dictionaryObj = accessDataList[dateValue];
                foreach (string moduleName in dictionaryObj.Keys)
                {
                    var next = dictionaryObj[moduleName];
                    next.Bandwidth = (bandwidths[dateValue])[moduleName];
                    addObj.AccessDetails.Add(next);
                    Console.WriteLine(moduleName);
                }

                // Merge existing record with current record
                if (existingRecords.ContainsKey(dateValue))
                {
                    if (existingRecords[dateValue] != null)
                    {
                        if (existingRecords[dateValue].StartTime != null)
                        {
                            //Update Start Time with minimum of both
                            if (existingRecords[dateValue].StartTime <= addObj.StartTime)
                                addObj.StartTime = existingRecords[dateValue].StartTime;
                        }

                        if (existingRecords[dateValue].EndTime != null)
                        {
                            //Update End Time with maximum of both
                            if (existingRecords[dateValue].EndTime >= addObj.EndTime)
                                addObj.EndTime = existingRecords[dateValue].EndTime;
                        }

                        foreach (AccessData existingaddObj in existingRecords[dateValue].AccessDetails)
                        {
                            string moduleName = existingaddObj.ModuleName;
                            //Check if record exist for this module
                            bool matchFound = false;
                            foreach (AccessData newaddObj in addObj.AccessDetails)
                            {
                                if (moduleName == newaddObj.ModuleName)
                                {
                                    matchFound = true;
                                    newaddObj.MainModuleCount += existingaddObj.MainModuleCount;
                                    newaddObj.SubModuleCount += existingaddObj.SubModuleCount;
                                    break;
                                }
                            }

                            if (matchFound == false)
                            {
                                addObj.AccessDetails.Add(existingaddObj);
                            }
                        }
                    }
                }

                //Serialize AccessDataDetail into JSON File
                string jsonValue = JsonConvert.SerializeObject(addObj, Formatting.Indented);
                Console.WriteLine(jsonValue);
                Console.WriteLine();

                try
                {
                    string tempPath = Path.GetTempPath();
                    string fileName = ComputeJsonName(addObj.DeviceId, addObj.AccessDate);
                    string filePath = Path.Combine(tempPath, fileName);
                    StreamWriter sw = new StreamWriter(filePath);
                    sw.WriteLine(jsonValue);
                    sw.Close();
                    result.Add(filePath);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return result;
        }

        //Upload JSON Files to Temp Path
        private bool UploadFile(IList<string> fileList)
        {
            if (!Directory.Exists(localPath))
                return false;

            foreach (string jsonPath in fileList)
            {
                FileInfo fInfo = new FileInfo(jsonPath);
                string destPath = Path.Combine(localPath, fInfo.Name);
                File.Copy(jsonPath, destPath, true);
            }
            return true;
        }

        //Upload JSON Files to blob container
        private bool UploadBlob(IList<string> fileList)
        {
            foreach (string filePath in fileList)
            {
                using (FileStream f = File.Open(filePath, FileMode.Open))
                {
                    try
                    {
                        var storage = new AzureBlobStorage();
                        string fileName = Path.GetFileName(filePath);
                        // Pattern to run an async code from a sync method
                        storage.Create(f, fileName, containerName, connectionString).ContinueWith(t =>
                        {
                            if (t.IsCompleted)
                            {
                                Console.Out.WriteLine("Blob uploaded");
                            }
                        }).Wait();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //Upload JSON Files to blob container
        public bool Upload(IList<string> fileList)
        {
            if (inCloud)
                return UploadBlob(fileList);

            return UploadFile(fileList);
        }

    }
}
