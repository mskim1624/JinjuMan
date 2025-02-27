#if UNITY_STANDALONE_WIN
//using System.Windows.Forms;
#endif

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Application = UnityEngine.Application;

public class VideoManager : MonoBehaviour
{
    [SerializeField]
    private TCPClient client;

    private static VideoManager instance;

    public static VideoManager Instance
    {
        set { instance = value; }
        get { return instance; }
    }

    private string videoFolderPath;

    [SerializeField]
    private List<VideoSelectButton> videoSelectButtons = new List<VideoSelectButton>();

    private void Start()
    {
        videoFolderPath = Path.Combine(Application.persistentDataPath, "Thumbnail");

        if (!Directory.Exists(videoFolderPath))
            Directory.CreateDirectory(videoFolderPath);

        OnSelecteButton(1);
    }

    private void OpneFileDialog()
    {
        //using (OpenFileDialog openFileDialog = new OpenFileDialog())
        //{
        //    openFileDialog.InitialDirectory = "c:\\";
        //    openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
        //    openFileDialog.RestoreDirectory = true;

        //    if (openFileDialog.ShowDialog() == DialogResult.OK)
        //    {
        //        string filePath = openFileDialog.FileName;
        //        LoadImage(filePath);
        //    }
        //}
    }

    private void LoadImage(string filePath)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);
        //displayImage.texture = tex;
    }

    public void OnSelecteButton(int index)
    {
        if (TCPSever.videoNum == index) return;
        TCPSever.videoNum = index;
        client.OnSendMessageToServer($"load:{TCPSever.videoNum},");

        foreach (VideoSelectButton button in videoSelectButtons)
        {
            if (button.index != index)
            {
                button.isSelect = false;
                button.selectImg.color = Color.white;
            }
            else
            {
                button.isSelect = true;
                button.selectImg.color = Color.green;
            }
        }
    }
}