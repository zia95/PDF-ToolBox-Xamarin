using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;


namespace PDF_ToolBox.ViewModels
{
    [QueryProperty(nameof(PageType), nameof(PageType))]
    class ToolSplitViewModel : BaseViewModel
    {
        public const string TypeSplit = "split";
        public const string TypeRemove = "remove";


        public Command SelectPdfCommand { get; }
        public Command SplitRemovePdfCommand { get; }

        private string _pageType = TypeSplit;
        public string PageType
        {
            get => this._pageType;
            set
            {
                this._pageType = value;
            }
        }

        private string _pdf_file;
        public string PdfFile
        {
            get => _pdf_file;
            set => SetProperty(ref _pdf_file, value);
        }

        private string _out_pdf;
        public string OutputPdfFile
        {
            get => _out_pdf;
            set => SetProperty(ref _out_pdf, value);
        }

        private string _page_ranges;
        public string PageRanges
        {
            get => _page_ranges;
            set => SetProperty(ref _page_ranges, value);
        }

        private bool _merge_ranges_into_one = true;
        public bool MergeRangesIntoOne
        {
            get => _merge_ranges_into_one;
            set => SetProperty(ref _merge_ranges_into_one, value);
        }

        private bool _showmerge = false;
        public bool ShowMergeRanges { get => _showmerge; set { _showmerge = value; OnPropertyChanged(nameof(this.ShowMergeRanges)); } }




        public ToolSplitViewModel()
        {
            this.SelectPdfCommand = new Command(OnSelectPdfClicked);
            this.PdfFile = "No Pdf File Selected.";

            this.SplitRemovePdfCommand = new Command(OnSplitRemovePdfClicked);

            
        }

        public void OnAppearing()
        {
            this.Title = this.PageType == ToolSplitViewModel.TypeSplit ? "Split PDF" : "Remove Pages";
            this.ShowMergeRanges = this.PageType == ToolSplitViewModel.TypeSplit;

            if (this.ShowMergeRanges == false)
                this.MergeRangesIntoOne = true;
        }

        private async void OnSelectPdfClicked()
        {
            var res = await PDF.FileSystem.PickAndShowPdfAsync();
            if(res != null)
            {
                bool valid = PDF.ToolHelper.IsValidPdfFile(res.FullPath);

                if(valid)
                {
                    this.PdfFile = res.FullPath;
                }
                else
                {
                    await Views.MessagePopup.ShowAsync("Failed", $"Your document is not a valid pdf file or its not supported.", "OK");
                }
            }
            
        }

        private bool CheckIfOutPdfAlreadyExists(string outpdf)
        {
            foreach (var m in this.PageType == TypeSplit ? PDF.FileSystem.GetAllSplitPdfFiles() : PDF.FileSystem.GetAllOtherPdfFiles())
            {
                if (m.FileName == outpdf)
                {
                    return true;
                }
            }
            return false;
        }


        private async void OnSplitRemovePdfClicked()
        {
            //check all data.... if incorrect return after reporting...
            if (string.IsNullOrWhiteSpace(this.OutputPdfFile))
            {
                await Views.MessagePopup.ShowAsync("Output Pdf file", "Enter Output Pdf file name.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.PdfFile) || !System.IO.File.Exists(this.PdfFile))
            {
                await Views.MessagePopup.ShowAsync("Input Pdf file", "Select Pdf file.", "OK");
                return;
            }

            var e_ranges = PDF.ToolHelper.ParseRanges(this.PageRanges);
            if(e_ranges == null || e_ranges.Count() <= 0)
            {
                await Views.MessagePopup.ShowAsync("Failed", $"Failed to parse the ranges", "OK");
                return;
            }


            //get output pdf path 
            //and if merge is false check if filename doesnt have '.pdf' extension append it.
            string outfile = this.OutputPdfFile;

            if(this.MergeRangesIntoOne)
            {
                string extension = System.IO.Path.GetExtension(outfile);

                if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase) == false)
                {
                    outfile += ".pdf";
                }
            }


            //split/removepages pdf file

            var ranges = new List<PDF.ToolHelper.PageRange>(e_ranges).ToArray();
            PDF.PdfTaskExecutor executor = null;

            if (this.PageType == ToolSplitViewModel.TypeSplit)
            {
                executor = PDF.PdfTaskExecutor.DoTaskSplitPdf(this.PdfFile, outfile, this.MergeRangesIntoOne, ranges);
            }
            else if (this.PageType == ToolSplitViewModel.TypeRemove)
            {
                executor = PDF.PdfTaskExecutor.DoTaskRemovePagesFromPdf(this.PdfFile, outfile, ranges);
            }
            else
            {
                throw new NotImplementedException($"Type {this.PageType} is not implemented.");
            }
            

            //run ui and split
            if (this.CheckIfOutPdfAlreadyExists(outfile))
            {
                await Views.MessagePopup.ShowAsync("Overwrite",
                    "It seems like output file/dir with the same name already exists.\nDo you want to overwrite?", "Cancel", "Yes", null,
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
    }
}
