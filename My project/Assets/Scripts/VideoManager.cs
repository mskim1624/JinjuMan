#if UNITY_STANDALONE_WIN
//using System.Windows.Forms;
#endif
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Application = UnityEngine.Application;


public class VideoManager : MonoBehaviour
{
    static VideoManager instance;
    public static VideoManager Instance
    {
    set { instance = value; }
    get { return instance; } 
    }
    string videoFolderPath;
    List<VideoSelectButton> videoSelectButtons = new List<VideoSelectButton>();
    
    void Start()
    {
        videoFolderPath  = Path.Combine(Application.persistentDataPath, "Thumbnail");

        if (!Directory.Exists(videoFolderPath))
            Directory.CreateDirectory(videoFolderPath); 

    }

    

    void OpneFileDialog()
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
    void LoadImage(string filePath)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(fileData);
        //displayImage.texture = tex;
    }

}
