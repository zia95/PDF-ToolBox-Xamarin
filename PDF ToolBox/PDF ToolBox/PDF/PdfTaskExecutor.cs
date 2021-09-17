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
        public ToolHelper.PageRange[] SplitRanges { get; private set; }
        //public ToolHelper.PdfProgressHandler ProgressTracker { get; private set; }

        public string Password { get; private set; }
        public bool LockOrUnlock { get; private set; }

        public string TaskType { get; set; }
        

        public PdfTaskExecutor(string infile, string outfile, bool merge, ToolHelper.PageRange[] ranges)
        {
            this.InputFiles = new string[] { infile };
            this.OutputFile = outfile;
            this.MergeIntoOne = merge;
            this.SplitRanges = ranges;
            this.TaskType = ViewModels.GeneratedPdfListViewModel.TypeSplit;
        }
        public PdfTaskExecutor(string[] infiles, string outfile)
        {
            this.InputFiles = infiles;
            this.OutputFile = outfile;
            this.TaskType = ViewModels.GeneratedPdfListViewModel.TypeMerge;
        }
        public PdfTaskExecutor(string infile, string outfile, string password, bool lockOrUnlock)
        {
            this.InputFiles = new string[] { infile };
            this.OutputFile = outfile;
            this.Password = password;
            this.LockOrUnlock = lockOrUnlock;
            this.TaskType = ViewModels.GeneratedPdfListViewModel.TypeSecurity;
        }


        public async Task<bool> ExecuteAsync(ToolHelper.PdfProgressHandler tracker)
        {
            if(TaskType == ViewModels.GeneratedPdfListViewModel.TypeSplit)
            {
                return await ToolHelper.SplitPDFAsync(this.InputFiles[0], this.OutputFile, this.MergeIntoOne, this.SplitRanges, tracker);
            }
            else if(TaskType == ViewModels.GeneratedPdfListViewModel.TypeMerge)
            {
                return await ToolHelper.MergePDFAsync(this.InputFiles, this.OutputFile, tracker);
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

            return false;
        }

    }
}
