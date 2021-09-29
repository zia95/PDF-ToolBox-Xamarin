using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.ObjectModel;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Threading.Tasks;

using PDF_ToolBox.Models;

namespace PDF_ToolBox.ViewModels
{
    [QueryProperty(nameof(PdfFile), nameof(PdfFile))]
    [QueryProperty(nameof(PageType), nameof(PageType))]
    public class GeneratedPdfListViewModel : BaseViewModel
    {
        public const string TypeSplit = "split";
        public const string TypeRemove = "remove";
        public const string TypeMerge = "merge";
        public const string TypeSecurity = "security";

        public const string TypeOther = "other";//other = 'not split' and 'not merge'

        private const string DefaultPageType = TypeMerge;

        private string _pageType;
        public string PageType
        {
            get => this._pageType;
            set
            {
                this._pageType = value;
            }
        }
        public string PdfFile { get; set; }


        private PdfFile _selectedItem;

        public ObservableCollection<PdfFile> Items { get; }


        public Command MergePdfListCommand { get; }
        public Command SplitPdfListCommand { get; }
        public Command OtherPdfListCommand { get; }


        public Command LoadItemsCommand { get; }
        public Command<PdfFile> ItemTapped { get; }

        public Command<PdfFile> DeletePdfCommand { get; }

        public delegate void ScrollTo(int index, int groupIndex = -1, ScrollToPosition position = ScrollToPosition.MakeVisible, bool animate = true);
        public ScrollTo PdfListScrollTo;
        //public CollectionView PdfList { get; set; }

        public GeneratedPdfListViewModel()
        {
            Items = new ObservableCollection<PdfFile>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<PdfFile>(OnItemSelected);

            DeletePdfCommand = new Command<PdfFile>(OnDeletePdf);


            MergePdfListCommand = new Command(OnMergePdfList);
            SplitPdfListCommand = new Command(OnSplitPdfList);
            OtherPdfListCommand = new Command(OnOtherPdfList);
        }

        Xamarin.Forms.Color _itmcolor;
        async Task ExecuteLoadItemsCommand()
        {
            Misc.CrashReporting.Log("GeneratedPdfListViewModel->In ExecuteLoadItemsCommand()");
            IsBusy = true;

            try
            {
                Items.Clear();
                this._itmcolor = Color.LightBlue;
                var items = await GeneratedPdfsDataStore.GetItemsAsync(true);

                if (this.PageType != TypeSplit && this.PageType != TypeRemove && this.PageType != TypeMerge && this.PageType != TypeSecurity && this.PageType != TypeOther)
                    this.PageType = DefaultPageType;


                int itm_to_scroll_to = -1;

                foreach (var item in items)
                {
                    this._itmcolor = this._itmcolor == Color.LightBlue ? Color.White : Color.LightBlue;


                    if (this.PageType == TypeSplit && item.PdfType == TypeSplit)
                    {
                        item.ItemColor = this._itmcolor;
                        Items.Add(item);


                        if(item.Id == this.PdfFile) itm_to_scroll_to = Items.Count - 1;

                        if (item.SplitRanges != null)
                        {
                            foreach (var subitm in item.SplitRanges)
                            {
                                subitm.ItemColor = this._itmcolor;
                                Items.Add(subitm);

                                if (item.Id == this.PdfFile) itm_to_scroll_to = Items.Count - 1;
                            }
                        }
                        
                    }
                    else if(this.PageType == TypeMerge && item.PdfType == TypeMerge)
                    {
                        item.ItemColor = this._itmcolor;
                        Items.Add(item);

                        if (item.Id == this.PdfFile) itm_to_scroll_to = Items.Count - 1;
                    }
                    else if(this.PageType != TypeMerge && this.PageType != TypeSplit)
                    {
                        if(item.PdfType == TypeOther ||
                           item.PdfType == TypeSecurity || 
                           item.PdfType == TypeRemove)
                        {
                            item.ItemColor = this._itmcolor;
                            Items.Add(item);

                            if (item.Id == this.PdfFile) itm_to_scroll_to = Items.Count - 1;
                        }
                    }
                }
                if(itm_to_scroll_to != -1) 
                    this.PdfListScrollTo?.Invoke(itm_to_scroll_to, position: ScrollToPosition.Start);

                this.PdfFile = null;
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

        private async void OnMergePdfList()
        {
            this.PageType = TypeMerge;
            await ExecuteLoadItemsCommand();
        }
        private async void OnSplitPdfList()
        {
            this.PageType = TypeSplit;
            await ExecuteLoadItemsCommand();
        }
        private async void OnOtherPdfList()
        {
            this.PageType = TypeOther;
            await ExecuteLoadItemsCommand();
        }
        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
        }

        public PdfFile SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        private async void OnDeletePdf(PdfFile item)
        {
            Misc.CrashReporting.Log("GeneratedPdfListViewModel->In OnDeletePdf()");
            if(item != null)
            {
                await Views.MessagePopup.ShowAsync($"Delete {item.FileName}",
                    $"Are you sure you want to delete {item.FileName}?" + (item.SplitRanges?.Length > 0 ? $" It includes {item.SplitRanges.Length} sub documents." : ""), 
                    "Cancel", "Yes", this, 
                    async (sender, e) => 
                    {
                        if(item.SplitRanges?.Length > 0)
                        {
                            System.IO.Directory.Delete(item.Id, true);
                        }
                        else
                        {
                            System.IO.File.Delete(item.Id);
                        }
                        await ExecuteLoadItemsCommand();
                    });
            }
        }

        private async void OnItemSelected(PdfFile item)
        {
            Misc.CrashReporting.Log("GeneratedPdfListViewModel->In OnItemSelected()");
            if (item?.Id != null)
            {
                if (System.IO.File.Exists(item.Id))
                {
                    await Launcher.OpenAsync(new OpenFileRequest(item.FileName, new ReadOnlyFile(item.Id)));
                }
            }
            
        }
    }
}
