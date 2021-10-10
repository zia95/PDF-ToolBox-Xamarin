using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PDF_ToolBox.ViewModels
{
    [QueryProperty(nameof(PageType), nameof(PageType))]
    class ToolLockUnlockPdfViewModel : BaseViewModel
    {
        public const string TypeLock = "lock";
        public const string TypeUnlock = "unlock";

        public Command SelectPdfCommand { get; }
        public Command StartPdfCommand { get; }

        private string _pageType = TypeLock;
        public string PageType { get => this._pageType; set => this._pageType = value; }

        //pdf input... output...............
        private string _pdf_file;
        public string PdfFile { get => _pdf_file; set => SetProperty(ref _pdf_file, value); }

        private string _out_pdf;
        public string OutputPdfFile { get => _out_pdf; set => SetProperty(ref _out_pdf, value); }


        private string _password;
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        public ToolLockUnlockPdfViewModel()
        {
            this.SelectPdfCommand = new Command(OnSelectPdfClicked);
            this.PdfFile = "No Pdf File Selected.";

            this.StartPdfCommand = new Command(OnStartPdfClicked);
        }

        public void OnAppearing()
        {
            if (this.PageType == ToolLockUnlockPdfViewModel.TypeLock)
            {
                this.Title = "Lock PDF";
            }
            else if (this.PageType == ToolLockUnlockPdfViewModel.TypeUnlock)
            {
                this.Title = "Unlock Pages";
            }
        }

        private async void OnSelectPdfClicked()
        {
            var res = await PDF.FileSystem.PickAndShowPdfAsync();
            if (res != null)
            {
                bool valid = PDF.ToolHelper.IsValidPdfFile(res.FullPath);

                if (valid)
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
            foreach (var m in PDF.FileSystem.GetAllOtherPdfFiles())
            {
                if (m.FileName == outpdf)
                {
                    return true;
                }
            }
            return false;
        }

        private async void OnStartPdfClicked()
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

            //get output pdf path 
            //and if merge is false check if filename doesnt have '.pdf' extension append it.
            string outfile = this.OutputPdfFile;
            string extension = System.IO.Path.GetExtension(outfile);

            if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                outfile += ".pdf";
            }

            //check password
            if (string.IsNullOrWhiteSpace(this.Password))
            {
                await Views.MessagePopup.ShowAsync("Failed", "Enter password", "OK");
                return;
            }


            //lock/unlock pdf file
            PDF.PdfTaskExecutor executor = null;

            if (this.PageType == TypeLock)
            {
                executor = PDF.PdfTaskExecutor.DoTaskLockOrUnlockPdf(this.PdfFile, outfile, this.Password, true);
            }
            else if (this.PageType == TypeUnlock)
            {
                executor = PDF.PdfTaskExecutor.DoTaskLockOrUnlockPdf(this.PdfFile, outfile, this.Password, false);
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
