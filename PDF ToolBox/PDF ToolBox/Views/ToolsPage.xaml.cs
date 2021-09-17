using PDF_ToolBox.Models;
using PDF_ToolBox.ViewModels;
using PDF_ToolBox.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PDF_ToolBox.Views
{
    public partial class ItemsPage : ContentPage
    {
        ToolsViewModel _viewModel;

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new ToolsViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("ItemsPage->OnAppearing()");
            _viewModel.OnAppearing();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("ItemsPage->OnDisappearing()");
        }
    }
}