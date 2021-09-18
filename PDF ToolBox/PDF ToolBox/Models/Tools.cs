using System;

namespace PDF_ToolBox.Models
{
    public class Tools
    {
        public enum Ids
        {
            Generated,
            Split,
            Merge,
            RemovePage,
            Lock,
            Unlock,
            Watermark,
            Compress,
            ImagesToPdf,
            ViewInformation,
        }
        public Ids Id { get; set; }
        public Xamarin.Forms.ImageSource Image { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
    }
}