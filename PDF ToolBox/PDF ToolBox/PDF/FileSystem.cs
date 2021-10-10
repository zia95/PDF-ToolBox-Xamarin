using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;

using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.IO;

namespace PDF_ToolBox.PDF
{
    public static class FileSystem
    {
        public static async Task<FileResult> PickAndShowPdfAsync(string title = "Select Pdf File") => await PickAndShowAsync(
            new PickOptions() { PickerTitle = title, FileTypes = FilePickerFileType.Pdf }
            );

        public static async Task<IEnumerable<FileResult>> PickAndShowPdfMultiAsync(string title = "Select Pdf Files") => await PickAndShowMultiAsync(
            new PickOptions() { PickerTitle = title, FileTypes = FilePickerFileType.Pdf }
            );

        public static async Task<FileResult> PickAndShowJpegAsync(string title = "Select Jpeg File") => await PickAndShowAsync(
            new PickOptions() { PickerTitle = title, FileTypes = FilePickerFileType.Images }
            );

        public static async Task<IEnumerable<FileResult>> PickAndShowJpegMultiAsync(string title = "Select Jpeg Files") => await PickAndShowMultiAsync(
            new PickOptions() { PickerTitle = title, FileTypes = FilePickerFileType.Images }
            );



        public static async Task<FileResult> PickAndShowAsync(PickOptions options) => await FilePicker.PickAsync(options);
        public static async Task<IEnumerable<FileResult>> PickAndShowMultiAsync(PickOptions options) => await FilePicker.PickMultipleAsync(options);

        //public static bool FileExist(string file) => File.Exists(file);


        public const string PdfOutputBaseDir = "pdfout";
        public static string GetPdfOutDir(string dirtype)
        {
            string app_storage_dir = DependencyService.Get<Services.IFileSystemHelper>().GetAppExternalStorage();


            var d = Directory.CreateDirectory(Path.Combine(app_storage_dir, PdfOutputBaseDir));

            if (dirtype == ViewModels.GeneratedPdfListViewModel.TypeSplit ||
                dirtype == ViewModels.GeneratedPdfListViewModel.TypeMerge ||
                dirtype == ViewModels.GeneratedPdfListViewModel.TypeSecurity ||
                dirtype == ViewModels.GeneratedPdfListViewModel.TypeOther)
            {
                return Directory.CreateDirectory(Path.Combine(d.FullName, dirtype)).FullName;
            }

            throw new NotSupportedException($"type '{dirtype}' is not supported.");
        }
        public static string GetSplitPdfOutDir() => GetPdfOutDir(ViewModels.GeneratedPdfListViewModel.TypeSplit);
        public static string GetMergePdfOutDir() => GetPdfOutDir(ViewModels.GeneratedPdfListViewModel.TypeMerge);
        public static string GetOtherPdfOutDir() => GetPdfOutDir(ViewModels.GeneratedPdfListViewModel.TypeOther);

        public static IEnumerable<Models.PdfFile> GetAllSplitPdfFiles()
        {
            string pdf_split = PDF.FileSystem.GetPdfOutDir(ViewModels.GeneratedPdfListViewModel.TypeSplit);

            DirectoryInfo dir = new DirectoryInfo(pdf_split);
            if (dir.Exists)
            {
                //single split pdf files
                foreach (FileInfo f in dir.EnumerateFiles())
                {
                    Models.PdfFile p = new Models.PdfFile();
                    p.Id = f.FullName;
                    p.FileName = f.Name;
                    p.FilePath = f.DirectoryName;
                    p.RelativePath = $"{dir.Name}/{p.FileName}";
                    p.PdfType = ViewModels.GeneratedPdfListViewModel.TypeSplit;
                    p.SplitRanges = null;

                    yield return p;
                }
                //pdf file which were split in multiple ranges pdf...
                foreach (DirectoryInfo sd in dir.EnumerateDirectories())
                {
                    Models.PdfFile p = new Models.PdfFile();
                    p.Id = sd.FullName;
                    p.FileName = sd.Name;
                    p.FilePath = sd.FullName;
                    p.RelativePath = $"{dir.Name}/{p.FileName}/";
                    p.PdfType = ViewModels.GeneratedPdfListViewModel.TypeSplit;


                    //pdf ranges files 
                    List<Models.PdfFile> pdfFiles = new List<Models.PdfFile>();

                    foreach (FileInfo f in sd.EnumerateFiles())
                    {
                        Models.PdfFile sp = new Models.PdfFile();
                        sp.Id = f.FullName;
                        sp.FileName = f.Name;
                        sp.FilePath = f.DirectoryName;
                        sp.RelativePath = $"{p.RelativePath}{sp.FileName}";
                        sp.PdfType = ViewModels.GeneratedPdfListViewModel.TypeSplit;
                        sp.SplitRanges = null;

                        pdfFiles.Add(sp);
                    }

                    p.SplitRanges = pdfFiles.ToArray();

                    yield return p;
                }
            }
        }
        public static IEnumerable<Models.PdfFile> GetAllPdfFilesOfType(string type)
        {
            //split pdf can also be a directory with multiple pdf that why standard method will not work bcz it doesn't look into subdirs
            if (type == ViewModels.GeneratedPdfListViewModel.TypeSplit)
            {
                foreach(var itm in GetAllSplitPdfFiles())
                    yield return itm;
                yield break;
            }

            //search for all pdf files in the directory....
            string out_dir = PDF.FileSystem.GetPdfOutDir(type);

            DirectoryInfo dir = new DirectoryInfo(out_dir);
            if(dir.Exists)
            {
                foreach(FileInfo f in dir.EnumerateFiles())
                {
                    Models.PdfFile p = new Models.PdfFile();
                    p.Id = f.FullName;
                    p.FileName = f.Name;
                    p.FilePath = f.DirectoryName;
                    p.RelativePath = $"{dir.Name}/{p.FileName}";
                    
                    p.PdfType = type;

                    yield return p;
                }
            }
        }

        public static IEnumerable<Models.PdfFile> GetAllMergePdfFiles() => GetAllPdfFilesOfType(ViewModels.GeneratedPdfListViewModel.TypeMerge);
        public static IEnumerable<Models.PdfFile> GetAllOtherPdfFiles() => GetAllPdfFilesOfType(ViewModels.GeneratedPdfListViewModel.TypeOther);
    }
}
