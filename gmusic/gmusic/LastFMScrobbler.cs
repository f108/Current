using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace gmusic
{
    class LastFMScrobbler
    {
        string API_KEY = "95c1708ee85a54463fe1f0455e2183a0";

        public LastFMScrobbler()
        {
            string token;
            Microsoft.Win32.RegistryKey rk;
            try
            {
                rk = Registry.CurrentUser.OpenSubKey(Program.regpath);
            }
            catch { return; };
            try
            {
                token = (string)rk.GetValue("lastfmtoken");
            }
            catch { };
        }
    }
}
