using SAJApi.Models;
using SepandAsa.MaliSystem.SalaryWage.Business.ConstantManagment;
using SepandAsa.MaliSystem.SalaryWage.Business.ReportCustomization;
using SepandAsa.MaliSystem.SalaryWage.Business.Utility;
using SepandAsa.MaliSystem.SalaryWage.Domain.BaseInfoManagment;
using SepandAsa.Shared.Business.BaseInfo;
using SepandAsa.Shared.Domain.BaseInfo;
using SepandAsa.Shared.Domain.Security;
using Stimulsoft.Report;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAJApi.Custom
{
  public class SalaryStrapManager
  {
    internal static List<KeyValueModel> GetUserCompanies(string userName)
    {
      List<CompanyInfo.CompanyRow> list = ((IEnumerable<CompanyInfo.CompanyRow>) Company.Instance.GetAllCompanies().Company).Where<CompanyInfo.CompanyRow>((Func<CompanyInfo.CompanyRow, bool>) (r => !r.IsWebLogoNameNull())).ToList<CompanyInfo.CompanyRow>();
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      using (List<CompanyInfo.CompanyRow>.Enumerator enumerator = list.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          CompanyInfo.CompanyRow current = enumerator.Current;
          keyValueModelList.Add(new KeyValueModel()
          {
            key = current.CompanyId.ToString(),
            value = current.CompanyName
          });
        }
      }
      return keyValueModelList;
    }

    internal static IEnumerable<int> GetYears(string userName, int companyId)
    {
      UserAccountInfo.UserAccountRow userFullInfo = Utils.GetUserFullInfo(userName);
      return (IEnumerable<int>) ((SalaryConstant) SalaryConstant.Instance).GetAllYear(companyId, (int) userFullInfo.EmployeeId).OrderByDescending<int, int>((Func<int, int>) (r => r));
    }

    internal static List<KeyValueModel> GetMonths(
      string userName,
      int companyId,
      string year)
    {
      UserAccountInfo.UserAccountRow userFullInfo = Utils.GetUserFullInfo(userName);
      MonthNameInfo monthNameList = GetListOffSalaryMonth.Instance.GetMonthNameList(year, (int) userFullInfo.EmployeeId, companyId);
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      using (IEnumerator<MonthNameInfo.MonthNameRow> enumerator = 
          monthNameList.MonthName.OrderByDescending(r => r.MonthCode).GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          MonthNameInfo.MonthNameRow current = enumerator.Current;
          keyValueModelList.Add(new KeyValueModel()
          {
            key = current.MonthCode,
            value = current.MonthDesc
          });
        }
      }
      return keyValueModelList;
    }

    internal static StiReport GetSalaryStrap(
      string userName,
      int companyId,
      string year,
      string month)
    {
      Utils.SAJLoginWithName(userName, true, companyId);
      UserAccountInfo.UserAccountRow userFullInfo = Utils.GetUserFullInfo(userName);
      StiReport stiReport = new StatmentReport().StatmentByPersonelNo(year, month, (int) userFullInfo.EmployeeId, companyId, 3);
      if (stiReport == null)
        throw new ApplicationException("فیش حقوقی یافت نشده");
      stiReport.Render();
      return stiReport;
    }
  }
}
