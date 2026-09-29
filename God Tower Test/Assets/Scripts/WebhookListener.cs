using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class WebhookListener : MonoBehaviour
{
    private const string OkResponse = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK";
    private const string NotFoundResponse = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\nContent-Length: 9\r\nConnection: close\r\n\r\nNot Found";

    private static WebhookListener activeInstance;

    [SerializeField] private int port = 56789;
    [SerializeField] private BumpEventController bumpEvent;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private TcpListener listener;
    private Thread listenThread;
    private volatile bool running;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(WebhookListener)} on '{name}' disabled.", this);
            enabled = false;
            return;
        }

        activeInstance = this;

        if (bumpEvent == null)
            Debug.LogWarning($"{nameof(WebhookListener)} has no {nameof(BumpEventController)} assigned.", this);
    }

    private void Start()
    {
        if (activeInstance == this)
            StartListening();
    }

    private void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
            action();
    }

    private void OnApplicationQuit() => StopListening();

    private void OnDestroy()
    {
        StopListening();
        if (activeInstance == this)
            activeInstance = null;
    }

    private void StartListening()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            running = true;
            listenThread = new Thread(ListenLoop) { IsBackground = true, Name = "BumpWebhook" };
            listenThread.Start();
            Debug.Log($"Bump webhook listening on port {port} (GET/POST /bump).");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Bump webhook failed to start on port {port}: {exception.Message}", this);
            StopListening();
        }
    }

    private void StopListening()
    {
        running = false;

        try
        {
            listener?.Stop();
        }
        catch (Exception)
        {
        }

        listener = null;

        if (listenThread != null && listenThread.IsAlive)
            listenThread.Join(500);

        listenThread = null;
    }

    private void ListenLoop()
    {
        while (running)
        {
            TcpClient client;
            try
            {
                client = listener.AcceptTcpClient();
            }
            catch (Exception)
            {
                if (!running)
                    return;

                Thread.Sleep(50);
                continue;
            }

            using (client)
            {
                try
                {
                    HandleClient(client);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        client.ReceiveTimeout = 2000;
        client.SendTimeout = 2000;

        NetworkStream stream = client.GetStream();
        var buffer = new byte[2048];
        int read = stream.Read(buffer, 0, buffer.Length);
        string request = Encoding.ASCII.GetString(buffer, 0, Mathf.Max(read, 0));

        bool isBump = IsBumpRequest(request);
        if (isBump)
            mainThreadActions.Enqueue(TriggerBump);

        byte[] response = Encoding.ASCII.GetBytes(isBump ? OkResponse : NotFoundResponse);
        stream.Write(response, 0, response.Length);
        stream.Flush();
    }

    private static bool IsBumpRequest(string request)
    {
        int lineEnd = request.IndexOf('\r');
        string requestLine = lineEnd >= 0 ? request.Substring(0, lineEnd) : request;
        string[] parts = requestLine.Split(' ');
        if (parts.Length < 2)
            return false;

        string method = parts[0].ToUpperInvariant();
        string path = parts[1];
        int queryStart = path.IndexOf('?');
        if (queryStart >= 0)
            path = path.Substring(0, queryStart);

        return (method == "GET" || method == "POST") && path.TrimEnd('/') == "/bump";
    }

    private void TriggerBump()
    {
        if (bumpEvent != null)
            bumpEvent.Trigger();
    }
}
