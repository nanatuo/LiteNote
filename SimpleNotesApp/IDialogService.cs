namespace SimpleNotesApp;

public interface IDialogService
{
    string? ShowInputDialog(string title, string prompt);
    void ShowErrorDialog(string message);
}
