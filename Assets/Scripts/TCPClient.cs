using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class TCPClient : MonoBehaviour
{
    static string ipAddress;

    TcpClient client;
    NetworkStream stream;
    StreamReader reader;
    StreamWriter writer;
    Thread clientThread;
    bool isRunning;

    byte[] receiveBuffer = new byte[1024];


    [SerializeField]
    Text txtIP;
    [SerializeField]
    Button btnConnect;

    private Text logText;
    private ScrollRect scrollRect;

    private bool isBtnClicked = false;

    private bool isConnected = false;
    private bool reconnecting = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        btnConnect.enabled = true;

        logText = GameObject.Find("log_Text").GetComponent<Text>();
        scrollRect = GameObject.Find("Scroll_View").GetComponent<ScrollRect>();

        if (logText != null)
        {
            TextLogMsg("������ ��ٸ��� �ֽ��ϴ�...");
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

    bool IsValidIP(string ip)
    {
        string pattern = @"^([0-9]{1,3}\.){3}[0-9]{1,3}$";
        return Regex.IsMatch(ip, pattern);
    }

    bool IsInRange(string ip)
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
            TextLogMsg("�������� ������ ���������ϴ�.");
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
    void PlayerControl(string str)
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
            TextLogMsg("������ ����Ǿ����ϴ�.");
            OnSendMessageToServer("load:1");
        }
        catch (Exception e)
        {
            TextLogMsg("���� ����: " + e.Message);
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
        // ���� �������� ������ �õ�
        while (!isConnected)
        {
            yield return new WaitForSeconds(5f); // 5�ʸ��� ������ �õ�
            TextLogMsg("������ �������� �õ��մϴ�...");
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
            TextLogMsg("������ ���� ����: " + e.Message);
        }
    }

    public void OnSendMessageToServer(string message)
    {
        try
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            stream.Flush();

            TextLogMsg("������ �޽��� ����: " + message);
        }
        catch (Exception e)
        {
            TextLogMsg("�޽��� ���� ����: " + e.Message);
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
        TextLogMsg("������ ����Ǿ����ϴ�.");
    }

    private void OnApplicationQuit()
    {
        CloseConnection();
    }


}
