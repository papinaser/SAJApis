﻿using Newtonsoft.Json;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Domain.Security;
using System;
using System.Text;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class LoginController : ApiController
  {
    public string Post([FromBody] loginModel loginInfo)
    {
      string username = loginInfo.username;
      SimpleResult simpleResult = new SimpleResult();
      if (Utils.SAJLogin(ref username, loginInfo.password))
      {
        UserAccountInfo.UserAccountRow userFullInfo = Utils.GetUserFullInfo(username);
        simpleResult.result = "200";
        simpleResult.message = (object) this.GetSignedInUserInfo(userFullInfo);
      }
      else
      {
        simpleResult.result = "501";
        simpleResult.message = (object) "نام کاربری/رمز عبور نامعتبر است";
      }
      return JsonConvert.SerializeObject((object) simpleResult);
    }

    private userInfo GetSignedInUserInfo(UserAccountInfo.UserAccountRow userInfo)
    {
      return new userInfo()
      {
        employeeNo = userInfo.EmployeeId.ToString(),
        title = userInfo.Name,
        username = this.Encrypt(string.Format("{0}-{1}", userInfo.UserName, (object) this.Request.GetClientIpAddress()))
      };
    }

    private string Encrypt(string clearText)
    {
      byte[] bytes = Encoding.Unicode.GetBytes(clearText);
      StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
      foreach (byte num in bytes)
        stringBuilder.AppendFormat("{0:X2}", (object) num);
      return stringBuilder.ToString();
    }

    private string Decrypt(string cipherText)
    {
      Encoding unicode = Encoding.Unicode;
      int length = cipherText.Length;
      byte[] bytes = new byte[length / 2];
      for (int startIndex = 0; startIndex < length; startIndex += 2)
        bytes[startIndex / 2] = Convert.ToByte(cipherText.Substring(startIndex, 2), 16);
      return unicode.GetString(bytes);
    }

    public LoginController()
    {
      
    }
  }
}