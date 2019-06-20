using System.Collections.Generic;

namespace SAJApi.Models
{
    public class AgGridColumn
    {
        public string headerName { get; set; }

        public string field { get; set; }

        public string sort { get; set; }

        public string filter { get; set; }

        public bool hide { get; set; }
    }
    public class AgGridModel
    {        
        public List<AgGridColumn> columnDefs { get; set; }    
        public object rowData { get; set; }

    }

    public class UploadLogExcelModel
    {
        public string excelFile { get; set; }

        public string xml1File { get; set; }

        public string xml2File { get; set; }

        public string modelRelationId { get; set; }

        public string token { get; set; }
    }
    public class GetDetailLogReportModel
    {
        public string modelRelationId { get; set; }

        public string roykardId { get; set; }

        public int setTypeItemId { get; set; }

        public string reportId { get; set; }

        public string tblIds { get; set; }

        public string token { get; set; }
    }



}
