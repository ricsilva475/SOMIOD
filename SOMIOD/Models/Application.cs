using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMIOD.Models
{
    public class Application
    {
        public int id { get; set; } 
        public string name { get; set; }

        public DateTime creation_dt { get; set; }
        
    }

    public class ApplicationTest
    {
        public string name { get; set; }

    }
}