using AS_Chat.View.Window;

namespace AS_Chat.Core;


public interface IChatClient
{
    public void BindViewModel(ChatWindowViewModel chatVM);
    void Start();
    void Stop();
    void SendMessage(string message);
}