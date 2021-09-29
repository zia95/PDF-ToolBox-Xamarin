using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;

using System.Linq;
using System.Threading.Tasks;
using PDF_ToolBox.Models;

namespace PDF_ToolBox.Services
{
    class GeneratedPdfFilesDataStore : IDataStore<PdfFile>
    {
        readonly List<PdfFile> items;

        public GeneratedPdfFilesDataStore()
        {
            items = new List<PdfFile>();
        }
        private void force_refresh()
        {
            items.Clear();

            string[] all_types = new string[] 
            { 
                ViewModels.GeneratedPdfListViewModel.TypeSplit, 
                ViewModels.GeneratedPdfListViewModel.TypeMerge, 
                ViewModels.GeneratedPdfListViewModel.TypeOther 
            };

            foreach(var ty in all_types)
            {
                foreach (var m in PDF.FileSystem.GetAllPdfFilesOfType(ty))
                {
                    items.Add(m);
                }
            }
        }

        public async Task<bool> AddItemAsync(PdfFile item)
        {
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(PdfFile item)
        {
            var oldItem = items.Where((PdfFile arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = items.Where((PdfFile arg) => arg.Id == id).FirstOrDefault();
            items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<PdfFile> GetItemAsync(string id)
        {
            return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<PdfFile>> GetItemsAsync(bool forceRefresh = false)
        {
            if(forceRefresh)
            {
                this.force_refresh();
            }
            return await Task.FromResult(items);
        }
    }
}
