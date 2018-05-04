using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Data.Odbc;

#if !MONO
using Microsoft.AspNet.SignalR;
#endif 

namespace RuMarket
{
    public class Currency
    {
        public int hash;
        public string rawline;

        public string curname; 
        public string direction;
        public string mvalueclass;
        public string state;
        public string statevalue;
        public float lastvalue;
        public float prevlastvalue;
        public string cvalue;
        public string cvalue3;

        public float todlastprice;
        public float todprevlastprice;
        public float todmin;
        public float todmax;
        public float todclosediff;
        public float todclosediffp;
        public float todclose;
        public float todopen;
        public string todstate;
        public string todpricecolor;

        public float tomlastprice;
        public float tomprevlastprice;
        public float tommin;
        public float tommax;
        public float tomclosediff;
        public float tomclosediffp;
        public float tomclose;
        public float tomopen;
        public string tomstate;
        public string tompricecolor;

    }
    public sealed class CurrencyQuotesProcessor
    {
        public static Dictionary<string, Currency> Quotes = new Dictionary<string, Currency>();
#if !MONO
        private static IHubContext _hubContext;
#endif
        private static readonly CurrencyQuotesProcessor QuotesProcessor = new CurrencyQuotesProcessor();
        private CurrencyQuotesProcessor() {
            Quotes["USD"] = new Currency();
            Quotes["EUR"] = new Currency();
#if !MONO
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<QuotesHub>();
            new Thread(() => CurrencyQuotesWorker()).Start();
#endif
        }
        public static CurrencyQuotesProcessor Instance
        { 
            get {
                return QuotesProcessor; 
            }
        }
        static public void Run()
        {
        }

        static string connectionString = "DSN=quikimporter;UID=quikimporter;PWD=quikimporter";
        static Object Lock = new Object();
        static bool inProc = true;

        public static void CurrencyQuotesWorker()
        {
            while (inProc)
            {
                try
                {
                    GetCurrencyQuotes();
                }
                catch { };
                Thread.Sleep(100);
            }
        }
        static private bool GetCurrencyQuotes()
        {
            string cur;
            string line;
            int newhash;
            OdbcConnection DbConnection = new OdbcConnection(connectionString);
            OdbcCommand myOleDbCommand = DbConnection.CreateCommand();
            myOleDbCommand.CommandText = "SELECT cur, line from v_curr_raw2";
            DbConnection.Open();
            OdbcDataReader myOleDbDataReader = myOleDbCommand.ExecuteReader();

            if (myOleDbDataReader.HasRows)
            {
                while (myOleDbDataReader.Read())
                {
                    cur = myOleDbDataReader["cur"].ToString();
                    line = myOleDbDataReader["line"].ToString();
                    newhash = line.GetHashCode();
                    if (Quotes[cur].hash!=newhash)
                    {
                        ParseQoutes(cur, line, newhash);
                    }
                }
            }
            myOleDbDataReader.Close();
            DbConnection.Close();
            return true;
        }

        static private void ParseQoutes(string cur, string line, int newhash)
        {
            float val;
            string[] values = line.Split('|');
            Quotes[cur].hash = newhash;
            Quotes[cur].rawline = line;

            Quotes[cur].todprevlastprice = Quotes[cur].todlastprice;
            Quotes[cur].todlastprice = float.Parse(values[3]);
            if (Quotes[cur].todprevlastprice < Quotes[cur].todlastprice)
                Quotes[cur].todpricecolor = "green";
            else
                Quotes[cur].todpricecolor = "red";
            Quotes[cur].todmin = float.Parse(values[5]);
            Quotes[cur].todmax = float.Parse(values[6]);
            Quotes[cur].todclosediff = float.Parse(values[7]);
            Quotes[cur].todclosediffp = float.Parse(values[8]);
            Quotes[cur].todclose = float.Parse(values[9]);
            Quotes[cur].todopen = float.Parse(values[10]);
            Quotes[cur].todstate = values[13];

            Quotes[cur].tomprevlastprice = Quotes[cur].tomlastprice;
            Quotes[cur].tomlastprice = float.Parse(values[17]);
            Quotes[cur].tommin = float.Parse(values[19]);
            Quotes[cur].tommax = float.Parse(values[20]);
            Quotes[cur].tomclosediff = float.Parse(values[21]);
            Quotes[cur].tomclosediffp = float.Parse(values[22]);
            Quotes[cur].tomclose = float.Parse(values[23]);
            Quotes[cur].tomopen = float.Parse(values[24]);
            Quotes[cur].tomstate = values[27];
            /*if (Quotes[cur].tomprevlastprice < Quotes[cur].tomlastprice)
                Quotes[cur].tompricecolor = "green";
            else
                Quotes[cur].tompricecolor = "red";*/

            if (Quotes[cur].todstate.Equals("OPEN"))
            {
                val = Quotes[cur].todlastprice;
                Quotes[cur].state = "TOM:";
                Quotes[cur].statevalue = Quotes[cur].tomlastprice.ToString("00.000", CultureInfo.InvariantCulture);
                Quotes[cur].prevlastvalue = Quotes[cur].lastvalue;
                Quotes[cur].lastvalue = val;
                if (Quotes[cur].todclosediff>0)
                    Quotes[cur].mvalueclass = "green";
                else
                    Quotes[cur].mvalueclass = "red";
            }
            else
            {
                val = Quotes[cur].tomlastprice;
                Quotes[cur].state = "TOMORROW";
                Quotes[cur].statevalue = "";
                Quotes[cur].prevlastvalue = Quotes[cur].lastvalue;
                Quotes[cur].lastvalue = val;
                if (Quotes[cur].tomclosediff > 0)
                    Quotes[cur].mvalueclass = "green";
                else
                    Quotes[cur].mvalueclass = "red";
            }

            if (Quotes[cur].lastvalue > Quotes[cur].prevlastvalue)
                Quotes[cur].direction = "<span id='directionanimate' style='color:green;'>" + "↑" + "</span>";
            else if (Quotes[cur].lastvalue < Quotes[cur].prevlastvalue)
                Quotes[cur].direction = "<span id='directionanimate' style='color:red;'>" + "↓" + "</span>";
            else Quotes[cur].direction = "";
            

            Quotes[cur].cvalue = (Math.Truncate(val * 100) / 100).ToString("0.00", CultureInfo.InvariantCulture);
            Quotes[cur].cvalue3 = (Math.Truncate(val * 1000) % 10).ToString("0");
#if !MONO
            _hubContext.Clients.All.broadcastMessage(cur, Quotes[cur].cvalue, Quotes[cur].cvalue3, Quotes[cur].mvalueclass, 
                Quotes[cur].state, Quotes[cur].statevalue, Quotes[cur].direction);
#endif
            //((QuotesHub)_hubContext).Send("uuu");
        }
    }
}