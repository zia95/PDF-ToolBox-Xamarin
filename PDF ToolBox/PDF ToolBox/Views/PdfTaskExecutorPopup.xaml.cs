using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Rg.Plugins.Popup;
using Rg.Plugins.Popup.Pages;

namespace PDF_ToolBox.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PdfTaskExecutorPopup : PopupPage
    {
        private string _message = null;
        private string _cancel = null;
        private string _accept = null;
        private float _progress = 0.0f;
        private bool _showprogress = true;
        private bool _showaccept = false;

        public float Progress       { get => _progress;         set { _progress = value;       OnPropertyChanged(nameof(this.Progress)); } }
        public bool ShowProgress    { get => _showprogress;     set { _showprogress = value;   OnPropertyChanged(nameof(this.ShowProgress)); } }
        public string Message       { get => _message;          set { _message = value;        OnPropertyChanged(nameof(this.Message)); } }
        public string Cancel        { get => _cancel;           set { _cancel = value;         OnPropertyChanged(nameof(this.Cancel)); } }
        public string Accept        { get => _accept;           set { _accept = value;         OnPropertyChanged(nameof(this.Accept));  } }
        public bool ShowAccept     { get => _showaccept;       set { _showaccept = value;     OnPropertyChanged(nameof(this.ShowAccept)); } }


        private bool _canExit = true;
        private bool _cancelButtonPressed = false;
        private PDF.ToolHelper.PdfProgressEventArgs _tracker = null;

        public bool Result { get { return this._tracker != null ? this._tracker.Sucessful : false; } }
        public object Tag { get; set; }
        public event EventHandler OnResult;

        private PDF.PdfTaskExecutor _executor;
        public PdfTaskExecutorPopup(PDF.PdfTaskExecutor executor, object tag = null, EventHandler on_result = null)
        {
            InitializeComponent();

            this.BindingContext = this;

            this.Cancel = "Cancel";
            this.Accept = "Open";

            this._executor = executor;

            this.Tag = tag;
            this.OnResult += on_result;


            this.ShowAccept = false;
        }
        /// <summary>
        /// show ui and perform pdf action
        /// </summary>
        /// <param name="executor">pdf executor which will perform pdf action</param>
        /// <param name="tag">any object you want to init to popup tag to access later on result </param>
        /// <param name="on_result">when user cancels or accepts the dialog</param>
        /// <returns>popup opject</returns>
        public static async Task<PdfTaskExecutorPopup> ShowAsync(PDF.PdfTaskExecutor executor, object tag = null, EventHandler on_result = null)
        {
            var exe = new PdfTaskExecutorPopup(executor, tag, on_result);
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(exe);

            return exe;
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            this._cancelButtonPressed = true;

            if(this._tracker?.Finished == true)
            {
                Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }
        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(Views.GeneratedPdfListPage)}?" +
                $"{nameof(ViewModels.GeneratedPdfListViewModel.PageType)}={this._executor.TaskType}&" +
                $"{nameof(ViewModels.GeneratedPdfListViewModel.PdfFile)}={this._executor.OutputFile}");


            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
        }



        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("PdfTaskExecutorPopup->OnAppearing()");

            this._canExit = false;

            this.ShowAccept = false;

            this.Progress = 0;
            this.Message = $"Progress: {this.Progress.ToString("0.00")}%";

            if (this._executor.TaskType == ViewModels.GeneratedPdfListViewModel.TypeSecurity)
                this.ShowProgress = false;

            await this._executor.ExecuteAsync(tracker);
        }
        private bool tracker(object sender, PDF.ToolHelper.PdfProgressEventArgs e)
        {
            this._tracker = e;
            this.Title = $"Pdf is being {this._executor.TaskType}";

            
            if (!this._tracker.Finished)
            {
                this.Progress = this._tracker.Progress / 100;

                if (!this._cancelButtonPressed)
                {
                    this.Message = $"Progress: {this._tracker.Progress.ToString("0.00")}%";
                }
                else
                {
                    this.Message = $"Please wait... cleaning up...";
                    return false;
                }
            }
            else
            {
                this._canExit = true;

                this.Progress = 1;

                if (this._tracker.Sucessful)
                {
                    this.Message = $"All done...\nFile(s): {this._executor.OutputFile}";
                    this.ShowAccept = true;
                }
                else
                {
                    this.Message = this._tracker.ErrorMessage;
                    if(this._cancelButtonPressed)
                    {
                        this.OnCancelClicked(this, EventArgs.Empty);
                    }
                }
            }
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("PdfTaskExecutorPopup->OnDisappearing()");
            this.OnResult?.Invoke(this, EventArgs.Empty);
        }

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            // Return true if you don't want to close this popup page when a back button is pressed
            return !this._canExit;
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            // Return false if you don't want to close this popup page when a background of the popup page is clicked
            return this._canExit;
        }

    }
}