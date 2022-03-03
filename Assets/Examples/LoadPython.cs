
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;  //需要添加这个名词空间，调用DataReceivedEventArg 
using System.Text;
using UnityEngine.UI;

using VTS.Examples;

public class LoadPython : MonoBehaviour
{

    [SerializeField]
    private Text info = null;

    private int room_id = 14917277; // 夸！

    private string sArguments;
    private string AppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private string AssetsPath = "";
    public static string temp;
    Process p;
    
    public ExamplePlugin mainScript;

    // Use this for initialization
    void Start()
    {
        temp = "";
        AssetsPath = Application.streamingAssetsPath;
        // mainScript = GameObject.Find("ExamplePlugin");
    }

    public void StartPy()
    {
        info.text = $"连接直播间{room_id}";
        ThreadStart childRef = new ThreadStart(ThreadTest1);
        Thread childThread = new Thread(childRef);
        childThread.Start();
    }

    public void EndPy()
    {
        p.Close();
        info.text = $"停止连接房间{room_id}";
    }

    // Update is called once per frame
    void Update()
    {
        // UnityEngine.Debug.Log(temp);
    }
    public void ThreadTest1()
    {
        // sArguments = @"/blivedm.py " + room_id.ToString();
        sArguments = room_id.ToString();
        print("ThreadTest1 is working");
        RunPythonScript(sArguments, "-u");
    }
    public void RunPythonScript(string sArgName, string args = "")
    {
        p = new Process();
        // string path = AssetsPath + sArgName;
        // string sArguments = path;

        // p.StartInfo.FileName = AppData + @"\Programs\Python\Python39\python.exe";
        p.StartInfo.FileName = AssetsPath + @"\blivedm\blivedm.exe";
        // UnityEngine.Debug.Log(path);
        // UnityEngine.Debug.Log(p.StartInfo.FileName);
        p.StartInfo.UseShellExecute = false;
        // p.StartInfo.Arguments = sArguments;
        p.StartInfo.Arguments = sArgName;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.BeginOutputReadLine();
        p.OutputDataReceived += new DataReceivedEventHandler(Out_RecvData);
        Console.ReadLine();
        p.WaitForExit();

    }
    void Out_RecvData(object sender, DataReceivedEventArgs e)
    {
        if (temp != e.Data)
        {
            temp = e.Data;
            if (temp != "" && temp != null)
            {
                UnityEngine.Debug.Log(temp);
                mainScript.receiveDanmu(temp);
            }
            
        }
    }
    public void ChangeRoomID(string msg)
    {
        int.TryParse(msg, out room_id);
        info.text = $"当前房间号：{room_id}";
    }
    void OnApplicationQuit()
    {
        p.Close();
    }

}