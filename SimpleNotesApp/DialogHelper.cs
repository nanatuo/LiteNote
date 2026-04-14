using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleNotesApp;

public static class DialogHelper
{
    private static Color PrimaryColor => (Color)Application.Current.FindResource("PrimaryColor");
    private static Color BackgroundColor => (Color)Application.Current.FindResource("BackgroundColor");
    private static Color TextColor => (Color)Application.Current.FindResource("TextColor");

    public static string? ShowInputDialog(string title, string prompt, Window owner)
    {
        string? result = null;
        Window dialog = CreateBaseDialog(title, 320, 180, owner);
        Grid contentGrid = new Grid();
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid titleBar = CreateTitleBar(title);
        Grid.SetRow(titleBar, 0);

        Grid contentArea = new Grid();
        Grid.SetRow(contentArea, 1);
        contentArea.Margin = new Thickness(20);
        contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
        contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
        contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

        TextBlock textBlock = new TextBlock
        {
            Text = prompt,
            Foreground = new SolidColorBrush(TextColor),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(textBlock, 0);

        TextBox textBox = new TextBox
        {
            Width = double.NaN,
            Height = 30,
            FontSize = 14,
            BorderBrush = new SolidColorBrush(PrimaryColor),
            BorderThickness = new Thickness(1),
            Background = Brushes.White
        };
        Grid.SetRow(textBox, 1);

        StackPanel buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(buttonPanel, 2);

        Button okButton = CreateButton("确定", 80, 24, true);
        okButton.Margin = new Thickness(0, 0, 10, 0);
        okButton.Click += (s, args) => { result = textBox.Text; dialog.Close(); };

        Button cancelButton = CreateButton("取消", 80, 24, false);
        cancelButton.Click += (s, args) => { result = null; dialog.Close(); };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        contentArea.Children.Add(textBlock);
        contentArea.Children.Add(textBox);
        contentArea.Children.Add(buttonPanel);
        contentGrid.Children.Add(titleBar);
        contentGrid.Children.Add(contentArea);
        dialog.Content = CreateDialogContainer(contentGrid);
        EnableDrag(titleBar, dialog);

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) { result = textBox.Text; dialog.Close(); }
        };

        dialog.ShowDialog();
        return result;
    }

    public static void ShowErrorDialog(string message, Window owner)
    {
        Window dialog = CreateBaseDialog("错误", 300, 150, owner);
        Grid contentGrid = new Grid();
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid titleBar = CreateTitleBar("错误");
        Grid.SetRow(titleBar, 0);

        Grid contentArea = new Grid();
        Grid.SetRow(contentArea, 1);
        contentArea.Margin = new Thickness(20);
        contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

        TextBlock errorText = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(TextColor),
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(errorText, 0);

        StackPanel buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(buttonPanel, 1);

        Button okButton = CreateButton("确定", 80, 24, true);
        okButton.Click += (s, args) => dialog.Close();
        buttonPanel.Children.Add(okButton);

        contentArea.Children.Add(errorText);
        contentArea.Children.Add(buttonPanel);
        contentGrid.Children.Add(titleBar);
        contentGrid.Children.Add(contentArea);
        dialog.Content = CreateDialogContainer(contentGrid);
        EnableDrag(titleBar, dialog);
        dialog.ShowDialog();
    }

    private static Window CreateBaseDialog(string title, double width, double height, Window owner)
    {
        return new Window
        {
            Title = title,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent
        };
    }

    private static Grid CreateDialogContainer(UIElement content)
    {
        Grid mainGrid = new Grid();
        Border backgroundBorder = new Border
        {
            Background = new SolidColorBrush(BackgroundColor),
            BorderBrush = new SolidColorBrush(PrimaryColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0)
        };
        mainGrid.Children.Add(backgroundBorder);
        mainGrid.Children.Add(content);
        return mainGrid;
    }

    private static Grid CreateTitleBar(string title)
    {
        Grid titleBar = new Grid { Background = new SolidColorBrush(PrimaryColor) };
        TextBlock titleText = new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleBar.Children.Add(titleText);
        return titleBar;
    }

    private static Button CreateButton(string content, double width, double height, bool isPrimary)
    {
        return new Button
        {
            Content = content,
            Width = width,
            Height = height,
            Background = isPrimary ? new SolidColorBrush(PrimaryColor) : Brushes.Transparent,
            Foreground = isPrimary ? Brushes.White : new SolidColorBrush(PrimaryColor),
            BorderBrush = new SolidColorBrush(PrimaryColor),
            BorderThickness = new Thickness(1)
        };
    }

    private static void EnableDrag(UIElement element, Window window)
    {
        element.MouseDown += (s, args) =>
        {
            if (args.LeftButton == MouseButtonState.Pressed) window.DragMove();
        };
    }
}
