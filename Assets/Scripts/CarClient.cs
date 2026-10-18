using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarClient : MonoBehaviour
{
    [Header("Connection UI")]
    public TMP_InputField ipInputField;
    public Button connectButton;
    public TMP_Text statusText;

    [Header("Driving Controls (Elements 1-3)")]
    public Slider steerSlider;
    public HoldButton steerLeftButton;
    public HoldButton steerRightButton;
    public HoldButton gasPedal;
    public HoldButton brakePedal;

    [Header("Car Parts (Elements 4-5)")]
    public Button headlightsButton;
    public TMP_Text headlightsBtnText;
    public Button doorButton;
    public TMP_Text doorBtnText;

    [Header("Network Config")]
    public int port = 7777;
    public float sendRateHz = 30f;

    private TcpClient client;
    private StreamWriter writer;
    private Thread clientThread;
    private volatile bool isConnected;
    private volatile string statusMessage = "Disconnected";

    private CarControlData currentData = new CarControlData();
    private float nextSendTime;

    private void Start()
    {
        Application.runInBackground = true;

        if (ipInputField != null && string.IsNullOrEmpty(ipInputField.text))
            ipInputField.text = "127.0.0.1";

        UpdateUI();
    }

    public void ConnectToServer()
    {
        if (isConnected)
        {
            Disconnect();
            return;
        }

        string ip = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
        statusMessage = $"Connecting to {ip}:{port}...";

        clientThread = new Thread(() => ConnectWorker(ip, port))
        {
            IsBackground = true
        };
        clientThread.Start();
    }

    private void ConnectWorker(string ip, int targetPort)
    {
        try
        {
            client = new TcpClient();
            client.NoDelay = true;
            client.Connect(ip, targetPort);

            writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            isConnected = true;
            statusMessage = $"Connected to {ip}:{targetPort}";
        }
        catch (Exception ex)
        {
            isConnected = false;
            statusMessage = $"Failed to connect: {ex.Message}";
        }
    }

    public void Disconnect()
    {
        isConnected = false;
        try { writer?.Close(); } catch { }
        try { client?.Close(); } catch { }
        client = null;
        writer = null;
        statusMessage = "Disconnected";
        UpdateUI();
    }

    public void ToggleHeadlights()
    {
        currentData.headlights = !currentData.headlights;
        currentData.cameraFocus = 4;
        UpdateUI();
    }

    public void ToggleDoor()
    {
        currentData.doorOpen = !currentData.doorOpen;
        currentData.cameraFocus = 5;
        UpdateUI();
    }

    public void SetCameraFocus(int focusIndex)
    {
        currentData.cameraFocus = focusIndex;
    }

    public void ResetSteerSlider()
    {
        if (steerSlider != null)
            steerSlider.value = 0f;
    }

    private void Update()
    {
        if (statusText != null)
            statusText.text = statusMessage;

        // 1. Gather steering input
        float steer = 0f;
        if (steerLeftButton != null && steerLeftButton.isPressed) steer -= 1f;
        if (steerRightButton != null && steerRightButton.isPressed) steer += 1f;

        if (steer == 0f && steerSlider != null)
            steer = steerSlider.value;

        currentData.steer = steer;

        // 2. Gather throttle input
        currentData.throttle = (gasPedal != null && gasPedal.isPressed) ? 1f : 0f;

        // 3. Gather brake input
        currentData.brake = (brakePedal != null && brakePedal.isPressed) ? 1f : 0f;

        // 4. Send over raw TCP socket at fixed rate (~30 Hz)
        if (isConnected && writer != null && Time.time >= nextSendTime)
        {
            nextSendTime = Time.time + (1f / sendRateHz);
            try
            {
                string json = JsonUtility.ToJson(currentData);
                writer.WriteLine(json);
            }
            catch
            {
                Disconnect();
            }
        }
    }

    private void UpdateUI()
    {
        if (connectButton != null)
        {
            TMP_Text btnText = connectButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = isConnected ? "Disconnect" : "Connect";
        }

        if (headlightsBtnText != null)
            headlightsBtnText.text = $"Headlights: {(currentData.headlights ? "ON" : "OFF")}";

        if (doorBtnText != null)
            doorBtnText.text = $"Door: {(currentData.doorOpen ? "OPEN" : "CLOSED")}";
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}
