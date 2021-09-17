using System;
using System.Collections.Generic;
using System.Text;

namespace PDF_ToolBox.Models
{
    public class PdfFile
    {
        public string Id { get; set; }

        public string PdfType { get; set; }
        public string RelativePath { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public PdfFile[] SplitRanges { get; set; }

        public Xamarin.Forms.Color ItemColor { get; set; }
    }
}
