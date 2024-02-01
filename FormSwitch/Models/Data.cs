using System.Xml.Serialization;

namespace FormSwitch.Models
{
    [XmlRoot("Data")]
    public class Data
    {
        [XmlElement("content")]
        public string Content { get; set; }

        public Data()
        {
        }

        public Data(string content)
        {
            Content = content;
        }
    }
}
