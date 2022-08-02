using System;
using System.Collections.Generic;
using System.IO;
using Spire.Doc;
using Spire.Doc.Documents;

namespace RenderTableCreator
{
    internal class AltDocument
    {
        private readonly Document _document; 



        internal AltDocument()
        {
            _document = new Document();
        }

        internal void AddHeading(string text, BuiltinStyle style)
        {
            Section section = style == BuiltinStyle.Title ? _document.AddSection() : _document.LastSection;

            Paragraph p1 = section.AddParagraph();
            p1.ApplyStyle(style); 
            p1.Text = text;
            
        }
        internal void AddParagraph(string text)
        {
            Section section = _document.LastSection; 
            Paragraph p1 = section.AddParagraph();
            p1.ApplyStyle(BuiltinStyle.Normal);
            p1.Text = text; 
        }

        internal void AddTable(string[] headings, List<RenderItem> tableData)
        {
            int maxRows = tableData.Count;
            int maxCols = headings.Length;
            //int maxHeaderCols = maxCols;
                        
            Section section = _document.LastSection;
            Table table = section.AddTable(true);
            table.ResetCells(maxRows + 2, maxCols);     // Add 1 for the header row.

            // Process Header Row
            for(int c = 0; c < headings.Length; c++)
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
        internal void SaveDocument(string filename)
        {
            FileFormat ff = FileFormat.Docx2013; 

            if (string.Compare(Environment.GetEnvironmentVariable("RTC_PDF_OUTPUT"), "1", StringComparison.OrdinalIgnoreCase) == 0)
            {
                filename = Path.ChangeExtension(filename, ".pdf");
                ff = FileFormat.PDF;
            }

            _document.SaveToFile(filename, ff);
        }

    }
}
