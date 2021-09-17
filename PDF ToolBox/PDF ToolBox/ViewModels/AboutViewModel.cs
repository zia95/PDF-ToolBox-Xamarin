using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PDF_ToolBox.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public ICommand OpenStoreCommand { get; }
        public ICommand OpenAppCommand { get; }

        public AboutViewModel()
        {
            Title = "About";

            OpenStoreCommand = new Command(async () => await Browser.OpenAsync(Properties.Resources.StoreLink));
            OpenAppCommand = new Command(async () => await Browser.OpenAsync(Properties.Resources.AppLink));
        }

        
    }
}