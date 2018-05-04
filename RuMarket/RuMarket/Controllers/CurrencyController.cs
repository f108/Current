using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RuMarket.Controllers
{
    public class CurrencyController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult CurTable(string currency)
        {
            if (string.IsNullOrEmpty(currency)) return View();
            ViewData["curname"] = currency;
            ViewData["direction"] = "*";
            ViewData["state"] = CurrencyQuotesProcessor.Quotes[currency].state;
            ViewData["statevalue"] = CurrencyQuotesProcessor.Quotes[currency].statevalue;

            ViewData["cvalue"] = CurrencyQuotesProcessor.Quotes[currency].cvalue;
            ViewData["cvalue3"] = CurrencyQuotesProcessor.Quotes[currency].cvalue3;
            ViewData["mvalueclass"] = CurrencyQuotesProcessor.Quotes[currency].mvalueclass;

            ViewData["todstate"]="OPEN";
            ViewData["todvalue"] = "65.32";
            ViewData["todpercent"] = "+0.2%";

            ViewData["todopen"] = "64.237";
            ViewData["todmin"] = "67.952";
            ViewData["todmax"] = "62.341";

            ViewData["todprevclose"] = "65.846";
            ViewData["todprevpercent"] = "-3.21%";

            return View();
        }
    }
}
