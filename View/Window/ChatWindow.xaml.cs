using System;

namespace AS_Chat.View.Window;


public partial class ChatWindow : System.Windows.Window
{
    private readonly ChatWindowViewModel m_vm;

    public ChatWindow(ChatWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = m_vm = vm;

        vm.ExitEvent += (object? sender, EventArgs e) =>
        {
            try     // Грёбаный ад....
            {
                this.Close();
            }
            catch { }
        };
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => m_vm.ChatClient.Stop();
}