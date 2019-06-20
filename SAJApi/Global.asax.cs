using System.IO;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using log4net.Config;

namespace SAJApi
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            
           XmlConfigurator.Configure();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);            
            //Microsoft.AspNet.WebApi.Cors
        }
    }
}
