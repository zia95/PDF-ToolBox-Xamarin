using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;
using System.Linq;
using PDF_ToolBox.Models;

namespace PDF_ToolBox.ViewModels
{
    [QueryProperty(nameof(PageType), nameof(PageType))]
    class ToolMergeViewModel : BaseViewModel
    {
        public const string TypeMerge = "split";
        public const string TypeImagesToPdf = "imagestopdf";

        private string _pageType = TypeMerge;
        public string PageType
        {
            get => this._pageType;
            set
            {
                this._pageType = value;
            }
        }
        
        private string _out_pdf;
        public string OutputPdfFile
        {
            get => _out_pdf;
            set => SetProperty(ref _out_pdf, value);
        }

        public ObservableCollection<MergeItem> Items { get; }


        public Command StartPdfCommand { get; }
        public Command AddItemCommand { get; }
        public Command AddMultipleItemsCommand { get; }
        public Command<MergeItem> ItemTapped { get; }

        public ToolMergeViewModel()
        {
            Items = new ObservableCollection<MergeItem>();

            this.ItemTapped = new Command<MergeItem>(OnItemSelected);

            this.AddItemCommand = new Command(OnAddItemClicked);
            this.AddMultipleItemsCommand = new Command(OnAddMultipleItemsClicked);
            this.StartPdfCommand = new Command(OnStartPdfClicked);
            
        }
        public void OnAppearing()
        {
            if (this.PageType == ToolMergeViewModel.TypeMerge)
            {
                this.Title = "Merge PDF";
            }
            else if (this.PageType == ToolMergeViewModel.TypeImagesToPdf)
            {
                this.Title = "Images To PDF";
            }
        }

        private async void OnAddItemClicked()
        {
            var files = this.PageType == ToolMergeViewModel.TypeMerge ? await PDF.FileSystem.PickAndShowPdfMultiAsync() : await PDF.FileSystem.PickAndShowJpegMultiAsync();

            if(files != null)
            {
                foreach (var f in files)
                {
                    if (this.PageType == ToolMergeViewModel.TypeMerge ? PDF.ToolHelper.IsValidPdfFile(f.FullPath) : PDF.ToolHelper.IsValidImageFile(f.FullPath))
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
        private async void OnAddMultipleItemsClicked()
        {
        }
        private bool CheckIfOutPdfAlreadyExists(string outpdf)
        {
            foreach (var m in this.PageType == TypeMerge ? PDF.FileSystem.GetAllMergePdfFiles() : PDF.FileSystem.GetAllOtherPdfFiles())
            {
                if(m.FileName == outpdf)
                {
                    return true;
                }
            }
            return false;
        }
        private async void OnStartPdfClicked()
        {
            //check params and return after reporting
            if(this.PageType == ToolMergeViewModel.TypeMerge && this.Items.Count < 1)
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
            string outfile = this.OutputPdfFile;
            string extension = System.IO.Path.GetExtension(outfile);

            if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                outfile += ".pdf";
            }

            string[] infiles = this.Items.Select(x => { return x.FilePath; }).ToArray();
            
            PDF.PdfTaskExecutor executor = null;


            if (this.PageType == ToolMergeViewModel.TypeMerge)
            {
                executor = PDF.PdfTaskExecutor.DoTaskMergePdf(infiles, outfile);
            }
            else if(this.PageType == ToolMergeViewModel.TypeImagesToPdf)
            {
                executor = PDF.PdfTaskExecutor.DoTaskImagesToPdf(infiles, outfile);
            }
            else
            {
                throw new NotImplementedException($"Type {this.PageType} is not implemented.");
            }

            //merge docs/images.....
            if (this.CheckIfOutPdfAlreadyExists(outfile))
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
                await Views.MessagePopup.ShowAsync(item.FileName, $"Do you want to remove '{item.FileName}' from list?", "No", "Yes", this, 
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
