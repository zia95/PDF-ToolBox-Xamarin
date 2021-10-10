using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PDF_ToolBox.ViewModels
{
    class ToolViewPdfInfoViewModel : BaseViewModel
    {
        public Command SelectPdfCommand { get; }
        public Command SavePdfCommand { get; }

        //pdf input... output...............
        private string _pdf_file;
        public string PdfFile { get => _pdf_file; set => SetProperty(ref _pdf_file, value); }

        private string _out_pdf;
        public string OutputPdfFile { get => _out_pdf; set => SetProperty(ref _out_pdf, value); }

        private bool _enablesave;
        public bool EnableSave { get => _enablesave; set => SetProperty(ref _enablesave, value); }

        //pdf info............
        private string _pdftitle;
        public string PdfTitle { get => _pdftitle; set => SetProperty(ref _pdftitle, value); }


        private string _pdfauthor;
        public string PdfAuthor { get => _pdfauthor; set => SetProperty(ref _pdfauthor, value); }


        private string _pdfsubject;
        public string PdfSubject { get => _pdfsubject; set => SetProperty(ref _pdfsubject, value); }


        private string _pdfkeywords;
        public string PdfKeywords { get => _pdfkeywords; set => SetProperty(ref _pdfkeywords, value); }


        private string _pdfcreator;
        public string PdfCreator { get => _pdfcreator; set => SetProperty(ref _pdfcreator, value); }


        private string _pdfproducer;
        public string PdfProducer { get => _pdfproducer; set => SetProperty(ref _pdfproducer, value); }


        private string _pdfcreationdate;
        public string PdfCreationDate { get => _pdfcreationdate; set => SetProperty(ref _pdfcreationdate, value); }

        private string _pdfmodificationdate;
        public string PdfModificationDate { get => _pdfmodificationdate; set => SetProperty(ref _pdfmodificationdate, value); }

        
        private void LoadPdfInfo(PDF.ToolHelper.PdfInfo info)
        {
            var cdt = info.CreationDate;
            var mdt = info.ModificationDate;

            this.PdfTitle = info.Title;
            this.PdfAuthor = info.Author;
            this.PdfSubject = info.Subject;
            this.PdfKeywords = info.Keywords;
            this.PdfCreator = info.Creator;
            this.PdfProducer = info.Producer;

            this.PdfProducer = info.Creator;

            //PDFsharp x.xx.xxxx (www.pdfsharp.com) (Original: ORIGINAL_PRODUCER_NAME)
            if (info.Producer.StartsWith("PDFsharp", StringComparison.CurrentCultureIgnoreCase))
            {
                string token = "(Original:";
                int idx = info.Producer.IndexOf(token);
                if (idx >= 0)
                {
                    try
                    {
                        this.PdfProducer = info.Producer.Substring(idx + token.Length + 1, info.Producer.Length - (idx + token.Length + 2));
                    }
                    catch (ArgumentOutOfRangeException) { }
                }
            }

            this.PdfCreationDate = $"{cdt.ToShortDateString()} - {cdt.ToShortTimeString()}";
            this.PdfModificationDate = $"{mdt.ToShortDateString()} - {mdt.ToShortTimeString()}";
        }
        private PDF.ToolHelper.PdfInfo GetPdfInfo()
        {
            return new PDF.ToolHelper.PdfInfo()
            {
                Title = this.PdfTitle,
                Author = this.PdfAuthor,
                Subject = this.PdfSubject,
                Keywords = this.PdfKeywords,
                Creator = this.PdfCreator,
            };
        }

        public ToolViewPdfInfoViewModel()
        {
            this.SelectPdfCommand = new Command(OnSelectPdfClicked);
            this.PdfFile = "No Pdf File Selected.";

            this.SavePdfCommand = new Command(OnSavePdfClicked);
        }
        public void OnAppearing()
        {
            this.Title = "View Pdf Info";
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
                    PDF.ToolHelper.PdfInfo info = new PDF.ToolHelper.PdfInfo();
                    if(await PDF.ToolHelper.ReadPdfInfoAsync(this.PdfFile, info, null))
                    {
                        this.LoadPdfInfo(info);
                    }
                    else
                    {
                        await Views.MessagePopup.ShowAsync("Failed", $"Failed to read pdf file.", "OK");
                    }
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

        private async void OnSavePdfClicked()
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

            
            this.IsBusy = true;     //in case save button in spammed...
            this.EnableSave = false;//in case save button in spammed...

            PDF.ToolHelper.PdfInfo info = this.GetPdfInfo();

            //run ui and split
            if (this.CheckIfOutPdfAlreadyExists(outfile))
            {
                await Views.MessagePopup.ShowAsync("Overwrite",
                    "It seems like output file/dir with the same name already exists.\nDo you want to overwrite?", "Cancel", "Yes", null,
                    async (sender, e) =>
                    {
                        if (((Views.MessagePopup)sender).Result)
                        {
                            //perform write op...
                            bool res = await PDF.ToolHelper.WritePdfInfoAsync(this.PdfFile, outfile, info, null);
                            await Views.MessagePopup.ShowAsync(res ? "Sucessful" : "Failed",
                                res ? "All infomation saved sucessfully.\nDo you want to browse pdf?" : "Failed to write and save changes.", "Cancel", res ? "Yes" : null, null,
                                async (sender2, e2) =>
                                {
                                    if (((Views.MessagePopup)sender2).Result)
                                    {
                                        await Shell.Current.GoToAsync(
                                            $"{nameof(Views.GeneratedPdfListPage)}?" +
                                            $"{nameof(ViewModels.GeneratedPdfListViewModel.PageType)}={GeneratedPdfListViewModel.TypeViewPdfInfo}&" +
                                            $"{nameof(ViewModels.GeneratedPdfListViewModel.PdfFile)}={outfile}");
                                    }
                                    this.IsBusy = false;
                                    this.EnableSave = true;
                                });
                        }
                        else
                        {
                            this.IsBusy = false;
                            this.EnableSave = true;
                        }
                    });
            }
            else
            {
                //perform write op...
                bool res = await PDF.ToolHelper.WritePdfInfoAsync(this.PdfFile, outfile, info, null);
                await Views.MessagePopup.ShowAsync(res ? "Sucessful" : "Failed",
                    res ? "All infomation saved sucessfully.\nDo you want to browse pdf?" : "Failed to write and save changes.", "Cancel", res ? "Yes" : null, null,
                    async (sender2, e2) =>
                    {
                        if (((Views.MessagePopup)sender2).Result)
                        {
                            await Shell.Current.GoToAsync(
                                $"{nameof(Views.GeneratedPdfListPage)}?" +
                                $"{nameof(ViewModels.GeneratedPdfListViewModel.PageType)}={GeneratedPdfListViewModel.TypeViewPdfInfo}&" +
                                $"{nameof(ViewModels.GeneratedPdfListViewModel.PdfFile)}={outfile}");
                        }
                        this.IsBusy = false;
                        this.EnableSave = true;
                    });
            }
            
        }
    }
}
