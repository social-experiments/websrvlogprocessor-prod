using ParserLib;
using System;
using System.IO;

namespace Application
{
    class ParserApp
    {
        static void Main(string[] args)
        {
            //try
            //{
                string connectionString = "";
                string containerName = "";

                LogParser parObj = new LogParser(connectionString, containerName, true);
                string fileName = @"access4.log";
                var streamObj = File.Open(fileName, FileMode.Open);
                var result = parObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);
                parObj.Upload(result);

                fileName = @"access5.log";
                parObj = new LogParser(connectionString, containerName, true);
                streamObj = File.Open(fileName, FileMode.Open);
                result = parObj.Parse("worldpossible@outlook.com_2019-09-03T00:40:26 00:00", streamObj);
                parObj.Upload(result);
            //}
            //catch(Exception)
            //{
            //    Console.WriteLine("Exception in Main");
            //}
        }
    }
}
