using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.IO;

namespace RuMarketFrontend.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CurTable(string currency)
        {

            string url = "http://db.indepico.com/Currency/CurTable?currency="+currency;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return Content(responseString);
        }

        public ActionResult About()
        {
            return View();
        }
    }
}