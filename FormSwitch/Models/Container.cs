using System.Xml.Serialization;

namespace FormSwitch.Models
{
    [XmlRoot("Container")]
    public class Container
    {
        [XmlElement("name")]
        public string Name { get; set; }

        // Construtor sem parâmetros necessário para a serialização
        public Container()
        {
        }

        public Container(string name)
        {
            Name = name;
        }
    }
}
