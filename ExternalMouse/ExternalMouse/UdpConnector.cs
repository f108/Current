using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ExternalMouse
{
    struct DatagramPacket
    {
        public IPAddress destination;
        public byte[] data;
    }
    class UdpConnector
    {
        ProcessInvitationBroadcastResponse IBRcallback;

        public const byte INVITATION_BROADCAST = 0x01;
        public const byte INVITATION_RESPONSE = 0x02;
        public const byte SHOW_PASSKEY = 0x03;
        public const byte CHECK_PASSKEY = 0x04;
        public const byte CONNECT_REQUEST = 0x05;

        public UdpConnector()
        {
            //
        }

        ConcurrentQueueWithEvent<DatagramPacket> SendQueue = new ConcurrentQueueWithEvent<DatagramPacket>(); 
        int listenPort = Properties.Settings.Default.listenPort;
        UdpClient listener;
        UdpClient sender;

        public void Open()
        {
            sender = new UdpClient
            {
                //DontFragment = true,
                EnableBroadcast = true,
                
            };
            sender.AllowNatTraversal(true);
            listener = new UdpClient(listenPort);
            listener.AllowNatTraversal(true);
            new Thread(() => ListenerThread()).Start();
            new Thread(() => SenderThread()).Start();
        }
        public void Close()
        {
            listener.Close();
            sender.Close();
        }

        public void SendInvitationBroadcast(ProcessInvitationBroadcastResponse IBResponse)
        {
            IBRcallback = IBResponse;
            byte[] bytes = { 0x01 };
            SendQueue.Enqueue(new DatagramPacket
            {
                destination = IPAddress.Parse( "255.255.255.255"),
                data = bytes
            });
        }
        private void ListenerThread()
        {
            Debug.WriteLine("Start UDP listener");

            List<IPAddress> localIPs = new List<IPAddress>();
            localIPs.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.
                Where(s => s.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork));

            IPEndPoint groupEP = new IPEndPoint(IPAddress.IPv6None, listenPort);

            while (Program.inProgress)
            {

                try
                {
                    byte[] bytes = listener.Receive(ref groupEP);
                    Program.PostLog("received "+bytes.Length + " bytes.");
                    //if (localIPs.Contains(groupEP.Address)) continue;

                    if (Program.pairedHosts.TryProcessMessage(groupEP.Address, bytes)) continue;

                    if (bytes.Length<1)
                    {
                        continue;
                    };

                    switch (bytes[0])
                    {
                        case INVITATION_BROADCAST: // invitation 
                            Program.PostLog("incoming INVITATION_BROADCAST from " + groupEP.Address.ToString());
                            SendQueue.Enqueue(new DatagramPacket {
                                destination = groupEP.Address,
                                data = AddingNewHost.MakeResponseOnInvitationBroadcast(bytes)
                            });
                            break;
                        case INVITATION_RESPONSE: // response to invitation
                            Program.PostLog("incoming INVITATION_RESPONSE");
                            IBRcallback(bytes, groupEP);
                            break;
                        case SHOW_PASSKEY: // 
                            Program.PostLog("incoming SHOW_PASSKEY");
                            Program.ShowPasskey();
                            break;
                        case CONNECT_REQUEST: // 
                            Program.PostLog("incoming CONNECT_REQUEST");
                            Program.destopsForm.PopupBroadcastNotification(groupEP.Address, bytes);
                            break;
                    }



                }
                catch (Exception e)
                {
                    //Thread.Sleep(200);
                    Debug.WriteLine(e.ToString());
                }
            };

        }

        private void SenderThread()
        {
            for (; Program.inProgress;)
            {
                if (SendQueue.IsEmpty && !SendQueue.WaitOne(1000)) continue;

                if (SendQueue.TryDequeue(out DatagramPacket dp))
                {
                    sender.Send(dp.data, dp.data.Length, new IPEndPoint(dp.destination, listenPort));
                    Program.PostLog("sended " + dp.data.Length + " bytes.");
                }
            }
        }

        public void Send(string dest, byte[] buf)
        {
            SendQueue.Enqueue(new DatagramPacket { destination = IPAddress.Parse(dest), data = buf });
        }

        public void Send(IPAddress dest, byte[] buf)
        {
            SendQueue.Enqueue(new DatagramPacket { destination = dest, data = buf });
        }
    }
}
