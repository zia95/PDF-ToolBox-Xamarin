using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PDF_ToolBox.PDF
{
    public class PdfTaskExecutor
    {
        public string[] InputFiles { get; private set; }
        public string OutputFile { get; private set; }
        public bool MergeIntoOne { get; private set; }
        public ToolHelper.PageRange[] PageRanges { get; private set; }

        public string Password { get; private set; }
        public bool LockOrUnlock { get; private set; }

        public string WatermarkText { get; private set; }
        public ToolHelper.WatermarkType WatermarkType { get; private set; }

        public string TaskType { get; set; }



        private PdfTaskExecutor() { }

        public static PdfTaskExecutor DoTaskMergePdf(string[] infiles, string outfile)
        {
            PdfTaskExecutor exe = new PdfTaskExecutor();
            exe.InputFiles = infiles;
            exe.OutputFile = outfile;
            exe.TaskType = ViewModels.GeneratedPdfListViewModel.TypeMerge;
            return exe;
        }
        public static PdfTaskExecutor DoTaskImagesToPdf(string[] infiles, string outfile)
        {
            PdfTaskExecutor exe = new PdfTaskExecutor();
            exe.InputFiles = infiles;
            exe.OutputFile = outfile;
            exe.TaskType = ViewModels.GeneratedPdfListViewModel.TypeImagesToPdf;
            return exe;
        }
        public static PdfTaskExecutor DoTaskLockOrUnlockPdf(string infile, string outfile, string password, bool lockOrUnlock)
        {
            PdfTaskExecutor exe = new PdfTaskExecutor();
            exe.InputFiles = new string[] { infile };
            exe.OutputFile = outfile;
            exe.Password = password;
            exe.LockOrUnlock = lockOrUnlock;
            exe.TaskType = ViewModels.GeneratedPdfListViewModel.TypeSecurity;
            return exe;
        }

        public static PdfTaskExecutor DoTaskSplitPdf(string infile, string outfile, bool merge, ToolHelper.PageRange[] ranges)
        {
            PdfTaskExecutor exe = new PdfTaskExecutor();
            exe.InputFiles = new string[] { infile };
            exe.OutputFile = outfile;
            exe.MergeIntoOne = merge;
            exe.PageRanges = ranges;
            exe.TaskType = ViewModels.GeneratedPdfListViewModel.TypeSplit;
            return exe;
        }
        public static PdfTaskExecutor DoTaskRemovePagesFromPdf(string infile, string outfile, ToolHelper.PageRange[] ranges)
        {
            PdfTaskExecutor exe = new PdfTaskExecutor();
            exe.InputFiles = new string[] { infile };
            exe.OutputFile = outfile;
            exe.MergeIntoOne = true;
            exe.PageRanges = ranges;
            exe.TaskType = ViewModels.GeneratedPdfListViewModel.TypeRemove;
            return exe;
        }
        public static PdfTaskExecutor DoTaskWatermarkPagesFromPdf(string infile, string outfile, string watermarkstring, ToolHelper.WatermarkType watermarktype, ToolHelper.PageRange[] ranges)
        {
            PdfTaskExecutor exe = new PdfTaskExecutor();
            exe.InputFiles = new string[] { infile };
            exe.OutputFile = outfile;
            exe.WatermarkText = watermarkstring;
            exe.WatermarkType = watermarktype;
            exe.MergeIntoOne = true;
            exe.PageRanges = ranges;
            exe.TaskType = ViewModels.GeneratedPdfListViewModel.TypeWatermark;
            return exe;
        }




        public async Task<bool> ExecuteAsync(ToolHelper.PdfProgressHandler tracker)
        {
            if(TaskType == ViewModels.GeneratedPdfListViewModel.TypeSplit)
            {
                return await ToolHelper.SplitPDFAsync(this.InputFiles[0], this.OutputFile, this.MergeIntoOne, this.PageRanges, tracker);
            }
            else if (TaskType == ViewModels.GeneratedPdfListViewModel.TypeRemove)
            {
                return await ToolHelper.RemovePagesFromPDFAsync(this.InputFiles[0], this.OutputFile, this.PageRanges, tracker);
            }
            else if (TaskType == ViewModels.GeneratedPdfListViewModel.TypeWatermark)
            {
                return await ToolHelper.WatermarkPdfAsync(this.InputFiles[0], this.OutputFile, this.WatermarkText, this.WatermarkType, this.PageRanges, tracker);
            }
            else if(TaskType == ViewModels.GeneratedPdfListViewModel.TypeMerge)
            {
                return await ToolHelper.MergePDFAsync(this.InputFiles, this.OutputFile, tracker);
            }
            else if (TaskType == ViewModels.GeneratedPdfListViewModel.TypeImagesToPdf)
            {
                return await ToolHelper.ImagesToPdfAsync(this.InputFiles, this.OutputFile, tracker);
            }
            else if(TaskType == ViewModels.GeneratedPdfListViewModel.TypeSecurity)
            {
                if(this.LockOrUnlock)
                {
                    return await ToolHelper.LockPdfAsync(this.InputFiles[0], this.OutputFile, this.Password, tracker);
                }
                else
                {
                    return await ToolHelper.UnlockPdfAsync(this.InputFiles[0], this.OutputFile, this.Password, tracker);
                }
            }
            else
            {
                throw new NotImplementedException($"Task {TaskType} is not implemented.");
            }
        }

    }
}
