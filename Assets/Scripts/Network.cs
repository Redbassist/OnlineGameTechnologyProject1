using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;
using System;
using System.Diagnostics;

public class Network : MonoBehaviour
{
    ArrayList updateMessage = new ArrayList(10000);
    ConnectionConfig config;
    int myReliableChannelId;
    int myUnreliableChannelId;
    HostTopology topology;
    int hostId;
    int socketId;
    int socketPort = 8888;
    int connectionId;
    byte error;
    bool sendPosition = false;
    int mainPlayer;
    int timer = 5;

    bool isHost = false;
    int numberPlayers = 0;

    Stopwatch messageStopWatch;
    float messageTime;

    bool deadReckoning = true;

    // Use this for initialization
    void Start()
    {
        messageStopWatch = new Stopwatch();

        GameObject temp;
        temp = GameObject.Find("YouWon1");
        temp.GetComponent<Image>().enabled = false;
        temp = GameObject.Find("YouWon2");
        temp.GetComponent<Image>().enabled = false;
        temp = GameObject.Find("YouLost1");
        temp.GetComponent<Image>().enabled = false;
        temp = GameObject.Find("YouLost2");
        temp.GetComponent<Image>().enabled = false;
        temp = GameObject.Find("IsOn1");
        temp.GetComponent<Image>().enabled = false;
        temp = GameObject.Find("IsOn2");
        temp.GetComponent<Image>().enabled = false;

        NetworkTransport.Init();
        config = new ConnectionConfig();
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        int maxConnections = 100;
        topology = new HostTopology(config, maxConnections);
        socketId = NetworkTransport.AddHost(topology, socketPort);
    }

    // Update is called once per frame
    void Update()
    {
        GameObject player;

        int recHostId;
        int recConnectionId;
        int recChannelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                UnityEngine.Debug.Log("Connection Occurred");

                if (isHost)
                {
                    numberPlayers++;

                    UnityEngine.Debug.Log("Number of players is: " + numberPlayers);

                    if (numberPlayers == 3)
                    {
                        System.Random random = new System.Random();
                        int randomNumber = random.Next(0, 2);

                        if (randomNumber == 0)
                        {
                            player = GameObject.Find("Player1");
                            player.GetComponent<Player>().IsOn(true);
                            GameObject tempy;
                            tempy = GameObject.Find("IsOn1");
                            tempy.GetComponent<Image>().enabled = true;
                        }
                        else
                        {
                            GameObject tempy;
                            tempy = GameObject.Find("IsOn2");
                            tempy.GetComponent<Image>().enabled = true;
                        }

                        updateMessage.Clear();
                        updateMessage.Add((byte)'S');
                        updateMessage.AddRange(System.BitConverter.GetBytes(randomNumber));

                        byte[] messageToSend = new byte[updateMessage.Count];
                        messageToSend = (byte[])updateMessage.ToArray(typeof(byte));

                        byte[] buffer = new byte[1024];
                        Stream streamy = new MemoryStream(buffer);
                        BinaryFormatter formattery = new BinaryFormatter();
                        formattery.Serialize(streamy, messageToSend);

                        NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, bufferSize, out error);

                        GameObject temp;
                        temp = GameObject.Find("background");
                        temp.transform.position = new Vector2(-1000000, 0);
                        temp = GameObject.Find("JoinButton");
                        temp.transform.position = new Vector2(-1000000, 0);
                        temp = GameObject.Find("HostButton");
                        temp.transform.position = new Vector2(-1000000, 0);
                        temp = GameObject.Find("ExitButton");
                        temp.transform.position = new Vector2(-1000000, 0);

                        sendPosition = true;
                    }
                }
                break;
            case NetworkEventType.DataEvent:
                Stream stream = new MemoryStream(recBuffer);
                BinaryFormatter formatter = new BinaryFormatter();
                byte[] message = formatter.Deserialize(stream) as byte[];

                char messageType = Convert.ToChar(message[0]);

                if (messageType == 'S')
                {
                    GameObject temp;
                    temp = GameObject.Find("background");
                    temp.transform.position = new Vector2(-1000000, 0);
                    temp = GameObject.Find("JoinButton");
                    temp.transform.position = new Vector2(-1000000, 0);
                    temp = GameObject.Find("HostButton");
                    temp.transform.position = new Vector2(-1000000, 0);
                    temp = GameObject.Find("ExitButton");
                    temp.transform.position = new Vector2(-1000000, 0);
                    sendPosition = true;

                    if (System.BitConverter.ToSingle(message, 1) != 0)
                    {
                        player = GameObject.Find("Player1");
                        player.GetComponent<Player>().IsOn(true);
                        GameObject tempy;
                        tempy = GameObject.Find("IsOn2");
                        tempy.GetComponent<Image>().enabled = true;
                    }
                    else
                    {
                        GameObject tempy;
                        tempy = GameObject.Find("IsOn1");
                        tempy.GetComponent<Image>().enabled = true;
                    }

                    messageStopWatch.Start();
                }

                //recieved update message from other player
                if (messageType == 'U')
                {
                    float posX = System.BitConverter.ToSingle(message, 1);
                    float posY = System.BitConverter.ToSingle(message, 5);
                    float velX = System.BitConverter.ToSingle(message, 9);
                    float velY = System.BitConverter.ToSingle(message, 13);
                    float rot = System.BitConverter.ToSingle(message, 17);
                    UnityEngine.Debug.Log("Player 2 is at (" + posX + ", " + posY + ") traveling (" + velX + ", " + velY + ")");

                    //interpolation methods
                    messageTime = (float)messageStopWatch.ElapsedMilliseconds;
                    var player2 = GameObject.Find("SecondPlayer");
                    Player temp = (Player)player2.GetComponent(typeof(Player));
                    if (deadReckoning)
                        temp.DeadReckoningUpdate(new Vector2(posX, posY), new Vector2(velX, velY));
                    else
                        temp.StartInterpolation(messageTime, new Vector2(posX, posY));

                    messageStopWatch.Reset();
                    messageStopWatch.Start();

                    //moving the player OLD
                    /*var player2 = GameObject.Find("SecondPlayer");
                    player2.transform.position = new Vector2(posX, posY);
                    player2.transform.rotation = Quaternion.Euler(0, 0, rot);
                    player2.GetComponent<Rigidbody2D>().MoveRotation(rot);*/

                }

                //recieved push message from other player
                if (messageType == 'P')
                {
                    float posX = System.BitConverter.ToSingle(message, 1);
                    float posY = System.BitConverter.ToSingle(message, 5);
                    Vector2 tempVec = new Vector2(posX, posY);

                    messageTime = (float)messageStopWatch.ElapsedMilliseconds;
                    var player1 = GameObject.Find("Player1");
                    Player temp = (Player)player1.GetComponent(typeof(Player));
                    temp.HandlePush(tempVec);

                    var player2 = GameObject.Find("SecondPlayer");
                    player2.GetComponent<ParticleSystem>().Play();

                }

                if (messageType == 'E')
                {
                    GameObject temp;
                    temp = GameObject.Find("Player1");

                    if (temp.GetComponent<Player>().IsPlayerOn())
                    {
                        temp = GameObject.Find("YouWon2");
                        temp.GetComponent<Image>().enabled = true;
                    }
                    else
                    {
                        temp = GameObject.Find("YouLost2");
                        temp.GetComponent<Image>().enabled = true;
                    }

                    GameObject tempy;
                    tempy = GameObject.Find("IsOn1");
                    tempy.GetComponent<Image>().enabled = false;
                    tempy = GameObject.Find("IsOn2");
                    tempy.GetComponent<Image>().enabled = false;
                }

                break;
            case NetworkEventType.DisconnectEvent:
                UnityEngine.Debug.Log("remote client event disconnected");
                break;
        }

        if (!deadReckoning)
        {
            timer--;

            if (sendPosition)
            {
                if (timer <= 0)
                {
                    SendPlayerUpdate();
                    timer = 3;
                }
            }
        }
    }

    public void createHost()
    {
        UnityEngine.Debug.Log("Socket Open. Socket ID = " + socketId);
        connectionId = NetworkTransport.Connect(socketId, "149.153.102.61", socketPort, 0, out error);

        GameObject myButton;

        myButton = GameObject.Find("HostButton");
        myButton.GetComponent<Button>().interactable = false;
        myButton = GameObject.Find("JoinButton");
        myButton.GetComponent<Button>().interactable = false;

        mainPlayer = 1;

        numberPlayers++;

        GameObject player;
        player = GameObject.Find("Player1");
        player.GetComponent<Player>().SetPlayer1(true);

        player = GameObject.Find("Player2");
        player.GetComponent<Player>().SetPlayer2(true);
        player.name = "SecondPlayer";

        isHost = true;
    }

    public void connectToHost()
    {
        connectionId = NetworkTransport.Connect(socketId, "149.153.102.56", socketPort, 0, out error);
        UnityEngine.Debug.Log("Connected to server. ConnectionId: " + connectionId);

        GameObject myButton;

        myButton = GameObject.Find("HostButton");
        myButton.GetComponent<Button>().interactable = false;
        myButton = GameObject.Find("JoinButton");
        myButton.GetComponent<Button>().interactable = false;

        mainPlayer = 2;

        GameObject player;

        player = GameObject.Find("Player1");
        player.GetComponent<Player>().SetPlayer2(true);
        player.name = "SecondPlayer";

        player = GameObject.Find("Player2");
        player.GetComponent<Player>().SetPlayer1(true);
        player.name = "Player1";

        isHost = false;

        //myButton = GameObject.Find("MessageButton");
        //myButton.GetComponent<Button>().interactable = true;
    }

    public void SendPlayerUpdate()
    {
        GameObject player;
        player = GameObject.Find("Player1");
        float posX = player.transform.position.x;
        float posY = player.transform.position.y;
        Vector2 velocity = player.GetComponent<Rigidbody2D>().velocity;

        updateMessage.Clear();
        updateMessage.Add((byte)'U');
        updateMessage.AddRange(System.BitConverter.GetBytes(posX));
        updateMessage.AddRange(System.BitConverter.GetBytes(posY));
        updateMessage.AddRange(System.BitConverter.GetBytes(velocity.x));
        updateMessage.AddRange(System.BitConverter.GetBytes(velocity.y));
        updateMessage.AddRange(System.BitConverter.GetBytes(velocity.y));
        Player other = (Player)player.GetComponent(typeof(Player));
        updateMessage.AddRange(System.BitConverter.GetBytes(other.GetAngle()));

        byte[] messageToSend = new byte[updateMessage.Count];
        messageToSend = (byte[])updateMessage.ToArray(typeof(byte));

        byte error;
        byte[] buffer = new byte[1024];
        Stream stream = new MemoryStream(buffer);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, messageToSend);

        int bufferSize = 1024;

        NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, bufferSize, out error);
    }

    private float RadianToDegree(float angle1)
    {
        return (float)(angle1 * (180.0 / Math.PI));
    }

    public void SendEndMessage()
    {
        if (isHost)
        {
            updateMessage.Clear();
            updateMessage.Add((byte)'E');

            byte[] messageToSend = new byte[updateMessage.Count];
            messageToSend = (byte[])updateMessage.ToArray(typeof(byte));

            byte[] buffer = new byte[1024];
            int bufferSize = 1024;
            Stream streamy = new MemoryStream(buffer);
            BinaryFormatter formattery = new BinaryFormatter();
            formattery.Serialize(streamy, messageToSend);

            NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, bufferSize, out error);

            GameObject temp;
            temp = GameObject.Find("Player1");

            if (temp.GetComponent<Player>().IsPlayerOn())
            {
                temp = GameObject.Find("YouWon1");
                temp.GetComponent<Image>().enabled = true;
            }
            else
            {
                temp = GameObject.Find("YouLost1");
                temp.GetComponent<Image>().enabled = true;
            }

            GameObject tempy;
            tempy = GameObject.Find("IsOn1");
            tempy.GetComponent<Image>().enabled = false;
            tempy = GameObject.Find("IsOn2");
            tempy.GetComponent<Image>().enabled = false;
        }
    }

    public void SendPush(Vector2 playerPos)
    {
        updateMessage.Clear();
        updateMessage.Add((byte)'P');
        updateMessage.AddRange(System.BitConverter.GetBytes(playerPos.x));
        updateMessage.AddRange(System.BitConverter.GetBytes(playerPos.y));

        byte[] messageToSend = new byte[updateMessage.Count];
        messageToSend = (byte[])updateMessage.ToArray(typeof(byte));

        byte[] buffer = new byte[1024];
        int bufferSize = 1024;
        Stream streamy = new MemoryStream(buffer);
        BinaryFormatter formattery = new BinaryFormatter();
        formattery.Serialize(streamy, messageToSend);

        NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, bufferSize, out error);
    }
}
