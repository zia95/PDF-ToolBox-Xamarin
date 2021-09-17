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
    public partial class GeneratedPdfListPage : ContentPage
    {
        GeneratedPdfListViewModel _viewModel;
        public GeneratedPdfListPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new GeneratedPdfListViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("GeneratedPdfListPage->OnAppearing()");
            _viewModel.PdfListScrollTo = ItemsListView.ScrollTo;
            _viewModel.OnAppearing();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("GeneratedPdfListPage->OnDisappearing()");
        }
    }
}