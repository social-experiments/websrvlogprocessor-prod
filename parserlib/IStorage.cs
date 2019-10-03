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
    public interface IStorage
    {
        Task Create(Stream stream, string path, string containerName, string storageConnectionString);
    }
}
