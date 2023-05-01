using System;

using System.Text;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Threading.Tasks;

using AS_Chat.View.Window;

namespace AS_Chat.Core.Model;


public class TcpServer : IChatClient
{
    private class ConnectionHandler
    {
        private class ExitEventArgs : EventArgs
        {
            public ExitEventArgs(Action exitEventInvoke) => ExitEventEvent = exitEventInvoke;
            public Action ExitEventEvent { get; init; }
        }
        private delegate void ExitEventHandler(ExitEventArgs e);
        private event ExitEventHandler Exit;

        private readonly TcpServer m_server;
        private readonly Socket m_sock;
        private readonly CancellationTokenSource m_CTS = new();
        private readonly CancellationToken m_cancellationToken;

        private async Task Receive()
        {
            while (!m_cancellationToken.IsCancellationRequested)
            {
                string message = string.Empty;
                string messagePart = string.Empty;
                while (!messagePart.Contains('\0')) // Нужно не забыть добавлять нуль-символ.
                {
                    var bytes = new byte[256];
                    try { await m_sock.ReceiveAsync(bytes, m_cancellationToken); }
                    catch (OperationCanceledException)
                    {
                        m_sock.Disconnect(false);
                        try { Exit.Invoke(new(() => m_server.ClientDisconnectedByServer.Invoke(this))); } catch (NullReferenceException) { }
                        return;
                    }
                    catch (SocketException)
                    {
                        m_CTS.Cancel();
                        try { Exit.Invoke(new(() => m_server.ClientDied.Invoke(this))); } catch (NullReferenceException) { }
                        return;
                    }
                    if (m_cancellationToken.IsCancellationRequested)
                    {
                        m_sock.Disconnect(false);
                        try { Exit.Invoke(new(() => m_server.ClientDisconnectedByServer.Invoke(this))); } catch (NullReferenceException) { }
                        return;
                    }
                    messagePart = Encoding.UTF8.GetString(bytes);
                    if (messagePart.Contains('\0'))
                        message += messagePart[..messagePart.IndexOf('\0')];
                    else
                        message += messagePart;
                };

                m_server.BroadcastMessage(message);
            }
            m_sock.Disconnect(false);
            try { Exit.Invoke(new(() => m_server.ClientDisconnectedByServer.Invoke(this))); } catch (NullReferenceException) { }
        }

        public async Task Send(string message)
        {
            if (m_cancellationToken.IsCancellationRequested)
            {
                try { m_server.SendFailed?.Invoke(this); } catch (NullReferenceException) { }
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            try { await m_sock.SendAsync(bytes, m_cancellationToken); }
            catch (OperationCanceledException)
            {
                try { m_server.SendFailed?.Invoke(this); } catch (NullReferenceException) { }
            }
            catch (SocketException)
            {
                m_CTS.Cancel();
                try
                {
                    m_server.SendFailed?.Invoke(this);
                    Exit.Invoke(new(() => m_server.ClientDied.Invoke(this)));
                }
                catch (NullReferenceException) { }
            }
        }

        public EndPoint? ClientIP { get { return m_sock.RemoteEndPoint; } }

        public void Disconnect() => m_CTS.Cancel(); // Exit должен вызваться сам.


        public ConnectionHandler(Socket clientSock, TcpServer server)
        {
            m_server = server;
            m_sock = clientSock;
            m_cancellationToken = m_CTS.Token;
            _ = Receive();
            Exit += ExitHandler;
        }

        private void ExitHandler(ExitEventArgs e)
        {
            e.ExitEventEvent.Invoke();

            //m_sock.Dispose(); // Пути сборщика мусора неисповедимы, т.ч. очищаем вручную...
            //m_CTS.Dispose();  // EDIT: пох, я уверовал.

            Exit -= ExitHandler;    // Защита от повторного выхода (из-за множества Exit'ов есть состояние гонки).
        }
    }

    private delegate void ConnectionHandleEventHandler(ConnectionHandler handle);
    private event ConnectionHandleEventHandler SendFailed;  // Похуй.
    private event ConnectionHandleEventHandler ClientDisconnectedByServer;
    private event ConnectionHandleEventHandler ClientDied;

    private delegate void TcpServerEventHandler(TcpServer server);
    private event TcpServerEventHandler ServerFailedToAcceptClient; // Похуй.

    private readonly Socket m_serverSock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly CancellationTokenSource m_serverCTS = new();

    private readonly List<ConnectionHandler> m_clients = new();

    private readonly SessionContext m_localSession;
    private ChatWindowViewModel? m_chatVM;  // Связь с ViewModel'ю.

    public void Start()
    {
        m_serverSock.Bind(m_localSession.ServerIpEndPoint!);
        m_serverSock.Listen(2);

        _ = HandleConnections(m_serverCTS.Token);
    }

    public void Stop()
    {
        m_serverCTS.Cancel();   // Потенциальное состояние гонки.
        m_serverSock.Close();
    }

    private async Task HandleConnections(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Socket clientSock;
            try { clientSock = await m_serverSock.AcceptAsync(cancellationToken); }
            catch (OperationCanceledException) { break; }
            catch (SocketException)
            {
                ServerFailedToAcceptClient.Invoke(this);
                continue;
            }

            ConnectionHandler handle = new(clientSock, this);
            m_clients.Add(handle);
            m_chatVM!.Messages.Add($"{handle.ClientIP} подключился ({DateTime.Now})");
            m_chatVM.ConnectedIPs.Add(handle.ClientIP!.ToString()!);
        }
        foreach (var client in m_clients)   // Можно (и нужно) было бы просто прокидывать токен от m_serverCTS, но я уже устал...
            client.Disconnect();    // Возможно, здесь нужен lock().
    }

    private void BroadcastMessage(string message)
    {
        message = $"({DateTime.Now}) " + message;
        m_chatVM!.Messages.Add(message);
        foreach (var client in m_clients) _ = client.Send(message); // Возможно, здесь нужен lock().
    }

    public void SendMessage(string m) => BroadcastMessage(m);


    public void BindViewModel(ChatWindowViewModel chatVM) => m_chatVM = chatVM;

    public TcpServer(SessionContext session)
    {
        m_localSession = new()    // Создание частной сессии, исходя из переданного синглтона.
        {
            Username = session.Username,    // Делаю "глубокую" копию.
            ServerIpEndPoint = session.ServerIpEndPoint
        };

        ClientDisconnectedByServer += RemoveDeadHandle;
        ClientDied += RemoveDeadHandle; // Яхз как обрабатывать смерть клиента...
    }

    private void RemoveDeadHandle(ConnectionHandler handle)
    {
        m_chatVM!.ConnectedIPs.Remove(handle.ClientIP?.ToString());  // При закрытии окна уже всё равно.
        try { m_chatVM.Messages.Add($"{handle.ClientIP} отключился ({DateTime.Now})"); } catch (NullReferenceException) { }
        m_clients.Remove(handle);  // Возможно, здесь нужен lock().
    }
}