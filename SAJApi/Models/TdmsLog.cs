using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SAJApi.Models
{    
    public class SaveDataEntryModel
    {
        public Dictionary<string, object> dataSource { get; set; }
        public Dictionary<string, object> initData { get; set; }
        public long masterLogId { get; set; }
        public string modelRelationId { get; set; }
        public long roykardId { get; set; }
        public short logType { get; set; } //0= new , 1=edit , 2=copy
        public string token { get; set; }
    }
    public class ListItemModel
    {        
        public int listValueId { get; set; }        
        public string listValueName { get; set; }        
        public int parentListValueId { get; set; }        
    }
    public class logParam
    {
        public long id { get; set; }

        public string name { get; set; }

        public string title { get; set; }

        public string unit { get; set; }

        public string type { get; set; }

        public bool isRequired { get; set; }

        public float minVal { get; set; }

        public float maxVal { get; set; }

        public bool isReadonly { get; set; }

        public bool isMultiSelect { get; set; }

        public string multiSelectSeprator { get; set; }

        public int listValueTypeId { get; set; }

        public int parentListValueTypeId { get; set; }
    }

    public class logGroupParam
    {
        public string groupName { get; set; }
        public int groupIndex { get; set; }        
        public List<logParam> masterFields { get; set; }
        public List<logParam> detailFields { get; set; }
    }

    public class logMRModel
    {
        public long id { get; set; }        
        public string title { get; set; }
        public bool isChild { get; set; }        
        public List<logGroupParam> groupParams { get; set; }
    }

    public class tdmsLogModel
    {
        public List<logParam> mainMasterParams { get; set; }
        public List<logMRModel> mrParams { get; set; }
        public Dictionary<string,object> dataSource { get; set; }
    }

}