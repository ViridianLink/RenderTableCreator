using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using RenderTableCreator.Controllers;

namespace RenderTableCreator;

public partial class MainWindow
{
    public static readonly string AppDataPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Oscar Six", "RenderTableCreator");
    
    private string? _selectedFile;
    private string? _renderTableFile;
    private string? _sceneName;

    private readonly List<string> _outputLog = new();

    private readonly RenderItemController _renderItemController;

    public MainWindow()
    {
        _renderItemController = new RenderItemController(this);
        
        InitializeComponent();
    }

    public void AddLog(string text)
    {
        _outputLog.Add(text);
        WindowOutput.Text = string.Join("\n", _outputLog.ToArray());
    }

    private void BrowseFiles_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Filter = "Script Files (*.docx)|*.docx|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        if (openFileDialog.ShowDialog() != true || openFileDialog.FileName == string.Empty)
            return;
            
        _selectedFile = openFileDialog.FileName.Trim();
        ChosenFile.Text = $"Selected File: {_selectedFile}";
        CreateRenderTableButton.Visibility = Visibility.Visible;
        _renderTableFile = Path.ChangeExtension(_selectedFile.Replace("DONE - ", ""), null) + " (Render Table).docx";
        _sceneName = Path.GetFileNameWithoutExtension(_selectedFile).Split(')')[1];
    }

    private void CreateRenderTableButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset the state each time the create render table button is clicked
            
        WindowOutput.Clear();
        _outputLog.Clear();
        _renderItemController.ResetState();

        Debug.Assert(_selectedFile != null, nameof(_selectedFile) + " != null");
        Debug.Assert(_renderTableFile != null, nameof(_renderTableFile) + " != null");
        Debug.Assert(_sceneName != null, nameof(_sceneName) + " != null");
        
        _renderItemController.ParseFile(_selectedFile, _renderTableFile, _sceneName);
        AddLog("Render Table Created.");
    }
}

// TODO: Phone Images
// TODO: Improve logging