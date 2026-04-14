namespace SimpleNotesApp;

/// <summary>
/// 数据持久化服务接口，提供配置和待办事项的读写操作
/// </summary>
public interface IDataService
{
    /// <summary>加载应用配置</summary>
    AppConfig LoadConfig();

    /// <summary>保存应用配置</summary>
    void SaveConfig(AppConfig config);

    /// <summary>加载待办事项列表</summary>
    List<TodoItem> LoadTodoItems();

    /// <summary>防抖保存待办事项（延迟写入磁盘）</summary>
    void SaveTodoItems(IEnumerable<TodoItem> items);

    /// <summary>立即将挂起的待办事项写入磁盘</summary>
    void FlushSave();

    /// <summary>从指定文件加载待办事项</summary>
    List<TodoItem> LoadTodoItemsFromFile(string filePath);

    /// <summary>获取当前已加载的待办事项</summary>
    List<TodoItem> GetLoadedTodoItems();

    /// <summary>获取当前待办文件路径</summary>
    string GetCurrentTodoFilePath();

    /// <summary>获取所有可用的待办文件路径</summary>
    List<string> GetAvailableTodoFiles();

    /// <summary>创建新的待办文件并返回文件路径</summary>
    string CreateNewTodoFile(string name);
}
