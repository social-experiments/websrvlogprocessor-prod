using ParserLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;

namespace ParserTest
{
    [TestClass]
    public class LogParserUT
    {
        [TestMethod]
        public void AllRecords_SameDate_Submodule()
        {
            string fileName = "access1.log";

            LogParser parserObj = new LogParser(string.Empty, string.Empty, false);

            var streamObj = File.Open(fileName, FileMode.Open);
            var result = parserObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);

            Assert.AreNotEqual(result, null);
            Assert.AreEqual(result.Count, 1);

            var content = File.ReadAllText(result[0]);
            AccessDataDetail addObj = JsonConvert.DeserializeObject<AccessDataDetail>(content);
            Assert.AreEqual(addObj.AccessDetails.Count, 1);
            Assert.AreEqual(addObj.AccessDetails[0].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[0].SubModuleCount, 1);
        }

        [TestMethod]
        public void AllRecords_SameDate_Mainmodule()
        {
            string fileName = "access2.log";
            LogParser parserObj = new LogParser(string.Empty, string.Empty, false);

            var streamObj = File.Open(fileName, FileMode.Open);
            var result = parserObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);

            Assert.AreNotEqual(result, null);
            Assert.AreEqual(result.Count, 1);

            var content = File.ReadAllText(result[0]);
            AccessDataDetail addObj = JsonConvert.DeserializeObject<AccessDataDetail>(content);
            Assert.AreEqual(addObj.AccessDetails.Count, 1);
            Assert.AreEqual(addObj.AccessDetails[0].MainModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[0].SubModuleCount, 0);
        }

        [TestMethod]
        public void AllRecords_SameDate_DiffTimes()
        {
            string fileName = "access3.log";
            LogParser parserObj = new LogParser(string.Empty, string.Empty, false);

            var streamObj = File.Open(fileName, FileMode.Open);
            var result = parserObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);

            Assert.AreNotEqual(result, null);
            Assert.AreEqual(result.Count, 1);

            var content = File.ReadAllText(result[0]);
            AccessDataDetail addObj = JsonConvert.DeserializeObject<AccessDataDetail>(content);
            Assert.AreEqual(addObj.AccessDetails.Count, 3);
            Assert.AreEqual(addObj.AccessDetails[0].ModuleName, "en-teachertraining");
            Assert.AreEqual(addObj.AccessDetails[0].MainModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[0].SubModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[1].ModuleName, "en-kolibri-index");
            Assert.AreEqual(addObj.AccessDetails[1].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[1].SubModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[2].ModuleName, "en-wikipedia");
            Assert.AreEqual(addObj.AccessDetails[2].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[2].SubModuleCount, 1);
        }

        [TestMethod]
        public void OverlapplingRecords()
        {
            string fileName = "access4.log";
            string uploadDir = "upload4";

            if (Directory.Exists(uploadDir))
                Directory.Delete(uploadDir);

            Directory.CreateDirectory(uploadDir);

            LogParser parserObj = new LogParser(string.Empty, string.Empty, false, uploadDir);

            var streamObj = File.Open(fileName, FileMode.Open);
            var result = parserObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);

            Assert.AreNotEqual(result, null);
            Assert.AreEqual(result.Count, 1);

            var content = File.ReadAllText(result[0]);
            AccessDataDetail addObj = JsonConvert.DeserializeObject<AccessDataDetail>(content);
            Assert.AreEqual(addObj.AccessDetails.Count, 3);
            Assert.AreEqual(addObj.AccessDetails[0].ModuleName, "en-teachertraining");
            Assert.AreEqual(addObj.AccessDetails[0].MainModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[0].SubModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[1].ModuleName, "en-kolibri-index");
            Assert.AreEqual(addObj.AccessDetails[1].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[1].SubModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[2].ModuleName, "en-wikipedia");
            Assert.AreEqual(addObj.AccessDetails[2].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[2].SubModuleCount, 1);

            var uploadResult = parserObj.Upload(result);
            Assert.AreEqual(uploadResult, true);

            fileName = "access5.log";
            LogParser overlapParserObj = new LogParser(string.Empty, string.Empty, false, uploadDir);

            var overlapStreamObj = File.Open(fileName, FileMode.Open);
            var overlapResult = parserObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);

            Assert.AreNotEqual(result, null);
            Assert.AreEqual(result.Count, 1);

            var overlapContent = File.ReadAllText(result[0]);
            AccessDataDetail overlapAddObj = JsonConvert.DeserializeObject<AccessDataDetail>(content);
            Assert.AreEqual(addObj.AccessDetails.Count, 4);
            Assert.AreEqual(addObj.AccessDetails[0].ModuleName, "en-teachertraining");
            Assert.AreEqual(addObj.AccessDetails[0].MainModuleCount, 2);
            Assert.AreEqual(addObj.AccessDetails[0].SubModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[1].ModuleName, "en-kolibri-index");
            Assert.AreEqual(addObj.AccessDetails[1].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[1].SubModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[2].ModuleName, "en-wikipedia");
            Assert.AreEqual(addObj.AccessDetails[2].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[2].SubModuleCount, 1);
            Assert.AreEqual(addObj.AccessDetails[2].ModuleName, "en-wikipedia_for_schools");
            Assert.AreEqual(addObj.AccessDetails[2].MainModuleCount, 0);
            Assert.AreEqual(addObj.AccessDetails[2].SubModuleCount, 1);

            var overlapUploadResult = parserObj.Upload(result);
            Assert.AreEqual(uploadResult, true);

            Directory.Delete(uploadDir);
        }
    }
}
