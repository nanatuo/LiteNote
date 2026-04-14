using System.Windows;

namespace SimpleNotesApp;

public class DialogService : IDialogService
{
    private Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public string? ShowInputDialog(string title, string prompt)
    {
        return DialogHelper.ShowInputDialog(title, prompt, _owner);
    }

    public void ShowErrorDialog(string message)
    {
        DialogHelper.ShowErrorDialog(message, _owner);
    }
}
