using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;


namespace PDF_ToolBox.ViewModels
{
    class ToolSplitViewModel : BaseViewModel
    {
        public Command SelectPdfCommand { get; }
        public Command SplitPdfCommand { get; }

        private readonly string pdf_split_dir = PDF.FileSystem.GetSplitPdfOutDir();

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

        private string _split_ranges;
        public string SplitRanges
        {
            get => _split_ranges;
            set => SetProperty(ref _split_ranges, value);
        }

        private bool _merge_ranges_into_one = true;
        public bool MergeRangesIntoOne
        {
            get => _merge_ranges_into_one;
            set => SetProperty(ref _merge_ranges_into_one, value);
        }




        public ToolSplitViewModel()
        {
            this.SelectPdfCommand = new Command(OnSelectPdfClicked);
            this.PdfFile = "No Pdf File Selected.";

            this.SplitPdfCommand = new Command(OnSplitPdfClicked);
        }

        private async void OnSelectPdfClicked(object obj)
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

        private bool CheckIfSplitOutPdfAlreadyExists(string outpdf)
        {
            foreach (var m in PDF.FileSystem.GetAllSplitPdfFiles())
            {
                if (m.Id == outpdf)
                {
                    return true;
                }
            }
            return false;
        }


        private async void OnSplitPdfClicked(object obj)
        {
            //check all data.... if incorrect return after reporting...
            if (string.IsNullOrWhiteSpace(this.OutputPdfFile))
            {
                await Views.MessagePopup.ShowAsync("Output Pdf file", "Enter Output Pdf file name.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.PdfFile) || !System.IO.File.Exists(this.PdfFile))
            {
                await Views.MessagePopup.ShowAsync("Input Pdf file", "Select Pdf file to split.", "OK");
                return;
            }

            var e_ranges = PDF.ToolHelper.ParseRanges(this.SplitRanges);
            if(e_ranges == null || e_ranges.Count() <= 0)
            {
                await Views.MessagePopup.ShowAsync("Failed", $"Failed to parse the ranges", "OK");
                return;
            }


            //get output pdf path 
            //and if merge is false check if filename doesnt have '.pdf' extension append it.
            string outfile = System.IO.Path.Combine(this.pdf_split_dir, this.OutputPdfFile);

            if(this.MergeRangesIntoOne)
            {
                string extension = System.IO.Path.GetExtension(outfile);

                if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase) == false)
                {
                    outfile += ".pdf";
                }
            }


            //split pdf file

            var ranges = new List<PDF.ToolHelper.PageRange>(e_ranges).ToArray();

            var executor = new PDF.PdfTaskExecutor(this.PdfFile, outfile, this.MergeRangesIntoOne, ranges);

            //run ui and split
            if (this.CheckIfSplitOutPdfAlreadyExists(outfile))
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
