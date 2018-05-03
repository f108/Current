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
        public string destination;
        public byte[] data;
        public bool sendUnencrypted;
    }
    class UdpConnector
    {
        ProcessInvitationBroadcastResponse IBRcallback;

        public const byte INVITATION_BROADCAST = 0x0001;
        public const byte INVITATION_RESPONSE = 0x0002;
        public const byte SHOW_PASSKEY = 0x0003;
        public const byte CHECK_PASSKEY = 0x0004;

        public UdpConnector()
        {
            AESCrypto.SetCodeword("pass");

            sender = new UdpClient();
            sender.DontFragment = true;
        }

        ConcurrentQueueWithEvent<DatagramPacket> SendQueue = new ConcurrentQueueWithEvent<DatagramPacket>(); 
        int listenPort = Properties.Settings.Default.listenPort;
        UdpClient listener;
        UdpClient sender;
        AES AESCrypto = new AES();

        public void SetCodeword(string codeword)
        {
            AESCrypto.SetCodeword(codeword);
        }
        public void Open()
        {
            listener = new UdpClient(listenPort);
            new Thread(() => StartListener()).Start();
            new Thread(() => sendThread()).Start();
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
                destination = "255.255.255.255",
                data = bytes,
                sendUnencrypted = true
            });
        }
        private void StartListener()
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

                    if (localIPs.Contains(groupEP.Address)) continue;

                    try
                    { // try to decrypt aes
                        byte[] data = AESCrypto.Decrypt(bytes);
                        MouseControl.ProcessReceivedMessage(data);// data);
                        continue;
                    }
                    catch { };
                    if (bytes.Length<1)
                    {
                        continue;
                    };

                    switch (bytes[0])
                    {
                        case INVITATION_BROADCAST: // invitation 
                            Program.PostLog("incoming INVITATION_BROADCAST");
                            SendQueue.Enqueue(new DatagramPacket {
                                destination = groupEP.Address.ToString(),
                                data = AddingNewHost.MakeResponseOnInvitationBroadcast(bytes),
                                sendUnencrypted = true });
                            break;
                        case INVITATION_RESPONSE: // response to invitation
                            Program.PostLog("incoming INVITATION_RESPONSE");
                            IBRcallback(bytes, groupEP);
                            break;
                        case SHOW_PASSKEY: // 
                            Program.PostLog("incoming SHOW_PASSKEY");
                            Program.ShowPasskey();
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

        private void _Send(string dest, byte[] buf, int length)
        {
            //byte[] encryptedbuf = RemoteRSA.Encrypt(buf, true);
            //sender.Send(encryptedbuf, encryptedbuf.Length, dest, listenPort);
            //byte[] data = AESCrypto.Encrypt(buf);
            //sender.Send(data, data.Length, dest, listenPort);
        }

        private void sendThread()
        {
            DatagramPacket dp;
            for (;Program.inProgress;)
            {
                if (SendQueue.IsEmpty)
                {
                    if (!SendQueue.WaitOne(1000)) continue;
                };
                if (SendQueue.TryDequeue(out dp))
                {
                    if (dp.sendUnencrypted)
                        sender.Send(dp.data, dp.data.Length, dp.destination, listenPort);
                    else
                    {
                        byte[] data = AESCrypto.Encrypt(dp.data);
                        sender.Send(data, data.Length, dp.destination, listenPort);
                    }

                }
                    //_Send(dp.destination, dp.data, dp.data.Length);
            }
        }

        public void Send(string dest, byte[] buf, bool SendUnencrypted=false)
        {
            SendQueue.Enqueue(new DatagramPacket { destination = dest, data = buf, sendUnencrypted = SendUnencrypted });
        }

    }
}
