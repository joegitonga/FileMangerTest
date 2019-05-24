using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReadingFiles.Models;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Collections.ObjectModel;

using NetworkCommsDotNet;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;
using NetworkCommsDotNet.DPSBase.SevenZipLZMACompressor;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using ProtoBuf;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ReadingFiles.Controllers
{
    public class HomeController : Controller
    {
        internal class StateObject
        {
            internal byte[] sBuffer;
            internal Socket sSocket;
            internal StateObject(int size, Socket sock)
            {
                sBuffer = new byte[size];
                sSocket = sock;
            }
        }
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public void Index(ReceivedFile incomingFiles)
        {
            try
            {
                IPAddress ipAddress = Dns.Resolve(Dns.GetHostName()).AddressList[0];

                IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, 1800);

                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listenSocket.Bind(ipEndpoint);
                listenSocket.Listen(1);
                IAsyncResult asyncAccept = listenSocket.BeginAccept(new AsyncCallback(HomeController.acceptCallback), listenSocket);
            }
            catch(Exception ee)
            {

            }
            
        }

        private void StartListening()
        {
            //Start listening for TCP connections
            //We want to select a random port on all available adaptors so provide 
            //an IPEndPoint using IPAddress.Any and port 0.
            Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, 0));
        }

        public static void acceptCallback(IAsyncResult asyncAccept)
        {
            Socket listenSocket = (Socket)asyncAccept.AsyncState;
            Socket serverSocket = listenSocket.EndAccept(asyncAccept);

            // arriving here means the operation completed
            // (asyncAccept.IsCompleted = true) but not
            // necessarily successfully
            if (serverSocket.Connected == false)
            {
                return;
            }
            else
            {
                listenSocket.Close();
            }
            StateObject stateObject = new StateObject(16, serverSocket);
        }

        internal static bool writeDot(IAsyncResult ar)
        {
            int i = 0;
            while (ar.IsCompleted == false)
            {
                if (i++ > 40)
                {
                    return false;
                }
                Thread.Sleep(500);
            }
            return true;
        }

    }
}