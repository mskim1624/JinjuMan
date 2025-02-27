using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class TCPSever : MonoBehaviour
{
    public class ClientData
    {
        public ClientData(bool isConneted, UserStatus status, TcpClient client)
        {
            this.isConneted = isConneted;
            this.status = status;
            this.client = client;
        }

        public TcpClient client { get; set; }
        public bool isConneted { get; set; }
        public UserStatus status { get; set; }
    }

    private TcpListener tcpListener;
    public int currentVideoIndex;

    private List<(TcpClient, NetworkStream)> clients = new List<(TcpClient, NetworkStream)>();
    private Dictionary<int, ClientData> userStatus = new Dictionary<int, ClientData>();

    [SerializeField]
    private List<UserStatus> users = new List<UserStatus>();

    public List<ClientData> clientDatas = new List<ClientData>();
    public TMP_Text serverStatusText;
    public Image severStatusImage;

    private async void Start()
    {
        for (int i = 0; i < users.Count; i++)
        {
            clientDatas.Add(new ClientData(false, users[i], null));
            userStatus.Add(i + 1, clientDatas[i]);
        }

        await StartServer();
    }

    public async Task StartServer()
    {
        serverStatusText.text = GetLocalIPAddress();
        IPAddress localAddr = IPAddress.Parse(serverStatusText.text);
        tcpListener = new TcpListener(localAddr, 7777);
        tcpListener.Start();

        _ = MonitorClientsAsync();

        try
        {
            while (true)
            {
                severStatusImage.color = Color.green;
                TcpClient client = await tcpListener.AcceptTcpClientAsync();

                foreach (var item in userStatus)
                {
                    if (item.Value.isConneted)
                        continue;
                    else
                    {
                        item.Value.status.status.text = $"{item.Key}\n100%";
                        item.Value.isConneted = true;
                        item.Value.client = client;
                        item.Value.status.image.color = Color.green;
                        break;
                    }
                }

                var networkStream = client.GetStream();

                lock (clients)
                {
                    clients.Add((client, networkStream));
                }
            }
        }
        catch (SocketException ex)
        {
            severStatusImage.color = Color.red;
            Console.WriteLine("SocketException: " + ex.Message);
        }
        finally
        {
            severStatusImage.color = Color.red;
            tcpListener.Stop();
        }
    }

    private async Task MonitorClientsAsync()
    {
        while (true)
        {
            CheckClientConnections();
            await Task.Delay(TimeSpan.FromSeconds(5)); // 5초마다 실행
        }
    }

    private void CheckClientConnections()
    {
        lock (clients)
        {
            for (int i = clients.Count - 1; i >= 0; i--)
            {
                var (client, stream) = clients[i];
                if (!IsConnected(client))
                {
                    foreach (var item in userStatus)
                    {
                        if (item.Value.isConneted && clients[i].Item1 == item.Value.client)
                        {
                            item.Value.isConneted = false;
                            item.Value.status.image.color = Color.red;
                            break;
                        }
                    }
                    clients.RemoveAt(i);
                }
                else
                {
                    //DebugBox("client online");
                    _ = ReceiveMessagesAsync(client, stream);
                }
            }
        }
    }

    private async Task ReceiveMessagesAsync(TcpClient client, NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        string clientId = client.Client.RemoteEndPoint.ToString(); // 클라이언트의 식별자로 IP와 포트 사용

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                // 클라이언트가 연결을 끊었을 경우
                Console.WriteLine($"Client {clientId} disconnected.");
                break;
            }

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // 메시지 처리
            BatteryLevel(message, client);
            //DebugBox(message);
        }
    }

    private bool IsConnected(TcpClient client)
    {
        try
        {
            if (client.Client == null) return false;
            if (client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                return client.Client.Receive(buff, SocketFlags.Peek) != 0; // 연결이 끊어진 경우 0을 반환
            }
        }
        catch (SocketException)
        {
            return false;
        }
        return true;
    }

    private void BatteryLevel(string level, TcpClient client)
    {
        foreach (var item in userStatus)
        {
            if (item.Value.isConneted && client.Client == item.Value.client.Client)
            {
                item.Value.status.status.text = $"{item.Key}\n{level}";
                //item.Value.Label.TextAlign = ContentAlignment.MiddleCenter;
            }
        }
    }

    private void OnApplicationQuit()
    {
        tcpListener.Stop();
    }

    private void SendMessageToClient(string message)
    {
        Debug.Log(message);
        foreach (KeyValuePair<int, ClientData> item in userStatus)
        {
            if (item.Value.client == null) continue;
            if (item.Value.isConneted)
            {
                SendMessageToAClient(item.Value.client);
            }
        }

        async void SendMessageToAClient(TcpClient client)
        {
            var clientWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };
            await clientWriter.WriteLineAsync(message);
        }
    }

    public static int videoNum = 1;

    public void OnFuncButton(string func)
    {
        switch (func)
        {
            case "start":
                SendMessageToClient($"start load{videoNum}");
                break;

            default:
                SendMessageToClient(func + ",");
                break;
        }
    }

    public void OnCurrentVideo(int index)
    {
        currentVideoIndex = index;
    }

    private string GetLocalIPAddress()
    {
        string hostName = Dns.GetHostName();
        IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);

        foreach (IPAddress ipAddress in hostAddresses)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return ipAddress.ToString();
            }
        }

        return string.Empty;
    }
}