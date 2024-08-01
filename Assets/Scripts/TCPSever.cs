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

    TcpListener tcpListener;
    public int currentVideoIndex;

    List<(TcpClient, NetworkStream)> clients = new List<(TcpClient, NetworkStream)>();
    Dictionary<int, ClientData> userStatus = new Dictionary<int, ClientData>();

    [SerializeField]
    List<UserStatus> users = new List<UserStatus>();
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
            await Task.Delay(TimeSpan.FromSeconds(5)); // 5�ʸ��� ����
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
        string clientId = client.Client.RemoteEndPoint.ToString(); // Ŭ���̾�Ʈ�� �ĺ��ڷ� IP�� ��Ʈ ���

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                // Ŭ���̾�Ʈ�� ������ ������ ���
                Console.WriteLine($"Client {clientId} disconnected.");
                break;
            }

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // �޽��� ó��
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
                return client.Client.Receive(buff, SocketFlags.Peek) != 0; // ������ ������ ��� 0�� ��ȯ
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
    void OnApplicationQuit()
    {
        tcpListener.Stop();
    }
    async void SendMessageToClient(string message)
    {
        foreach (KeyValuePair<int, ClientData> item in userStatus)
        {
            if (item.Value.client == null) continue;
            if (item.Value.isConneted)
            {
                StreamWriter clientWriter = new StreamWriter(item.Value.client.GetStream()) { AutoFlush = true };
                await clientWriter.WriteLineAsync(message);
            }
        }
    }
    public void OnFuncButton(string func)
    {
        switch (func)
        {
            case "start":
                SendMessageToClient("start: load1,");
                break;
            default:
                SendMessageToClient(func + ",");
                break;
        }
        Debug.Log(func);
    }
    public void OnCurrentVideo(int index)
    {
        currentVideoIndex = index;
    }
    string GetLocalIPAddress()
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
