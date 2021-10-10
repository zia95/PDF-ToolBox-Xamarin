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
    public partial class ToolViewPdfInfoPage : ContentPage
    {
        PDF_ToolBox.ViewModels.ToolViewPdfInfoViewModel _viewmodel = null;
        public ToolViewPdfInfoPage()
        {
            InitializeComponent();

            this.BindingContext = _viewmodel = new ViewModels.ToolViewPdfInfoViewModel();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("ToolViewPdfInfoPage->OnAppearing()");

            this._viewmodel.OnAppearing();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("ToolViewPdfInfoPage->OnDisappearing()");
        }
    }
}