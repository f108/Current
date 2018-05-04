using System;
using System.Collections.Generic;
using System.Web;

#if !MONO
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace RuMarket
{
    [HubName("QuotesHub")]
    public class QuotesHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.broadcastMessage(name, message);
        }
        public void Send(string curname, string direction, string state, string statevalue,
            string cvalue, string cvalue3)
        {
            Clients.All.broadcastMessage(curname, direction, state, statevalue, cvalue, cvalue3);
        }
        public void Send(string cvalue)
        {
            Clients.All.broadcastMessage(cvalue);
        }
    }
}

#endif