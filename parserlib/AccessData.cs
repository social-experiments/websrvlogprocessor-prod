using System;
using System.Collections.Generic;
using System.Text;

namespace ParserLib
{
    //Represents a record in Access Log
    public class AccessData
    {
        public string ModuleName { get; set; }
        public long MainModuleCount { get; set; }
        public long SubModuleCount { get; set; }
        public DateTime UpLoadTime { get; set; }
        public long Bandwidth { get; set; }
    }
}
