﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMIOD.Models
{
    public class Container

    {
        public int id { get; set; }
        public string name { get; set; }

        public DateTime creation_dt { get; set; }

        public int parent { get; set; }
    }
}
