using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;
using System.Linq;

namespace PDF_ToolBox.ViewModels
{
    class ToolMergeViewModel : BaseViewModel
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
        private readonly string pdf_merge_dir = PDF.FileSystem.GetMergePdfOutDir();


        private string _out_pdf;
        public string OutputPdfFile
        {
            get => _out_pdf;
            set => SetProperty(ref _out_pdf, value);
        }

        public ObservableCollection<MergeItem> Items { get; }


        public Command MergeItemsCommand { get; }
        public Command AddItemCommand { get; }
        public Command<MergeItem> ItemTapped { get; }

        public ToolMergeViewModel()
        {
            Items = new ObservableCollection<MergeItem>();

            this.ItemTapped = new Command<MergeItem>(OnItemSelected);

            this.AddItemCommand = new Command(OnAddItem);
            this.MergeItemsCommand = new Command(OnMergeItem);
            
        }

        private async void OnAddItem()
        {
            var files = await PDF.FileSystem.PickAndShowMultiAsync();
            if(files != null)
            {
                foreach (var f in files)
                {
                    if (PDF.ToolHelper.IsValidPdfFile(f.FullPath))
                    {
                        this.Items.Add(new MergeItem(f));
                    }
                    else
                    {
                        await Views.MessagePopup.ShowAsync("Failed", $"Failed to add item to merge because its invalid or not supported.\nItem: {f.FullPath}", "OK");
                    }
                }
            }
        }
        private bool CheckIfMergeOutPdfAlreadyExists(string outpdf)
        {
            foreach (var m in PDF.FileSystem.GetAllMergePdfFiles())
            {
                if(m.Id == outpdf)
                {
                    return true;
                }
            }
            return false;
        }
        private async void OnMergeItem()
        {
            //check params and return after reporting
            if(this.Items.Count < 1)
            {
                await Views.MessagePopup.ShowAsync("Error", "Need at least 2 pdfs to merge.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.OutputPdfFile))
            {
                await Views.MessagePopup.ShowAsync("Output pdf file", "Enter Output Pdf file name.", "OK");
                return;
            }


            //check out file has .pdf extension if not append it.
            //setup all the args....
            string outfile = System.IO.Path.Combine(this.pdf_merge_dir, this.OutputPdfFile);
            string extension = System.IO.Path.GetExtension(outfile);

            if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                outfile += ".pdf";
            }

            var executor = new PDF.PdfTaskExecutor(this.Items.Select(x => { return x.FilePath; }).ToArray(), outfile);

            //merge docs.....
            if (this.CheckIfMergeOutPdfAlreadyExists(outfile))
            {
                await Views.MessagePopup.ShowAsync("Overwrite",
                    "It seems like output file with the same name already exists.\nDo you want to overwrite?", "Cancel", "Yes", null,
                    async (sender, e) =>
                    {
                        if (((Views.MessagePopup)sender).Result)
                        {
                            await Views.PdfTaskExecutorPopup.ShowAsync(executor);
                        }
                    });
            }
            else
            {
                await Views.PdfTaskExecutorPopup.ShowAsync(executor);
            }
        }
        
        private async void OnItemSelected(MergeItem item)
        {
            if(item != null)
            {
                await Views.MessagePopup.ShowAsync(item.FileName, $"Do you want to remove '{item.FileName}' from merge list?", "No", "Yes", this, 
                    (sender, e) => 
                    {
                        Views.MessagePopup msg = (Views.MessagePopup)sender;
                        if(msg.Result)
                        {
                            this.Items.Remove(item);
                        }
                    });
            }
            
        }
    }
}
