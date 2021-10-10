using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PDF_ToolBox.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ToolLockUnlockPdfPage : ContentPage
    {
        PDF_ToolBox.ViewModels.ToolLockUnlockPdfViewModel _viewmodel = null;
        public ToolLockUnlockPdfPage()
        {
            InitializeComponent();

            this.BindingContext = _viewmodel = new ViewModels.ToolLockUnlockPdfViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("ToolLockUnlockPdfViewModel->OnAppearing()");

            this._viewmodel.OnAppearing();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("ToolLockUnlockPdfViewModel->OnDisappearing()");
        }
    }
}