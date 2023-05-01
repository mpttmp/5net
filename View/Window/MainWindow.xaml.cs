using System.Windows.Controls;
using System.Windows.Input;

namespace AS_Chat.View.Window;


public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textbox)
            return;

        if (e.Key == Key.Return)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textbox), null);
            Keyboard.ClearFocus();
        }
    }
}