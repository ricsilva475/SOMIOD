using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace SOMIOD.Models
{
    public class Subscription
    {
        public int id { get; set; }
        public string name { get; set; }

        public DateTime creation_dt { get; set; }
        public int parent { get; set; }
        public string subscription_event { get; set; } //1 para criacao e 2 para excluir

        public string endpoint { get; set; }
    }
}


