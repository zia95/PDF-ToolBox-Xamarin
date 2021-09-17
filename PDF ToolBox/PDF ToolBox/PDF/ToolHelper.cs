using System;
using System.Collections.Generic;
using System.Text;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;


using System.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Drawing;
using System.IO;
using System.Threading.Tasks;

using PDF_ToolBox.Misc;

namespace PDF_ToolBox.PDF
{
    public class ToolHelper
    {
        public static bool IsValidPdfFile(string file) => PdfReader.TestPdfFile(file) != 0;


        public class PdfProgressEventArgs : EventArgs
        {
            public float Progress { get; set; } = 0.0f;
            public bool Finished { get; set; } = false;
            public bool Sucessful { get; set; } = false;
            public string ErrorMessage { get; set; } = null;
            public int TotalPages { get; set; } = -1;
            public int PagesDone { get; set; } = 0;

            public bool Cancel { get; set; } = false;

            public PdfProgressEventArgs Failed(string msg)
            {
                this.ErrorMessage = msg;
                this.Sucessful = false;
                this.Finished = true;
                return this;
            }
            public PdfProgressEventArgs Sucess(int pages_done)
            {
                this.Progress = 100.0f;
                this.Finished = true;
                this.Sucessful = true;
                this.ErrorMessage = null;
                this.TotalPages = pages_done;
                this.PagesDone = pages_done;
                return this;
            }
            public PdfProgressEventArgs Sucess() => Sucess(-1);
        }

        public delegate void PdfProgressHandler(object sender, PdfProgressEventArgs e);


        public struct PageRange
        {
            public int From { get; set; }
            public int To { get; set; }
            public int Single { get { return this.IsSingle ? this.From : -1; } set { this.From = this.To = value; } }
            public bool IsSingle { get { return this.From == this.To; } }
            public bool IsValid { get { return this.From <= this.To; } }

            public override string ToString()
            {
                return $"{this.From}-{this.To}";
            }
        }


        public static IEnumerable<PageRange> ParseRanges(string rawstring, bool checking = false)
        {
            if(string.IsNullOrWhiteSpace(rawstring) == false)
            {
                string[] ranges = rawstring.Split(',');

                if(ranges != null && ranges.Length > 0)
                {
                    for(int i = 0; i < ranges.Length; i++)
                    {
                        string curr_range = ranges[i];
                        if(string.IsNullOrEmpty(curr_range) == false)
                        {
                            if(curr_range.Contains("-"))
                            {
                                string[] c = curr_range.Split('-');
                                
                                if(c?.Length >= 2)
                                {
                                    if (int.TryParse(c[0], out int from) && int.TryParse(c[1], out int to) )
                                    {
                                        if(checking == true && from <= to)
                                        {
                                            yield return new PageRange { From = from, To = to };
                                        }
                                        else if(checking == false)
                                        {
                                            yield return new PageRange { From = from, To = to };
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if(int.TryParse(curr_range, out int fromto))
                                {
                                    yield return new PageRange { From = fromto, To = fromto };
                                }
                            }
                        }
                    }
                }
            }
        }

        private static int GetTotalPagesInRanges(PageRange[] ranges)
        {
            int total = 0;
            if(ranges?.Length > 0)
            {
                foreach(var r in ranges)
                {
                    total += r.To - (r.From-1);
                }
            }
            return total == 0 ? -1 : total;
        }



        /// <summary>
        /// Split pdf files
        /// </summary>
        /// <param name="infile">input pdf file</param>
        /// <param name="outfile">output pdf file which going to be saved in disk in case ranges are to be merged. Otherwise this must be a directory where all pdf files will be created.</param>
        /// <param name="merge">merge all ranges or create a directory and store all ranges as seperate pdf files</param>
        /// <param name="ranges">ranges to extract from pdf file</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>split document was sucessful or not</returns>
        public static async Task<bool> SplitPDFAsync(string infile, string outfile, bool merge, PageRange[] ranges, PdfProgressHandler tracker)
        {
            CrashReporting.Log($"In SplitPDFAsync({infile}, {outfile}, {merge}, <{ranges?.Length}>, {tracker != null})");


            // Open the file
            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();
            e_progress.TotalPages = GetTotalPagesInRanges(ranges);

            CrashReporting.Log("Split-> Opening input pdf.");

            PdfDocument doc;
            try
            {
                doc = PdfReader.Open(infile, PdfDocumentOpenMode.Import);
            }
            catch (PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log("Split-> Exception while opening input pdf.");
                tracker?.Invoke(null, e_progress.Failed($"InternalError: {ex.Message}."));
                return await Task.FromResult(false);
            }
            if (doc == null)
            {
                CrashReporting.Log("Split-> While opening input pdf doc retured <null>.");
                tracker?.Invoke(null, e_progress.Failed($"Doc object <null>."));
                return await Task.FromResult(false);
            }

            PdfDocument outputDocument = null;

            CrashReporting.Log("Split-> Checking ranges are out of range or invalid.");
            //check ranges are not out of bound before split begins
            int pgcount = doc.PageCount;
            for (int i = 0; i < ranges.Length; i++)
            {
                int from_idx = ranges[i].From - 1;
                int to_idx = ranges[i].To;

                if (from_idx >= to_idx || to_idx > pgcount)
                {
                    doc.Close();
                    e_progress.Failed($"Failed because range '{ranges[i]}' is not valid. Make sure ranges are valid. Note that the document page count is '{pgcount}'.");
                    tracker?.Invoke(null, e_progress);
                    return await Task.FromResult(false);
                }
            }
            CrashReporting.Log("Split-> In pdf spliting loop.");
            // start spliting...
            for (int i = 0; i < ranges.Length; i++)
            {
                if (outputDocument == null)
                {
                    CrashReporting.Log("Split-> Creating document.");
                    outputDocument = new PdfDocument();

                    outputDocument.Version = doc.Version;
                    outputDocument.Info.Title = doc.Info.Title;
                    outputDocument.Info.Creator = doc.Info.Creator;
                }

                //extract pages and add to output document
                int from_idx = ranges[i].From - 1;
                int to_idx = ranges[i].To;

                for (int idx = from_idx; idx < to_idx; idx++)
                {
                    await Task.Delay(1000);//delay for test....

                    // Add the page and save it
                    var page = outputDocument.AddPage(doc.Pages[idx]);
                    e_progress.PagesDone++;
                    e_progress.Progress = ((float)e_progress.PagesDone / (float)e_progress.TotalPages) * 100.0f;
                    tracker?.Invoke(null, e_progress);

                    if(e_progress.Cancel)
                    {
                        doc.Close();
                        outputDocument.Close();
                        e_progress.Failed("Failed because user exit.");
                        tracker?.Invoke(null, e_progress);
                        return await Task.FromResult(false);
                    }
                }

                //if merge is false save the document where range of pages has been added and set document object to null so it can create at start of loop.

                if(merge == false)
                {
                    CrashReporting.Log("Split-> Trying to save, @CanSave() (merge=false)");
                    string submsg = null;
                    if (outputDocument.CanSave(ref submsg))
                    {
                        //if the pdf directory doesn't exist create it
                        if (Directory.Exists(outfile) == false)
                        {
                            var dirinfo = Directory.CreateDirectory(outfile);

                            if (dirinfo.Exists == false)
                            {
                                doc.Close();
                                outputDocument.Close();
                                e_progress.Failed("Failed to create directory to save pdf files.");
                                tracker?.Invoke(null, e_progress);
                                return await Task.FromResult(false);
                            }
                        }

                        CrashReporting.Log("Split-> Saving generated pdf file (merge=false).");
                        //save the file in created dir
                        string to_save = Path.Combine(outfile, $"{ranges[i]}.pdf");

                        outputDocument.Save(to_save);
                        outputDocument.Close();
                        outputDocument = null;
                    }
                    else
                    {
                        CrashReporting.Log($"Split-> CanSave() retured false. Returning... (merge=false; msg: {submsg}).");
                        doc.Close();
                        outputDocument.Close();
                        e_progress.Failed($"InternalError: {submsg}");
                        tracker?.Invoke(null, e_progress);
                        return await Task.FromResult(false);
                    }
                }
            }

            //if merge is true than just save the file
            if(merge)
            {
                CrashReporting.Log("Split-> Trying to save, @CanSave() (merge=true)");
                string msg = null;
                if (outputDocument.CanSave(ref msg))
                {
                    CrashReporting.Log("Split-> Saving generated pdf file (merge=true).");
                    outputDocument.Save(outfile);
                    outputDocument.Close();
                }
                else
                {
                    CrashReporting.Log($"Split-> CanSave() retured false. Returning... (merge=true; msg: {msg}).");
                    doc.Close();
                    outputDocument.Close();

                    e_progress.Failed($"InternalError: {msg}");
                    tracker?.Invoke(null, e_progress);
                    return await Task.FromResult(false);
                }
            }

            CrashReporting.Log($"Exiting SplitPDFAsync()");

            //throw new Exception("Test exception in split()");

            doc.Close();
            e_progress.Sucess(e_progress.TotalPages);
            tracker?.Invoke(null, e_progress);
            return await Task.FromResult(true);
        }



        /// <summary>
        /// merge pdf files
        /// </summary>
        /// <param name="infiles">input pdf files</param>
        /// <param name="outfile">output pdf file which going to be saved in disk in case ranges are to be merged. Otherwise this must be a directory where all pdf files will be created.</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>merge document was sucessful or not</returns>
        public static async Task<bool> MergePDFAsync(string[] infiles, string outfile, PdfProgressHandler tracker)
        {
            CrashReporting.Log($"In MergePDFAsync({infiles?.Length}, {outfile}, {tracker != null})");
            // Create the output document
            PdfDocument odoc = new PdfDocument();

            List<PdfDocument> idocs = new List<PdfDocument>();

            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();
            e_progress.TotalPages = 0;
            //calc number of total pages needed to measure merge progress
            foreach(string s in infiles)
            {
                PdfDocument doc;
                CrashReporting.Log($"Merge-> Trying to open pdf ({s})");
                try
                {
                    doc = PdfReader.Open(s, PdfDocumentOpenMode.Import);
                }
                catch (PdfSharp.Pdf.IO.PdfReaderException ex)
                {
                    CrashReporting.Log($"Merge-> Exception while trying to open pdf ({s}) (ex: {ex.Message})");
                    tracker?.Invoke(null, e_progress.Failed($"InternalError: {ex.Message}."));
                    return await Task.FromResult(false);
                }
                if (doc == null)
                {
                    CrashReporting.Log($"Merge-> While opening input pdf doc retured <null>.");
                    tracker?.Invoke(null, e_progress.Failed($"Doc object <null>."));
                    return await Task.FromResult(false);
                }



                e_progress.TotalPages += doc.PageCount;

                idocs.Add(doc);
            }

            CrashReporting.Log($"Merge-> In merge loop.");
            //merge all the documents in input docs list
            foreach (var idoc in idocs)
            {
                if (idoc.PageCount > 0)
                {
                    foreach (var pg in idoc.Pages)
                    {
                        await Task.Delay(50);//FOR TEST..........

                        odoc.AddPage(pg);

                        e_progress.PagesDone++;
                        e_progress.Progress = ((float)e_progress.PagesDone / (float)e_progress.TotalPages) * 100.0f;
                        tracker?.Invoke(null, e_progress);

                        if (e_progress.Cancel)
                        {
                            foreach (var d in idocs)
                                d.Close();

                            odoc.Close();

                            e_progress.Failed("Failed because user exit.");
                            tracker?.Invoke(null, e_progress);
                            return await Task.FromResult(false);
                        }
                    }
                }
                //there is no need for the input document anymore as it is already added to the output document
                idoc.Close();
            }
            //clear input docuemnt list
            idocs.Clear();
            idocs = null;

            CrashReporting.Log($"Merge-> Trying to save merged pdf. (at CanSave())");
            //save output document
            string msg = null;
            if (odoc.CanSave(ref msg))
            {
                CrashReporting.Log($"Merge-> Trying to save merged pdf.");
                odoc.Save(outfile);
                odoc.Close();
            }
            else
            {
                CrashReporting.Log($"Merge-> Failed because CanSave() = false (msg: {msg}).");
                odoc.Close();

                e_progress.Failed($"InternalError: {msg}");
                tracker?.Invoke(null, e_progress);
                return await Task.FromResult(false);
            }

            CrashReporting.Log("Exiting MergePDFAsync()");

            e_progress.Sucess(e_progress.TotalPages);
            tracker?.Invoke(null, e_progress);
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Lock or unlock pdf file.
        /// </summary>
        /// <param name="infile">input pdf file</param>
        /// <param name="outfile">location where output pdf going to be saved.</param>
        /// <param name="currpassword">current password</param>
        /// <param name="password">password to change to (null in case to remove password)</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>lock or unlock pdf was sucessful or not</returns>
        public static async Task<bool> LockPdfAsync(string infile, string outfile, string currpassword, string password, PdfProgressHandler tracker)
        {
            CrashReporting.Log($"In LockPdfAsync({infile}, {outfile}, {currpassword}, {password}, {tracker != null})");

            CrashReporting.Log($"LockPdf-> Trying to open pdf doc.");
            PdfDocument doc = null;
            try
            {
                doc = PdfReader.Open(infile, currpassword, PdfDocumentOpenMode.Modify);
            }
            catch(PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log($"LockPdf-> Failed to open pdf doc (ex: {ex.Message})");
                tracker?.Invoke(null, new PdfProgressEventArgs().Failed($"InternalError: {ex.Message}."));
                return await Task.FromResult(false);
            }
            
            if(doc == null)
            {
                CrashReporting.Log($"LockPdf-> Failed to open pdf doc because object <null>.");
                tracker?.Invoke(null, new PdfProgressEventArgs().Failed($"Doc object <null>."));
                return await Task.FromResult(false);
            }

            CrashReporting.Log($"LockPdf-> Settings password, Is password null {password != null}.");

            PdfSecuritySettings securitySettings = doc.SecuritySettings;

            // Setting one of the passwords automatically sets the security level to 
            // PdfDocumentSecurityLevel.Encrypted128Bit.
            
            securitySettings.UserPassword = password;
            securitySettings.OwnerPassword = password;

            if (password == null)
                securitySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.None;

            CrashReporting.Log($"LockPdf-> Trying to save pdf doc.");
            string msg = null;
            if(doc.CanSave(ref msg))
            {
                CrashReporting.Log($"LockPdf-> Saving pdf doc.");
                doc.Save(outfile);
                doc.Close();
                tracker?.Invoke(null, new PdfProgressEventArgs().Sucess());
                return await Task.FromResult(true);
            }

            CrashReporting.Log($"LockPdf-> Failed to save pdf doc (msg: {msg}).");
            CrashReporting.Log($"Exiting LockPdfAsync()");
            doc.Close();
            tracker?.Invoke(null, new PdfProgressEventArgs().Failed(msg));
            return await Task.FromResult(false);
        }
        public static async Task<bool> LockPdfAsync(string infile, string outfile, string password, PdfProgressHandler tracker) => await LockPdfAsync(infile, outfile, null, password, tracker);
        public static async Task<bool> UnlockPdfAsync(string infile, string outfile, string currpassword, PdfProgressHandler tracker) => await LockPdfAsync(infile, outfile, currpassword, null, tracker);
    }
}
