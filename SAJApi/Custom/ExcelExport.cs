using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using SAJExcelImporter.TDMS;
using SepandAsa.RepairManagment.Business;
using SepandAsa.RepairManagment.Business.Excel;
using SepandAsa.RepairManagment.Domain;
using SepandAsa.RepairManagment.Domain.Excel;

namespace SAJApi.Custom
{
    public class ExcelExport
    {
        public static ExcelExport Instance = new ExcelExport();

        public void Export(long roykardConfigId,
            long destinationRoykardConfigId, long masterLogId, string excelFile)
        {
            var roykardConfigInfo = RoykardConfig.Instance.
                GetRoykardConfigWithParamsById(roykardConfigId);
            SetNullGroupName(roykardConfigInfo);
            var excelConfigInfo = ExcelImportConfigsManager.Instance
                .GetByRoykardTypeIdRoykardConfigId(
                    roykardConfigInfo.RoykardConfig[0].RoykardTypeId,
                    roykardConfigId);
            if (excelConfigInfo.ExcelImportConfigs.Count == 0)
            {
                throw new ApplicationException(
                    "برای رویکرد انتخاب شده هنوز کانفیگی تعریف نشده است ");
            }
            excelConfigInfo = ExcelImportConfigsManager.Instance.GetConfigById(
                excelConfigInfo.ExcelImportConfigs[0].ExcelImportConfigId, true);


            var formConfigType = MasterLog.Instance.GetFormConfigTypes(roykardConfigInfo);
            switch (formConfigType)
            {
                case FormConfigTypes.WithoutChildMaster:
                case FormConfigTypes.WithoutChildMasterDetail:
                    SetWithoutChild(masterLogId, excelFile, roykardConfigInfo,
                        excelConfigInfo);
                    break;
                case FormConfigTypes.JustChildMaster:
                    SetJustChildMaster(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
                    break;
                case FormConfigTypes.JustChildMasterDetail:
                    SetJustChildMasterDetail(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
                    break;
                case FormConfigTypes.WithChildMaster:
                    SetWithChildMaster(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
                    break;
                case FormConfigTypes.WithChildMasterDetail:
                    SetWithChildMasterDetail(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
                    break;
                case FormConfigTypes.WithChildMasterDetailChildIsMaster:
                    WithChildMasterDetailChildIsMaster(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
                    break;
                default:
                    throw new ApplicationException("برای این نوع پیکیر بندی امکان ویرایش اکسل پیاده سازی نشده");
            }
        }

        private void WithChildMasterDetailChildIsMaster(long masterLogId, string excelFile, RoykardConfigInfo roykardConfigInfo, ExcelImportConfigsInfo excelConfigInfo)
        {
            SetWithoutChild(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
            SetJustChildMaster(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
        }

        private void SetWithChildMasterDetail(long masterLogId, string excelFile, RoykardConfigInfo roykardConfigInfo, ExcelImportConfigsInfo excelConfigInfo)
        {
            SetWithoutChild(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
            SetJustChildMasterDetail(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
        }

        private void SetWithChildMaster(long masterLogId, string excelFile,
            RoykardConfigInfo roykardConfigInfo,
            ExcelImportConfigsInfo excelConfigInfo)
        {
            SetWithoutChild(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
            SetJustChildMaster(masterLogId, excelFile, roykardConfigInfo, excelConfigInfo);
        }

        private void SetJustChildMasterDetail(long masterLogId, string excelFile,
            RoykardConfigInfo roykardConfigInfo, ExcelImportConfigsInfo excelConfigInfo)
        {
            //var masterLogInfo = MasterLog.Instance.GetMasterLogById(masterLogId);
            //var _modelRelationRow= ModelRelation.Instance.getModerelation
            var childs = roykardConfigInfo.RoykardConfigParams
                .GroupBy(r => r.SetTypeItemIdInParameter);
            FileInfo newFile = new FileInfo(excelFile);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (var child in childs)
                {

                    var childRelationInfo =
                        excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                            r => r.FieldId == (SajExcelTdmsManager._childPerfix + child.Key));
                    if (childRelationInfo != null)
                    {
                        //انتظار می رود به ازای هر موجودیت از نوع جاری ما یک شیت داشته باشیم

                        string tablename = ClsDinamicallyTable.Instance.GetTableName(child.Key,
                            roykardConfigInfo.RoykardConfig[0].RoykardConfigId);
                        DataTable dt =
                            ClsDinamicallyTable.Instance.GetSelectedRowFromDataTable(tablename,
                                $"MasterLogId= {masterLogId}");
                        dt.TableName = tablename;
                        SetGroupColumn(dt);
                        //این جدول به ازای هر موجودیت و هر گروه یک رکورد دارد
                        VerifyModelRelationNameIsNull(dt, child.Key);
                        var gbMRN = from b in dt.AsEnumerable()
                                    group b by b.Field<string>("DetailModelRelationName");
                        foreach (var grp in gbMRN)
                        {
                            var modelRelationRow =
                                ModelRelation.Instance.GetModelRelationById(
                                    (long)grp.First()["DetailModelRelationId"]).ModelRelation[0];

                            var sheetName = GetSheetNameByModelRelationName(modelRelationRow, package);
                            var dataForThisModelRelatin = grp.CopyToDataTable();
                            dataForThisModelRelatin.TableName = tablename;
                            SetFieldsInExcel(dataForThisModelRelatin, package, roykardConfigInfo, excelConfigInfo,
                                sheetName);
                            dataForThisModelRelatin.Dispose();
                        }
                    }
                }
                package.Save();
            }
        }

        private string GetSheetNameByModelRelationName(ModelRelationInfo.ModelRelationRow mrRow,
            ExcelPackage package)
        {
            var names = new List<string>();
            foreach (var workbookWorksheet in package.Workbook.Worksheets)
            {
                if (workbookWorksheet.Name.Contains(mrRow.ModelRelationName))
                {
                    names.Add(workbookWorksheet.Name);
                }
            }

            if (names.Count == 0)
            {
                return null;
            }

            if (names.Count > 1)
            {
                foreach (var rst in names)
                {
                    if (rst.Contains(mrRow.SetItemTypeName))
                    {
                        return rst;
                    }
                }
            }

            return names[0];
        }

        private void SetJustChildMaster(long masterLogId, string excelFile,
            RoykardConfigInfo roykardConfigInfo, ExcelImportConfigsInfo excelConfigInfo)
        {
            var childs = roykardConfigInfo.RoykardConfigParams.GroupBy(r => r.SetTypeItemIdInParameter);
            FileInfo newFile = new FileInfo(excelFile);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                foreach (var child in childs)
                {
                    var childRelationInfo =
                        excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                            r => r.FieldId == (SajExcelTdmsManager._childPerfix + child.Key));
                    if (childRelationInfo != null)
                    {
                        int rowIndex = package.Workbook.Worksheets[childRelationInfo.SheetName]
                            .Cells[childRelationInfo.CellAddress].Start.Row;
                        int colIndex = package.Workbook.Worksheets[childRelationInfo.SheetName]
                            .Cells[childRelationInfo.CellAddress].Start.Column;

                        string tablename = ClsDinamicallyTable.Instance.GetTableName(child.Key,
                            roykardConfigInfo.RoykardConfig[0].RoykardConfigId);
                        DataTable dt =
                            ClsDinamicallyTable.Instance.GetSelectedRowFromDataTable(tablename,
                                $"MasterLogId= {masterLogId}");
                        dt.TableName = tablename;
                        SetGroupColumn(dt);
                        //این جدول به ازای هر موجودیت و هر گروه یک رکورد دارد
                        VerifyModelRelationNameIsNull(dt, child.Key);
                        var gbMRN = from b in dt.AsEnumerable()
                                    group b by b.Field<string>("DetailModelRelationName");
                        foreach (var grp in gbMRN)
                        {
                            ExcelWorksheet worksheet =
                                package.Workbook.Worksheets[childRelationInfo.SheetName];

                            worksheet.Cells[rowIndex, colIndex].Value =
                                grp.Key;
                            SetFieldsInExcel(dt, package, roykardConfigInfo, excelConfigInfo, childRelationInfo.SheetName, rowIndex, grp.Key);
                            rowIndex++;
                        }
                    }
                }
                package.Save();

            }

        }

        private void VerifyModelRelationNameIsNull(DataTable dt, int childKey)
        {
            if (!dt.Columns.Contains("DetailModelRelationName"))
                throw new ApplicationException($"ستون نام موجودیت در جدول {childKey} وجود ندارد");
            foreach (DataRow dataRow in dt.Rows)
            {
                if (dataRow.IsNull("DetailModelRelationName"))
                {
                    throw new ApplicationException($"نام موجودیت در جدول {childKey} نامشخص می باشد");
                }
            }
        }

        private static void SetFieldsInExcel(DataTable dt,
            ExcelPackage package, RoykardConfigInfo roykardConfigInfo,
            ExcelImportConfigsInfo excelConfigInfo, string sheetName = null, int startRowIndex = 0, string modelRelationName = "")
        {
            IEnumerable<IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow>> groupByGroupName = null;
            if (dt.TableName.Contains("tbl"))
            {
                var setTypeItemId = int.Parse(dt.TableName.Split(new[] { "tbl" }, StringSplitOptions.RemoveEmptyEntries)[0]);

                groupByGroupName = roykardConfigInfo.RoykardConfigParams
                    .Where(r => r.SetTypeItemIdInParameter == setTypeItemId)
                    .GroupBy(r => r.GroupParameterName);
            }
            else
            {
                groupByGroupName = roykardConfigInfo.RoykardConfigParams
                    .GroupBy(r => r.GroupParameterName);
            }

            foreach (var gbgn in groupByGroupName)
            {
                foreach (var gr in gbgn)
                {
                    var cellRelation = excelConfigInfo.ExcelCellRelations.SingleOrDefault(r =>
                        r.FieldId == gr.RoykardConfigParamsId.ToString());

                    bool isDetail = gr.MasterDetailTypeId == (int)MasterDetailTypes.Detail;
                    if (cellRelation != null)
                    {
                        if (sheetName == null)
                        {
                            sheetName = cellRelation.SheetName;
                        }

                        var dataRows = dt.Select("GroupName='" + gbgn.Key + "'");
                        int inx = package.Workbook.Worksheets[sheetName]
                            .Cells[cellRelation.CellAddress].Start.Row;
                        int rowIndex = 0;
                        if (startRowIndex == 0)
                        {
                            rowIndex = inx;
                        }
                        else
                        {
                            rowIndex = startRowIndex;
                        }
                        int colIndex = package.Workbook.Worksheets[sheetName]
                            .Cells[cellRelation.CellAddress].Start.Column;
                        //util.GetRowIndex(xlWorkSheet, cellRelation.CellAddress);
                        foreach (var row in dataRows)
                        {
                            if (modelRelationName != "" && row["DetailModelRelationName"].ToString() != modelRelationName)
                            {
                                continue;
                            }
                            if (!row.IsNull("col" + gr.SetTypeItemParameterId))
                            {
                                ExcelWorksheet worksheet =
                                    package.Workbook.Worksheets[sheetName];

                                worksheet.Cells[rowIndex, colIndex].Value =
                                    row["col" + gr.SetTypeItemParameterId];
                                rowIndex++;
                            }

                            if (!isDetail)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            package.Save();
        }

        private static void SetWithoutChild(long masterLogId, string excelFile,
            RoykardConfigInfo roykardConfigInfo, ExcelImportConfigsInfo excelConfigInfo)
        {
            var tblName =
                ClsDinamicallyTable.Instance.GetTableName(roykardConfigInfo.RoykardConfig[0].SetTypeItemId,
                    roykardConfigInfo.RoykardConfig[0].RoykardConfigId);

            DataTable dt =
                ClsDinamicallyTable.Instance.GetSelectedRowFromDataTable(tblName, "MasterLogId=" + masterLogId);
            SetGroupColumn(dt);
            //این جدول فقط یک رکود دارد به ازای هر گروه            
            FileInfo newFile = new FileInfo(excelFile);
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                SetFieldsInExcel(dt, package, roykardConfigInfo, excelConfigInfo);
                package.Save();
            }
        }

        private static void SetGroupColumn(DataTable dt)
        {
            if (!dt.Columns.Contains("GroupName"))
            {
                dt.Columns.Add("GroupName", typeof(string));
                foreach (DataRow dataRow in dt.Rows)
                {
                    dataRow["GroupName"] = "بدون گروه";
                }
            }
        }

        private void SetNullGroupName(RoykardConfigInfo roykardConfigInfo)
        {
            foreach (var roykardConfigParam in roykardConfigInfo.RoykardConfigParams.Where(r => r.IsGroupParameterNameNull()))
            {
                roykardConfigParam.GroupParameterName = "بدون گروه";
            }
        }
    }
}
