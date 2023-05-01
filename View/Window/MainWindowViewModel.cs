using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AS_Chat.Core;
using AS_Chat.Core.Model;
using System.Windows;

namespace AS_Chat.View.Window;


public partial class MainWindowViewModel : ObservableObject
{
    private readonly SessionContext m_session;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateNewChatCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConnectToChatCommand))]
    private string m_username = "Admin";

    [RelayCommand(CanExecute = nameof(IsUsernameFilled))]
    public void CreateNewChat()
    {
        m_session.Username = Username;
        if (IsServerIpEndPointFilled())
            m_session.ServerIpEndPoint = IPEndPoint.Parse(ServerIpEndPoint);
        else
            m_session.ServerIpEndPoint = new IPEndPoint(IPAddress.Loopback, 8888);

        try { new ChatWindow(new(new TcpServer(m_session))).Show(); } catch (SocketException) { MessageBox.Show("Попробуйте другой IP/Порт"); }
    }
    private bool IsUsernameFilled() => !string.IsNullOrEmpty(Username);


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectToChatCommand))]
    private string m_serverIpEndPoint = "127.0.0.1:8888";

    [RelayCommand(CanExecute = nameof(CanConnectToChat))]
    public void ConnectToChat()
    {
        m_session.Username = Username;
        m_session.ServerIpEndPoint = IPEndPoint.Parse(ServerIpEndPoint);

        try { new ChatWindow(new(new Core.Model.TcpClient(m_session))).Show(); } catch (SocketException) { MessageBox.Show("Попробуйте другой IP/Порт"); }
    }
    private bool CanConnectToChat() => IsServerIpEndPointFilled() && IsUsernameFilled();
    private bool IsServerIpEndPointFilled()
        => !string.IsNullOrEmpty(ServerIpEndPoint) && ServerIpEndPoint.Contains(':') && IPEndPoint.TryParse(ServerIpEndPoint, out _);


    public MainWindowViewModel(SessionContext session) => m_session = session;
}