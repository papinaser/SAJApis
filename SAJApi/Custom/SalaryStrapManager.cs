using System;
using System.Collections.Generic;
using System.Linq;
using SAJApi.Models;
using SepandAsa.MaliSystem.SalaryWage.Business.ConstantManagment;
using SepandAsa.MaliSystem.SalaryWage.Business.ReportCustomization;
using SepandAsa.MaliSystem.SalaryWage.Business.Utility;
using SepandAsa.MaliSystem.SalaryWage.Domain.BaseInfoManagment;
using SepandAsa.Shared.Business.BaseInfo;
using SepandAsa.Shared.Domain.BaseInfo;
using SepandAsa.Shared.Domain.Security;
using Stimulsoft.Report;

namespace SAJApi.Custom
{
  public class SalaryStrapManager
  {
    internal static List<KeyValueModel> GetUserCompanies(string userName)
    {
      List<CompanyInfo.CompanyRow> list = Company.Instance.GetAllCompanies().Company.Where(r => !r.IsWebLogoNameNull()).ToList();
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      using (List<CompanyInfo.CompanyRow>.Enumerator enumerator = list.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          CompanyInfo.CompanyRow current = enumerator.Current;
          keyValueModelList.Add(new KeyValueModel
          {
            value = current.CompanyId.ToString(),
            label = current.CompanyName
          });
        }
      }
      return keyValueModelList;
    }

    internal static IEnumerable<int> GetYears(string userName, int companyId)
    {
      UserAccountInfo.UserAccountRow userFullInfo = Utils.GetUserFullInfo(userName);
      return SalaryConstant.Instance.GetAllYear(companyId, (int) userFullInfo.EmployeeId).OrderByDescending(r => r);
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
          keyValueModelList.Add(new KeyValueModel
          {
            value = current.MonthCode,
            label = current.MonthDesc
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
