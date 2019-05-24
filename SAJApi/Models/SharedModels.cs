namespace SAJApi.Models
{
    public class KeyValueModel
    {
        public string key { get; set; }
        public string value { get; set; }
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
        public System.Collections.Generic.List<treeModel> childs { get; set; }
    }
}