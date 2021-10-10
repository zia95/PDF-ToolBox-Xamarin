using PDF_ToolBox.ViewModels;
using PDF_ToolBox.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace PDF_ToolBox
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(ToolSplitPage), typeof(ToolSplitPage));
            Routing.RegisterRoute(nameof(ToolMergePage), typeof(ToolMergePage));

            Routing.RegisterRoute(nameof(ToolLockUnlockPdfPage), typeof(ToolLockUnlockPdfPage));
            Routing.RegisterRoute(nameof(ToolViewPdfInfoPage), typeof(ToolViewPdfInfoPage));


            Routing.RegisterRoute(nameof(GeneratedPdfListPage), typeof(GeneratedPdfListPage));
        }

        private async void OnOutputPdfClicked(object sender, EventArgs e)
        {
            //string dir = PDF.FileSystem.GetPdfOutDirectory(PDF.FileSystem.PdfOutDir.Base) + "/split/out.pdf";

            //await Xamarin.Essentials.Launcher.OpenAsync(new Xamarin.Essentials.OpenFileRequest { File = new Xamarin.Essentials.ReadOnlyFile(dir) });

            //var res = await Xamarin.Essentials.Launcher.TryOpenAsync(new Xamarin.Essentials.OpenFileRequest { File = new Xamarin.Essentials.ReadOnlyFile(dir) });
            //if(!res)
            //{
            //    await DisplayAlert("Failed", $"Unexpected error while trying to open output directory. Please manually navigate to {dir}", "OK");
            //}
        }
    }
}
