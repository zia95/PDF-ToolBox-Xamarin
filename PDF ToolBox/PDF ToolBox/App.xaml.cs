using PDF_ToolBox.Services;
using PDF_ToolBox.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PDF_ToolBox.Misc;

namespace PDF_ToolBox
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();


            CrashReporting.Enable();

            string dev_id = DependencyService.Get<Services.IFileSystemHelper>().GetDeviceId();

            if (dev_id != null)
                CrashReporting.SetUserId(dev_id);


            DependencyService.Register<ToolsDataStore>();
            DependencyService.Register<GeneratedPdfFilesDataStore>();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
