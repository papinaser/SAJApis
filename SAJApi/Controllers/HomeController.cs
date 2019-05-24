using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Web.Mvc;

namespace SAJApi.Controllers
{
  public class HomeController : Controller
  {
    public ActionResult Index()
    {      
      return View();
    }    
  }
}
