// Decompiled with JetBrains decompiler
// Type: SAJApi.Controllers.ValuesController
// Assembly: SAJApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: E75874B1-C49B-4EF7-8C32-B89971E1CBDC
// Assembly location: C:\Users\papinaser\Downloads\SAJApi.dll

using System.Collections.Generic;
using System.Web.Http;

namespace SAJApi.Controllers
{
  public class ValuesController : ApiController
  {
    public IEnumerable<string> Get()
    {
      return (IEnumerable<string>) new string[2]
      {
        "value1",
        "value2"
      };
    }

    public string Get(int id)
    {
      return "value";
    }

    public void Post([FromBody] string value)
    {
    }

    public void Put(int id, [FromBody] string value)
    {
    }

    public void Delete(int id)
    {
    }

    public ValuesController()
    {
      
    }
  }
}
