using AS_Chat.View.Window;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Threading;

namespace AS_Chat.Core.Model;


/// <summary>
/// //Компроментирущий материал удалён//
/// </summary>
public class TcpClient : IChatClient
{
    private Socket m_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private CancellationTokenSource m_CTS = new();

    private HashSet<string> names = new();  // ???

    private readonly SessionContext m_localSession;
    private ChatWindowViewModel? m_chatVM;  // Связь с ViewModel'ю.

    public void Start()
    {
        try
        {
            m_server.ConnectAsync(m_localSession.ServerIpEndPoint!, m_CTS.Token);
        }
        catch (SocketException)
        {
            // ...
            Stop();
            return;
        }
        _ = RecieveMessage();
    }

    public void Stop()
    {
        m_CTS.Cancel();
        m_server.Dispose();
        m_chatVM?.Exit();
    }

    private async Task RecieveMessage()
    {
        while (!m_CTS.Token.IsCancellationRequested)
        {
            string message = string.Empty;
            string messagePart = string.Empty;
            while (!messagePart.Contains('\0')) // Нужно не забыть добавлять нуль-символ.
            {
                var bytes = new byte[256];
                try { await m_server.ReceiveAsync(bytes, m_CTS.Token); }
                catch (OperationCanceledException)
                {
                    m_server.Disconnect(false);
                    // ...
                    Stop();
                    return;
                }
                catch (SocketException)
                {
                    // ...
                    Stop();
                    return;
                }
                if (m_CTS.Token.IsCancellationRequested)
                {
                    m_server.Disconnect(false);
                    // ...
                    Stop();
                    return;
                }
                messagePart = Encoding.UTF8.GetString(bytes);
                if (messagePart.Contains('\0'))
                    message += messagePart[..messagePart.IndexOf('\0')];
                else
                    message += messagePart;
            };

            if (message.Length == 0)    // Костыль.
            {
                Stop();
                return;
            }
            m_chatVM?.Messages.Add(message);
        }
        m_server.Disconnect(false);
        Stop();
    }

    public async void SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + '\0');
        try
        {
            await m_server.SendAsync(bytes, m_CTS.Token);
        }
        catch (OperationCanceledException) { }
        catch (SocketException)
        {
            // ...
            Stop();
        }
    }


    public void BindViewModel(ChatWindowViewModel chatVM) => m_chatVM = chatVM;

    public TcpClient(SessionContext session)
    {
        m_localSession = new()    // Создание частной сессии, исходя из переданного синглтона.
        {
            Username = session.Username,    // Делаю "глубокую" копию.
            ServerIpEndPoint = session.ServerIpEndPoint
        };
    }
}