using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
using FileManagerTest.Model;

namespace FileManagerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<ConnectionInfo, Dictionary<long, byte[]>> incomingDataCache = new Dictionary<ConnectionInfo, Dictionary<long, byte[]>>();

        Dictionary<ConnectionInfo, Dictionary<long, SendFile>> incomingDataInfoCache = new Dictionary<ConnectionInfo, Dictionary<long, SendFile>>();

        SendReceiveOptions customOptions = new SendReceiveOptions<ProtobufSerializer>();

        object syncRoot = new object();

        static volatile bool windowClosing = false;


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            windowClosing = true;
            NetworkComms.Shutdown();
        }

        private void AddLineToLog(string logLine)
        {
            //Use dispatcher incase method is not called from GUI thread
            logBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                logBox.Text += DateTime.Now.ToShortTimeString() + " - " + logLine + "\n";

                //Update the scroller so that we are always at the bottom
                scroller.ScrollToBottom();
            }));
        }

        public MainWindow()
        {
            InitializeComponent();
            StartListening();
        }

        private void StartListening()
        {
            //Trigger the method OnConnectionClose so that we can do some clean-up
            //NetworkComms.AppendGlobalConnectionCloseHandler(OnConnectionClose);

            //Start listening for TCP connections
            //We want to select a random port on all available adaptors so provide 
            //an IPEndPoint using IPAddress.Any and port 0.
            Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, 0));

            //Write out some useful debugging information the log window
            AddLineToLog("Initialised WPF file transfer example. Accepting TCP connections on:");
            foreach (IPEndPoint listenEndPoint in Connection.ExistingLocalListenEndPoints(ConnectionType.TCP))
                AddLineToLog(listenEndPoint.Address + ":" + listenEndPoint.Port);
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Multiselect = false;

            //If a file was selected
            if (openDialog.ShowDialog() == true)
            {
                //Disable the send and compression buttons
                sendFileButton.IsEnabled = false;

                //Parse the necessary remote information
                string filename = openDialog.FileName;
                string remoteIP = this.remoteIP.Text;
                string remotePort = this.remotePort.Text;

                //Perform the send in a task so that we don't lock the GUI
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        //Create a fileStream from the selected file
                        FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);

                        //Wrap the fileStream in a threadSafeStream so that future operations are thread safe
                        StreamTools.ThreadSafeStream safeStream = new StreamTools.ThreadSafeStream(stream);

                        //Get the filename without the associated path information
                        string shortFileName = System.IO.Path.GetFileName(filename);

                        //Parse the remote connectionInfo
                        //We have this in a separate try catch so that we can write a clear message to the log window
                        //if there are problems
                        ConnectionInfo remoteInfo;
                        try
                        {
                            remoteInfo = new ConnectionInfo(remoteIP, int.Parse(remotePort));
                        }
                        catch (Exception)
                        {
                            throw new InvalidDataException("Failed to parse remote IP and port. Check and try again.");
                        }

                        //Get a connection to the remote side
                        Connection connection = TCPConnection.GetConnection(remoteInfo);

                        //Break the send into 20 segments. The less segments the less overhead 
                        //but we still want the progress bar to update in sensible steps
                        long sendChunkSizeBytes = (long)(stream.Length / 20.0) + 1;

                        //Limit send chunk size to 500MB
                        long maxChunkSizeBytes = 500L * 1024L * 1024L;
                        if (sendChunkSizeBytes > maxChunkSizeBytes) sendChunkSizeBytes = maxChunkSizeBytes;

                        long totalBytesSent = 0;
                        do
                        {
                            //Check the number of bytes to send as the last one may be smaller
                            long bytesToSend = (totalBytesSent + sendChunkSizeBytes < stream.Length ? sendChunkSizeBytes : stream.Length - totalBytesSent);

                            //Wrap the threadSafeStream in a StreamSendWrapper so that we can get NetworkComms.Net
                            //to only send part of the stream.
                            StreamTools.StreamSendWrapper streamWrapper = new StreamTools.StreamSendWrapper(safeStream, totalBytesSent, bytesToSend);

                            //We want to record the packetSequenceNumber
                            long packetSequenceNumber;
                            //Send the select data
                            connection.SendObject("PartialFileData", streamWrapper, customOptions, out packetSequenceNumber);
                            //Send the associated SendInfo for this send so that the remote can correctly rebuild the data
                            connection.SendObject("PartialFileDataInfo", new SendFile(shortFileName, stream.Length, totalBytesSent, packetSequenceNumber), customOptions);

                            totalBytesSent += bytesToSend;

                        } while (totalBytesSent < stream.Length);

                        //Clean up any unused memory
                        GC.Collect();

                        AddLineToLog("Completed file send to '" + connection.ConnectionInfo.ToString() + "'.");
                    }
                    catch (CommunicationException)
                    {
                        //If there is a communication exception then we just write a connection
                        //closed message to the log window
                        AddLineToLog("Failed to complete send as connection was closed.");
                    }
                    catch (Exception ex)
                    {
                        //If we get any other exception which is not an InvalidDataException
                        //we log the error
                        if (!windowClosing && ex.GetType() != typeof(InvalidDataException))
                        {
                            AddLineToLog(ex.Message.ToString());
                            LogTools.LogException(ex, "SendFileError");
                        }
                    }

                    //Once complete enable the send button again
                    sendFileButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        sendFileButton.IsEnabled = true;
                    }));
                });
            }
        }

    }
}
