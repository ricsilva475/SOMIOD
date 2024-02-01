using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FormLightBulb.Models
{
    [XmlRoot("Application")]

    public class Application
    {

        [XmlElement("name")]
        public string name { get; set; }

        public Application()
        {
        }

        public Application(string nameVal)
        {
            name = nameVal;
        }
    }

}
