using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PDF_ToolBox.ViewModels;

namespace PDF_ToolBox.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ToolSplitPage : ContentPage
    {
        private ToolSplitViewModel _viewmodel = null;
        public ToolSplitPage()
        {
            InitializeComponent();

            BindingContext = _viewmodel = new ToolSplitViewModel();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("ToolSplitPage->OnAppearing()");

            this._viewmodel.OnAppearing();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("ToolSplitPage->OnDisappearing()");
        }
    }
}