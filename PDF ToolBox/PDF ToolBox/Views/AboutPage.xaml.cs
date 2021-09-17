using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PDF_ToolBox.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Misc.CrashReporting.Log("AboutPage->OnAppearing()");
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Misc.CrashReporting.Log("AboutPage->OnDisappearing()");
        }
    }
}