using PDF_ToolBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PDF_ToolBox.Services
{
    public class ToolsDataStore : IDataStore<Tools>
    {
        readonly List<Tools> items;

        public ToolsDataStore()
        {
            items = new List<Tools>()
            {
                new Tools { Id = Tools.Ids.Generated, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_savedpdfs.png", typeof(ToolsDataStore).Assembly), Text = "Generated Pdfs", Description="Output pdf files." },
                new Tools { Id = Tools.Ids.Split, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_split.png", typeof(ToolsDataStore).Assembly), Text = "Split", Description="Split pdf file." },
                new Tools { Id = Tools.Ids.Merge, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_merge.png", typeof(ToolsDataStore).Assembly), Text = "Merge", Description="Merge pdf file." },
                new Tools { Id = Tools.Ids.RemovePage, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_remove.png", typeof(ToolsDataStore).Assembly), Text = "Remove Page(s)", Description="Remove pages from pdf file." },
                new Tools { Id = Tools.Ids.Lock, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_lock.png", typeof(ToolsDataStore).Assembly), Text = "Lock", Description="Lock pdf file." },
                new Tools { Id = Tools.Ids.Unlock, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_unlock.png", typeof(ToolsDataStore).Assembly), Text = "Unlock", Description="Remove password from pdf file." },
                new Tools { Id = Tools.Ids.Watermark, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_compress.png", typeof(ToolsDataStore).Assembly), Text = "Watermark", Description="Add watermark on pdf file." },
                new Tools { Id = Tools.Ids.Compress, Image = ImageSource.FromResource("PDF_ToolBox.Resources.Images.tool_compress.png", typeof(ToolsDataStore).Assembly), Text = "Compress", Description="Compress pdf file." }
            };
        }

        public async Task<bool> AddItemAsync(Tools item)
        {
            items.Add(item);
            
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(Tools item)
        {
            var oldItem = items.Where((Tools arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            if(int.TryParse(id, out int iid))
            {
                var oldItem = items.Where((Tools arg) => arg.Id == (Tools.Ids)iid).FirstOrDefault();
                items.Remove(oldItem);

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        public async Task<Tools> GetItemAsync(string id)
        {
            if (int.TryParse(id, out int iid))
            {
                return await Task.FromResult(items.FirstOrDefault(s => s.Id == (Tools.Ids)iid));
            }
            return await Task.FromResult((Tools)null);
        }

        public async Task<IEnumerable<Tools>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(items);
        }
    }
}