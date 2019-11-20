using System.Collections.Generic;

namespace Dasync.Hosting.AspNetCore.Errors
{
    public class Error
    {
        public int Code { get; set; }

        public string Type { get; set; }

        public string Message { get; set; }

        public List<Error> Errors { get; set; }

        //public string Domain { get; set; }

        //public string Reason { get; set; }

        //public string Location { get; set; }

        //public string LocationType { get; set; }

        public string ExtendedHelp { get; set; }

        //public string SendReport { get; set; }

        public string StackTrace { get; set; }

        public System.Collections.IDictionary ExtendedProperties { get; set; }
    }
}
