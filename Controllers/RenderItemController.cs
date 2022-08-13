using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spire.Doc;

namespace RenderTableCreator.Controllers;

public class RenderItemController
{
    private readonly Dictionary<string, RenderItem> _scenes = new();
    private readonly Dictionary<string, RenderItem> _animations = new();
    private List<RenderItem> _renderList = new();

    private string _version = string.Empty;
    private string _errorText = "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:";
    private string _warnText = "WARNINGS:";

    private string _selectedFile = string.Empty;
    private string _renderTableFile = string.Empty;
    private string _sceneName = string.Empty;

    private readonly MainWindow _mainWindow;
    private readonly List<string> _notes = new();

    public RenderItemController(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void ResetState()
    {
        _scenes.Clear();

        _errorText = "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:";
        _warnText = "WARNINGS:";
    }

    private void CreateRenderItem(string line, int lineNumber)
    {
        string[] lineArgs = line.Split(' ');

        string imageName = $"ep2s{_version}_{_scenes.Count + 1:00}";
        string imageDesc = string.Join(' ', lineArgs[2..]);
        
        // if (line.ToLower().StartsWith("#!"))
        // {
        //     imageDesc += "KIWII IMAGE: ";
        // }

        if (_scenes.ContainsKey(imageDesc.ToLower()))
        {
            _scenes[imageDesc.ToLower()].RefCount++;
            return;
        }
        
        _scenes.Add(imageDesc.ToLower(), new RenderItem(
            imageName,
            imageDesc,
            lineNumber));
    }

    private void CreateAnimationItem(RenderItem animation)
    {
        if (_animations.ContainsKey(animation.Description.ToLower()))
        {
            _animations[animation.Description.ToLower()].RefCount++;
            return;
        }
        
        _animations.Add(animation.Description.ToLower(), animation);
    }

    public int TotalRenderCount()
    {
        return _scenes.Values.Sum(r => r.RefCount);
    }

    public int ReusedRenderCount()
    {
        return _scenes.Values.Sum(r => r.RefCount - 1);
    }

    public void ParseFile(string selectedFile, string renderTableFile, string sceneName)
    {
        _selectedFile = selectedFile;
        _renderTableFile = renderTableFile;
        _sceneName = sceneName;

        Document document = new();
        document.LoadFromFile(_selectedFile);
        document.SaveToTxt(Path.Join(MainWindow.AppDataPath, "data.txt"), Encoding.UTF8);

        using (StreamReader file = new(Path.Join(MainWindow.AppDataPath, "data.txt")))
        {
            int lineNumber = 0;
            RenderItem? animation = null;

            while (file.ReadLine() is { } line)
            {
                lineNumber++;
                line = line.Trim();

                if (animation is not null)
                {
                    animation.Description += line + Environment.NewLine;

                    if (!string.IsNullOrWhiteSpace(line)) continue;
                
                    CreateAnimationItem(animation);
                    animation = null;
                }
            
                else if (line.StartsWith("-sid"))
                {
                    _version = line.Split(' ').Last();
                }

                else if (line.StartsWith("-location"))
                {
                    _notes.Add($"Location: {string.Join(' ', line.Split(' ')[1..])}");
                }

                else if (line.StartsWith("-outfit"))
                {
                    _notes.Add($"Outfit: {string.Join(' ', line.Split(' ')[1..])}");
                }

                else if (line.StartsWith("-time"))
                {
                    _notes.Add($"Time: {string.Join(' ', line.Split(' ')[1..])}");
                }

                else if (line.StartsWith("-prop"))
                {
                    _notes.Add($"Props: {string.Join(' ', line.Split(' ')[1..])}");
                }
            
                else if (line.StartsWith("-image"))
                {
                    CreateRenderItem(line, lineNumber);
                }

                else if (line.StartsWith("ANIMATION"))
                {
                    animation = new RenderItem($"ep2s{_version}_{_animations.Count + 1:00}_anim", lineNumber: lineNumber);
                }
            }
        }
        
        if (_errorText == "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:" && _warnText == "WARNINGS:")
        {
            SuccessfulConvert();
        }
        else
        {
            FailedConvert();
        }
    }
    
    private void SuccessfulConvert()
    {
        _renderList = _scenes.Values.ToList();
        //renderList.Sort(Comparison);  // TEMP BUG FIX- inconsistent scene name syntax are causing sort function to blow up.
        OrderList(ref _renderList);
        
        AltDocument _ = new(this, _sceneName, _renderTableFile, _version, _notes, _renderList, _animations.Values.ToList());
    }
    
    private static void OrderList(ref List<RenderItem> list)
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

    private void FailedConvert()
    {
        _mainWindow.AddLog($"Failed to create render table for {_sceneName}.");
        if (_errorText != "ERRORS FOUND IN TRANSCRIPT. FIX THEM AND TRY AGAIN:")
        {
            _mainWindow.AddLog(_errorText);
        }
        if (_warnText != "WARNINGS:")
        {
            _mainWindow.AddLog(_warnText);
        }
    }
}