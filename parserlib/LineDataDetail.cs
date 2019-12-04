using System;
using System.Collections.Generic;
using System.Text;

namespace ParserLib
{
    //contains information taken out of each line of the log
    public class LineDataDetail
    {
        public string ClientIP { get; set; }
        public string Date { get; set; }
        public string Module { get; set; }
        public long Bandwidth { get; set; }

        //note whether this particular line is of JSON type or of "tokenized" type
        //these are the only types of log lines that we know of, for now
        public string LineType { get; set; }

        //to account for some specific condition in "tokenized" lines that the lines must be skipped
        public bool Skip { get; set; }
    }
}
