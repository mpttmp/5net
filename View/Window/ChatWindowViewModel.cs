using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AS_Chat.Core;
using System.Net.Sockets;
using System.Windows;

namespace AS_Chat.View.Window;


public partial class ChatWindowViewModel : ObservableObject
{
    public readonly IChatClient ChatClient;

    public event EventHandler? ExitEvent;

    public ObservableCollection<string> ConnectedIPs { get; set; } = new();
    public ObservableCollection<string> Messages { get; set; } = new();

    public string Message { get; set; } = string.Empty;

    [RelayCommand]
    private void Send()
    {
        if (string.IsNullOrEmpty(Message))
            return;
        ChatClient.SendMessage(Message);
    }

    [RelayCommand]
    public void Exit() => ExitEvent?.Invoke(this, EventArgs.Empty); // public - костыль.


    public ChatWindowViewModel(IChatClient chatClient)
    {
        ChatClient = chatClient;
        ChatClient.BindViewModel(this);
        try { ChatClient.Start(); } catch (SocketException) { throw; }
    }
}