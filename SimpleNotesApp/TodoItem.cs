namespace SimpleNotesApp;

public class TodoItem : ViewModelBase
{
    private string _content = string.Empty;
    private bool _isCompleted;
    private bool _isEditing;

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }
}
