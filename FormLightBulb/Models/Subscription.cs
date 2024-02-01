using System.Xml.Serialization;

namespace FormLightBulb.Models
{
    [XmlRoot("Subscription")]
    public class Subscription
    {
        [XmlElement("name")]
        public string name_sub { get; set; }

        [XmlElement("subscription_event")]
        public string subscription_event { get; set; }

        [XmlElement("endpoint")]
        public string endpoint { get; set; }

        public Subscription()
        {
            
        }
        public Subscription(string nameVal, string subscription_eventVal, string endpointVal) 
        {
            name_sub = nameVal;
            subscription_event = subscription_eventVal;
            endpoint = endpointVal;
        }
    }
}
