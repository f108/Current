using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RuMarket.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.Mvc;
    using System.Data.Odbc;
    using System.Threading;

    namespace ORAMVC.Controllers
    {
        public class HomeController : Controller
        {
            static string connectionString = "DSN=quikimporter;UID=quikimporter;PWD=quikimporter";
            static string CurrentCurrencyQuotes;
            static uint CurrencyQuotesHash = 0;
            //static Helpers.EventMultipleWait ResultEvent = new Helpers.EventMultipleWait();
            static Object Lock = new Object();
            static bool inProc = true;

            public static void CurrencyQuotesWorker()
            {
                string res;
                uint CurrentHash;
                while (inProc)
                {
                    try
                    {
                        res = GetCurrencyQuotes();
                        CurrentHash = (uint)res.GetHashCode();
                        if (CurrentHash != CurrencyQuotesHash)
                            lock (Lock)
                            {
                                CurrentCurrencyQuotes = res;
                                CurrencyQuotesHash = CurrentHash;
                                //ResultEvent.Set();
                            };
                    }
                    catch { };
                    Thread.Sleep(300);
                }
            }
            static private string GetCurrencyQuotes()
            {
                List<string> ret = new List<string>();
                OdbcConnection DbConnection = new OdbcConnection(connectionString);
                OdbcCommand myOleDbCommand = DbConnection.CreateCommand();
                myOleDbCommand.CommandText = "SELECT * from v_curr_raw";
                DbConnection.Open();
                OdbcDataReader myOleDbDataReader = myOleDbCommand.ExecuteReader();

                if (myOleDbDataReader.HasRows)
                {
                    while (myOleDbDataReader.Read())
                    {
                        ret.Add("" + myOleDbDataReader["line"]);
                    }
                }
                myOleDbDataReader.Close();
                DbConnection.Close();
                return string.Join("\n", ret.ToArray());
            }
            public ActionResult Req()
            {
                string ret;

                OdbcConnection DbConnection = new OdbcConnection(connectionString);

                OdbcCommand myOleDbCommand = DbConnection.CreateCommand();

                myOleDbCommand.CommandText = "SELECT count(*) as cnt from alltradescur;";

                DbConnection.Open();

                OdbcDataReader myOleDbDataReader = myOleDbCommand.ExecuteReader();

                myOleDbDataReader.Read();

                ret = myOleDbDataReader["CNT"] + "Welcome to ASP.NET MVC!";
                myOleDbDataReader.Close();
                DbConnection.Close();

                return Content(ret);
            }

            public ActionResult Index()
            {
                OdbcConnection DbConnection = new OdbcConnection(connectionString);

                OdbcCommand myOleDbCommand = DbConnection.CreateCommand();

                myOleDbCommand.CommandText = "SELECT count(*) as cnt from alltradescur;";

                DbConnection.Open();

                OdbcDataReader myOleDbDataReader = myOleDbCommand.ExecuteReader();

                myOleDbDataReader.Read();

                ViewData["Message"] = myOleDbDataReader["CNT"] + "Welcome to ASP.NET MVC!";
                myOleDbDataReader.Close();
                DbConnection.Close();


                return View();
            }

            public ActionResult CurrencyQuotes()
            {
                string ret;
                lock (Lock)
                {
                    ret = CurrentCurrencyQuotes;
                }
                return Content(ret);
            }

            public ActionResult CurrencyQuotesHashed(string hash = "")
            {
                string ret;
                uint Hash;
                if (!uint.TryParse(hash, out Hash)) Hash = 0;
                int counter = 0;

                while (Hash == CurrencyQuotesHash && counter < 50)
                {
                    Thread.Sleep(200);
                    counter++;
                }

                lock (Lock)
                {
                    ret = CurrentCurrencyQuotes;
                    Hash = CurrencyQuotesHash;
                }

                return Content(Hash.ToString() + "\n" + ret);
            }
            
           

            public ActionResult About()
            {
                return View();
            }

        }
    }
}