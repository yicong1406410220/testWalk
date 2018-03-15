using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class Walk : MonoBehaviour {

    Socket socket;
    const int BUFFER_SIZE = 1024;
    public byte[] readBuff = new byte[BUFFER_SIZE];
    //玩家列表
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    //消息列表
    List<string> msgList = new List<string>();
    //Player预设
    public GameObject prefab;
    //自己的ip和端口
    string id;

    //添加玩家
    void AddPlayer(string id, Vector3 pos)
    {
        GameObject player = (GameObject)Instantiate(prefab, pos, Quaternion.identity);
        TextMesh textMesh = player.GetComponentInChildren<TextMesh>();
        textMesh.text = id;
        players.Add(id, player);
    }

    //发送位置协议
    void SendPos()
    {
        GameObject player = players[id];
        Vector3 pos = player.transform.position;
        //组装协议
        string str = "POS ";
        str += id + " ";
        str += pos.x.ToString() + " ";
        str += pos.y.ToString() + " ";
        str += pos.z.ToString() + " ";

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
        socket.Send(bytes);
        Debug.Log("发送" + str);
    }

    //发送离开协议
    void SendLeave()
    {
        //组装协议
        string str = "LEAVE";
        str += id + " ";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
        socket.Send(bytes);
        Debug.Log("发送" + str);

    }

    void Move()
    {
        if (id == "")
            return;

        GameObject player = players[id];
        //上
        if (Input.GetKey(KeyCode.UpArrow))
        {
            player.transform.position += new Vector3(0, 0, 1);
            SendPos();
        }
        //下
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            player.transform.position += new Vector3(0, 0, -1);
            SendPos();
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            player.transform.position += new Vector3(-1, 0, 0);
            SendPos();
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            player.transform.position += new Vector3(1, 0, 0);
            SendPos();
        }


    }

    //离开
    private void OnDestroy()
    {
        SendLeave();
    }



    private void Start()
    {
        Connect();

        //请求其他玩家列表
        //把自己放进一个随机位置
        UnityEngine.Random.seed = (int)DateTime.Now.Ticks;
        float x = 100 + UnityEngine.Random.Range(-30, 30);
        float y = 1;
        float z = 100 + UnityEngine.Random.Range(-20, 20);
        Vector3 pos = new Vector3(x, y, z);
        AddPlayer(id, pos);
        //同步
        SendPos();
    }

    //连接
    private void Connect()
    {
        //Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //connect
        socket.Connect("127.0.0.1", 8041);
        id = socket.LocalEndPoint.ToString();
        //Recv
        socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCB, null);


    }

    //接收回调
    private void ReceiveCB(IAsyncResult ar)
    {
        try {
            int count = socket.EndReceive(ar);
            //数据处理
            string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, BUFFER_SIZE);
            msgList.Add(str);
            //继续接收
            socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCB, null);

        }
        catch(Exception e)
        {
            socket.Close();
        }
    }

    // 如果 MonoBehaviour 已启用，则在每一帧都调用 Update
    private void Update()
    {
        //处理消息列表
        for (int i = 0; i < msgList.Count; i++)
        {
            HandMsg();
        }
        //移动
        Move();

    }
    //处理消息列表
    private void HandMsg()
    {
        //获取一条消息
        if (msgList.Count <= 0)
            return;
        string str = msgList[0];
        msgList.RemoveAt(0);
        //协议
        string[] args = str.Split(' ');
        if(args[0] == "POS")
        {
            OnRecvPos(args[1], args[2], args[3], args[4]);
        }
        else if (args[0] == "LEAVE")
        {
            OnRecvLeave(args[1]);
        }


    }

    private void OnRecvPos(string id, string xStr, string yStr, string zStr)
    {
        //不更新自己的位置
        if (id == this.id)
            return;
        //解析协议
        float x = float.Parse(xStr);
        float y = float.Parse(yStr);
        float z = float.Parse(zStr);
        Vector3 pos = new Vector3(x, y, z);
        if (players.ContainsKey(id))
        {
            players[id].transform.position = pos;
        }
        //没有初始化的玩家
        else
        {
            AddPlayer(id, pos);
        }


    }

    private void OnRecvLeave(string v)
    {
        if (players.ContainsKey(id))
        {
            Destroy(players[id]);
            players[id] = null;
        }

    }

    

}
