using System.IO;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace SimpleNotesApp;

public class DataService : IDataService
{
    private readonly string _configFilePath;
    private readonly string _notesFolder;
    private string _currentTodoFilePath;
    private List<TodoItem> _loadedItems = new();

    private DispatcherTimer? _saveTimer;
    private IEnumerable<TodoItem>? _pendingSaveItems;
    private static readonly TimeSpan SaveDebounceInterval = TimeSpan.FromMilliseconds(500);

    public DataService()
    {
        _configFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");
        _notesFolder = EnsureNotesFolder();
        _currentTodoFilePath = Path.Combine(_notesFolder, "todo.json");
    }

    public AppConfig LoadConfig()
    {
        if (!File.Exists(_configFilePath)) return new AppConfig();
        try
        {
            string json = File.ReadAllText(_configFilePath);
            var config = JsonConvert.DeserializeObject<AppConfig>(json);
            return config ?? new AppConfig();
        }
        catch (Exception ex) { Logger.Error("配置文件读取失败", ex); return new AppConfig(); }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            File.WriteAllText(_configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch (Exception ex) { Logger.Error("配置文件保存失败", ex); }
    }

    public List<TodoItem> LoadTodoItems()
    {
        string todoFilePath = ResolveTodoFilePath();
        if (!File.Exists(todoFilePath)) return new List<TodoItem>();

        try
        {
            var loaded = JsonConvert.DeserializeObject<List<TodoItem>>(File.ReadAllText(todoFilePath));
            if (loaded != null)
            {
                _currentTodoFilePath = todoFilePath;
                _loadedItems = loaded;
                return loaded;
            }
        }
        catch (Exception ex) { Logger.Error($"读取待办事项文件失败: {todoFilePath}", ex); }

        return new List<TodoItem>();
    }

    public void SaveTodoItems(IEnumerable<TodoItem> items)
    {
        // 创建快照，防止防抖期间集合被修改导致保存不一致
        _pendingSaveItems = items.ToList();
        _saveTimer ??= CreateSaveTimer();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void FlushSave()
    {
        _saveTimer?.Stop();
        if (_pendingSaveItems != null)
        {
            WriteTodoItemsToDisk(_pendingSaveItems);
            _pendingSaveItems = null;
        }
    }

    public List<TodoItem> LoadTodoItemsFromFile(string filePath)
    {
        try
        {
            var loaded = JsonConvert.DeserializeObject<List<TodoItem>>(File.ReadAllText(filePath));
            if (loaded != null)
            {
                _currentTodoFilePath = filePath;
                _loadedItems = loaded;
                WriteLastUsedFile(Path.GetFileName(filePath));
                return loaded;
            }
        }
        catch (Exception ex) { Logger.Error($"从文件加载待办事项失败: {filePath}", ex); }

        return new List<TodoItem>();
    }

    public List<TodoItem> GetLoadedTodoItems() => _loadedItems;

    public string GetCurrentTodoFilePath() => _currentTodoFilePath;

    public List<string> GetAvailableTodoFiles()
    {
        var files = new List<string>();
        if (Directory.Exists(_notesFolder))
        {
            foreach (string file in Directory.GetFiles(_notesFolder, "*.json"))
            {
                files.Add(file);
            }
        }
        return files;
    }

    public string CreateNewTodoFile(string name)
    {
        string fileName = name.Trim() + ".json";
        string filePath = Path.Combine(_notesFolder, fileName);

        if (File.Exists(filePath))
            throw new InvalidOperationException("文件已存在！");

        File.WriteAllText(filePath, "[]");
        WriteLastUsedFile(fileName);
        _currentTodoFilePath = filePath;
        return filePath;
    }

    private DispatcherTimer CreateSaveTimer()
    {
        var timer = new DispatcherTimer { Interval = SaveDebounceInterval };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            if (_pendingSaveItems != null)
            {
                WriteTodoItemsToDisk(_pendingSaveItems);
                _pendingSaveItems = null;
            }
        };
        return timer;
    }

    private void WriteTodoItemsToDisk(IEnumerable<TodoItem> items)
    {
        if (string.IsNullOrEmpty(_currentTodoFilePath))
        {
            _currentTodoFilePath = Path.Combine(_notesFolder, "todo.json");
        }

        try
        {
            File.WriteAllText(_currentTodoFilePath, JsonConvert.SerializeObject(items, Formatting.Indented));
            WriteLastUsedFile(Path.GetFileName(_currentTodoFilePath));
        }
        catch (Exception ex) { Logger.Error($"保存待办事项文件失败: {_currentTodoFilePath}", ex); }
    }

    private string ResolveTodoFilePath()
    {
        string lastUsedFilePath = Path.Combine(_notesFolder, "last_used.txt");
        if (File.Exists(lastUsedFilePath))
        {
            try
            {
                string lastUsedFile = File.ReadAllText(lastUsedFilePath).Trim();
                if (!string.IsNullOrEmpty(lastUsedFile))
                {
                    string lastUsedPath = Path.Combine(_notesFolder, lastUsedFile);
                    if (File.Exists(lastUsedPath)) return lastUsedPath;
                }
            }
            catch (Exception ex) { Logger.Error("读取last_used.txt失败", ex); }
        }
        return Path.Combine(_notesFolder, "todo.json");
    }

    private void WriteLastUsedFile(string fileName)
    {
        try
        {
            File.WriteAllText(Path.Combine(_notesFolder, "last_used.txt"), fileName);
        }
        catch (Exception ex) { Logger.Error("写入last_used.txt失败", ex); }
    }

    private static string EnsureNotesFolder()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Notes");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return folder;
    }
}
