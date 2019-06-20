using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using SAJApi.Models;
using SAJExcelImporter.TDMS;
using SepandAsa.RepairManagment.Business;
using SepandAsa.RepairManagment.Business.Excel;
using SepandAsa.RepairManagment.Business.Summary;
using SepandAsa.RepairManagment.DataAccess;
using SepandAsa.RepairManagment.Domain;
using SepandAsa.RepairManagment.Domain.Excel;
using SepandAsa.Shared.Business.Attachments;
using SepandAsa.Shared.Business.Reports;
using SepandAsa.Shared.Business.SubSystemManagement;
using SepandAsa.Shared.Business.Utilities;
using SepandAsa.Shared.DataAccess;
using SepandAsa.Shared.Domain.Attachments;
using SepandAsa.Shared.Domain.ParamDataType;
using SepandAsa.UtilityClasses;
using msExcel = Microsoft.Office.Interop.Excel;

namespace SAJApi.Custom
{
    internal class TDMSMojodiats : IDisposable
    {
        public ModelRelationInfo.ModelRelationRow _modelRelationRow;
        private MasterLogInfo.MasterLogRow _masterLogRowForDelete;
        private MasterLogInfo _masterLogInfoForCreate;
        private bool _isEditMode;
        private ExcelImportConfigsInfo _excelConfigInfo;
        private RoykardConfigInfo _fullRoykardConfigInfo;
        internal List<string> lstErrs;
        private bool hasDic;
        private bool hasConfig;
        private const string _dicSheetName = "Dictionery";

        private Dictionary<long, DataTable> _listSavedTables;
        private bool _withXml;
        internal short countInserted;

        public void Dispose()
        {
            _modelRelationRow = null;
            _masterLogRowForDelete = null;
            _excelConfigInfo = null;
            _fullRoykardConfigInfo = null;
            _listSavedTables = null;
            lstErrs = null;
            GC.Collect();
        }
        private void ReadTypeOfExcel(msExcel.Workbook xlWorkBook)
        {
            msExcel.Worksheet xlWorkSheet = null;

            hasDic = false;
            hasConfig = false;
            foreach (msExcel.Worksheet sheet in xlWorkBook.Sheets)
            {
                if (sheet.Name == _dicSheetName)
                {
                    hasDic = true;
                    break;
                }
            }

            if (hasDic)
            {
                xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[_dicSheetName];
                msExcel.Range cell = xlWorkSheet.Range["$A$2"];
                var val = cell.Value == null ? "" : cell.Value.ToString();
                if (string.IsNullOrEmpty(val))
                {
                    hasDic = false;
                }
                else
                {
                    cell = xlWorkSheet.Range["$C$2"];
                    val = cell.Value == null ? "" : cell.Value.ToString();
                    if (!string.IsNullOrEmpty(val))
                    {
                        hasConfig = true;
                    }
                }
            }
        }

        internal void UploadExcelLog(UploadLogExcelModel model)
        {
            var excelFile = Utils.CreateTempFileForRead(model.excelFile, "excel", "xlsm", model.token);
            string[] xmlFiles = new string[2];
            if (!string.IsNullOrEmpty(model.xml1File))
            {
                xmlFiles[0] = Utils.CreateTempFileForRead(model.xml1File, "xml1", "xml", model.token);
            }
            if (!string.IsNullOrEmpty(model.xml1File))
            {
                xmlFiles[1] = Utils.CreateTempFileForRead(model.xml1File, "xml2", "xml", model.token);
            }
            StartImport(excelFile, 0);
        }

        private long SetRoykardConfigInfoByFormData(out MasterLogInfo dsMasterLog,
            out RoykardConfigInfo roykardConfigInfo, long rcId)
        {
            roykardConfigInfo = RoykardConfig.Instance.
                GetRoykardConfigWithParamsById(rcId);
            if (roykardConfigInfo.RoykardConfig.Count != 0)
            {
                if (_masterLogInfoForCreate != null)
                {
                    dsMasterLog = _masterLogInfoForCreate;
                }
                else
                {
                    dsMasterLog = MasterLog.Instance.CreateMasterLogWithUserDate(
                    _modelRelationRow.ModelRelationId, rcId,
                    SepandServer.CurDate, SepandServer.CurTime, "", 1);
                }

            }
            else
            {
                dsMasterLog = null;
            }
            return rcId;
        }

        internal tdmsLogModel InitForEntryLog(long mrId, long rcId, long mlId = 0) //ml==0 --> isNew
        {
            var roykardInfo = RoykardConfig.Instance.GetRoykardConfigWithParamsById(rcId);
            var modelRelationInfo = ModelRelation.Instance.GetModelRelationById(mrId);
            Dictionary<string, object> dataSource = new Dictionary<string, object>();

            //set whitout group
            var hasGroup = roykardInfo.RoykardConfigParams
                .Any(r => !r.IsGroupParameterNameNull() && string.IsNullOrEmpty(r.GroupParameterName));
            var withoutGroups = roykardInfo.RoykardConfigParams
                .Where(r => r.IsGroupParameterNameNull() || string.IsNullOrEmpty(r.GroupParameterName));

            foreach (var param in withoutGroups) //TODO:: performanc issue : can do in bank
            {
                param.GroupParameterName = hasGroup ? "بدون گروه" : "گروه اصلی";
                param.GroupParameterSortIndex = 1000;
            }

            //------------

            DataSet ds = GetDataSetByMasterLogId(mlId, roykardInfo);


            var mainMasterParams = roykardInfo.RoykardConfigParams
                .Where(r => !r.IsMasterDetailTypeIdNull() && r.MasterDetailTypeId == (int)MasterDetailTypes.MainMaster);

            var setTypeItems = roykardInfo.RoykardConfigParams.Select(r => r.SetTypeItemIdInParameter).Distinct().ToList();
            var mrModels = new List<logMRModel>();
            var dsModelRelation = ModelRelation.Instance.GetFilteredSubModelRelationBySpitialSetTypeItemId
                    (modelRelationInfo.ModelRelation[0], setTypeItems, roykardInfo.RoykardConfig[0]);

            //TODO:: کند بودن در صورت که تعداد موجودیتها زیاد باشد
            foreach (var mr in dsModelRelation.ModelRelation)
            {

                var newMrModel = new logMRModel
                {
                    id = mr.ModelRelationId
                    ,
                    title = mr.SetItemTypeName + " " + mr.ModelRelationName
                    ,
                    isChild = mr.ModelRelationId != modelRelationInfo.ModelRelation[0].ModelRelationId
                    ,
                    groupParams = GetLogGroupParam(mr.SetTypeItemId, roykardInfo, ds, ref dataSource, mr.ModelRelationId)
                };
                mrModels.Add(newMrModel);
            }
            return new tdmsLogModel
            {
                mainMasterParams =
                ConvertToListLogParams(mainMasterParams, ds, ref dataSource, MasterDetailTypes.MainMaster, 0),
                mrParams = mrModels,
                dataSource = dataSource
            };
        }

        internal long SaveLogDataEntry(SaveDataEntryModel saveModel)
        {
            MasterLogInfo dsMasterLog = null;
            var curDate = SepandServer.CurDate;
            var curTime = SepandServer.CurTime;
            var modelRelationId = long.Parse(Decrypt(saveModel.modelRelationId));
            Dictionary<string, Dictionary<string, DataTable>> listDataForSave
                = new Dictionary<string, Dictionary<string, DataTable>>();


            var roykardInfo = RoykardConfig.Instance.GetRoykardConfigWithParamsById(saveModel.roykardId);

            //set whitout group            
            var withoutGroups = roykardInfo.RoykardConfigParams
                .Where(r => r.IsGroupParameterNameNull() || string.IsNullOrEmpty(r.GroupParameterName));

            foreach (var param in withoutGroups) //TODO:: performanc issue : can do in bank
            {
                param.GroupParameterName = "بدون گروه";
                param.GroupParameterSortIndex = 1000;
            }

            List<string> tableNamesThatHaveMainMasterInfo = new List<string>();
            List<DataTable> listSavedTables = new List<DataTable>();
            foreach (var key in saveModel.dataSource.Keys.OrderBy(r => r))
            {
                var dicRow = saveModel.dataSource.Single(r => r.Key == key);
                var tblName = GetTableNameOfDicRow(dicRow, roykardInfo);
                if ((!dicRow.Key.StartsWith("MA") || listDataForSave.ContainsKey(tblName))
                    &&
                    !AreInitAndCurrentDifferent(saveModel.initData.Single(r => r.Key == key), dicRow))
                {
                    continue;
                }

                //CHECK:: group must be first

                var mrId = long.Parse(key.Split('_')[1]);

                var tblData = GetDataByMasterLogId(saveModel.masterLogId, tblName);
                var groupName = GetGroupNameOfDicRow(dicRow, roykardInfo);
                var mrAndgroupName = groupName + "_" + mrId;
                DataTable groupTable = null;

                if (key.StartsWith("MM"))
                {
                    tableNamesThatHaveMainMasterInfo.Add(tblName);
                    //با توجه مرتب سازی :
                    foreach (var dataForSave in listDataForSave[tblName])
                    {
                        groupTable = dataForSave.Value;
                        UpdateGroupTableForMasters(ref groupTable, dicRow, mrId, groupName);
                    }
                    continue;
                }
                if (!listDataForSave.ContainsKey(tblName))
                {
                    listDataForSave.Add(tblName, new Dictionary<string, DataTable>());
                }

                if (!listDataForSave[tblName].ContainsKey(mrAndgroupName))
                {
                    groupTable = GetGroupTable(tblData, groupName, mrId);
                    listDataForSave[tblName].Add(mrAndgroupName, groupTable);
                }

                groupTable = listDataForSave[tblName][mrAndgroupName];

                if (key.StartsWith("group"))
                {
                    UpdateGroupTableForDetail(ref groupTable, (JArray)dicRow.Value, mrId,
                        groupName);
                }
                else
                {
                    UpdateGroupTableForMasters(ref groupTable, dicRow, mrId, groupName);
                }
            }

            using (var scope = new TransactionScope())
            using (new DbConnectionScope())
            {
                if (saveModel.logType == 2 || saveModel.logType == 0)//copy or new
                {
                    int verifierUserId = 1;
                    //if (!_fullRoykardInfo.RoykardConfig[0].IsMailOrbitIdNull())
                    //{
                    //    verifierUserId = (int)cmbVerifierId.SelectedValue;
                    //}
                    dsMasterLog = MasterLog.Instance.CreateMasterLogWithUserDate(
                        modelRelationId, saveModel.roykardId,
                        curDate, curTime, "", verifierUserId);
                }
                else //edit
                {
                    dsMasterLog = MasterLog.Instance.GetMasterLogById(saveModel.masterLogId);
                    dsMasterLog.MasterLog[0].LogDate = curDate;
                    dsMasterLog.MasterLog[0].LogTime = curTime;
                    MasterLog.Instance.SaveMasterLog(dsMasterLog);
                }

                foreach (var dataForSave in listDataForSave)
                {
                    DataTable finalTable = new DataTable(dataForSave.Key);
                    foreach (var dataTable in dataForSave.Value)
                    {
                        finalTable.Merge(dataTable.Value);
                    }

                    CheckAndSetParents(finalTable, modelRelationId, dsMasterLog.MasterLog[0].MasterLogId,
                        saveModel.roykardId);

                    new ClsDinamicallyTableDB(dataForSave.Key).Save(finalTable);
                    finalTable.AcceptChanges();
                    if (tableNamesThatHaveMainMasterInfo.Contains(finalTable.TableName))
                        RoykardConfig.Instance.SaveMainMaster(dsMasterLog, finalTable);

                    listSavedTables.Add(finalTable);
                }

                MasterLog.Instance.SaveFaraInPlaceInfo(dsMasterLog, roykardInfo, listSavedTables);
                MasterLog.Instance.VerifyIndex(dsMasterLog.MasterLog[0], listSavedTables);
                SaveSummary(dsMasterLog, roykardInfo);
                if (!roykardInfo.RoykardConfig[0].IsMailOrbitIdNull())
                {
                    MasterLog.Instance.MakeMailForLog(dsMasterLog.MasterLog[0]);
                }
                scope.Complete();
                return dsMasterLog.MasterLog[0].MasterLogId;
            }


            //if (SystemModals.Instance.IsModalActive(258))
            //{
            //    var changeType = IsNewAction && !IsCopy ? "ثبت" :
            //        IsCopy ? "کپی" : "ویرایش";
            //    var description = PersianInputBox.Show("توضیحات تغییرات لاگ را وارد کنید", ".");
            //    MasterLog.Instance.AddLogTracker(description, dsMasterLog.MasterLog[0].MasterLogId,
            //        _dsRoykardConfig.RoykardConfig[0].RoykardConfigName, changeType);
            //}

        }

        private bool AreInitAndCurrentDifferent(KeyValuePair<string, object> initDicRow, KeyValuePair<string, object> dsDicRow)
        {
            //keys are same
            if (!initDicRow.Key.StartsWith("group"))
            {
                if (initDicRow.Value == null)
                    return dsDicRow.Value != null;
                return !Equals(initDicRow.Value, dsDicRow.Value);
            }
            return true;
        }

        private static void SaveSummary(MasterLogInfo masterLogInfo, RoykardConfigInfo roykardConfigInfo)
        {
            if (SummaryManager.Instance.GetIsDisableState())
                return;
            var mlR = masterLogInfo.MasterLog.Count > 0 ? masterLogInfo.MasterLog[0] : null;
            var prvSaveSummaryInfo = SummaryManager.Instance.InitailForUpdate(roykardConfigInfo, mlR);
            SummaryManager.Instance.SaveSummary(masterLogInfo.MasterLog[0].MasterLogId, prvSaveSummaryInfo, true);
        }

        public void CheckAndSetParents(DataTable finalTable, long modelRelationId,
            long masterLogId,
            long roykardConfigId)
        {
            var mrrRowbyId = ModelRelation.Instance.GetModelRelationById(modelRelationId);
            ModelRelationInfo dsModelRelations = ModelRelation.Instance.GetAllParentModelRelation
                (mrrRowbyId);
            ModelRelationInfo allChilds = ModelRelation.Instance.GetChildModelRelation(mrrRowbyId.ModelRelation[0]); ;
            dsModelRelations.Merge(mrrRowbyId);
            dsModelRelations.Merge(allChilds);

            //foreach (ModelRelationInfo.ModelRelationRow modelRelationRow in dsModelRelation.ModelRelation)
            //{
            //    var parcolName = "parent" + modelRelationRow.SetTypeItemId;
            //    if (!finalTable.Columns.Contains(parcolName))
            //    {
            //        throw new InvalidDataException("ستون پرنت در جدول موجود نیست" + " " + parcolName + " " +
            //                                       finalTable.TableName);
            //        //string descp =
            //        //   "EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'" +
            //        //   modelRelationRow.SetItemTypeName + "'" +
            //        //   ", @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'" +
            //        //   "tbl" + modelRelationRow.SetTypeItemId.ToString() + "tbl" +
            //        //  roykardConfigId.ToString()//(long)cmbRoykardConfig.SelectedValue
            //        //   + "', @level2type=N'COLUMN',@level2name=N'parent" + modelRelationRow.SetTypeItemId + "' ";
            //        //ClsDinamicallyTable.Instance.AddNewDynamicColumn(
            //        //    finalTable.TableName, modelRelationRow.SetTypeItemId.ToString(), descp,
            //        //    "parent", (int)ParameterDataTypes.StringParameter, false);

            //        //finalTable.Columns.Add("parent" + modelRelationRow.SetTypeItemId);
            //    }

            //}

            foreach (DataRow dataRow in finalTable.Rows)
            {
                if (dataRow.RowState == DataRowState.Deleted)
                    continue;
                foreach (DataColumn c in finalTable.Columns)
                {
                    if (c.ColumnName.StartsWith("col"))
                        continue;

                    if (c.ColumnName == "RoykardConfigId")
                    {
                        dataRow[c.ColumnName] = roykardConfigId;
                    }
                    else if (c.ColumnName == "MasterLogId")
                    {
                        dataRow[c.ColumnName] = masterLogId;
                    }
                    else if (c.ColumnName == ClsDinamicallyTable._DetailModelRelationName)
                    {
                        var mrInfo =
                            dsModelRelations.ModelRelation.FindByModelRelationId
                                ((long)dataRow["DetailModelRelationId"]);
                        if (mrInfo == null)
                        {
                            throw new InvalidDataException("اطلاعات موجودیت یافت نشد" + " " + dataRow["DetailModelRelationId"]);
                        }
                        dataRow[ClsDinamicallyTable._DetailModelRelationName] = mrInfo.ModelRelationName;
                    }
                    else if (c.ColumnName.StartsWith("parent"))
                    {
                        //tbl.Rows[0][c.ColumnName] = listofparent[int.Parse( c.ColumnName.Substring(6))];

                        DataRow[] dr = dsModelRelations.ModelRelation.Select("SetTypeItemId = "
                                                                            + c.ColumnName.Substring(6));

                        if (dr.Length > 0)
                        {
                            var row = dr[0] as ModelRelationInfo.ModelRelationRow;
                            dataRow[c.ColumnName] = row.FullRelationName;
                        }
                        else
                        {
                            throw new InvalidDataException("پرنت برای این مورد پیدا نشد" + " " + c.ColumnName);
                        }
                    }
                }
            }


        }


        private static void DoSpecialValidates(DataTable dataForSave, bool isNewAction, RoykardConfigInfo roykardInfo)
        {
            const string tblName = "tbl1044tbl485";
            if (roykardInfo.RoykardConfig[0].RoykardConfigId == 485)
            {
                var curRow = dataForSave.Rows[0];
                string criteria = $"(col7569 >=N'{curRow["col7569"]}')";
                if (!isNewAction)
                {
                    criteria += $" AND (MasterLogId <> {curRow["MasterLogId"]})";
                }

                var byCriteria = ClsDinamicallyTable.Instance
                    .GetSelectedRowFromDataTable(tblName, criteria);
                if (byCriteria.Rows.Count > 0)
                {
                    throw new ApplicationException(
                        "قبلا یک رکورد دیگر ثبت شده که تاریخ شروع آن بزرگتر مساوی همین لاگ است");
                }

                criteria = $"col7569<N'{curRow["col7569"]}' AND col7570 IS NULL";
                var prvData = ClsDinamicallyTable.Instance
                    .GetSelectedRowFromDataTable(tblName, criteria);
                var curStartDate = PersianDateTime.Parse
                    (dataForSave.Rows[0]["col7569"].ToString());
                if (prvData.Rows.Count > 0)
                {
                    if (dataForSave.Rows.Count < prvData.Rows.Count)
                    {
                        throw new ApplicationException("تعداد شرکت های وارد شده کمتر از لاگ قبلی است");
                    }
                    var date = curStartDate.AddDays(-1).ToString(DateFormat.YYYY_MM_DD);
                    for (int i = 0; i < prvData.Rows.Count; i++)
                    {
                        prvData.Rows[i]["col7570"] = date;
                    }
                }

                ClsDinamicallyTable.Instance.JustUpdate(prvData);
            }
        }

        private void UpdateGroupTableForMasters(ref DataTable groupTable,
            KeyValuePair<string, object> dicRow, long mrId, string groupName)
        {
            var paramId = int.Parse(dicRow.Key.Split('_')[2]); //always key must start with MM or MA 
            if (dicRow.Key.StartsWith("MM"))
            {
                if (groupTable.Rows.Count == 0)
                {
                    throw new ApplicationException("ذخیره اطلاعات مستر اصلی بدون جزئیات امکان پذیر نمی باشد");
                }
                foreach (DataRow row in groupTable.Rows)
                {
                    row["col" + paramId] = GetValue(dicRow.Value);
                }
                return;
            }

            if (groupTable.Rows.Count == 0)
            {
                var row = groupTable.NewRow();
                groupTable.Rows.Add(row);
            }
            foreach (DataRow row in groupTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;
                row["DetailModelRelationId"] = mrId;
                if (groupTable.Columns.Contains("GroupName"))
                {
                    row["GroupName"] = groupName;
                }


                row["col" + paramId] = GetValue(dicRow.Value);
            }
        }

        private static object GetValue(object val)
        {
            object value = DBNull.Value;
            if (val != null && !string.IsNullOrEmpty(val.ToString()))
            {
                value = val;
            }
            return value;
        }

        private static void UpdateGroupTableForDetail(ref DataTable groupTable, JArray rows, long mrId, string groupName)
        {
            List<long> dontDeletedIds = new List<long>();
            List<long> idsForDelete = new List<long>();
            var pkName = groupTable.PrimaryKey[0].ColumnName;
            foreach (var row in rows)
            {
                var rowValues = row.ToObject<Dictionary<string, object>>();
                var idVal = rowValues["id"];
                var exist = groupTable.Rows.Find(idVal);
                if (Convert.ToInt64(idVal) < 0 || exist == null)
                {
                    exist = groupTable.NewRow();
                    groupTable.Rows.Add(exist);
                }

                exist["DetailModelRelationId"] = mrId;
                if (groupTable.Columns.Contains("GroupName"))
                {
                    exist["GroupName"] = groupName;
                }
                foreach (var rowValue in rowValues)
                {
                    if (rowValue.Key == "id")
                    {
                        dontDeletedIds.Add(long.Parse(rowValue.Value.ToString()));
                    }
                    else
                    {
                        object value = DBNull.Value;
                        if (rowValue.Value != null && !string.IsNullOrEmpty(rowValue.ToString()))
                        {
                            value = rowValue.Value;
                        }
                        exist[rowValue.Key] = value;
                    }
                }
            }

            foreach (DataRow row in groupTable.Rows)
            {
                var id = Convert.ToInt64(row[pkName]);
                if (row.RowState != DataRowState.Added && id > 0 && !dontDeletedIds.Contains(id))
                {
                    idsForDelete.Add(id);
                }
            }

            foreach (var id in idsForDelete)
            {
                groupTable.Rows.Find(id).Delete();
            }
        }

        private static DataTable GetGroupTable(DataTable tblData, string groupName, long modelRelationId)
        {
            tblData.DefaultView.RowFilter = $"DetailModelRelationId={modelRelationId}";
            if (tblData.Columns.Contains("GroupName"))
            {
                tblData.DefaultView.RowFilter += $" AND GroupName='{groupName}'";
            }

            var result = tblData.DefaultView.ToTable(tblData.TableName);

            var pkName = tblData.TableName + "Id";
            if (result.PrimaryKey.Length == 0)
            {
                result.Columns[pkName].AutoIncrement = true;
                result.Columns[pkName].AutoIncrementSeed = -1;
                result.Columns[pkName].AutoIncrementStep = -1;
                result.Columns[pkName].AllowDBNull = false;
                result.Columns[pkName].Unique = true;
                result.PrimaryKey = new[] { result.Columns[pkName] };
            }
            tblData.DefaultView.RowFilter = "";
            result.AcceptChanges();
            return result;
        }

        private static string GetGroupNameOfDicRow(KeyValuePair<string, object> dicRow, RoykardConfigInfo roykardInfo)
        {
            RoykardConfigInfo.RoykardConfigParamsRow param;
            if (dicRow.Key.StartsWith("group"))
            {
                var groupIndex = int.Parse(dicRow.Key.Split('_')[2]);
                param = roykardInfo.RoykardConfigParams.First(r => r.GroupParameterSortIndex == groupIndex);
            }
            else
            {
                var setTypeItemParamId = int.Parse(dicRow.Key.Split('_')[2]);
                param = roykardInfo.RoykardConfigParams.Single(r => r.SetTypeItemParameterId == setTypeItemParamId);
            }
            //if (dicRow.Key.StartsWith("MM"))
            //{
            //    var firtInThisTable = roykardInfo.RoykardConfigParams.First(
            //        r => r.SetTypeItemIdInParameter == param.SetTypeItemIdInParameter &&
            //        r.MasterDetailTypeId != (int)MasterDetailTypes.MainMaster);
            //    return firtInThisTable.GroupParameterName;
            //}

            return param.GroupParameterName;
        }

        private static string GetTableNameOfDicRow(KeyValuePair<string, object> dicRow, RoykardConfigInfo roykardInfo)
        {
            RoykardConfigInfo.RoykardConfigParamsRow param;
            if (dicRow.Key.StartsWith("group"))
            {
                var groupIndex = int.Parse(dicRow.Key.Split('_')[2]);
                param = roykardInfo.RoykardConfigParams.First(r => r.GroupParameterSortIndex == groupIndex);
            }
            else
            {
                var setTypeItemParamId = int.Parse(dicRow.Key.Split('_')[2]);
                param = roykardInfo.RoykardConfigParams.Single(r => r.SetTypeItemParameterId == setTypeItemParamId);
            }

            return $"tbl{param.SetTypeItemIdInParameter}tbl{param.RoykardConfigId}";
        }

        private IEnumerable<long> GetModelRelatinsInDataSource(Dictionary<string, object> dataSource)
        {
            var ids = dataSource.Keys.Where(r => HasAnyData(r, dataSource)).Select(r => r.Split('_')[1]).Distinct();
            return ids.Select(r => long.Parse(r));
        }

        private bool HasAnyData(string key, Dictionary<string, object> dataSource)
        {
            if (key.StartsWith("group"))//is detail
            {
                if (dataSource[key] is Array val && val.Length > 0)
                {
                    return true;
                }
            }
            else
            {
                if (dataSource[key] != null && !string.IsNullOrEmpty(dataSource[key].ToString()))
                {
                    return true;
                }
            }

            return false;
        }
        private bool GetHasModelRelationAnyData(Dictionary<string, object> dataSource, ModelRelationInfo.ModelRelationRow modelRelation)
        {

            var filterByMR = dataSource.Keys.Where(r => r.Contains($"_{modelRelation.ModelRelationId}_")
            || r.Contains($"_{0}_"));
            foreach (var key in filterByMR)
            {
                if (HasAnyData(key, dataSource))
                {
                    return true;
                }
            }

            return false;
        }

        private static DataSet GetDataSetByMasterLogId(long masterLogId, RoykardConfigInfo roykardInfo)
        {
            //load datasource //TODO:: performance issue
            if (masterLogId != 0)
            {
                var ds = new DataSet();
                var groups = roykardInfo.RoykardConfigParams.GroupBy(r => r.SetTypeItemIdInParameter);
                foreach (var grp in groups)
                {
                    var tblName = ClsDinamicallyTable.Instance.GetTableName(grp.Key, roykardInfo.RoykardConfig[0].RoykardConfigId);
                    ds.Merge(GetDataByMasterLogId(masterLogId, tblName));
                }
                return ds;
            }
            return null;
            //------------
        }

        private static List<logParam> ConvertToListLogParams(IEnumerable<RoykardConfigInfo.RoykardConfigParamsRow> paramList,
            DataSet ds, ref Dictionary<string, object> dataSource, MasterDetailTypes type,
            long modelRelationId, short groupIndex = 0, string groupName = "")
        {
            if (paramList.Count() == 0)
            {
                return null;
            }
            List<logParam> result = new List<logParam>();
            var unOrderd = paramList.Where(r => r.IsSortNoNull());
            foreach (var un in unOrderd)//TODO:: performance issue
            {
                un.SortNo = 1000;
            }
            foreach (var param in paramList.OrderBy(r => r.SortNo))
            {
                if (!param.IsFaraSelectedFieldIdNull())
                    continue;
                result.Add(ConvertToLogParam(param, type, modelRelationId));


                //Set current val in datasource
                if (type != MasterDetailTypes.Detail)
                {
                    var key = GetKeyForDataSource(type, modelRelationId, param);
                    var value = GetDefaultOrCurrentValue(param, modelRelationId, ds);
                    dataSource.Add(key, value);
                }
                //------------
            }
            if (type == MasterDetailTypes.Detail)
            {
                result.Add(new logParam { id = 0, name = "id", isReadonly = true, title = "شناسه", type = "numericColumn" });
                var key = "group_" + modelRelationId + "_" + groupIndex;
                //با توجه به اینکه متد فقط در حالتی صدا زده میشود که 
                //گروه پامتر مشخص شده پس 
                //پارامترهای پاس داده شده فقط مربوط به آن گروه است
                //و چون نوعش دیتل است پس این پامترهای گروه و دیتل می باشند
                var value = GetDetailRowsOrDefaultValues(paramList, modelRelationId, ds, groupName);
                dataSource.Add(key, value);
            }
            return result;
        }

        private static DataTable GetDetailRowsOrDefaultValues(
            IEnumerable<RoykardConfigInfo.RoykardConfigParamsRow> paramList, long modelRelationId, DataSet ds, string groupName)
        {
            if (ds == null)
            {
                return new DataTable();
            }
            //TODO:: performance issue
            string tblName = "tbl" + paramList.First().SetTypeItemIdInParameter + "tbl" + paramList.First().RoykardConfigId;

            ds.Tables[tblName].DefaultView.RowFilter = $"DetailModelRelationId={modelRelationId} ";
            if (ds.Tables[tblName].Columns.Contains("GroupName"))
            {
                ds.Tables[tblName].DefaultView.RowFilter += $" AND GroupName='{groupName}'";
            }
            if (ds.Tables[tblName].DefaultView.Count == 0)
            {
                return new DataTable();
            }
            var result = ds.Tables[tblName].DefaultView.ToTable();
            var idCol = (tblName + "id").ToLower();
            foreach (DataColumn col in ds.Tables[tblName].Columns)
            {
                if (paramList.All(r => col.ColumnName != "col" + r.SetTypeItemParameterId)
                    && col.ColumnName.ToLower() != idCol)
                {
                    result.Columns.Remove(col.ColumnName);
                }
                else if (col.ColumnName.ToLower() == idCol)
                {
                    result.Columns[col.ColumnName].ColumnName = "id";
                }
            }
            ds.Tables[tblName].DefaultView.RowFilter = "";
            return result;
        }

        private static object GetDefaultOrCurrentValue(RoykardConfigInfo.RoykardConfigParamsRow param, long modelRelationId, DataSet ds)
        {
            if (ds == null)
            {
                if (!param.IsDefaultValueNull())
                {
                    return param.DefaultValue;
                }
                return "";
            }

            var tblName = ClsDinamicallyTable.Instance.GetTableName(param.SetTypeItemIdInParameter, param.RoykardConfigId);//TODO :: performance issue when is use in loop
            var criteria = "";
            if (modelRelationId != 0)
            {
                criteria = $" AND DetailModelRelationId={modelRelationId}";
            }
            ds.Tables[tblName].DefaultView.RowFilter = $"col{param.SetTypeItemParameterId} IS NOT NULL{criteria}";

            var val = ds.Tables[tblName].DefaultView.Count > 0 ? ds.Tables[tblName].DefaultView[0][$"col{param.SetTypeItemParameterId}"] : "";
            ds.Tables[tblName].DefaultView.RowFilter = "";
            return val;
        }

        private static string GetKeyForDataSource(MasterDetailTypes type, long modelRelationId, RoykardConfigInfo.RoykardConfigParamsRow param)
        {
            if (type == MasterDetailTypes.Detail)
            {
                return "col" + param.SetTypeItemParameterId;
            }
            string result = "";
            if (type == MasterDetailTypes.MainMaster)
            {
                result = "MM_";
            }
            else if (type == MasterDetailTypes.Master)
            {
                result = "MA_";
            }
            result = string.Concat(result, modelRelationId, "_", param.SetTypeItemParameterId);
            return result;
        }

        private static DataTable GetDataByMasterLogId(long masterLogId, string tblName)
        {
            var dt = ClsDinamicallyTable.Instance.GetSelectedRowFromDataTable(tblName, $"MasterLogId={masterLogId}");


            return dt;
        }
        private static logParam ConvertToLogParam(RoykardConfigInfo.RoykardConfigParamsRow param,
            MasterDetailTypes type, long modelRelationId)
        {
            return new logParam
            {
                id = param.RoykardConfigParamsId,
                name = GetKeyForDataSource(type, modelRelationId, param),//TODO : SET SAME KEY
                title = param.SetTypeItemParameterName,
                unit = param.IsParameterUnitNameNull() ? "" : param.ParameterUnitName,
                type = GetParamDataType(param),
                isRequired = !param.IsMustBeFillNull() && param.MustBeFill,
                isMultiSelect = !param.IsIsMultySelectForListValueNull() && param.IsMultySelectForListValue,
                multiSelectSeprator = param.IsMultiSelectSepratorNameNull() ? "" : param.MultiSelectSepratorName,
                listValueTypeId = param.IsListValueTypeIdNull() ? 0 : param.ListValueTypeId,
                parentListValueTypeId = param.IsParentListValueTypeIdNull() ? 0 : param.ParentListValueTypeId
            };
        }
        private static List<logGroupParam> GetLogGroupParam(int stii, RoykardConfigInfo roykardInfo, DataSet ds,
            ref Dictionary<string, object> dataSource, long modelRelationId)
        {
            List<logGroupParam> result = new List<logGroupParam>();
            var stiParams = roykardInfo.RoykardConfigParams.Where(r => r.SetTypeItemIdInParameter == stii);
            var groups = stiParams.GroupBy(r => r.GroupParameterName);
            short groupIndex = 1;
            foreach (var grp in groups)
            {
                groupIndex = grp.First().GroupParameterSortIndex;
                //foreach(var el in grp)
                //{
                //    el.GroupParameterSortIndex = groupIndex;                    
                //}
                List<logParam> masterFields = ConvertToListLogParams(grp.Where(r => r.IsMasterDetailTypeIdNull()
                || r.MasterDetailTypeId == (int)MasterDetailTypes.Master), ds, ref dataSource, MasterDetailTypes.Master, modelRelationId);
                List<logParam> detailFields = ConvertToListLogParams(grp.Where(r => !r.IsMasterDetailTypeIdNull()
                && r.MasterDetailTypeId == (int)MasterDetailTypes.Detail), ds, ref dataSource,
                MasterDetailTypes.Detail, modelRelationId, groupIndex, grp.Key);

                var newGroupParam = new logGroupParam
                {
                    groupName = grp.Key,
                    groupIndex = groupIndex,
                    masterFields = masterFields,
                    detailFields = detailFields
                };

                result.Add(newGroupParam);
                //groupIndex++;
            }
            return result;
            //result.Add(new logGroupParam { })            
        }

        private static string GetParamDataType(RoykardConfigInfo.RoykardConfigParamsRow param)
        {
            switch ((ParameterDataTypes)param.ParameterDataTypeId)
            {
                case ParameterDataTypes.BooleanParameter:
                    return "bool";
                case ParameterDataTypes.DateParameter:
                    return "date";
                case ParameterDataTypes.StringParameter:
                case ParameterDataTypes.TextParameter:
                    return "text";
                case ParameterDataTypes.TimeParameter:
                    return "time";
                case ParameterDataTypes.ListParameter:
                case ParameterDataTypes.DynamicListParameter:
                    return "list";
                default:
                    return "number";
            }
        }

        private static IEnumerable<IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow>> GetRoykardParams
            (RoykardConfigInfo roykardConfigInfo)
        {
            foreach (var param in roykardConfigInfo.RoykardConfigParams.Where(r => r.IsGroupParameterNameNull()))
            {
                param.GroupParameterName = "بدون گروه";
            }
            foreach (var param in roykardConfigInfo.RoykardConfigParams.Where(r => r.IsGroupParameterSortIndexNull()))
            {
                param.GroupParameterSortIndex = 1000;
            }
            var paramGroups = roykardConfigInfo.RoykardConfigParams.
                OrderBy(r => r.GroupParameterSortIndex).GroupBy(r => r.GroupParameterName);
            return paramGroups;
        }
        private void StartImport(string fileName, long roykardId)
        {
            MasterLogInfo dsMasterLog = null;
            RoykardConfigInfo roykardConfigInfo = null;
            msExcel.Application xlApp = null;
            msExcel.Workbook xlWorkBook = null;
            lstErrs = new List<string>();

            var util = new ExcelUtility.Utils();

            xlApp = new msExcel.Application();
            msExcel.Worksheet xlWorkSheet = null;
            xlApp.Visible = false;
            //ImportXml(xlWorkBook,);                
            try
            {
                xlWorkBook = xlApp.Workbooks.Open(fileName);
                ReadTypeOfExcel(xlWorkBook);
                if (!hasDic && _modelRelationRow == null)
                {
                    throw new ApplicationException(
                        "با توجه به محتویات فایل اکسل شما باید موجودیت مورد نظرتان را انتخاب کنید");
                }
                if (hasDic)
                {
                    //در این حالت به ازای هر روک شیت کار رو تکرار می کنیم  و در ضمن متغیر موجودیت رو هم مشخص می کنیم
                    //درصورتی که هر شیت اطلاتش ذخیره شد آنرا مارک می کنیم
                    //و قبل از اینکه یک شیت ذخیره بشود باید بررسی کنیم آیا قبلا ذخیره نشده است
                    int rowIndexInDic = 2;
                    var dicWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[_dicSheetName];
                    string sheetName = null;
                    do
                    {
                        msExcel.Range cell = dicWorkSheet.Range["$A$" + rowIndexInDic];
                        sheetName = cell.Value == null ? "" : cell.Value.ToString();
                        if (!string.IsNullOrEmpty(sheetName))
                        {
                            cell = dicWorkSheet.Range["$D$" + rowIndexInDic];
                            var val = cell.Value == null ? "" : cell.Value.ToString();
                            if (string.IsNullOrEmpty(val) || val != "Yes")
                            {

                                cell = dicWorkSheet.Range["$B$" + rowIndexInDic];
                                var mrId = cell.Value == null ? "" : cell.Value.ToString();
                                if (string.IsNullOrEmpty(mrId))
                                {
                                    mrId = 0;
                                }
                                _modelRelationRow =
                                    ModelRelation.Instance.GetModelRelationRowById(long.Parse(mrId));
                                if (_modelRelationRow == null)
                                {
                                    throw new ApplicationException("خطا:: شماره موجودیت در ردیف شماره " + " " +
                                                                   rowIndexInDic +
                                                                   " " +
                                                                   "دیکشنری نامعتبر است");
                                }
                                if (hasConfig)
                                {
                                    cell = dicWorkSheet.Range["$C$" + rowIndexInDic];
                                    var rcId = cell.Value == null ? "" : cell.Value.ToString();
                                    if (string.IsNullOrEmpty(rcId))
                                    {
                                        rcId = roykardId;
                                    }
                                    SetRoykardConfigInfoByFormData(out dsMasterLog,
                                        out roykardConfigInfo, long.Parse(rcId.ToString()));
                                    if (roykardConfigInfo == null || roykardConfigInfo.RoykardConfig.Count == 0)
                                    {
                                        throw new ApplicationException("خطا:: شماره پیکربندی در ردیف شماره " +
                                                                       " " +
                                                                       rowIndexInDic +
                                                                       " " +
                                                                       "دیکشنری نامعتبر است");
                                    }
                                    if (!roykardConfigInfo.RoykardConfig[0].IsMailOrbitIdNull())
                                    {
                                        MasterLog.Instance.MakeMailForLog(dsMasterLog.MasterLog[0]);
                                    }

                                    _excelConfigInfo = ExcelImportConfigsManager.Instance
                                        .GetByRoykardTypeIdRoykardConfigId(
                                            roykardConfigInfo.RoykardConfig[0].RoykardTypeId,
                                            long.Parse(rcId.ToString()));
                                    if (_excelConfigInfo.ExcelImportConfigs.Count == 0)
                                    {
                                        throw new ApplicationException(
                                            "برای رویکرد انتخاب شده هنوز کانفیگی تعریف نشده است ");
                                    }
                                    _excelConfigInfo = ExcelImportConfigsManager.Instance.GetConfigById(
                                        _excelConfigInfo.ExcelImportConfigs[0].ExcelImportConfigId, true);

                                    var paramGroups = GetRoykardParams(roykardConfigInfo);
                                    string firstGroupName = paramGroups.First().Key;
                                    var formConfigType = MasterLog.Instance.GetFormConfigTypes(roykardConfigInfo);
                                    SaveExcelData(formConfigType, xlApp, xlWorkBook, util, long.Parse(rcId.ToString()),
                                        dsMasterLog,
                                        roykardConfigInfo, paramGroups, firstGroupName, sheetName);

                                    cell = dicWorkSheet.Range["$D$" + rowIndexInDic];
                                    cell.Value = "Yes";
                                    countInserted++;
                                }
                                else
                                {

                                    SetRoykardConfigInfoByFormData(out dsMasterLog,
                                       out roykardConfigInfo, roykardId);

                                    _excelConfigInfo = ExcelImportConfigsManager.Instance
                                        .GetByRoykardTypeIdRoykardConfigId(roykardConfigInfo.RoykardConfig[0].RoykardTypeId, roykardId);
                                    if (_excelConfigInfo.ExcelImportConfigs.Count == 0)
                                    {
                                        throw new ApplicationException(
                                            "برای رویکرد و نوع لاگ انتخاب شده هنوز کانفیگی تعریف نشده است ");
                                    }
                                    _excelConfigInfo = ExcelImportConfigsManager.Instance.GetConfigById(
                                        _excelConfigInfo.ExcelImportConfigs[0].ExcelImportConfigId, true);

                                    var paramGroups = GetRoykardParams(roykardConfigInfo);
                                    string firstGroupName = paramGroups.First().Key;
                                    var formConfigType = MasterLog.Instance.GetFormConfigTypes(roykardConfigInfo);
                                    SaveExcelData(formConfigType, xlApp, xlWorkBook, util, roykardId,
                                        dsMasterLog,
                                        roykardConfigInfo, paramGroups, firstGroupName, sheetName);

                                    cell = dicWorkSheet.Range["$D$" + rowIndexInDic];
                                    cell.Value = "Yes";
                                    countInserted++;
                                }
                            }
                        }
                        rowIndexInDic++;
                    } while (!string.IsNullOrEmpty(sheetName));

                }
                else
                {
                    SetRoykardConfigInfoByFormData(out dsMasterLog, out roykardConfigInfo, roykardId);
                    _excelConfigInfo = ExcelImportConfigsManager.Instance
                        .GetByRoykardTypeIdRoykardConfigId(
                            roykardConfigInfo.RoykardConfig[0].RoykardTypeId, roykardId);
                    if (_excelConfigInfo.ExcelImportConfigs.Count == 0)
                    {
                        throw new ApplicationException(
                            "برای رویکرد و نوع لاگ انتخاب شده هنوز کانفیگی تعریف نشده است ");
                    }
                    _excelConfigInfo = ExcelImportConfigsManager.Instance.GetConfigById(
                        _excelConfigInfo.ExcelImportConfigs[0].ExcelImportConfigId, true);

                    var paramGroups = GetRoykardParams(roykardConfigInfo);
                    string firstGroupName = paramGroups.First().Key;
                    var formConfigType = MasterLog.Instance.GetFormConfigTypes(roykardConfigInfo);

                    SaveExcelData(formConfigType, xlApp, xlWorkBook, util, roykardId, dsMasterLog,
                        roykardConfigInfo, paramGroups, firstGroupName);
                }
            }

            catch (Exception ex)
            {
                //اگر خطایی رخ داده باید تمام اطلاعات مربوط به لاگ هم پاک شود                
                if (dsMasterLog.MasterLog.Count > 0)
                {
                    MasterLog.Instance.SurveyAndDeleteMasterLogRow(dsMasterLog.MasterLog[0], !_isEditMode);
                }

                Utils.log.Error(ex.Message, ex);
                lstErrs.Add(ex.Message);
                //PersianMessageBox.Show("بعد از رفع خطا ها ، می بایست فرم بسته شود و دوباره فایل اکسل خوانده شود - "+ ex.Message);                        
                //throw;
            }
            finally
            {
                if (SystemModals.Instance.IsModalActive(57))
                {
                    SummarySyncsManager.Instance.RunWebService();
                }

                //xlWorkBook.Save();                  
                xlWorkBook.Close(SaveChanges: true);
                xlApp.Quit();

            }


            if (_masterLogRowForDelete != null)
                MasterLog.Instance.SurveyAndDeleteMasterLogRow(_masterLogRowForDelete);

        }
        private void SaveWithoutChildMaster(msExcel.Application xlApp, msExcel.Workbook xlWorkBook,
           ExcelUtility.Utils util, long roykardConfigId, MasterLogInfo dsMasterLog,
           RoykardConfigInfo roykardConfigInfo,
           IEnumerable<IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow>> paramGroups, string firstGroupName
           , ref int errIndex, string sheetNameForRead = null)
        {

            //TODO::
            //قاعده این است که
            //هر گروه رکوردهای خودش را دارد و
            //فیلدهای مستر گروه اول برای تمام رکوردهای گروهای دیگر کپی می شود

            string tablename = ClsDinamicallyTable.Instance.
                        GetTableName(_modelRelationRow.SetTypeItemId, roykardConfigId);
            DataTable tbl = ClsDinamicallyTable.Instance.
                GetDynamicallyTableForSpecifyDetailModelRelation(tablename,
                    _modelRelationRow.ModelRelationId.ToString(), -1);
            if (tbl == null)
            {
                throw new ApplicationException("پيکربندي مورد نظر را در ساختار داده ويراش نماييد");
            }

            EnumerableRowCollection<RoykardConfigInfo.RoykardConfigParamsRow> masterParams = null;
            if (roykardConfigInfo.RoykardConfig[0].IsIsMasterDetailConfigNull() ||
                !roykardConfigInfo.RoykardConfig[0].IsMasterDetailConfig)
            {
                masterParams = roykardConfigInfo.RoykardConfigParams.AsEnumerable();
            }
            if (masterParams == null || !masterParams.Any())
                return;

            var masterParamsValues = new Dictionary<long, string>();

            foreach (var roykardConfigParamsRow in masterParams)
            {
                RoykardConfigInfo.RoykardConfigParamsRow row = roykardConfigParamsRow;
                var crs = _excelConfigInfo.ExcelCellRelations.Where(
                    r => r.FieldId == row.RoykardConfigParamsId.ToString());
                if (crs.Count() > 1)
                {
                    //ERROR:
                    var cr = crs.First();
                    StringBuilder sb = new StringBuilder();
                    foreach (var excelCellRelationsRow in crs)
                    {
                        sb.Append(string.Format("{0}:{1} ,", excelCellRelationsRow.SheetName,
                                                excelCellRelationsRow.CellAddress));
                    }
                    AddError(ref errIndex, cr.SheetName, cr.CellAddress,
                             string.Format("{0}-{1} چندبار تعریف شده : {2}",
                                           roykardConfigParamsRow.RoykardConfigParamsId,
                                           roykardConfigParamsRow.SetTypeItemParameterName, sb));
                    continue;
                    //---
                }
                var cellRelation =
                    _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                        r => r.FieldId == roykardConfigParamsRow.RoykardConfigParamsId.ToString());

                if (cellRelation != null)
                {
                    if (sheetNameForRead == null)
                    {
                        sheetNameForRead = cellRelation.SheetName;
                    }
                    msExcel.Worksheet xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetNameForRead];
                    msExcel.Range cell = xlWorkSheet.Range[cellRelation.CellAddress];
                    var cellVal = GetCellValue(cell.Value, roykardConfigParamsRow);
                    masterParamsValues.Add(roykardConfigParamsRow.SetTypeItemParameterId, cellVal);
                    string errMsg = VerifyTDMSValidations(roykardConfigParamsRow, roykardConfigParamsRow.SetTypeItemParameterId, cellVal);
                    if (errMsg != "")
                    {
                        AddError(ref errIndex, sheetNameForRead, cellRelation.CellAddress, errMsg);
                    }
                }
            }
            if (errIndex > 1)
                return;
            //نیاز نیست که اطلاعات مستر دگر خوانده شود (در حال حاضر)

            int tableNewRowIndex = 0;
            var fristGroupParams = paramGroups.Single(r => r.Key == firstGroupName);
            foreach (var pg in paramGroups)
            {
                //ما در این حالت به ازای هر کروه یک رکود مستر داریم :                
                DataRow tblRow;
                tblRow = tbl.NewRow();
                tbl.Rows.InsertAt(tblRow, tableNewRowIndex);
                SetMasterInfos(dsMasterLog, roykardConfigInfo, tbl, pg, masterParamsValues, fristGroupParams, tblRow);
                tableNewRowIndex++;
            }
            ClsDinamicallyTable.Instance.CheckAndSetParents(_modelRelationRow.ModelRelationId,
                                                                roykardConfigInfo.RoykardConfig[0].
                                                                    RoykardConfigId,
                                                                tbl);
            ClsDinamicallyTable.Instance.UpdateDynamicDataTable(tbl);
            _listSavedTables.Add(_modelRelationRow.SetTypeItemId, tbl);

        }
        private void SaveWithoutChildMasterDetail(msExcel.Application xlApp, msExcel.Workbook xlWorkBook,
            ExcelUtility.Utils util, long roykardConfigId, MasterLogInfo dsMasterLog,
            RoykardConfigInfo roykardConfigInfo,
            IEnumerable<IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow>> paramGroups,
            string firstGroupName
            , ref int errIndex, string sheetNameForRead = null)
        {
            string tablename = ClsDinamicallyTable.Instance.
                GetTableName(_modelRelationRow.SetTypeItemId, roykardConfigId);

            DataTable tbl = ClsDinamicallyTable.Instance.
                GetDynamicallyTableForSpecifyDetailModelRelation(tablename,
                    _modelRelationRow.ModelRelationId.ToString(),
                    -1);
            if (tbl == null)
            {
                throw new ApplicationException("پيکربندي مورد نظر را در ساختار داده ويراش نماييد");
            }

            var masterParams =
                      roykardConfigInfo.RoykardConfigParams.Where(r => !r.IsMasterDetailTypeIdNull() &&
                                                                      (r.MasterDetailTypeId ==
                                                                       (int)MasterDetailTypes.Master || r.MasterDetailTypeId == (int)MasterDetailTypes.MainMaster));


            var masterParamsValues = new Dictionary<long, string>();

            foreach (var roykardConfigParamsRow in masterParams)
            {
                RoykardConfigInfo.RoykardConfigParamsRow row = roykardConfigParamsRow;
                var crs = _excelConfigInfo.ExcelCellRelations.Where(
                    r => r.FieldId == row.RoykardConfigParamsId.ToString());
                if (crs.Count() > 1)
                {
                    var cr = crs.First();
                    StringBuilder sb = new StringBuilder();
                    foreach (var excelCellRelationsRow in crs)
                    {
                        sb.Append(string.Format("{0}:{1} ,", excelCellRelationsRow.SheetName,
                            excelCellRelationsRow.CellAddress));
                    }
                    AddError(ref errIndex, cr.SheetName, cr.CellAddress,
                        string.Format("{0}-{1} چندبار تعریف شده : {2}",
                            roykardConfigParamsRow.RoykardConfigParamsId,
                            roykardConfigParamsRow.SetTypeItemParameterName, sb));
                    continue;
                }
                var cellRelation =
                    _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                        r => r.FieldId == roykardConfigParamsRow.RoykardConfigParamsId.ToString());

                if (cellRelation != null)
                {
                    if (sheetNameForRead == null)
                    {
                        sheetNameForRead = cellRelation.SheetName;
                    }
                    var xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetNameForRead];
                    msExcel.Range cell = xlWorkSheet.Range[cellRelation.CellAddress];
                    var cellVal = GetCellValue(cell.Value, roykardConfigParamsRow);
                    masterParamsValues.Add(roykardConfigParamsRow.SetTypeItemParameterId, cellVal);
                    string errMsg = VerifyTDMSValidations(roykardConfigParamsRow,
                        roykardConfigParamsRow.SetTypeItemParameterId, cellVal);
                    if (errMsg != "")
                    {
                        AddError(ref errIndex, sheetNameForRead
                            , cellRelation.CellAddress, errMsg);
                    }
                }
            }



            //قاعده :
            //هر گروه رکوردهای خودش را دارد
            //اگر گروهی همش مستر بود فقط یک رکورد
            //و اگر دیتیل بود و یا مستر دیتل بود به ازای هر دیتیل یک رکورد و مسترهایش هم در آن کپی می شود
            //در آخر هم مستر های گروه اول در همه رکوردها (ستونهایش پر می شود(
            var fristGroupParams = paramGroups.Single(r => r.Key == firstGroupName);
            int tableNewRowIndex = 0;
            foreach (var pg in paramGroups)
            {
                int groupStartIndex = tableNewRowIndex;
                var groupMasters = pg.Where(r => !r.IsMasterDetailTypeIdNull() &&
                                                 (r.MasterDetailTypeId ==
                                                  (int)MasterDetailTypes.Master || r.MasterDetailTypeId ==
                                                  (int)MasterDetailTypes.MainMaster));
                var groupDetails = pg.Where(r => !r.IsMasterDetailTypeIdNull() &&
                                                 r.MasterDetailTypeId ==
                                                 (int)MasterDetailTypes.Detail);
                if (!groupDetails.Any())
                {
                    //فقط یک رکورد برای گروه
                    DataRow tblRow;
                    tblRow = tbl.NewRow();
                    tbl.Rows.InsertAt(tblRow, tableNewRowIndex);
                    SetMasterInfos(dsMasterLog, roykardConfigInfo, tbl, pg, masterParamsValues, fristGroupParams, tblRow);
                    tableNewRowIndex++;
                }
                else
                {


                    foreach (var param in groupDetails)
                    {
                        RoykardConfigInfo.RoykardConfigParamsRow row = param;
                        var crs = _excelConfigInfo.ExcelCellRelations.Where(
                            r => r.FieldId == row.RoykardConfigParamsId.ToString());
                        if (crs.Count() > 1)
                        {
                            var cr = crs.First();
                            StringBuilder sb = new StringBuilder();
                            foreach (var excelCellRelationsRow in crs)
                            {
                                sb.Append(string.Format("{0}:{1} ,", excelCellRelationsRow.SheetName,
                                    excelCellRelationsRow.CellAddress));
                            }
                            AddError(ref errIndex, cr.SheetName, cr.CellAddress,
                                string.Format("{0}-{1} چندبار تعریف شده : {2}",
                                    param.RoykardConfigParamsId,
                                    param.SetTypeItemParameterName, sb));
                            continue;
                        }

                        var cellRelation =
                            _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                                r => r.FieldId == param.RoykardConfigParamsId.ToString());
                        int tableInsertIndex = groupStartIndex;
                        if (cellRelation != null)
                        {
                            if (sheetNameForRead == null)
                            {
                                sheetNameForRead = cellRelation.SheetName;
                            }
                            var xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetNameForRead];
                            int ColumnIndex = util.GetColumnIndex(xlWorkSheet, cellRelation.CellAddress);

                            int rowIndex = util.GetRowIndex(xlWorkSheet, cellRelation.CellAddress);
                            msExcel.Range cell = xlWorkSheet.Cells[rowIndex, ColumnIndex];
                            // .Range[cellRelation.CellAddress];

                            var otherColumns =
                                              groupDetails.Where(r => r.RoykardConfigParamsId != param.RoykardConfigParamsId);
                            while (!IsRowNull(xlWorkSheet, otherColumns, cellRelation, rowIndex))//!string.IsNullOrEmpty(Convert.ToString(cell.Value)))
                            {
                                DataRow tblRow;
                                if (tableInsertIndex > tbl.Rows.Count - 1)
                                {
                                    tblRow = tbl.NewRow();
                                    tbl.Rows.InsertAt(tblRow, tableInsertIndex);
                                }
                                else
                                    tblRow = tbl.Rows[tableInsertIndex];

                                var value = GetCellValue(cell.Value, param);

                                var dataColumn = tblRow.Table.Columns["col" + param.SetTypeItemParameterId];

                                string errMsg = VerifyTDMSValidations(row, param.SetTypeItemParameterId, value);
                                if (errMsg != "")
                                {
                                    AddError(ref errIndex, sheetNameForRead, cell.Address,
                                        errMsg);
                                }
                                else
                                {
                                    if (dataColumn.DataType == typeof(double) && !string.IsNullOrEmpty(value))
                                    {
                                        tblRow["col" + param.SetTypeItemParameterId] = double.Parse(value);
                                    }
                                    else if (dataColumn.DataType == typeof(decimal) && !string.IsNullOrEmpty(value))
                                    {
                                        tblRow["col" + param.SetTypeItemParameterId] = decimal.Parse(value);
                                    }
                                    else if (dataColumn.DataType == typeof(bool))
                                    {
                                        tblRow["col" + param.SetTypeItemParameterId] = value == "بله";
                                    }
                                    else
                                    {
                                        var val = value == null ? DBNull.Value : (object)value;
                                        tblRow["col" + param.SetTypeItemParameterId] = val;
                                    }
                                }
                                SetMasterInfos(dsMasterLog, roykardConfigInfo, tbl, pg, masterParamsValues, fristGroupParams, tblRow);
                                tableInsertIndex++;

                                rowIndex++;
                                cell = xlWorkSheet.Cells[rowIndex, ColumnIndex];

                            }
                        }

                        if (tableNewRowIndex < tableInsertIndex)
                            tableNewRowIndex = tableInsertIndex;
                    }

                }//end else               
            }
            if (errIndex > 1)
                return;
            ClsDinamicallyTable.Instance.CheckAndSetParents(_modelRelationRow.ModelRelationId,
                                                       roykardConfigInfo.RoykardConfig[0].
                                                           RoykardConfigId,
                                                       tbl);
            ClsDinamicallyTable.Instance.UpdateDynamicDataTable(tbl);
            _listSavedTables.Add(_modelRelationRow.SetTypeItemId, tbl);

        }

        private void SaveJustChildMaster(msExcel.Application xlApp, msExcel.Workbook xlWorkBook,
          ExcelUtility.Utils util, long roykardConfigId, MasterLogInfo dsMasterLog,
          RoykardConfigInfo roykardConfigInfo, ref int errIndex,
            string sheetNameForRead = null)
        {
            //TODO::
            //قاعده این است که
            //هر گروه رکوردهای خودش را دارد و
            //فیلدهای مستر گروه اول برای تمام رکوردهای گروهای دیگر کپی می شود
            //و اینکه برای هر نوع از زیر مجموعه جداول خاص داریم و در آن جداول
            //فیلد 
            //modelrelationId
            //برابر کامبوی مربوطه باید باشد

            var childs = roykardConfigInfo.RoykardConfigParams.GroupBy(r => r.SetTypeItemIdInParameter);

            var modelRelationChilds = ModelRelation.Instance.GetModelRelationChildModel(_modelRelationRow);
            foreach (var child in childs)
            {
                var childRelationInfo =
                    _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                        r => r.FieldId == (SajExcelTdmsManager._childPerfix + child.Key));
                if (childRelationInfo != null)
                {

                    string tablename = ClsDinamicallyTable.Instance.GetTableName(child.Key, roykardConfigId);
                    DataTable tbl = ClsDinamicallyTable.Instance.GetEmptyDataTable(tablename);
                    //ClsDinamicallyTable.Instance.
                    //    GetDynamicallyTableForSpecifyDetailModelRelation(tablename,
                    //        _modelRelationRow.ModelRelationId.ToString(), -1);
                    if (tbl == null)
                    {
                        throw new ApplicationException("پيکربندي مورد نظر را در ساختار داده ويراش نماييد");
                    }
                    int tableNewRowIndex = 0;
                    //فرض بر این است که تمام فیلدهای مربوط به این نوع در همین شیت پیدا شده می باشند
                    //و اینکه اگه قرار بود خارج از این قاعده باشد
                    //باید ابتدا یک دیکشنری برای ان در اکسل ذخیره شود و برنامه بر اساس آن بخواند که فیلدهای هر چاه در کچا ها ذخیره شده است                    
                    var childGroups = child.OrderBy(r => r.GroupParameterSortIndex).GroupBy(r => r.GroupParameterName);
                    var childFirstGroupParams = childGroups.Single(r => r.Key == childGroups.First().Key);
                    if (sheetNameForRead == null)
                    {
                        sheetNameForRead = childRelationInfo.SheetName;
                    }
                    var xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetNameForRead];
                    int columnIndex = util.GetColumnIndex(xlWorkSheet, childRelationInfo.CellAddress);
                    int rowIndex = util.GetRowIndex(xlWorkSheet, childRelationInfo.CellAddress);
                    msExcel.Range cell = xlWorkSheet.Cells[rowIndex, columnIndex];
                    // .Range[cellRelation.CellAddress];
                    //TODO:شرط خروج اینه که اگر در ردیفی مقدار موجودیت ست نشده است
                    //ادامه درج رکورد متوقف می شود

                    while (!string.IsNullOrEmpty(Convert.ToString(cell.Value)))
                    {
                        var childName = cell.Value.ToString();
                        var modelRelation =
                            modelRelationChilds.ModelRelation.SingleOrDefault(
                                r => r.SetTypeItemId == child.Key && r.ModelRelationName == childName);
                        if (modelRelation == null)
                        {
                            rowIndex++;
                            cell = xlWorkSheet.Cells[rowIndex, columnIndex];
                            continue;
                            //AddError(ref errIndex, childRelationInfo.SheetName, cell.Address,
                            //    "چاه وارد شده نامعتبر است");
                        }

                        //ابتدا تمام مقادیر مربوط به رکورد
                        //به ازای هر گروه رکوردهای خاص خودش را داریم
                        Dictionary<long, string> childParams = ReadChildParams(child, xlWorkSheet,
                            util.GetRowIndex(xlWorkSheet, cell.Address), ref errIndex, util);
                        foreach (var childGroup in childGroups)
                        {
                            DataRow tblRow;
                            tblRow = tbl.NewRow();
                            tblRow["DetailModelRelationId"] = modelRelation.ModelRelationId;
                            tbl.Rows.InsertAt(tblRow, tableNewRowIndex);
                            SetMasterInfos(dsMasterLog, roykardConfigInfo, tbl, childGroup, childParams,
                                childFirstGroupParams, tblRow);
                            tblRow["DetailModelRelationId"] = modelRelation.ModelRelationId;
                            tableNewRowIndex++;
                        }
                        ClsDinamicallyTable.Instance.CheckAndSetParents(modelRelation.ModelRelationId,
                            roykardConfigInfo.RoykardConfig[0].RoykardConfigId, tbl, true);
                        rowIndex++;
                        cell = xlWorkSheet.Cells[rowIndex, columnIndex];
                    }
                    if (errIndex > 1)
                        return;
                    List<string> ids = new List<string>();
                    foreach (DataRow dataRow in tbl.Rows)
                    {
                        ids.Add(dataRow["DetailModelRelationId"].ToString());
                    }
                    ClsDinamicallyTable.Instance.UpdateDynamicDataTable(tbl);
                    _listSavedTables.Add(child.Key, tbl);
                }

            }
        }
        private void SaveJustChildMasterDetail(msExcel.Application xlApp, msExcel.Workbook xlWorkBook,
            ExcelUtility.Utils util, long roykardConfigId, MasterLogInfo dsMasterLog, RoykardConfigInfo roykardConfigInfo,
            ref int errIndex, string sheetNameForRead = null)
        {
            //TODO::
            //قاعده این است که
            //هر گروه رکوردهای خودش را دارد و
            //فیلدهای مستر گروه اول برای تمام رکوردهای گروهای دیگر کپی می شود
            //و این کار باید برای هر نوع زیر مجوعه تکرار شود
            var childs = roykardConfigInfo.RoykardConfigParams.GroupBy(r => r.SetTypeItemIdInParameter);
            var modelRelationChilds = ModelRelation.Instance.GetModelRelationChildModel(_modelRelationRow);
            foreach (var child in childs)
            {
                string tablename = ClsDinamicallyTable.Instance.
                        GetTableName(child.Key, roykardConfigId);
                DataTable tbl = ClsDinamicallyTable.Instance.GetEmptyDataTable(tablename);
                //GetDynamicallyTableForSpecifyDetailModelRelation(tablename,
                //    _modelRelationRow.ModelRelationId.ToString(), -1);
                if (tbl == null)
                {
                    throw new ApplicationException("پيکربندي مورد نظر را در ساختار داده ويراش نماييد");
                }

                int tableNewRowIndex = 0;
                foreach (var modelRelation in modelRelationChilds.ModelRelation.Where(r => r.SetTypeItemId == child.Key))
                {
                    string sheetName = IsSheetNameExist(modelRelation, xlWorkBook);

                    if (sheetName == null)
                        continue;

                    var masterParams =
                    child.Where(r => !r.IsMasterDetailTypeIdNull() &&
                                                                     (r.MasterDetailTypeId ==
                                                                     (int)MasterDetailTypes.Master || r.MasterDetailTypeId == (int)MasterDetailTypes.MainMaster));
                    var masterParamsValues = new Dictionary<long, string>();
                    if (IsSheetEmpty(sheetName, xlWorkBook, child))
                    {
                        continue;
                    }
                    foreach (var roykardConfigParamsRow in masterParams)
                    {
                        RoykardConfigInfo.RoykardConfigParamsRow row = roykardConfigParamsRow;
                        var crs = _excelConfigInfo.ExcelCellRelations.Where(
                            r => r.FieldId == row.RoykardConfigParamsId.ToString());
                        if (crs.Count() > 1)
                        {
                            var cr = crs.First();
                            var sb = new StringBuilder();
                            foreach (var excelCellRelationsRow in crs)
                            {
                                sb.Append(string.Format("{0}:{1} ,", excelCellRelationsRow.SheetName,
                                    excelCellRelationsRow.CellAddress));
                            }
                            AddError(ref errIndex, sheetName, cr.CellAddress,
                                string.Format("{0}-{1} چندبار تعریف شده : {2}",
                                    roykardConfigParamsRow.RoykardConfigParamsId,
                                    roykardConfigParamsRow.SetTypeItemParameterName, sb));
                            continue;
                        }
                        var cellRelation =
                            _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                                r => r.FieldId == roykardConfigParamsRow.RoykardConfigParamsId.ToString());

                        if (cellRelation != null)
                        {
                            var xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetName];
                            msExcel.Range cell = xlWorkSheet.Range[cellRelation.CellAddress];
                            var cellVal = GetCellValue(cell.Value, roykardConfigParamsRow);
                            masterParamsValues.Add(roykardConfigParamsRow.SetTypeItemParameterId,
                                cellVal);

                            string errMsg = VerifyTDMSValidations(roykardConfigParamsRow,
                                roykardConfigParamsRow.SetTypeItemParameterId, cellVal);
                            if (errMsg != "")
                            {
                                AddError(ref errIndex, sheetName, cellRelation.CellAddress, errMsg);
                            }
                        }
                    }

                    //قاعده :
                    //هر گروه رکوردهای خودش را دارد
                    //اگر گروهی همش مستر بود فقط یک رکورد
                    //و اگر دیتیل بود و یا مستر دیتل بود به ازای هر دیتیل یک رکورد و مسترهایش هم در آن کپی می شود
                    //در آخر هم مستر های گروه اول در همه رکوردها (ستونهایش پر می شود(
                    var childGroups = child.OrderBy(r => r.GroupParameterSortIndex).GroupBy(r => r.GroupParameterName);
                    var childFirstGroupParams = childGroups.Single(r => r.Key == childGroups.First().Key);
                    foreach (var pg in childGroups)
                    {
                        int groupStartIndex = tableNewRowIndex;
                        var groupMasters = pg.Where(r => !r.IsMasterDetailTypeIdNull() &&
                                                         (r.MasterDetailTypeId ==
                                                         (int)MasterDetailTypes.Master || r.MasterDetailTypeId == (int)MasterDetailTypes.MainMaster));
                        var groupDetails = pg.Where(r => !r.IsMasterDetailTypeIdNull() &&
                                                         r.MasterDetailTypeId ==
                                                         (int)MasterDetailTypes.Detail);
                        if (!groupDetails.Any())
                        {
                            //فقط یک رکورد برای گروه
                            DataRow tblRow;
                            tblRow = tbl.NewRow();
                            tbl.Rows.InsertAt(tblRow, tableNewRowIndex);
                            SetMasterInfos(dsMasterLog, roykardConfigInfo, tbl, pg, masterParamsValues, childFirstGroupParams, tblRow);
                            tblRow["DetailModelRelationId"] = modelRelation.ModelRelationId;
                            tableNewRowIndex++;
                        }
                        else
                        {

                            foreach (var param in groupDetails)
                            {

                                RoykardConfigInfo.RoykardConfigParamsRow row = param;
                                var crs = _excelConfigInfo.ExcelCellRelations.Where(
                                    r => r.FieldId == row.RoykardConfigParamsId.ToString());
                                if (crs.Count() > 1)
                                {
                                    var cr = crs.First();
                                    StringBuilder sb = new StringBuilder();
                                    foreach (var excelCellRelationsRow in crs)
                                    {
                                        sb.Append(string.Format("{0}:{1} ,", excelCellRelationsRow.SheetName,
                                            excelCellRelationsRow.CellAddress));
                                    }
                                    AddError(ref errIndex, sheetName, cr.CellAddress,
                                        string.Format("{0}-{1} چندبار تعریف شده : {2}",
                                            param.RoykardConfigParamsId,
                                            param.SetTypeItemParameterName, sb));
                                    continue;
                                }

                                var cellRelation =
                                    _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                                        r => r.FieldId == param.RoykardConfigParamsId.ToString());
                                int tableInsertIndex = groupStartIndex;
                                if (cellRelation != null)
                                {
                                    var xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetName];
                                    int ColumnIndex = util.GetColumnIndex(xlWorkSheet, cellRelation.CellAddress);
                                    int rowIndex = util.GetRowIndex(xlWorkSheet, cellRelation.CellAddress);
                                    msExcel.Range cell = xlWorkSheet.Cells[rowIndex, ColumnIndex];
                                    // .Range[cellRelation.CellAddress];

                                    var otherColumns =
                                      groupDetails.Where(r => r.RoykardConfigParamsId != param.RoykardConfigParamsId);
                                    while (!IsRowNull(xlWorkSheet, otherColumns, cellRelation, rowIndex))//!string.IsNullOrEmpty(Convert.ToString(cell.Value)))
                                    {
                                        DataRow tblRow;
                                        if (tableInsertIndex > tbl.Rows.Count - 1)
                                        {
                                            tblRow = tbl.NewRow();
                                            tbl.Rows.InsertAt(tblRow, tableInsertIndex);
                                        }
                                        else
                                            tblRow = tbl.Rows[tableInsertIndex];
                                        var cellVal = GetCellValue(cell.Value, param);
                                        var dataColumn = tblRow.Table.Columns["col" + param.SetTypeItemParameterId];

                                        string errMsg = VerifyTDMSValidations(row, param.SetTypeItemParameterId, cellVal);
                                        if (errMsg != "")
                                        {
                                            AddError(ref errIndex, sheetName, cell.Address,
                                                errMsg);
                                        }
                                        else
                                        {
                                            if (dataColumn.DataType == typeof(double) && !string.IsNullOrEmpty(cellVal))
                                            {
                                                tblRow["col" + param.SetTypeItemParameterId] = double.Parse(cellVal);
                                            }
                                            else if (dataColumn.DataType == typeof(decimal) && !string.IsNullOrEmpty(cellVal))
                                            {
                                                tblRow["col" + param.SetTypeItemParameterId] = decimal.Parse(cellVal);
                                            }
                                            else if (dataColumn.DataType == typeof(bool))
                                            {
                                                tblRow["col" + param.SetTypeItemParameterId] = cellVal == "بله";
                                            }
                                            else
                                            {
                                                var val = cellVal == null ? DBNull.Value : (object)cellVal;
                                                tblRow["col" + param.SetTypeItemParameterId] = val;
                                            }
                                        }
                                        SetMasterInfos(dsMasterLog, roykardConfigInfo, tbl, pg, masterParamsValues, childFirstGroupParams, tblRow);
                                        tblRow["DetailModelRelationId"] = modelRelation.ModelRelationId;
                                        tableInsertIndex++;

                                        rowIndex++;
                                        cell = xlWorkSheet.Cells[rowIndex, ColumnIndex];

                                    }
                                }

                                if (tableNewRowIndex < tableInsertIndex)
                                    tableNewRowIndex = tableInsertIndex;
                            }

                        }//end else                                   
                    }
                    ClsDinamicallyTable.Instance.CheckAndSetParents(modelRelation.ModelRelationId,
                                                           roykardConfigInfo.RoykardConfig[0].
                                                               RoykardConfigId, tbl);
                }
                if (errIndex > 1)
                    return;
                ClsDinamicallyTable.Instance.UpdateDynamicDataTable(tbl);
                _listSavedTables.Add(child.Key, tbl);
            }
        }

        private void SaveWithChildMaster(msExcel.Application xlApp, msExcel.Workbook xlWorkBook,
            ExcelUtility.Utils util, long roykardConfigId, MasterLogInfo dsMasterLog,
            RoykardConfigInfo roykardConfigInfo, ref int errIndex, string sheetNameForRead = null)
        {
            var tempRoykardInfo = new RoykardConfigInfo();
            tempRoykardInfo.RoykardConfig.ImportRow(roykardConfigInfo.RoykardConfig[0]);
            foreach (var roykardConfigParam in
                roykardConfigInfo.RoykardConfigParams.Where(r => r.SetTypeItemIdInParameter == roykardConfigInfo.RoykardConfig[0].SetTypeItemId))
            {
                tempRoykardInfo.RoykardConfigParams.ImportRow(roykardConfigParam);
            }
            var paramGroups = tempRoykardInfo.RoykardConfigParams.
                    OrderBy(r => r.GroupParameterSortIndex).GroupBy(r => r.GroupParameterName);
            string firstGroupName = paramGroups.First().Key;
            SaveWithoutChildMaster(xlApp, xlWorkBook, util, roykardConfigId,
                dsMasterLog, tempRoykardInfo, paramGroups,
                firstGroupName, ref errIndex, sheetNameForRead);

            tempRoykardInfo.RoykardConfigParams.Clear();
            tempRoykardInfo.AcceptChanges();
            foreach (var roykardConfigParam in roykardConfigInfo.RoykardConfigParams.Where
                (r => r.SetTypeItemIdInParameter != roykardConfigInfo.RoykardConfig[0].SetTypeItemId))
            {
                tempRoykardInfo.RoykardConfigParams.ImportRow(roykardConfigParam);
            }
            SaveJustChildMaster(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog, tempRoykardInfo, ref errIndex, sheetNameForRead);
        }
        private void SaveWithChildMasterDetail(msExcel.Application xlApp, msExcel.Workbook xlWorkBook, ExcelUtility.Utils util,
           long roykardConfigId, MasterLogInfo dsMasterLog,
           RoykardConfigInfo roykardConfigInfo, ref int errIndex,
           string sheetNameForRead,
           FormConfigTypes configType)
        {
            var tempRoykardInfo = new RoykardConfigInfo();
            tempRoykardInfo.RoykardConfig.ImportRow(roykardConfigInfo.RoykardConfig[0]);
            foreach (var roykardConfigParam in roykardConfigInfo.RoykardConfigParams.Where
                (r => r.SetTypeItemIdInParameter == roykardConfigInfo.RoykardConfig[0].SetTypeItemId))
            {
                tempRoykardInfo.RoykardConfigParams.ImportRow(roykardConfigParam);
            }
            var paramGroups = tempRoykardInfo.RoykardConfigParams.
                    OrderBy(r => r.GroupParameterSortIndex).GroupBy(r => r.GroupParameterName);
            string firstGroupName = paramGroups.First().Key;
            SaveWithoutChildMasterDetail(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog, tempRoykardInfo, paramGroups,
                firstGroupName, ref errIndex, sheetNameForRead);

            tempRoykardInfo.RoykardConfigParams.Clear();
            tempRoykardInfo.AcceptChanges();
            foreach (var roykardConfigParam in roykardConfigInfo.RoykardConfigParams.Where
                (r => r.SetTypeItemIdInParameter != roykardConfigInfo.RoykardConfig[0].SetTypeItemId))
            {
                tempRoykardInfo.RoykardConfigParams.ImportRow(roykardConfigParam);
            }
            if (configType == FormConfigTypes.WithChildMasterDetail)
                SaveJustChildMasterDetail(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog, tempRoykardInfo,
                    ref errIndex, sheetNameForRead);
            else
                SaveJustChildMaster(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog, tempRoykardInfo, ref errIndex);
        }
        private void SaveExcelData(FormConfigTypes formConfigType, msExcel.Application xlApp,
           msExcel.Workbook xlWorkBook,
           ExcelUtility.Utils util, long roykardConfigId, MasterLogInfo dsMasterLog, RoykardConfigInfo roykardConfigInfo,
           IEnumerable<IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow>> paramGroups,
           string firstGroupName, string sheetName = null)
        {

            //TransactionOptions op = new TransactionOptions();
            //op.Timeout = new TimeSpan(0, 0, 10, 0);
            //TransactionScopeOption.Required, op,
            //EnterpriseServicesInteropOption.Automatic

            _fullRoykardConfigInfo = RoykardConfig.Instance.
                GetRoykardConfigWithParamsById(_excelConfigInfo.ExcelImportConfigs[0].RoykardConfigId);
            _listSavedTables = new Dictionary<long, DataTable>();

            int ErrIndex = 1;
            if (formConfigType == FormConfigTypes.WithoutChildMaster)
            {
                SaveWithoutChildMaster(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog,
                    roykardConfigInfo,
                    paramGroups, firstGroupName, ref ErrIndex, sheetName);
            }
            else if (formConfigType == FormConfigTypes.WithoutChildMasterDetail)
            {
                SaveWithoutChildMasterDetail(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog,
                    roykardConfigInfo, paramGroups, firstGroupName, ref ErrIndex, sheetName);
            }
            else if (formConfigType == FormConfigTypes.JustChildMaster)
            {
                SaveJustChildMaster(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog, roykardConfigInfo,
                    ref ErrIndex, sheetName);
            }
            else if (formConfigType == FormConfigTypes.JustChildMasterDetail)
            {
                SaveJustChildMasterDetail(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog,
                    roykardConfigInfo,
                    ref ErrIndex, sheetName);
            }
            else if (formConfigType == FormConfigTypes.WithChildMaster)
            {
                SaveWithChildMaster(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog, roykardConfigInfo,
                    ref ErrIndex, sheetName);
            }
            else if (formConfigType == FormConfigTypes.WithChildMasterDetail ||
                     formConfigType == FormConfigTypes.WithChildMasterDetailChildIsMaster)
            {
                SaveWithChildMasterDetail(xlApp, xlWorkBook, util, roykardConfigId, dsMasterLog,
                    roykardConfigInfo,
                    ref ErrIndex, sheetName, formConfigType);
            }
            MasterLog.Instance.SaveMainMasterLog(dsMasterLog, _fullRoykardConfigInfo, _listSavedTables);
            MasterLog.Instance.VerifyIndex(dsMasterLog.MasterLog[0], _listSavedTables.Values.ToList());

            if (ErrIndex > 1)
            {
                //اگر خطایی رخ داده باید تمام اطلاعات مربوط به لاگ هم پاک شود
                //MasterLog.Instance.SurveyAndDeleteMasterLogRow(dsMasterLog.MasterLog[0]);
                throw new ApplicationException(
                    "لطفا خطاهای موجود را رفع کرده فرم را ببندید و دوباره فایل اکسل را انتخاب کنید");
                //throw new ApplicationException("لطفا خطاهای موجود را رفع کرده و دوباره دکمه ذخیره را بزنید");
            }

            //با توجه به اینکه در روش اکسل از خود جداول استفاده می شود برای ذخیره سازی مستر اصلی 
            //پس متد زیر لازم نیست
            //SaveMainMastersInRelatedTables(listSavedTables, dsMasterLog.MasterLog[0].MasterLogId);
            MasterLog.Instance.SaveFaraInPlaceInfo(dsMasterLog, _fullRoykardConfigInfo, _listSavedTables.Values.ToList());
            if (_listSavedTables.Count > 0)
                MasterLog.Instance.SaveInAllGroupTable(_listSavedTables.Values, dsMasterLog.MasterLog[0].MasterLogId,
                    roykardConfigInfo);
            MasterLog.Instance.VerifyIndex(dsMasterLog.MasterLog[0], _listSavedTables.Values.ToList());



            SaveSummary(dsMasterLog.MasterLog[0]);

            //if (SystemModals.Instance.IsModalActive(258))
            //{
            //    var changeType = "آپلود";
            //    MasterLog.Instance.AddLogTracker("آپلود فایل اکسل", dsMasterLog.MasterLog[0].MasterLogId,
            //        roykardConfigInfo.RoykardConfig[0].RoykardConfigName, changeType);
            //}

        }

        public void SaveSummary(MasterLogInfo.MasterLogRow mlr)
        {

            var prvSaveSummaryInfo = SummaryManager.Instance.InitailForUpdate(_fullRoykardConfigInfo, mlr);
            SummaryManager.Instance.SaveSummary(mlr.MasterLogId, prvSaveSummaryInfo, false);
        }

        private string CheckNeedXmlTabdil(string[] xmlFiles, bool tabdil)
        {
            string xmlFile = xmlFiles[0];
            if (xmlFiles.Length > 1) //multiple
            {
                xmlFile = JoinXmls(xmlFiles);
            }
            if (tabdil)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlFile);
                FileInfo output = new FileInfo(xmlFile);
                var resultFileName = Path.Combine(output.DirectoryName, output.Name + "-sajSTD.xml");
                XmlTextWriter writer = new XmlTextWriter(resultFileName, Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.WriteStartElement("Table");
                bool dateIsWrite = false;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    string elementName = "";
                    foreach (XmlNode locNode in node)
                    {
                        if (locNode.Name == "ZoneLName" || locNode.Name == "FieldLName" || locNode.Name == "FieldType")
                        {
                            if (string.IsNullOrEmpty(elementName))
                            {
                                elementName = locNode.InnerText.Replace(" ", "");
                            }
                            else
                            {
                                elementName = string.Join("_", elementName, locNode.InnerText.Replace(" ", ""));
                            }
                        }
                        else if (locNode.Name == "Date")
                        {
                            if (!dateIsWrite)
                            {
                                CreateNode("Date", locNode.InnerText.Replace(" ", ""), writer);
                                dateIsWrite = true;
                            }
                        }
                        else
                        {
                            CreateNode(string.Join("_", elementName, locNode.Name), locNode.InnerText.Replace(" ", ""), writer);
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                return resultFileName;
            }
            return xmlFile;
        }

        private string JoinXmls(string[] xmlFiles)
        {

            FileInfo output = new FileInfo(xmlFiles[0]);
            var resultFileName = Path.Combine(output.DirectoryName, output.Name + "-merged.xml");

            var masterfile = new XDocument();
            XElement newDocument = new XElement("DocumentElement");
            masterfile.Add(newDocument);
            foreach (var file in xmlFiles)
            {
                XDocument xdoc = XDocument.Load(file);
                masterfile.Root.Add(xdoc.Descendants("spCreateXML")); //your root note
            }
            //masterfile.Dump();
            masterfile.Save(resultFileName);

            return resultFileName;



        }

        private void CreateNode(string elemetNamt, string elementValue, XmlTextWriter writer)
        {
            writer.WriteStartElement(elemetNamt);
            writer.WriteString(elementValue);
            writer.WriteEndElement();
        }
        internal void ImportLog(long mrId, long roykardConfigId, string fileName, long masterLogIdForDelete = 0, MasterLogInfo masterLogInfoForCreate = null)
        {
            _modelRelationRow = ModelRelation.Instance.GetModelRelationById(mrId).ModelRelation[0];
            _masterLogInfoForCreate = masterLogInfoForCreate;
            _isEditMode = _masterLogInfoForCreate != null;
            if (masterLogIdForDelete != 0)
            {
                _masterLogRowForDelete = MasterLog.Instance.GetMasterLogById(masterLogIdForDelete).MasterLog[0];
            }

            StartImport(fileName, roykardConfigId);
        }
        internal string GetLogForm(string token)
        {
            return Utils.GetTempFileName(token, "LogForm", "xlsm");

        }

        internal void ImportXml(msExcel.Workbook xlWorkBook, string[] xmlFileAddress, bool tabdil)
        {
            try
            {
                if (!_withXml)
                {
                    return;
                }
                var xml = CheckNeedXmlTabdil(xmlFileAddress, tabdil);
                if (xlWorkBook.XmlMaps.Count == 0)
                    throw new ApplicationException("مپینگ فایل اکس ام ال انجام نشده است");
                var map = xlWorkBook.XmlMaps[1];
                xlWorkBook.XmlImport(xml, out map, true);
                xlWorkBook.Save();
            }
            catch (Exception)
            {
                xlWorkBook.Close();
                throw;
            }
        }

        private string IsSheetNameExist(ModelRelationInfo.ModelRelationRow modelRelation, msExcel.Workbook xlWorkBook)
        {
            var result = new List<string>();
            foreach (msExcel.Worksheet sheet in xlWorkBook.Sheets)
            {
                if (sheet.Name.Contains(modelRelation.ModelRelationName))
                    result.Add(sheet.Name);
            }
            if (result.Count == 0)
                return null;
            if (result.Count > 1)
            {
                foreach (var rst in result)
                {
                    if (rst.Contains(modelRelation.SetItemTypeName))
                    {
                        return rst;
                    }
                }
            }
            return result[0];
        }


        private Dictionary<long, string> ReadChildParams(IGrouping<int, RoykardConfigInfo.RoykardConfigParamsRow> child, msExcel.Worksheet xlWorkSheet, int rowIndex, ref int errIndex, ExcelUtility.Utils util)
        {
            var result = new Dictionary<long, string>();
            foreach (var roykardConfigParamsRow in child)
            {
                //RoykardConfigInfo.RoykardConfigParamsRow row = roykardConfigParamsRow;
                var crs = _excelConfigInfo.ExcelCellRelations.Where(
                    r => r.FieldId == roykardConfigParamsRow.RoykardConfigParamsId.ToString());
                if (crs.Count() > 1)
                {
                    //ERROR:
                    var cr = crs.First();
                    StringBuilder sb = new StringBuilder();
                    foreach (var excelCellRelationsRow in crs)
                    {
                        sb.Append(string.Format("{0}:{1} ,", excelCellRelationsRow.SheetName,
                                                excelCellRelationsRow.CellAddress));
                    }
                    AddError(ref errIndex, cr.SheetName, cr.CellAddress,
                             string.Format("{0}-{1} چندبار تعریف شده : {2}",
                                           roykardConfigParamsRow.RoykardConfigParamsId,
                                           roykardConfigParamsRow.SetTypeItemParameterName, sb));
                    continue;
                    //---
                }
                var cellRelation =
                    _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                        r => r.FieldId == roykardConfigParamsRow.RoykardConfigParamsId.ToString());

                if (cellRelation != null)
                {
                    int ColumnIndex = util.GetColumnIndex(xlWorkSheet, cellRelation.CellAddress);
                    msExcel.Range cell = (msExcel.Range)xlWorkSheet.Cells[rowIndex, ColumnIndex];
                    var cellVal = GetCellValue(cell.Value, roykardConfigParamsRow);
                    result.Add(roykardConfigParamsRow.SetTypeItemParameterId,
                                           cellVal);
                    string errMsg = VerifyTDMSValidations(roykardConfigParamsRow, roykardConfigParamsRow.SetTypeItemParameterId, cellVal);
                    if (errMsg != "")
                    {
                        AddError(ref errIndex, cellRelation.SheetName, cell.Address, errMsg);
                    }
                }
            }
            return result;
        }

        private bool IsRowNull(msExcel.Worksheet xlWorkSheet, IEnumerable<RoykardConfigInfo.RoykardConfigParamsRow> groupDetails, ExcelImportConfigsInfo.ExcelCellRelationsRow cellRelation, int rowIndex)
        {
            var util = new ExcelUtility.Utils();
            int ColumnIndex = util.GetColumnIndex(xlWorkSheet, cellRelation.CellAddress);

            msExcel.Range cell = (msExcel.Range)xlWorkSheet.Cells[rowIndex, ColumnIndex];
            var isNull = string.IsNullOrEmpty(Convert.ToString(cell.Value));
            if (isNull)
            {
                foreach (var param in groupDetails)
                {
                    cellRelation =
                            _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                                r => r.FieldId == param.RoykardConfigParamsId.ToString());
                    if (cellRelation != null)
                    {
                        ColumnIndex = util.GetColumnIndex(xlWorkSheet, cellRelation.CellAddress);
                        cell = (msExcel.Range)xlWorkSheet.Cells[rowIndex, ColumnIndex];
                        isNull = string.IsNullOrEmpty(Convert.ToString(cell.Value));
                        if (!isNull)
                            return false;
                        //var otherParams = groupDetails.Where(r => r.RoykardConfigParamsId != param.RoykardConfigParamsId).ToList();
                        //isNull = IsRowNull(xlWorkSheet, otherParams, cellRelation,rowIndex);
                        //if (!isNull)
                        //    break;
                    }
                }
            }
            return isNull;
        }




        private string VerifyTDMSValidations(RoykardConfigInfo.RoykardConfigParamsRow configParamRow, long setTypeItemParameterId, string value)
        {
            var isMustBeFill = !configParamRow.IsMustBeFillNull() && configParamRow.MustBeFill;

            if (!isMustBeFill && string.IsNullOrEmpty(value))
                return "";
            if (isMustBeFill && string.IsNullOrEmpty(value))
                return "مقدار فیلد اجباری می باشد";
            var setTypeItemParameterInfo =
                SetTypeItemParameter.Instance.GetSetTypeItemParameterById(setTypeItemParameterId);

            var parameterDataTypeId = setTypeItemParameterInfo.SetTypeItemParameter[0].ParameterDataTypeId;
            if ((ParameterDataTypes)parameterDataTypeId == ParameterDataTypes.Fara)
            {
                var paramId = setTypeItemParameterInfo.SetTypeItemParameter[0].IsFaraSelectedFieldIdNull()
                        ? 0
                    : setTypeItemParameterInfo.SetTypeItemParameter[0].FaraSelectedFieldId;
                if (paramId == 0)
                {
                    parameterDataTypeId = (int)ParameterDataTypes.StringParameter;
                }
                else
                {

                    SetTypeItemParameterInfo paramInfo =
                        SetTypeItemParameter.Instance.GetSetTypeItemParameterById(paramId);
                    parameterDataTypeId = paramInfo.SetTypeItemParameter[0].ParameterDataTypeId;
                }
            }
            switch ((ParameterDataTypes)parameterDataTypeId)
            {
                case ParameterDataTypes.TimeParameter:
                    var regExp = new Regex("^([0-9]|0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$");
                    if (!regExp.IsMatch(value))
                        return "زمان دقیق وارد نشده است";
                    break;
                case ParameterDataTypes.DateParameter:
                    if (!setTypeItemParameterInfo.SetTypeItemParameter[0].IsIsMiladiNull() && setTypeItemParameterInfo.SetTypeItemParameter[0].IsMiladi)
                    {
                        DateTime dt;
                        if (!DateTime.TryParse(value, out dt))
                            return "تاریخ دقیق وارد نشده است";
                    }
                    else
                    {
                        PersianDateTime pdt;
                        if (!PersianDateTime.TryParse(value, out pdt))
                            return "تاریخ دقیق وارد نشده است";
                    }
                    break;
                case ParameterDataTypes.DecimalParameter:
                case ParameterDataTypes.DoubleNumeric:
                case ParameterDataTypes.NumericParameter:
                case ParameterDataTypes.LongParameter:
                case ParameterDataTypes.IntParameter:
                    double num;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!double.TryParse(value, out num))
                            return "مقدار عددی باید وارد شود";
                        if ((!setTypeItemParameterInfo.SetTypeItemParameter[0].IsMaxValueNull()) &&
                            (num > setTypeItemParameterInfo.SetTypeItemParameter[0].MaxValue))
                        {

                            return "محدوده مجاز : كوچكتر از " +
                                   setTypeItemParameterInfo.SetTypeItemParameter[0].MaxValue;
                        }
                        if ((!setTypeItemParameterInfo.SetTypeItemParameter[0].IsMinValueNull()) &&
                            (num < setTypeItemParameterInfo.SetTypeItemParameter[0].MinValue))
                        {
                            return "محدوده مجاز : بزرگتر از " +
                                   setTypeItemParameterInfo.SetTypeItemParameter[0].MaxValue;
                        }
                    }
                    break;
                case ParameterDataTypes.BooleanParameter:
                    if (value != "بلی" && value != "خیر")
                        return "مقدار منطقی باید وارد شود";
                    break;

                case ParameterDataTypes.ListParameter:
                    if (setTypeItemParameterInfo.SetTypeItemParameter[0].IsListValueTypeIdNull())
                        return "";
                    ListValueInfo ds = ListValue.Instance.GetByListValueTypeId(setTypeItemParameterInfo.SetTypeItemParameter[0].ListValueTypeId, true, _modelRelationRow);
                    if (ds.ListValue.All(r => r.ListValueName != value))
                        return "مقدار باید از لیست انتخاب شود";
                    break;
                case ParameterDataTypes.DynamicListParameter:
                    //TODO: در اکسل باید این نوع پارامتر را هم کامبو کرد
                    return "";
            }
            return "";
        }

        private void AddError(ref int errIndex, string sheetName, string cellAddress, string format)
        {
            lstErrs.Add(errIndex + " " + sheetName + " " + cellAddress + " " + format);
            errIndex++;
        }




        private void VerifyExcelFileIsValid(ExcelImportConfigsInfo excelConfigInfo, string fileExcelAddress)
        {
            //TODO: در ابتدا باید یک شیت در تملیت به نام اطلاعات فایل درست کرد و نوع لاگ و رویکرد را در آن ذخیره نمود سپس 
            //در این قسمت اطلاعات آن شیت را لود کرد که اگر اطلاعات دخیره شده با نوع لاگ و رویکرد انتخاب شده مغایرت داشت 
            //خطا رخ دهد
        }

        private bool IsSheetEmpty(string sheetName, msExcel.Workbook xlWorkBook,
        IEnumerable<RoykardConfigInfo.RoykardConfigParamsRow> paramsRows)
        {
            foreach (var param in paramsRows)
            {
                var cellRelation =
                          _excelConfigInfo.ExcelCellRelations.SingleOrDefault(
                              r => r.FieldId == param.RoykardConfigParamsId.ToString());

                if (cellRelation != null)
                {
                    var xlWorkSheet = (msExcel.Worksheet)xlWorkBook.Worksheets[sheetName];
                    msExcel.Range cell = xlWorkSheet.Range[cellRelation.CellAddress];
                    var val = Convert.ToString(cell.Value);
                    if (!string.IsNullOrEmpty(val) && !string.IsNullOrWhiteSpace(val))
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        private string GetCellValue(object cellVal, RoykardConfigInfo.RoykardConfigParamsRow row)
        {
            var result = Convert.ToString(cellVal);
            if (row.ParameterDataTypeId == (int)ParameterDataTypes.DateParameter
                || row.ParameterDataTypeId == (int)ParameterDataTypes.Fara)//و فیلد فراحوانی تاریخی است
            {
                if (result.Length == 8 && !result.Contains("/"))
                {
                    result = result.Insert(4, "/");
                    result = result.Insert(7, "/");
                }
            }
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private void SetMasterInfos(MasterLogInfo dsMasterLog, RoykardConfigInfo roykardConfigInfo, DataTable tbl,
            IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow> pg,
            Dictionary<long, string> masterParamsValues,
            IGrouping<string, RoykardConfigInfo.RoykardConfigParamsRow> fristGroupParams,
            DataRow tblRow)
        {
            tblRow["MasterLogId"] = dsMasterLog.MasterLog[0].MasterLogId;
            tblRow["DetailModelRelationId"] = _modelRelationRow.ModelRelationId;
            tblRow["RoykardConfigId"] = roykardConfigInfo.RoykardConfig[0].RoykardConfigId;
            if (tblRow.Table.Columns.Contains(ClsDinamicallyTable._groupNameColumn))
            {
                tblRow[ClsDinamicallyTable._groupNameColumn] = pg.Key;
            }
            foreach (var masterParamsValue in masterParamsValues)
            {
                if (pg.Any(r => r.SetTypeItemParameterId == masterParamsValue.Key)
                    || fristGroupParams.Any(r => r.SetTypeItemParameterId == masterParamsValue.Key))
                {
                    var setTypeItemParameterId = masterParamsValue.Key;
                    var dataColumn = tblRow.Table.Columns["col" + setTypeItemParameterId];
                    if (dataColumn.DataType == typeof(double) && !string.IsNullOrEmpty(masterParamsValue.Value))
                    {
                        tblRow["col" + setTypeItemParameterId] = double.Parse(masterParamsValue.Value);
                    }
                    else if (dataColumn.DataType == typeof(decimal) && !string.IsNullOrEmpty(masterParamsValue.Value))
                    {
                        tblRow["col" + setTypeItemParameterId] = decimal.Parse(masterParamsValue.Value);
                    }
                    else if (dataColumn.DataType == typeof(bool))
                    {
                        tblRow["col" + setTypeItemParameterId] = masterParamsValue.Value == "بله";
                    }
                    else
                    {
                        var val = masterParamsValue.Value == null ? DBNull.Value : (object)masterParamsValue.Value;
                        tblRow["col" + setTypeItemParameterId] = val;
                    }
                }
            }
        }
        internal string GetDetailLogReport(string mrId, string rcId, int stIId, string reportId, string tblIds, string token)
        {
            var modelRelationId = long.Parse(Decrypt(mrId));

            var modelRelationRow = ModelRelation.Instance.GetModelRelationById(modelRelationId);
            var roykardConfigId = long.Parse(rcId);
            var roykardInfo = RoykardConfig.Instance.GetRoykardConfigById(roykardConfigId);

            int rId = int.Parse(reportId);
            var rptInfo = new ReportMaintenance().GetByReportId(rId);

            var ids = tblIds.Split('_');
            var flag = ids.Any(r => long.Parse(r) == 0);
            tblIds = string.Join(",", ids);
            var tblId = ClsDinamicallyTable.Instance.GetTableName(stIId, roykardConfigId) + "Id";

            var sm = new
                DetailsLogInLevelSearchManager(modelRelationRow.ModelRelation[0],
                    roykardInfo.RoykardConfig[0], stIId, filter: $"{tblId} IN ({string.Join(",", tblIds)})");


            sm.Search("");
            var dv = sm.GetSearchDataView();


            return Utils.GetStiReportAsPdfByDataSoure(rptInfo, dv, token);
        }
        internal AgGridModel InitialLogList(string mrId, string rcId, string viewType, string setTypeItem, string filterYear = "ALL")
        {
            var modelRelationId = long.Parse(Decrypt(mrId));
            var setTypeItemId = int.Parse(setTypeItem);
            var modelRelationRow = ModelRelation.Instance.GetModelRelationById(modelRelationId);
            var roykardConfigId = long.Parse(rcId);
            var roykardInfo = RoykardConfig.Instance.GetRoykardConfigById(roykardConfigId);
            DataTable dataSource = null;
            if (viewType == "detail")
            {
                if (filterYear != "ALL" && !Utils.IsNumberic(filterYear))//SQL Inject Check
                {
                    throw new ApplicationException("فیلتر سال نامعتبر است");
                }

                string yearFilter = "";
                //var sm = new DetailsLogInLevelSearchManager(modelRelationId);	         
                if (!string.IsNullOrEmpty(filterYear) && filterYear != "ALL")
                {
                    yearFilter = $"LogDate LIKE '{filterYear}/%'";
                }

                var sm = new
                    DetailsLogInLevelSearchManager(modelRelationRow.ModelRelation[0],
                        roykardInfo.RoykardConfig[0], setTypeItemId, yearFilter);

                sm.Search("");
                dataSource = sm.GetSearchDataView().Table;
            }
            else
            {
                dataSource = MasterLog.Instance.
                    GetMasterLogByModelRelationIdAndRoykardConfigId
                        (modelRelationId, roykardConfigId).MasterLog;
            }
            return Utils.GetAgGridModel(dataSource);
        }



        internal List<KeyValueModel> GetRoyardConfigLogTypes(string id)
        {
            var roykardId = long.Parse(id);
            var roykardInfo = RoykardConfig.Instance.GetSetTypeItemsInRoykardConfigParams(roykardId);
            var result = new List<KeyValueModel>();
            foreach (var type in roykardInfo)
            {
                result.Add(new KeyValueModel
                {
                    value = type.SetTypeItemIdInParameter.ToString(),
                    label = type.SetItemTypeName
                });
            }
            return result;
        }

        public List<KeyValueModel> GetRoykardConfigs(string id, bool forReport = false)
        {
            var mrId = long.Parse(Decrypt(id));
            var moderRelationInfo = ModelRelation.Instance.GetModelRelationById(mrId);
            RoykardConfigInfo roykardConfigs = null;
            Dictionary<int, string> stinames = new Dictionary<int, string>();
            if (forReport)
            {
                var stiTems = ModelRelation.Instance.GetDistinctSetTypeItem(moderRelationInfo.ModelRelation[0]);
                roykardConfigs = new RoykardConfigInfo();
                foreach (var mr in stiTems.ModelRelation)
                {
                    stinames.Add(mr.SetTypeItemId, mr.SetItemTypeName);
                    roykardConfigs.Merge(RoykardConfig.Instance.GetRoykardConfigsBySetTypeItemIdForCurUser(mr.SetTypeItemId));
                }
            }
            else
            {
                roykardConfigs = RoykardConfig.Instance.GetRoykardConfigsByRoykardTypeIdAndSetTypeItemId(
                (int)RoykardTypes.AmalkardTajhiz, moderRelationInfo.ModelRelation[0].SetTypeItemId);
            }

            var result = new List<KeyValueModel>();
            foreach (var royakrdRow in roykardConfigs.RoykardConfig)
            {
                string title = royakrdRow.RoykardConfigName;
                if (forReport)
                {
                    title = $"{stinames[royakrdRow.SetTypeItemId]}- {royakrdRow.RoykardConfigName}";
                }
                result.Add(new KeyValueModel
                {
                    value = royakrdRow.RoykardConfigId.ToString(),
                    label = title
                });
            }

            return result;
        }

        public ModelRelationInfo GetMojodiats(string id)
        {
            long parentId = 0;
            if (id == "root")
            {
                parentId = ModelRelation.
                    Instance.GetAllModelRelationRoot().ModelRelation[0].ModelRelationId;
            }
            else
            {
                id = id.Replace("child-", "");
                id = Decrypt(id);
                parentId = long.Parse(id);
            }
            var allFirstRowNodes = ModelRelation.Instance.GetChildByParentId(parentId);
            return allFirstRowNodes;
        }

        internal string Encrypt(string clearText)
        {
            Encoding encoding = Encoding.Unicode;
            Byte[] stringBytes = encoding.GetBytes(clearText);
            StringBuilder sbBytes = new StringBuilder(stringBytes.Length * 2);
            foreach (byte b in stringBytes)
            {
                sbBytes.AppendFormat("{0:X2}", b);
            }
            return sbBytes.ToString();
        }
        internal string Decrypt(string cipherText)
        {
            cipherText = cipherText.Replace("child-", "");
            Encoding encoding = Encoding.Unicode;
            int numberChars = cipherText.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(cipherText.Substring(i, 2), 16);
            }
            return encoding.GetString(bytes);
        }

        public IEnumerable<ListItemModel> LookupDataSource(long modelRelationId, long roykardConfigId, string paramName)
        {

            var id = paramName.StartsWith("col") ? long.Parse(paramName.Substring(3)) : long.Parse(paramName.Split('_')[2]);
            var stiParamInfo = SetTypeItemParameter.Instance.GetSetTypeItemParameterById(id);
            if (stiParamInfo.SetTypeItemParameter.Count == 0)
            {
                throw new InvalidDataException("شناسه پارامتر نامعتبر است");
            }

            if (stiParamInfo.SetTypeItemParameter[0].IsListValueTypeIdNull())
            {
                throw new InvalidDataException("پارامتر از نوع لیست نمی باشد");
            }

            var modelRelationInfo = ModelRelation.Instance.GetModelRelationById(modelRelationId);
            ListValueInfo ds = ListValue.Instance.GetByListValueTypeId(stiParamInfo.SetTypeItemParameter[0].ListValueTypeId, true, modelRelationInfo.ModelRelation[0]);
            var result = Utils.CreateListFromTable<ListItemModel>(ds.ListValue);
            return result;
        }

        internal IEnumerable<AttachmentGroupModel> GetAttachments(
            string mrId,
            out long headerId)
        {
            return ArchivesManager.GroupAttachmentsList
                (GetAttachmentsByModelRelationId(long.Parse(Decrypt(mrId)), out headerId));
        }

        private static IEnumerable<AttachmentInfoModel> GetAttachmentsByModelRelationId(
            long modelRelationId,
            out long headerId)
        {
            ModelRelationInfo.ModelRelationRow modelRelationRow = ModelRelation.Instance.GetModelRelationById(modelRelationId).ModelRelation[0];
            headerId = ModelRelation.Instance.GetModelRelationAttachmentId(modelRelationId);
            AttachmentsInfo attachments = Attachments.GetAttachments(headerId);
            attachments.Merge(Attachments.GetAttachments(SetTypeModels.GetSetTypeModelsAttachmentId(modelRelationRow.SetTypeModelsId)));
            if (!modelRelationRow.IsTMCCodeNull())
                attachments.Merge(Attachments.GetAttachments("1" + modelRelationRow.TMCCode, 5));
            attachments.Merge(Attachments.GetAttachments(SetTypeItem.GetSetTypeItemAttachmentId(modelRelationRow.SetTypeItemId)));
            return Utils.CreateListFromTable<AttachmentInfoModel>(attachments.Attachments);
        }
    }
}