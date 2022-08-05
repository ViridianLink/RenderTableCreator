using System;
using System.Collections.Generic;
using System.IO;
using RenderTableCreator.Controllers;
using Spire.Doc;
using Spire.Doc.Documents;

namespace RenderTableCreator;

public class AltDocument
{
    private readonly Document _document;
    private readonly string _sceneName;
    private readonly string _renderTableFile;
    private readonly string _version;
    private readonly List<string> _notes;
    private readonly List<RenderItem> _renderList;
    private readonly List<RenderItem> _animationList;
    
    private readonly RenderItemController _renderItemController;

    internal AltDocument(RenderItemController renderItemController, string sceneName, string renderTableFile,
        string version, List<string> notes, List<RenderItem> renderList, List<RenderItem> animationList)
    {
        _renderItemController = renderItemController;

        _version = version;
        _sceneName = sceneName;
        _renderTableFile = renderTableFile;
        _renderList = renderList;
        _animationList = animationList;
        _notes = notes;
        
        _document = new Document();
        CreateTemplateDocument();
    }

    private void AddHeading(string text, BuiltinStyle style)
    {
        Section section = style == BuiltinStyle.Title ? _document.AddSection() : _document.LastSection;

        Paragraph p1 = section.AddParagraph();
        p1.ApplyStyle(style); 
        p1.Text = text;
            
    }

    private void AddParagraph(string text)
    {
        Section section = _document.LastSection; 
        Paragraph p1 = section.AddParagraph();
        p1.ApplyStyle(BuiltinStyle.Normal);
        p1.Text = text; 
    }

    private void AddTable(IReadOnlyList<string> headings, IReadOnlyList<RenderItem> tableData)
    {
        int maxRows = tableData.Count;
        int maxCols = headings.Count;
        //int maxHeaderCols = maxCols;
                        
        Section section = _document.LastSection;
        Table table = section.AddTable(true);
        table.ResetCells(maxRows + 2, maxCols);     // Add 1 for the header row.

        // Process Header Row
        for(int c = 0; c < headings.Count; c++)
        {
            Paragraph p1 = table.Rows[0].Cells[c].AddParagraph();
            p1.AppendText(headings[c]);
        }

        for(int r = 0; r < maxRows; r++)
        {               
                
            for(int c = 0; c < maxCols; c++)
            {
                // ImageName 
                Paragraph p1 = table.Rows[r + 1].Cells[c].AddParagraph();
                p1.AppendText(tableData[r].ImageName);
                c++;

                // Image Description 
                Paragraph p2 = table.Rows[r + 1].Cells[c].AddParagraph();
                p2.AppendText(tableData[r].Description);
                c++;

                // Occurrences 
                Paragraph p3 = table.Rows[r + 1].Cells[c].AddParagraph();
                p3.AppendText(tableData[r].RefCount.ToString()); 

            }
                
        }            
    }

    private void CreateTemplateDocument()
    {
        AddHeading($"{_version}) {_sceneName} Render Table", BuiltinStyle.Title);
        AddHeading("Scene Notes:", BuiltinStyle.Heading1);
        AddParagraph(string.Join("\n", _notes));
        AddParagraph($"Unique Render count: {_renderList.Count}");
        AddParagraph($"Total Render count: {_renderItemController.TotalRenderCount()}");
        AddParagraph($"Reused Render count: {_renderItemController.ReusedRenderCount()}");

        int percent = (int)  (100 * (_renderItemController.ReusedRenderCount() / (double) _renderItemController.TotalRenderCount()));

        AddParagraph($"Reused %: {percent}");
        AddHeading("Image Table:", BuiltinStyle.Heading1);
        AddParagraph(string.Empty);

        string[] tableHeadings = { "Scene", "Description", "Occurrences" };
        AddTable(tableHeadings, _renderList);
        
        // Animation Table
        AddHeading("Animation Table:", BuiltinStyle.Heading1);
        AddParagraph(string.Empty);
        AddTable(tableHeadings, _animationList);
        
        SaveDocument(_renderTableFile);
        // ($"Render Table Created Successfully for {_sceneName}");

    }

    private void SaveDocument(string filename)
    {
        FileFormat ff = FileFormat.Docx2013;

        if (Environment.GetEnvironmentVariable("RTC_PDF_OUTPUT") == "1")
        {
            filename = Path.ChangeExtension(filename, ".pdf");
            ff = FileFormat.PDF;
        }

        _document.SaveToFile(filename, ff);
    }

}