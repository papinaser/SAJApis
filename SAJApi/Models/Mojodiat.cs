using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAJApi.Models
{
    public class MojodiatModel
    {
        public string code { get; set; }

        public string parentCode { get; set; }

        public string name { get; set; }
    }
    public class GetMojodiatsModel
    {
        public string result { get; set; }
        public string parentCode { get; set; }
        public List<MojodiatModel> ChildList { get; set; }
    }
}