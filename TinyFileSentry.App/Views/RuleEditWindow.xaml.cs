using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using TinyFileSentry.Core.Models;
using TinyFileSentry.App.ViewModels;

namespace TinyFileSentry.App.Views;

public partial class RuleEditWindow : Window
{
    public WatchRule? Rule { get; private set; }
    public bool IsEditMode { get; private set; }
    
    public RuleEditWindow(WatchRule? rule = null)
    {
        InitializeComponent();
        
        IsEditMode = rule != null;
        Rule = rule ?? new WatchRule();
        
        // Настройка заголовка
        if (IsEditMode)
        {
            WindowTitleText.Text = "Edit Rule";
            Title = "Edit Rule";
            SaveButton.Content = "Save Changes";
        }
        
        // Заполнение полей
        SourceFileTextBox.Text = Rule.SourceFile;
        DestinationFolderTextBox.Text = Rule.DestinationRoot;
        
        // Установка post-action
        foreach (System.Windows.Controls.ComboBoxItem item in PostActionComboBox.Items)
        {
            if (item.Tag?.ToString() == Rule.PostAction.ToString())
            {
                PostActionComboBox.SelectedItem = item;
                break;
            }
        }
        
        // Подписка на изменения для валидации
        SourceFileTextBox.TextChanged += (s, e) => ValidateSource();
        DestinationFolderTextBox.TextChanged += (s, e) => ValidateDestination();
    }
    
    private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Source File",
            Filter = "All Files (*.*)|*.*",
            CheckFileExists = true
        };
        
        if (!string.IsNullOrEmpty(SourceFileTextBox.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(SourceFileTextBox.Text);
        }
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SourceFileTextBox.Text = dialog.FileName;
        }
    }
    
    private void BrowseDestinationButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FolderBrowserDialog
        {
            Description = "Select Destination Folder",
            UseDescriptionForTitle = true
        };
        
        if (!string.IsNullOrEmpty(DestinationFolderTextBox.Text))
        {
            dialog.SelectedPath = DestinationFolderTextBox.Text;
        }
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DestinationFolderTextBox.Text = dialog.SelectedPath;
        }
    }
    
    private void ValidateSource()
    {
        var sourcePath = SourceFileTextBox.Text?.Trim();
        
        if (string.IsNullOrEmpty(sourcePath))
        {
            ShowSourceError("Please select a source file");
            return;
        }
        
        if (!File.Exists(sourcePath))
        {
            ShowSourceError("The selected file does not exist");
            return;
        }
        
        // Проверка размера файла (>10MB)
        try
        {
            var fileInfo = new FileInfo(sourcePath);
            if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
            {
                ShowSourceError("Files larger than 10MB are not supported");
                return;
            }
        }
        catch (Exception ex)
        {
            ShowSourceError($"Error reading file: {ex.Message}");
            return;
        }
        
        HideSourceError();
    }
    
    private void ValidateDestination()
    {
        var destinationPath = DestinationFolderTextBox.Text?.Trim();
        
        if (string.IsNullOrEmpty(destinationPath))
        {
            ShowDestinationError("Please select a destination folder");
            return;
        }
        
        if (!Directory.Exists(destinationPath))
        {
            try
            {
                Directory.CreateDirectory(destinationPath);
            }
            catch (Exception ex)
            {
                ShowDestinationError($"Cannot create directory: {ex.Message}");
                return;
            }
        }
        
        // Проверка прав доступа
        try
        {
            var testFile = Path.Combine(destinationPath, "test_write_access.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            ShowDestinationError($"No write access to directory: {ex.Message}");
            return;
        }
        
        HideDestinationError();
    }
    
    private void ShowSourceError(string message)
    {
        SourceErrorText.Text = message;
        SourceErrorText.Visibility = Visibility.Visible;
    }
    
    private void HideSourceError()
    {
        SourceErrorText.Visibility = Visibility.Collapsed;
    }
    
    private void ShowDestinationError(string message)
    {
        DestinationErrorText.Text = message;
        DestinationErrorText.Visibility = Visibility.Visible;
    }
    
    private void HideDestinationError()
    {
        DestinationErrorText.Visibility = Visibility.Collapsed;
    }
    
    private bool ValidateForm()
    {
        ValidateSource();
        ValidateDestination();
        
        return SourceErrorText.Visibility == Visibility.Collapsed &&
               DestinationErrorText.Visibility == Visibility.Collapsed;
    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm())
            return;
        
        // Создание или обновление правила
        Rule!.SourceFile = SourceFileTextBox.Text.Trim();
        Rule.DestinationRoot = DestinationFolderTextBox.Text.Trim();
        
        if (PostActionComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
        {
            if (Enum.TryParse<PostActionType>(selectedItem.Tag?.ToString(), out var postAction))
            {
                Rule.PostAction = postAction;
            }
        }
        
        DialogResult = true;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}