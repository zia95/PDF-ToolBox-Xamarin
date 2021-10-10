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
                await Shell.Current.GoToAsync($"{nameof(ToolSplitPage)}?{nameof(ToolSplitViewModel.PageType)}={ToolSplitViewModel.TypeSplit}");
            else if (item.Id == Tools.Ids.RemovePage)
                await Shell.Current.GoToAsync($"{nameof(ToolSplitPage)}?{nameof(ToolSplitViewModel.PageType)}={ToolSplitViewModel.TypeRemove}");
            //else if (item.Id == Tools.Ids.Watermark)
            //    await Shell.Current.GoToAsync($"{nameof(ToolSplitPage)}?{nameof(ToolSplitViewModel.PageType)}={ToolSplitViewModel.TypeWatermark}");
            else if(item.Id == Tools.Ids.Merge)
                await Shell.Current.GoToAsync($"{nameof(ToolMergePage)}?{nameof(ToolMergeViewModel.PageType)}={ToolMergeViewModel.TypeMerge}");
            //else if (item.Id == Tools.Ids.ImagesToPdf)
            //    await Shell.Current.GoToAsync($"{nameof(ToolMergePage)}?{nameof(ToolMergeViewModel.PageType)}={ToolMergeViewModel.TypeImagesToPdf}");
            else if (item.Id == Tools.Ids.Generated)
                await Shell.Current.GoToAsync($"{nameof(GeneratedPdfListPage)}");
            else if (item.Id == Tools.Ids.Lock)
                await Shell.Current.GoToAsync($"{nameof(ToolLockUnlockPdfPage)}?{nameof(ToolLockUnlockPdfViewModel.PageType)}={ToolLockUnlockPdfViewModel.TypeLock}");
            else if (item.Id == Tools.Ids.Unlock)
                await Shell.Current.GoToAsync($"{nameof(ToolLockUnlockPdfPage)}?{nameof(ToolLockUnlockPdfViewModel.PageType)}={ToolLockUnlockPdfViewModel.TypeUnlock}");
            else if (item.Id == Tools.Ids.ViewInformation)
                await Shell.Current.GoToAsync($"{nameof(ToolViewPdfInfoPage)}");
            else
            {
                await Views.MessagePopup.ShowAsync("Not Supported", "This feature will be added soon.", "OK");
            }
        }
    }
}