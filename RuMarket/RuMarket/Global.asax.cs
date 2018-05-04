using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

#if !MONO
using Microsoft.Owin;
using Owin;
#endif

#if !MONO
[assembly: OwinStartup(typeof(RuMarket.Startup))]
#endif

namespace RuMarket
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }

#if !MONO
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            CurrencyQuotesProcessor.Run();
            app.MapSignalR();
        }
    }
#endif

}

