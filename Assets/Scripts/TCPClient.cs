using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TCPClient : MonoBehaviour
{
    private static string ipAddress;

    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread clientThread;
    private bool isRunning;

    private byte[] receiveBuffer = new byte[1024];

    //public Text txtIP;
    [SerializeField]
    private InputField txtIP;

    [SerializeField]
    private Button btnConnect;

    private Text logText;
    private ScrollRect scrollRect;

    private bool isBtnClicked = false;

    private bool isConnected = false;
    private bool reconnecting = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private const string SAVEIP = "SAVEIP";

    private void Start()
    {
        txtIP.text = PlayerPrefs.GetString(SAVEIP, "127.0.0.1");
        btnConnect.enabled = true;

        logText = GameObject.Find("log_Text").GetComponent<Text>();
        scrollRect = GameObject.Find("Scroll_View").GetComponent<ScrollRect>();

        if (logText != null)
        {
            TextLogMsg("접속을 기다리고 있습니다...");
        }
    }

    public void OnStartBtnClick()
    {
        string strIP = txtIP.text;
        if (Parse(strIP))
        {
            isBtnClicked = true;

            btnConnect.enabled = false;

            ipAddress = strIP;

            ConnectToServer();
        }
    }

    private bool Parse(string strIP)
    {
        bool isIP = IsValidIP(strIP);
        bool isInRange = IsInRange(strIP);

        return isIP && isInRange;
    }

    private bool IsValidIP(string ip)
    {
        string pattern = @"^([0-9]{1,3}\.){3}[0-9]{1,3}$";
        return Regex.IsMatch(ip, pattern);
    }

    private bool IsInRange(string ip)
    {
        var segments = ip.Split('.');
        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out int num))
            {
                if (num < 0 || num > 255) return false;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private void TextLogMsg(string msg)
    {
        logText.text += msg + "\n";
    }

    private void Update()
    {
        if (isConnected && (client == null || !client.Connected))
        {
            isConnected = false;
            TextLogMsg("서버와의 연결이 끊어졌습니다.");
            StartReconnection();

            return;
        }

        if (client != null && client.Connected)
        {
            if (stream.DataAvailable)
            {
                ReadDataFromServer();
            }
        }
    }

    private void PlayerControl(string str)
    {
        //if (!player.isPlaying)
        //{
        //    if (str.Contains("load"))
        //    {
        //        string temp = str.Replace("start load", "");
        //        if (int.TryParse(temp, out int vedioNum))
        //        {
        //            if (vedioNum == 1 && player.videoUrlList.Count > 0 )
        //            {
        //                if (!player.videoUrl.Equals(player.videoUrlList[vedioNum - 1]))
        //                {
        //                    player.videoUrl = player.videoUrlList[vedioNum - 1];
        //                    player.Load(player.videoUrl, false);
        //                }
        //            }

        //        }

        //    }
        //    if (str.Contains("start"))
        //    {
        //        StartCoroutine(LoadScene(1));
        //    }

        //}
        //else
        //{
        //    if (str.Contains("pause"))
        //        player.Pause();
        //    else if (str.Contains("estop"))
        //        player.Stop();
        //    else if (str.Contains("reset"))
        //        player.Replay();

        //    SceneManager.LoadScene(2);
        //}
    }

    public void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            client.Connect(ipAddress, 6000);
            stream = client.GetStream();
            isConnected = true;
            reconnecting = false;
            TextLogMsg("서버에 연결되었습니다.");
            OnSendMessageToServer($"load:{TCPSever.videoNum},");
            PlayerPrefs.SetString(SAVEIP, ipAddress);
        }
        catch (Exception e)
        {
            TextLogMsg("연결 실패: " + e.Message);
        }
    }

    private void StartReconnection()
    {
        if (!reconnecting)
        {
            reconnecting = true;
            StartCoroutine(Reconnect());
        }
    }

    private System.Collections.IEnumerator Reconnect()
    {
        // 일정 간격으로 재접속 시도
        while (!isConnected)
        {
            yield return new WaitForSeconds(5f); // 5초마다 재접속 시도
            TextLogMsg("서버에 재접속을 시도합니다...");
            ConnectToServer();
        }
    }

    private void ReadDataFromServer()
    {
        try
        {
            int bytesRead = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
            if (bytesRead > 0)
            {
                string receivedMessage = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                if (logText != null)
                    TextLogMsg(receivedMessage);
                if (receivedMessage.Contains("CloseServer"))
                    CloseConnection();

                PlayerControl(receivedMessage);
            }
        }
        catch (Exception e)
        {
            TextLogMsg("데이터 수신 오류: " + e.Message);
        }
    }

    public void OnSendMessageToServer(string message)
    {
        if (!isConnected) return;
        try
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            stream.Flush();

            TextLogMsg("서버로 메시지 전송: " + message);
        }
        catch (Exception e)
        {
            TextLogMsg("메시지 전송 실패: " + e.Message);
        }
    }

    private void CloseConnection()
    {
        if (stream != null)
        {
            stream.Close();
        }

        if (client != null)
        {
            client.Close();
            client = null;
        }
        isConnected = false;
        reconnecting = false;
        TextLogMsg("연결이 종료되었습니다.");
    }

    private void OnApplicationQuit()
    {
        CloseConnection();
    }
}