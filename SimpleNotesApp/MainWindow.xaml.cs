using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleNotesApp;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private bool isDragging = false;
    private bool potentialDrag = false;
    private bool dragOccurred = false;
    private Point startPoint;
    private const double DragThreshold = 5.0;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new DialogService(this), new DataService());
        SetupEventHandlers();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.CloseRequested += () => Close();
        ViewModel.TextColorChanged += OnTextColorChanged;
        ViewModel.ThemeColorChanged += OnThemeColorChanged;
        ViewModel.TodoItems.CollectionChanged += TodoItems_CollectionChanged;
        UpdateBackgroundOpacity();
        UpdateCheckMarks();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.BackgroundAlpha))
            UpdateBackgroundOpacity();
        if (e.PropertyName == nameof(MainViewModel.IsTopmost) || e.PropertyName == nameof(MainViewModel.IsTransparent))
            UpdateCheckMarks();
    }

    private void SetupEventHandlers()
    {
        titleBar.PreviewMouseDown += TitleBar_PreviewMouseDown;
        PreviewMouseMove += MainWindow_PreviewMouseMove;
        PreviewMouseUp += MainWindow_PreviewMouseUp;
        MouseDown += MainWindow_MouseDown;
        Deactivated += MainWindow_Deactivated;
        lstTodoItems.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(EditTextBox_KeyDown));
        lstTodoItems.AddHandler(TextBox.LostFocusEvent, new RoutedEventHandler(EditTextBox_LostFocus));
    }

    private void TitleBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            potentialDrag = true;
            isDragging = false;
            dragOccurred = false;
            startPoint = e.GetPosition(this);
        }
    }

    private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not TextBox)
        {
            EndAnyEditing();
        }
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        EndAnyEditing();
    }

    private void EndAnyEditing()
    {
        foreach (var item in ViewModel.TodoItems)
        {
            if (item.IsEditing)
            {
                ViewModel.EndEditCommand.Execute(item);
                break;
            }
        }
    }

    private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is not TextBox tb || tb.DataContext is not TodoItem item) return;
        if (e.Key == Key.Enter)
        {
            ViewModel.EndEditCommand.Execute(item);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            item.IsEditing = false;
            e.Handled = true;
        }
    }

    private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox tb || tb.DataContext is not TodoItem item) return;
        if (item.IsEditing)
        {
            ViewModel.EndEditCommand.Execute(item);
        }
    }

    private void EditTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectionStart = tb.Text.Length;
            tb.SelectionLength = 0;
        }
    }

    private void EditTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox tb && tb.IsVisible && tb.DataContext is TodoItem item && item.IsEditing)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.Focus();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
    }

    private void TodoItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?.Count > 0 && e.NewItems[0] is TodoItem newItem)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lstTodoItems.ScrollIntoView(newItem);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (lstTodoItems.ItemContainerGenerator.ContainerFromItem(newItem) is ListBoxItem lbi)
                    {
                        var tb = FindVisualChild<TextBox>(lbi);
                        if (tb != null && newItem.IsEditing) tb.Focus();
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (potentialDrag && !isDragging)
        {
            Point p = e.GetPosition(this);
            double dx = p.X - startPoint.X;
            double dy = p.Y - startPoint.Y;
            if (dx * dx + dy * dy > DragThreshold * DragThreshold)
            {
                isDragging = true;
                dragOccurred = true;
                Mouse.Capture(this);
            }
        }
        if (isDragging)
        {
            Point p = e.GetPosition(this);
            Left += p.X - startPoint.X;
            Top += p.Y - startPoint.Y;
        }
    }

    private void MainWindow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (isDragging)
        {
            isDragging = false;
            potentialDrag = false;
            Mouse.Capture(null);
        }
        else
        {
            potentialDrag = false;
            dragOccurred = false;
        }
    }

    private void UpdateBackgroundOpacity()
    {
        byte alpha = ViewModel.BackgroundAlpha;
        var bgColor = (Color)Application.Current.Resources["BackgroundColor"];
        var themeColor = (Color)Application.Current.Resources["ThemeColor"];
        var themeDarkColor = (Color)Application.Current.Resources["ThemeDarkColor"];

        mainBackground.Background = new SolidColorBrush(Color.FromArgb(alpha, bgColor.R, bgColor.G, bgColor.B));
        mainBackground.BorderBrush = new SolidColorBrush(Color.FromArgb(alpha, themeColor.R, themeColor.G, themeColor.B));
        titleBar.Background = new SolidColorBrush(Color.FromArgb(alpha, themeDarkColor.R, themeDarkColor.G, themeDarkColor.B));
    }

    private void UpdateCheckMarks()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateCheckMark(0, ViewModel.IsTopmost);
            UpdateCheckMark(1, ViewModel.IsTransparent);
        }), System.Windows.Threading.DispatcherPriority.Render);
    }

    private void UpdateCheckMark(int buttonIndex, bool isChecked)
    {
        if (menuPopup.Child is not Border menuBorder) return;
        if (menuBorder.Child is not StackPanel sp) return;
        if (sp.Children.Count <= buttonIndex || sp.Children[buttonIndex] is not Button btn) return;
        if (btn.Template.FindName("grid", btn) is not Grid g || g.Children.Count == 0) return;
        if (g.Children[0] is TextBlock checkMark)
            checkMark.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
    }

    private void btnMenu_Click(object sender, RoutedEventArgs e)
    {
        if (dragOccurred)
        {
            dragOccurred = false;
            return;
        }
        menuPopup.IsOpen = true;
        UpdateCheckMarks();
    }

    private void OnTopmostButtonLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            UpdateSingleCheckMark(btn, ViewModel.IsTopmost);
    }

    private void OnTransparentButtonLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            UpdateSingleCheckMark(btn, ViewModel.IsTransparent);
    }

    private void UpdateSingleCheckMark(Button btn, bool isChecked)
    {
        btn.ApplyTemplate();
        if (btn.Template.FindName("grid", btn) is Grid g && g.Children.Count > 0 && g.Children[0] is TextBlock checkMark)
            checkMark.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
    {
        loadMenuItems.Children.Clear();
        foreach (string file in ViewModel.GetAvailableTodoFiles())
        {
            Button fileButton = new Button
            {
                Content = System.IO.Path.GetFileName(file),
                Style = (Style)FindResource("MenuItemButtonStyle")
            };
            string filePath = file;
            fileButton.Click += (s, args) =>
            {
                ViewModel.LoadTodoItemsFromFile(filePath);
                loadMenuPopup.IsOpen = false;
            };
            loadMenuItems.Children.Add(fileButton);
        }
        loadMenuPopup.IsOpen = true;
    }

    private void MenuItem_FontColor_Click(object sender, RoutedEventArgs e)
    {
        txtCustomColor.Text = ViewModel.TextColorHex;
        fontColorPopup.IsOpen = true;
    }

    private void PresetFontColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            ViewModel.SetTextColorCommand.Execute(hex);
            txtCustomColor.Text = hex;
        }
    }

    private void ApplyCustomFontColor_Click(object sender, RoutedEventArgs e)
    {
        string hex = txtCustomColor.Text.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        ViewModel.SetTextColorCommand.Execute(hex);
    }

    private void OnTextColorChanged()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (fontColorPreview != null)
            {
                var brush = Application.Current.Resources["TextBrush"] as SolidColorBrush;
                if (brush != null)
                    fontColorPreview.Background = brush;
            }
        }), System.Windows.Threading.DispatcherPriority.Render);
    }

    private void MenuItem_ThemeColor_Click(object sender, RoutedEventArgs e)
    {
        txtCustomThemeColor.Text = ViewModel.ThemeColorHex;
        themeColorPopup.IsOpen = true;
    }

    private void PresetThemeColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            ViewModel.SetThemeColorCommand.Execute(hex);
            txtCustomThemeColor.Text = hex;
        }
    }

    private void ApplyCustomThemeColor_Click(object sender, RoutedEventArgs e)
    {
        string hex = txtCustomThemeColor.Text.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        ViewModel.SetThemeColorCommand.Execute(hex);
    }

    private void OnThemeColorChanged()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateBackgroundOpacity();
        }), System.Windows.Threading.DispatcherPriority.Render);
    }
}
