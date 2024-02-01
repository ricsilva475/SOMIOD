using System.Xml.Serialization;

namespace FormLightBulb.Models
{
    [XmlRoot("Container")]
    public class Container
    {
        [XmlElement("name")]
        public string nameCont { get; set; }


        public Container()
        {
        }
        public Container(string nameVal)
        {
            nameCont = nameVal;

        }
    }
}
