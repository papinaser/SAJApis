using System.Collections.Generic;

namespace SAJApi.Models
{
    public class KeyValueModel
    {
        public string value { get; set; }
        public string label { get; set; }
    }
    public class selectDateModel
    {
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int daysDiff { get; set; }
    }
    public class treeModel
    {
        public string id { get; set; }
        public string title { get; set; }
        public bool hasChildren { get; set; }
        public List<treeModel> childs { get; set; }
    }
}