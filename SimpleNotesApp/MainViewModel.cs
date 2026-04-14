using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleNotesApp;

public class MainViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly IDataService _dataService;
    private bool _isTopmost;
    private bool _isTransparent;
    private TodoItem? _selectedItem;
    private byte _backgroundAlpha = 255;
    private string _textColorHex = "#999999";
    private string _completedTextColorHex = "#2D2D2D";
    private string _themeColorHex = "#4CAF50";

    public ObservableCollection<TodoItem> TodoItems { get; } = new();

    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    public bool IsTransparent
    {
        get => _isTransparent;
        set
        {
            if (SetProperty(ref _isTransparent, value))
            {
                BackgroundAlpha = value ? (byte)0 : (byte)255;
                ReapplyTextColorResources();
            }
        }
    }

    public byte BackgroundAlpha
    {
        get => _backgroundAlpha;
        set => SetProperty(ref _backgroundAlpha, value);
    }

    public TodoItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public string TextColorHex
    {
        get => _textColorHex;
        set
        {
            if (SetProperty(ref _textColorHex, value))
            {
                var baseColor = ColorHelper.FromHex(value);
                var contrastColor = DeriveContrastColor(baseColor);
                CompletedTextColorHex = ColorHelper.ToHex(contrastColor);
                ApplyTextColorResources(baseColor, contrastColor);
                SaveConfig();
            }
        }
    }

    public string CompletedTextColorHex
    {
        get => _completedTextColorHex;
        private set => SetProperty(ref _completedTextColorHex, value);
    }

    public string ThemeColorHex
    {
        get => _themeColorHex;
        set
        {
            if (SetProperty(ref _themeColorHex, value))
            {
                ApplyThemeColorResources(value);
                SaveConfig();
            }
        }
    }

    public ICommand AddCommand { get; }
    public ICommand ToggleTopmostCommand { get; }
    public ICommand ToggleTransparentCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand DeleteCompletedCommand { get; }
    public ICommand CheckBoxChangedCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SetTextColorCommand { get; }
    public ICommand SetThemeColorCommand { get; }
    public ICommand StartEditCommand { get; }
    public ICommand EndEditCommand { get; }

    public event Action? CloseRequested;
    public event Action? TextColorChanged;
    public event Action? ThemeColorChanged;

    public MainViewModel(IDialogService dialogService, IDataService dataService)
    {
        _dialogService = dialogService;
        _dataService = dataService;

        AddCommand = new RelayCommand(ExecuteAdd);
        ToggleTopmostCommand = new RelayCommand(ExecuteToggleTopmost);
        ToggleTransparentCommand = new RelayCommand(ExecuteToggleTransparent);
        CreateNewCommand = new RelayCommand(ExecuteCreateNew);
        DeleteCommand = new RelayCommand(ExecuteDelete, () => SelectedItem != null);
        DeleteCompletedCommand = new RelayCommand(ExecuteDeleteCompleted);
        CheckBoxChangedCommand = new RelayCommand(ExecuteCheckBoxChanged);
        CloseCommand = new RelayCommand(ExecuteClose);
        SetTextColorCommand = new RelayCommand<string>(ExecuteSetTextColor);
        SetThemeColorCommand = new RelayCommand<string>(ExecuteSetThemeColor);
        StartEditCommand = new RelayCommand<TodoItem?>(ExecuteStartEdit);
        EndEditCommand = new RelayCommand<TodoItem?>(ExecuteEndEdit);

        LoadFromDataService();
    }

    private void LoadFromDataService()
    {
        var config = _dataService.LoadConfig();
        IsTopmost = config.Topmost;
        IsTransparent = config.Transparent;

        string textColor = !string.IsNullOrEmpty(config.TextColor) ? config.TextColor : _textColorHex;
        _textColorHex = textColor;
        var baseColor = ColorHelper.FromHex(textColor);
        var contrastColor = DeriveContrastColor(baseColor);
        _completedTextColorHex = ColorHelper.ToHex(contrastColor);
        ApplyTextColorResources(baseColor, contrastColor);

        if (!string.IsNullOrEmpty(config.ThemeColor))
        {
            _themeColorHex = config.ThemeColor;
            ApplyThemeColorResources(config.ThemeColor);
        }

        var items = _dataService.LoadTodoItems();
        foreach (var item in items) TodoItems.Add(item);
    }

    private void ExecuteSetTextColor(string? hex)
    {
        if (!string.IsNullOrEmpty(hex))
        {
            try { ColorHelper.FromHex(hex); TextColorHex = hex; }
            catch { }
        }
    }

    private void ExecuteSetThemeColor(string? hex)
    {
        if (!string.IsNullOrEmpty(hex))
        {
            try { ColorHelper.FromHex(hex); ThemeColorHex = hex; }
            catch { }
        }
    }

    private void ExecuteStartEdit(TodoItem? item)
    {
        if (item == null || item.IsCompleted) return;
        foreach (var i in TodoItems) i.IsEditing = false;
        item.IsEditing = true;
    }

    private void ExecuteEndEdit(TodoItem? item)
    {
        if (item == null) return;
        item.IsEditing = false;
        if (string.IsNullOrWhiteSpace(item.Content))
        {
            TodoItems.Remove(item);
            if (SelectedItem == item) SelectedItem = null;
        }
        else
        {
            SaveTodoItems();
        }
    }

    private void ApplyTextColorResources(Color baseColor, Color contrastColor)
    {
        Color uncompletedColor, completedColor;
        if (IsTransparent)
        {
            uncompletedColor = baseColor;
            completedColor = contrastColor;
        }
        else
        {
            uncompletedColor = contrastColor;
            completedColor = baseColor;
        }

        Application.Current.Resources["TextColor"] = uncompletedColor;
        Application.Current.Resources["CompletedTextColor"] = completedColor;
        Application.Current.Resources["TextBrush"] = new SolidColorBrush(uncompletedColor);
        Application.Current.Resources["CompletedTextBrush"] = new SolidColorBrush(completedColor);
        TextColorChanged?.Invoke();
    }

    private void ReapplyTextColorResources()
    {
        var baseColor = ColorHelper.FromHex(_textColorHex);
        var contrastColor = DeriveContrastColor(baseColor);
        ApplyTextColorResources(baseColor, contrastColor);
    }

    private static Color DeriveContrastColor(Color baseColor)
    {
        double luminance = (baseColor.R * 0.299 + baseColor.G * 0.587 + baseColor.B * 0.114) / 255.0;
        return luminance > 0.5
            ? ColorHelper.Darken(baseColor, 0.4)
            : ColorHelper.Lighten(baseColor, 0.4);
    }

    private void ApplyThemeColorResources(string hex)
    {
        var themeColor = ColorHelper.FromHex(hex);
        var themeDarkColor = ColorHelper.Darken(themeColor, 0.15);
        var bgColor = ColorHelper.Lighten(themeColor, 0.3);

        Application.Current.Resources["ThemeColor"] = themeColor;
        Application.Current.Resources["ThemeDarkColor"] = themeDarkColor;
        Application.Current.Resources["PrimaryColor"] = themeColor;
        Application.Current.Resources["BackgroundColor"] = bgColor;
        Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(themeColor);
        Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(bgColor);
        Application.Current.Resources["ThemeBrush"] = new SolidColorBrush(themeColor);
        Application.Current.Resources["ThemeDarkBrush"] = new SolidColorBrush(themeDarkColor);
        ThemeColorChanged?.Invoke();
    }

    private void ExecuteAdd()
    {
        foreach (var i in TodoItems) i.IsEditing = false;
        var newItem = new TodoItem { Content = string.Empty, IsCompleted = false, IsEditing = true };
        TodoItems.Add(newItem);
    }

    private void ExecuteToggleTopmost()
    {
        IsTopmost = !IsTopmost;
        SaveConfig();
    }

    private void ExecuteToggleTransparent()
    {
        IsTransparent = !IsTransparent;
        SaveConfig();
    }

    private void ExecuteCreateNew()
    {
        string? input = _dialogService.ShowInputDialog("新建任务单", "请输入任务单名称:");
        if (string.IsNullOrEmpty(input)) return;

        string newFilePath;
        try
        {
            newFilePath = _dataService.CreateNewTodoFile(input);
        }
        catch (InvalidOperationException)
        {
            _dialogService.ShowErrorDialog("文件已存在！");
            return;
        }
        catch (Exception ex)
        {
            Logger.Error("创建新任务单失败", ex);
            _dialogService.ShowErrorDialog("创建新任务单失败");
            return;
        }

        SaveTodoItems();
        TodoItems.Clear();
        foreach (var item in _dataService.LoadTodoItemsFromFile(newFilePath))
        {
            TodoItems.Add(item);
        }
    }

    private void ExecuteDelete()
    {
        if (SelectedItem != null)
        {
            TodoItems.Remove(SelectedItem);
            SelectedItem = null;
            SaveTodoItems();
        }
    }

    private void ExecuteDeleteCompleted()
    {
        try
        {
            var completedItems = TodoItems.Where(i => i.IsCompleted).ToList();
            if (completedItems.Count == 0) return;

            foreach (var item in completedItems)
            {
                if (SelectedItem == item) SelectedItem = null;
                TodoItems.Remove(item);
            }
            SaveTodoItems();
        }
        catch (Exception ex)
        {
            Logger.Error("删除已完成项失败", ex);
        }
    }

    private void ExecuteCheckBoxChanged()
    {
        SaveTodoItems();
    }

    private void ExecuteClose()
    {
        _dataService.FlushSave();
        CloseRequested?.Invoke();
    }

    public void LoadTodoItemsFromFile(string filePath)
    {
        SaveTodoItems();
        TodoItems.Clear();
        foreach (var item in _dataService.LoadTodoItemsFromFile(filePath))
        {
            TodoItems.Add(item);
        }
    }

    public List<string> GetAvailableTodoFiles() => _dataService.GetAvailableTodoFiles();

    private void SaveConfig()
    {
        _dataService.SaveConfig(new AppConfig
        {
            Topmost = IsTopmost,
            Transparent = IsTransparent,
            TextColor = TextColorHex,
            ThemeColor = ThemeColorHex
        });
    }

    private void SaveTodoItems()
    {
        _dataService.SaveTodoItems(TodoItems);
    }
}
