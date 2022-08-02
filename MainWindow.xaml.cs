using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Spire.Doc.Documents;

namespace RenderTableCreator
{
    public partial class MainWindow
    {
        private List<RenderItem> _renderList = new();
        private readonly SortedDictionary<string, RenderItem> _scenes = new();

        private string _selectedFile;
        private string _renderTableFile;
        private string _sceneName;

        // BUGFIX - Need to enforce consistent version numbers
        // which prevents having a mix of scene v15s2_9a and v14s15_9a 
        // in the same file. Inconsistent version numbers will report an error
        // and block creating the table
        private string _version = string.Empty;

        private static string _errorText = "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:";
        private static string _warnText = "WARNINGS:";

        private readonly List<string> _outputLog = new();
        private readonly List<string> _notes = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddLog(string text)
        {
            _outputLog.Add(text);
            WindowOutput.Text = string.Join("\n", _outputLog.ToArray());
        }

        private void BrowseFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "RenPy files (*.rpy)|*.rpy|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (!openFileDialog.ShowDialog() != true) return;
            _selectedFile = openFileDialog.FileName;
            ChosenFile.Text = $"Selected File: {_selectedFile}";
            CreateRenderTableButton.Visibility = Visibility.Visible;
            _renderTableFile = Path.ChangeExtension(_selectedFile.Trim(), ".docx");

            _sceneName = _renderTableFile.Split('\\').Last().Split('.').First().Replace("scene", "Scene ");
        }

        private void CreateRenderTableButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset the state each time the create render table button is clicked
            
            WindowOutput.Clear();
            _outputLog.Clear();
            _notes.Clear();
            _scenes.Clear();
            _errorText = "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:";
            _warnText = "WARNINGS:";

            StreamReader file = new(_selectedFile);

            int lineNumber = 0;
            bool inNotes = true;

            while (file.ReadLine() is { } line)
            {
                lineNumber++;
                line = line.Trim();

                if (line.StartsWith("#") && inNotes && !line.StartsWith("#!") )
                {
                    _notes.Add(line[1..].Trim());
                    continue;
                }

                inNotes = false;

                CreateRenderItem(line, lineNumber); 
            }

            if (_errorText == "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:" && _warnText == "WARNINGS:")
            {
                
                SuccessfulConvert();
            }
            else { FailedConvert(); }

        }

        private void CreateRenderItem(string line, int lineNumber)
        {
            if (!line.ToLower().StartsWith("scene") && !line.ToLower().StartsWith("show") &&
                !line.ToLower().StartsWith("#!"))
            {
                return;
            }

            string[] lineArgs = line.Split(' ');
            string imageName = lineArgs[1];
            
            if (imageName.EndsWith('_'))
            {
                _errorText +=
                    $"\nImage name {imageName} on line {lineNumber} ends with an underscore (missing scene number).";
            }

            
            if (string.IsNullOrEmpty(_version))
            {
                _version = imageName[..3];
            }
            else
            {
                string currentVersion = imageName[..3];
                if (0 != string.Compare(_version, currentVersion, StringComparison.OrdinalIgnoreCase))
                {
                    _errorText +=
                        $"\n{imageName}: Conflicting version found at line {lineNumber}.\nThe version should be {_version}";
                }
            }

            if (0 == string.Compare(imageName, "black", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string imageDesc = string.Empty;

            if (line.ToLower().StartsWith("#!"))
            {
                imageDesc += "KIWII IMAGE: ";
            }

            if (lineArgs.Length > 2)
            {
                imageDesc += string.Join(' ', lineArgs[3..]);
            }

            // Normalize the description
            imageDesc = imageDesc.Replace('#', ' ').Trim();

            // current scene not in the list; New Scene
            if (!_scenes.ContainsKey(imageName))
            {
                // New scene without a render description; Error case
                if (string.IsNullOrEmpty(imageDesc))
                {
                    _warnText += $"\n{imageName}: Missing description at line {lineNumber}";
                }
                // New scene with a proper render description; add to list
                else
                {
                    _scenes.Add(imageName, new RenderItem(
                        imageName,
                        imageDesc,
                        lineNumber));
                }
            }
            // current scene is in the list; Potential reuse
            else
            {
                // Scene Reuse; legit use case 
                if (string.IsNullOrEmpty(imageDesc))
                {
                    _scenes[imageName].RefCount++;
                }
                //Existing scene with a different render description than previous; Error case
                else if (!imageDesc.Equals(_scenes[imageName].Description))
                {
                    int originalLineNumber = _scenes[imageName].LineNumber;

                    _errorText +=
                        $"\n{imageName}: Conflicting description found at line {lineNumber} with original description at line {originalLineNumber}.";
                }
            }
        }

        private int TotalRenderCount()
        {
            return _scenes.Values.Sum(r => r.RefCount);
        }

        private int ReusedRenderCount()
        {
            return _scenes.Values.Sum(r => r.RefCount - 1);
        }
        
        private void CreateDocument()
        {
            AltDocument document = new();
  
            document.AddHeading($"{_sceneName} Render Table", BuiltinStyle.Title);
            document.AddHeading("Scene Notes:", BuiltinStyle.Heading1);
            document.AddParagraph(string.Join("\n", _notes));
            document.AddParagraph($"Unique Render count: {_renderList.Count}");
            document.AddParagraph($"Total Render count: {TotalRenderCount()}");
            document.AddParagraph($"Reused Render count: {ReusedRenderCount()}");

            int percent = (int)  (100 * (ReusedRenderCount() / (double) TotalRenderCount()));

            document.AddParagraph($"Reused %: {percent}");
            document.AddHeading("Render Table:", BuiltinStyle.Heading1);
            document.AddParagraph(string.Empty);

            string[] tableHeadings = { "Scene", "Description", "Occurrences" };
            document.AddTable(tableHeadings, _renderList);

            document.SaveDocument(_renderTableFile);
            AddLog($"Render Table Created Successfully for {_sceneName}");

        }

        private void SuccessfulConvert()
        {
            _renderList = _scenes.Values.ToList();
            //renderList.Sort(Comparison);  // TEMP BUG FIX- inconsistent scene name syntax are causing sort function to blow up.
            OrderList2(ref _renderList);
            CreateDocument();
        }

        private void FailedConvert()
        {
            AddLog($"Failed to create render table for {_sceneName}.");
            if (_errorText != "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:")
            {
                AddLog(_errorText);
            }
            if (_warnText != "WARNINGS:")
            {
                AddLog(_warnText);
            }
        }

        private static void OrderList2(ref List<RenderItem> list)
        {
            bool change;

            do
            {
                change = false;

                for (int previous = 0; previous < list.Count - 1; previous++)
                {
                    int current = previous + 1;

                    int result = list[current].CompareTo(list[previous]);

                    if (-1 != result) continue;
                    SwapListEntries(previous, current, ref list);
                    change = true;

                }
            } while (change);
        }

        private static void SwapListEntries(int posA, int posB, ref List<RenderItem> list)
        {
            (list[posB], list[posA]) = (list[posA], list[posB]);
        }
       
    }
}