using System;
using System.Collections.Generic;
using System.Text;

namespace PDF_ToolBox.Models
{
    public class MergeItem
    {
        public string Id { get; private set; }
        public string FileName { get; private set; }
        public string FilePath { get; private set; }

        public MergeItem(string name, string path)
        {
            this.Id = Guid.NewGuid().ToString();
            this.FilePath = path;
            this.FileName = name;
        }
        public MergeItem(Xamarin.Essentials.FileResult file)
        {
            this.Id = Guid.NewGuid().ToString();
            this.FilePath = file.FullPath;
            this.FileName = file.FileName;
        }
    }
}
