using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;
using UnityEngine;

public class CarServer : MonoBehaviour
{
    [Header("References")]
    public CarController carController;
    public TMP_Text statusText;
    public TMP_Text ipAddressText;

    [Header("Network Config")]
    public int port = 7777;

    private TcpListener serverListener;
    private TcpClient connectedClient;
    private Thread serverThread;
    private volatile bool isRunning;

    private readonly object lockObj = new object();
    private CarControlData latestData;
    private bool hasNewData;
    private string currentStatus = "Starting...";

    private void Start()
    {
        Application.runInBackground = true;
        DisplayLocalIP();
        StartServer();
    }

    private void DisplayLocalIP()
    {
        string localIP = "127.0.0.1";
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch { }

        if (ipAddressText != null)
            ipAddressText.text = $"Server IP: {localIP}  |  Port: {port}";
    }

    public void StartServer()
    {
        isRunning = true;
        serverThread = new Thread(ServerWorker)
        {
            IsBackground = true
        };
        serverThread.Start();
    }

    private void ServerWorker()
    {
        try
        {
            serverListener = new TcpListener(IPAddress.Any, port);
            serverListener.Start();
            currentStatus = "Waiting for mobile client...";

            while (isRunning)
            {
                TcpClient client = serverListener.AcceptTcpClient();
                client.NoDelay = true;

                connectedClient = client;
                currentStatus = $"Client connected ({client.Client.RemoteEndPoint})";

                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (isRunning && client.Connected)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line)) break;

                        try
                        {
                            CarControlData data = JsonUtility.FromJson<CarControlData>(line);
                            if (data != null)
                            {
                                lock (lockObj)
                                {
                                    latestData = data;
                                    hasNewData = true;
                                }
                            }
                        }
                        catch { }
                    }
                }

                client.Close();
                connectedClient = null;
                currentStatus = "Client disconnected. Waiting for connection...";

                // Failsafe stop when client disconnects
                lock (lockObj)
                {
                    latestData = new CarControlData { brake = 1f };
                    hasNewData = true;
                }
            }
        }
        catch (SocketException)
        {
            // Listener stopped
        }
        catch (Exception ex)
        {
            currentStatus = $"Server error: {ex.Message}";
        }
    }

    private void Update()
    {
        if (statusText != null)
            statusText.text = currentStatus;

        if (hasNewData && carController != null)
        {
            CarControlData dataToApply;
            lock (lockObj)
            {
                dataToApply = latestData;
                hasNewData = false;
            }

            if (dataToApply != null)
                carController.ApplyInputs(dataToApply);
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
        try { connectedClient?.Close(); } catch { }
        try { serverListener?.Stop(); } catch { }
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }
}
