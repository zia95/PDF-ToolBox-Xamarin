using PDF_ToolBox.Models;
using PDF_ToolBox.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PDF_ToolBox.ViewModels
{
    public class ToolsViewModel : BaseViewModel
    {
        private Tools _selectedItem;

        public ObservableCollection<Tools> Items { get; }
        public Command LoadItemsCommand { get; }
        public Command<Tools> ItemTapped { get; }

        public ToolsViewModel()
        {
            Title = "Tools";
            Items = new ObservableCollection<Tools>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<Tools>(OnItemSelected);
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await ToolsDataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
            await this.ExecuteLoadItemsCommand();
        }

        public Tools SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        async void OnItemSelected(Tools item)
        {
            if (item == null)
                return;

            if(item.Id == Tools.Ids.Split)
                await Shell.Current.GoToAsync($"{nameof(ToolSplitPage)}");
            else if(item.Id == Tools.Ids.Merge)
                await Shell.Current.GoToAsync($"{nameof(ToolMergePage)}");
            else if (item.Id == Tools.Ids.Generated)
                await Shell.Current.GoToAsync($"{nameof(GeneratedPdfListPage)}");
            else if (item.Id == Tools.Ids.Lock || item.Id == Tools.Ids.Unlock)
            {
                var file = await PDF.FileSystem.PickAndShowPdfAsync();
                if(file != null)
                {
                    if(PDF.ToolHelper.IsValidPdfFile(file.FullPath))
                    {
                        await Views.InputPopup.ShowAsync("File Name", Keyboard.Default, "File Name", $"Enter new file name for {item.Id} file", "Cancel", "OK", this,
                            async (ss, ee) => {
                                if (((InputPopup)ss).Result)
                                {
                                    await Views.InputPopup.ShowAsync("Lock/Unlock", Keyboard.Default, "Password", $"Enter Password to {item.Id} Pdf.", "Cancel", "OK", this,
                                    async (sender, e) => {
                                        if (((InputPopup)sender).Result)
                                        {
                                            string infile = file.FullPath;
                                            string outfile = ((InputPopup)ss).Input;
                                            string password = ((InputPopup)sender).Input;


                                            string extension = System.IO.Path.GetExtension(outfile);
                                            if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase) == false)
                                            {
                                                outfile += ".pdf";
                                            }

                                            string outfilepath = PDF.FileSystem.GetSecurityPdfOutDir();
                                            outfile = System.IO.Path.Combine(outfilepath, outfile);


                                            var exe = new PDF.PdfTaskExecutor(infile, outfile, password, item.Id == Tools.Ids.Lock);

                                            await PdfTaskExecutorPopup.ShowAsync(exe);
                                        }
                                    });
                                }
                                
                            });

                        
                    }
                    else
                    {
                        await Views.MessagePopup.ShowAsync("Invalid File", "Pdf File in invalid or not supported", "OK");
                    }
                }
            }
            else
            {
                await Views.MessagePopup.ShowAsync("Not Supported", "This feature will be added soon.", "OK");
            }
        }
    }
}