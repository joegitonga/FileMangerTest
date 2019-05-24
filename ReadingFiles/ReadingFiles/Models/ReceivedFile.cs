using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NetworkCommsDotNet;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using ProtoBuf;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ReadingFiles.Models
{
    public class ReceivedFile
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

        ObservableCollection<ReceivedFile> receivedFiles = new ObservableCollection<ReceivedFile>();

        Dictionary<ConnectionInfo, Dictionary<string, ReceivedFile>> receivedFilesDict = new Dictionary<ConnectionInfo, Dictionary<string, ReceivedFile>>();

        Dictionary<ConnectionInfo, Dictionary<long, byte[]>> incomingDataCache = new Dictionary<ConnectionInfo, Dictionary<long, byte[]>>();

        public string Filename { get; private set; }

        public ConnectionInfo SourceInfo { get; private set; }

        public long SizeBytes { get; private set; }

        public long ReceivedBytes { get; private set; }

        public string SourceInfoStr
        {
            get { return "[" + SourceInfo.RemoteEndPoint.ToString() + "]"; }
        }

        public bool IsCompleted
        {
            get { return ReceivedBytes == SizeBytes; }
        }

        object SyncRoot = new object();

        Stream data;

        public static void receiveCallback(IAsyncResult asyncReceive)
        {
            StateObject stateObject = (StateObject)asyncReceive.AsyncState;
            int bytesReceived = stateObject.sSocket.EndReceive(asyncReceive);
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

            // this call passes the StateObject because it 
            // needs to pass the buffer as well as the socket
            IAsyncResult asyncReceive = serverSocket.BeginReceive(stateObject.sBuffer,
                0,
                stateObject.sBuffer.Length,
                SocketFlags.None,
                new AsyncCallback(receiveCallback),
                stateObject);

        }

        private void AddNewReceivedItem(ReceivedFile file)
        {
            receivedFiles.Add(file);
        }
        public ReceivedFile(string filename, ConnectionInfo sourceInfo, long sizeBytes)
        {
            this.Filename = filename;
            this.SourceInfo = sourceInfo;
            this.SizeBytes = sizeBytes;
            data = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8 * 1024, FileOptions.DeleteOnClose);
        }

        public void AddData(long dataStart, int bufferStart, int bufferLength, byte[] buffer)
        {
            lock (SyncRoot)
            {
                data.Seek(dataStart, SeekOrigin.Begin);
                data.Write(buffer, (int)bufferStart, (int)bufferLength);

                ReceivedBytes += (int)(bufferLength - bufferStart);

                //Ensure the data is correctly flushed if we have received everything
                if (ReceivedBytes == SizeBytes)
                    data.Flush();
            }
        }

        public void SaveFileToDisk(string saveLocation)
        {
            if (ReceivedBytes != SizeBytes)
                throw new Exception("Attempted to save out file before data is complete.");

            if (!File.Exists(Filename))
                throw new Exception("The transferred file should have been created within the local application directory. Where has it gone?");

            File.Delete(saveLocation);
            File.Copy(Filename, saveLocation);
        }

        public void Close()
        {
            try
            {
                data.Dispose();
            }
            catch (Exception) { }

            try
            {
                data.Close();
            }
            catch (Exception) { }
        }

    }
}