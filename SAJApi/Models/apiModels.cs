using System;
using System.Collections.Generic;

namespace SAJApi.Models
{
    public class OptionField
    {
        public object dataSource { get; set; }
        public string displayField { get; set; }
        public string valueField { get; set; }
        public bool isMultiSelect { get; set; }
    }
    public class FormFieldModel
    {
        public string name { get; set; }

        public string caption { get; set; }

        public string type { get; set; }

        public bool isRequired { get; set; }

        public float minValue { get; set; }

        public float maxValue { get; set; }

        public bool isReadOnly { get; set; }

        public string groupName { get; set; }

        public OptionField options { get; set; }

        public object value { get; set; }
    }
    public class FormInfoModel
    {
        public List<FormFieldModel> fields { get; set; }

        public string formMode { get; set; }
    }
    public class PermisssioCheckModel
    {
        public string token { get; set; }

        public string[] actions { get; set; }

        public bool[] hasPermit { get; set; }
    }

    public class biDash
    {
        public Guid itemID { get; set; }

        public string name { get; set; }

        public Guid parentID { get; set; }

        public string description { get; set; }

        public int type { get; set; }
    }
}