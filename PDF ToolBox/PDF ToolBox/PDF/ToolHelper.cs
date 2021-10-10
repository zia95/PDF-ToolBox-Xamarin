using System;
using System.Collections.Generic;
using System.Text;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;

using System.Linq;

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
        public static bool IsValidImageFile(string file) => XImage.ExistsFile(file);

        public class PdfProgressEventArgs : EventArgs
        {
            public float Progress { get; set; } = 0.0f;
            public bool Finished { get; set; } = false;
            public bool Sucessful { get; set; } = false;
            public string ErrorMessage { get; set; } = null;
            public int TotalPages { get; set; } = -1;
            public int PagesDone { get; set; } = 0;

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
            public PdfProgressEventArgs SucessNoPages() => Sucess(-1);
        }

        /// <summary>
        /// Handler for tracking progress
        /// </summary>
        /// <param name="sender">source</param>
        /// <param name="e">event args</param>
        /// <returns>continue with operation or cancel</returns>
        public delegate bool PdfProgressHandler(object sender, PdfProgressEventArgs e);


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
            CrashReporting.Log("Split", $"Parameters: ({infile}, {outfile}, {merge}, <{ranges?.Length}>, {tracker != null})");

            //-------------------------------------------------------------------------------------//
            //              Opening document and getting reading to perform op                     //
            //-------------------------------------------------------------------------------------//

            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetSplitPdfOutDir(), outfile);

            // Open the file
            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();

            CrashReporting.Log("Split", "Opening input pdf.");

            PdfDocument doc;
            try
            {
                doc = PdfReader.Open(infile, PdfDocumentOpenMode.Import);
            }
            catch (PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log("Split", $"Exception while opening input pdf. (msg: {ex.Message})");
                tracker?.Invoke(null, e_progress.Failed($"InternalError: {ex.Message}."));
                return await Task.FromResult(false);
            }
            if (doc == null)
            {
                CrashReporting.Log("Split", "While opening input pdf doc retured <null>.");
                tracker?.Invoke(null, e_progress.Failed($"Doc object <null>."));
                return await Task.FromResult(false);
            }

            PdfDocument outputDocument = null;


            CrashReporting.Log("Split", "Checking ranges are not greater than page count or invalid.");

            e_progress.TotalPages = GetTotalPagesInRanges(ranges);
            // are total ranges 0?
            if (e_progress.TotalPages <= 0)
            {
                CrashReporting.Log("Split", "No pages to split.");
                doc.Close();

                e_progress.Failed("Failed because there are no pages to split.");
                tracker?.Invoke(null, e_progress);
                return await Task.FromResult(false);
            }

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
            CrashReporting.Log("Split", "In pdf spliting loop.");
            //-------------------------------------------------------------------------------------//
            //              Split page loop                                                        //
            //-------------------------------------------------------------------------------------//
            // start spliting...
            for (int i = 0; i < ranges.Length; i++)
            {
                if (outputDocument == null)
                {
                    CrashReporting.Log("Split", "Creating document.");
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

                    if(tracker != null)
                    {
                        if (tracker.Invoke(null, e_progress) == false)
                        {
                            doc.Close();
                            outputDocument.Close();
                            e_progress.Failed("Failed because user exit.");
                            tracker?.Invoke(null, e_progress);
                            return await Task.FromResult(false);
                        }
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

            doc.Close();
            e_progress.Sucess(e_progress.TotalPages);
            tracker?.Invoke(null, e_progress);
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Remove pages from pdf file
        /// </summary>
        /// <param name="infile">input pdf file</param>
        /// <param name="outfile">output pdf file which going to be saved in disk in case ranges are to be merged. Otherwise this must be a directory where all pdf files will be created.</param>
        /// <param name="ranges">ranges to extract from pdf file</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>removeing pages from document was sucessful or not</returns>
        public static async Task<bool> RemovePagesFromPDFAsync(string infile, string outfile, PageRange[] ranges, PdfProgressHandler tracker)
        {
            CrashReporting.Log($"In RemovePagesFromPDFAsync({infile}, {outfile}, <{ranges?.Length}>, {tracker != null})");

            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetOtherPdfOutDir(), outfile);

            // Open the file
            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();

            CrashReporting.Log("RemovePages-> Opening input pdf.");

            PdfDocument doc;
            try
            {
                doc = PdfReader.Open(infile, PdfDocumentOpenMode.Import);
            }
            catch (PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log("RemovePages", $"Exception while opening input pdf. (msg: {ex.Message})");
                tracker?.Invoke(null, e_progress.Failed($"InternalError: {ex.Message}."));
                return await Task.FromResult(false);
            }
            if (doc == null)
            {
                CrashReporting.Log("RemovePages-> While opening input pdf doc retured <null>.");
                tracker?.Invoke(null, e_progress.Failed($"Doc object <null>."));
                return await Task.FromResult(false);
            }

            CrashReporting.Log("RemovePages-> Checking ranges are out of range or invalid.");
            //check ranges are not out of bound before remove pages begins
            int pgcount = doc.PageCount;
            e_progress.TotalPages = pgcount;
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

            PdfDocument outputDocument = new PdfDocument();

            outputDocument.Version = doc.Version;
            outputDocument.Info.Title = doc.Info.Title;
            outputDocument.Info.Creator = doc.Info.Creator;

            CrashReporting.Log("RemovePages-> In pdf removing pages loop.");
            // start removing pages...
            for (int i = 0; i < pgcount; i++)
            {
                bool do_add = !ranges.Any(rng => (i >= (rng.From - 1) && i < rng.To));
                e_progress.PagesDone++;

                if (do_add == false)
                    continue;

                await Task.Delay(200);//delay for test....
                var page = outputDocument.AddPage(doc.Pages[i]);


                // notify progress
                e_progress.Progress = ((float)e_progress.PagesDone / (float)e_progress.TotalPages) * 100.0f;

                if(tracker != null)
                {
                    if (tracker.Invoke(null, e_progress) == false)
                    {
                        doc.Close();
                        outputDocument.Close();
                        e_progress.Failed("Failed because user exit.");
                        tracker?.Invoke(null, e_progress);
                        return await Task.FromResult(false);
                    }
                }
                
            }
            doc.Close();

            if(outputDocument.PageCount <= 0)
            {
                CrashReporting.Log($"RemovePages-> Outdoc page count was 0. Exiting...");
                outputDocument.Close();

                e_progress.Failed($"Failed because there are no pages to save.");
                tracker?.Invoke(null, e_progress);
                return await Task.FromResult(false);
            }

            //save document
            CrashReporting.Log("RemovePages-> Trying to save, @CanSave()");
            string msg = null;
            if (outputDocument.CanSave(ref msg))
            {
                CrashReporting.Log("RemovePages-> Saving generated pdf file.");
                outputDocument.Save(outfile);
                outputDocument.Close();

                CrashReporting.Log($"Exiting RemovePagesFromPDFAsync()");

                e_progress.Sucess(e_progress.TotalPages);
                tracker?.Invoke(null, e_progress);
                return await Task.FromResult(true);
            }

            CrashReporting.Log($"RemovePages-> CanSave() retured false. Returning... (msg: {msg}).");
            outputDocument.Close();
            CrashReporting.Log($"Exiting RemovePagesFromPDFAsync()");

            e_progress.Failed($"InternalError: {msg}");
            tracker?.Invoke(null, e_progress);
            return await Task.FromResult(false);
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

            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetMergePdfOutDir(), outfile);

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

                        if(tracker != null)
                        {
                            if (tracker?.Invoke(null, e_progress) == false)
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
        public static async Task<bool> LockUnlockPdfAsync(string infile, string outfile, string currpassword, string password, PdfProgressHandler tracker)
        {
            CrashReporting.Log("LockUnlockPdfAsync", $"({infile}, {outfile}, {currpassword}, {password}, {tracker != null})");
            
            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetOtherPdfOutDir(), outfile);

            CrashReporting.Log("LockUnlockPdfAsync", $"Trying to open pdf doc.");
            PdfDocument doc = null;
            try
            {
                doc = PdfReader.Open(infile, currpassword, PdfDocumentOpenMode.Modify);
            }
            catch(PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log("LockUnlockPdfAsync", $"Failed to open pdf doc (ex: {ex.Message})");
                tracker?.Invoke(null, new PdfProgressEventArgs().Failed($"InternalError: {ex.Message}."));
                return await Task.FromResult(false);
            }
            
            if(doc == null)
            {
                CrashReporting.Log("LockUnlockPdfAsync", $"Failed to open pdf doc because object <null>.");
                tracker?.Invoke(null, new PdfProgressEventArgs().Failed($"Doc object <null>."));
                return await Task.FromResult(false);
            }

            CrashReporting.Log("LockUnlockPdfAsync", $"Settings password, Is password null {password != null}.");

            PdfSecuritySettings securitySettings = doc.SecuritySettings;

            // Setting one of the passwords automatically sets the security level to 
            // PdfDocumentSecurityLevel.Encrypted128Bit.
            
            securitySettings.UserPassword = password;
            securitySettings.OwnerPassword = password;

            if (password == null)
                securitySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.None;

            CrashReporting.Log("LockUnlockPdfAsync", $"Trying to save pdf doc.");
            string msg = null;
            if(doc.CanSave(ref msg))
            {
                CrashReporting.Log("LockUnlockPdfAsync", $"Saving pdf doc.");
                doc.Save(outfile);
                doc.Close();
                tracker?.Invoke(null, new PdfProgressEventArgs().SucessNoPages());
                return await Task.FromResult(true);
            }

            CrashReporting.Log("LockUnlockPdfAsync", $"Failed to save pdf doc (msg: {msg}).");
            doc.Close();
            tracker?.Invoke(null, new PdfProgressEventArgs().Failed(msg));
            return await Task.FromResult(false);
        }
        public static async Task<bool> LockPdfAsync(string infile, string outfile, string password, PdfProgressHandler tracker) => await LockUnlockPdfAsync(infile, outfile, null, password, tracker);
        public static async Task<bool> UnlockPdfAsync(string infile, string outfile, string currpassword, PdfProgressHandler tracker) => await LockUnlockPdfAsync(infile, outfile, currpassword, null, tracker);


        public enum WatermarkType
        {
            Text,
            Outline,
            Transparent,
        };

        private static bool DrawWatermarkToPage(PdfPage page, string watermarkstring, WatermarkType watermarktype)
        {
            //sample-url: http://www.pdfsharp.com/PDFsharp/index.php?option=com_content&task=view&id=40&Itemid=47

            XGraphics gfx = null;
            try
            {
                gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);
            }
            catch(Exception)
            {
                return false;
            }

            if (gfx == null)
                return false;


            XFont font = new XFont(new Font(FontFamily.GenericSerif, 8, FontStyle.Bold));//new XFont("Verdana", 8, XFontStyle.Bold);
            
            
            // Get the size (in point) of the text
            XSize size = gfx.MeasureString(watermarkstring, font);
            
            // Define a rotation transformation at the center of the page
            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
            gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

            if (watermarktype == WatermarkType.Text)
            {
                // Create a string format
                XStringFormat format = new XStringFormat();
                format.Alignment = XStringAlignment.Near;
                format.LineAlignment = XLineAlignment.Near;

                // Create a dimmed red brush
                XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));

                // Draw the string
                gfx.DrawString(watermarkstring, font, brush,
                  new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                  format);
            }
            else if (watermarktype == WatermarkType.Outline)
            {
                // Create a graphical path
                XGraphicsPath path = new XGraphicsPath();

                // Add the text to the path
                path.AddString(watermarkstring, font.FontFamily, XFontStyle.BoldItalic, 150,
                  new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                  XStringFormats.Default);

                // Create a dimmed red pen
                XPen pen = new XPen(XColor.FromArgb(128, 255, 0, 0), 2);

                // Stroke the outline of the path
                gfx.DrawPath(pen, path);
            }
            else if (watermarktype == WatermarkType.Transparent)
            {
                // Create a graphical path
                XGraphicsPath path = new XGraphicsPath();

                // Add the text to the path
                path.AddString(watermarkstring, font.FontFamily, XFontStyle.BoldItalic, 150,
                  new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                  XStringFormats.Default);

                // Create a dimmed red pen and brush
                XPen pen = new XPen(XColor.FromArgb(50, 75, 0, 130), 3);
                XBrush brush = new XSolidBrush(XColor.FromArgb(50, 106, 90, 205));

                // Stroke the outline of the path
                gfx.DrawPath(pen, brush, path);
            }
            else
            {
                throw new NotSupportedException($"Type '{watermarktype}' is not supported.");
            }

            

            return true;
        }

        /// <summary>
        /// Add watermark to all pages in pdf document.
        /// </summary>
        /// <param name="infile">input pdf file</param>
        /// <param name="outfile">output pdf file</param>
        /// <param name="watermarkstring">watermark string</param>
        /// <param name="watermarktype">type of watermark</param>
        /// <param name="ranges">ranges to add watermark too</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>watermark was sucessful or not</returns>
        public static async Task<bool> WatermarkPdfAsync(string infile, string outfile, string watermarkstring, WatermarkType watermarktype, PageRange[] ranges, PdfProgressHandler tracker)
        {
            CrashReporting.Log("WatermarkPdf", $"Parameters: ({infile}, {outfile}, {watermarkstring}, {watermarktype}, <{ranges?.Length}>, {tracker != null})");

            //-------------------------------------------------------------------------------------//
            //              Opening document and getting reading to perform op                     //
            //-------------------------------------------------------------------------------------//

            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetOtherPdfOutDir(), outfile);

            // Open the file
            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();

            CrashReporting.Log("WatermarkPdf", "Opening input pdf.");

            PdfDocument doc;
            try
            {
                doc = PdfReader.Open(infile, PdfDocumentOpenMode.Modify);
            }
            catch (PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log("WatermarkPdf", $"Exception while opening input pdf. (msg: {ex.Message})");
                tracker?.Invoke(null, e_progress.Failed($"InternalError: {ex.Message}."));
                return await Task.FromResult(false);
            }
            if (doc == null)
            {
                CrashReporting.Log("WatermarkPdf", "While opening input pdf doc retured <null>.");
                tracker?.Invoke(null, e_progress.Failed($"Doc object <null>."));
                return await Task.FromResult(false);
            }

            //-------------------------------------------------------------------------------------//
            //              CHECK RANGES                                                           //
            //-------------------------------------------------------------------------------------//

            CrashReporting.Log("WatermarkPdf", "Checking ranges are greater than page count or invalid.");

            e_progress.TotalPages = GetTotalPagesInRanges(ranges);
            // are total ranges 0?
            if (e_progress.TotalPages <= 0)
            {
                CrashReporting.Log("WatermarkPdf", "No pages to watermark.");
                doc.Close();

                e_progress.Failed("Failed because there are no pages to save.");
                tracker?.Invoke(null, e_progress);
                return await Task.FromResult(false);
            }

            //check ranges are not out of bound before remove pages begins
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
            

            CrashReporting.Log("WatermarkPdf-> In pdf removing pages loop.");
            //-------------------------------------------------------------------------------------//
            //              BEGIN ADDING WATERMARK TO PAGES                                        //
            //-------------------------------------------------------------------------------------//

            // start added watermark to pages...
            for (int i = 0; i < ranges.Length; i++)
            {

                //get pages and add to watermark to it
                int from_idx = ranges[i].From - 1;
                int to_idx = ranges[i].To;

                for (int idx = from_idx; idx < to_idx; idx++)
                {
                    await Task.Delay(1000);//delay for test....

                    if (DrawWatermarkToPage(doc.Pages[i], watermarkstring, watermarktype) == false)
                    {
                        doc.Close();
                        e_progress.Failed("Failed to add watermark to page.");
                        tracker?.Invoke(null, e_progress);
                        return await Task.FromResult(false);
                    }

                    e_progress.PagesDone++;
                    e_progress.Progress = ((float)e_progress.PagesDone / (float)e_progress.TotalPages) * 100.0f;

                    if (tracker != null)
                    {
                        if (tracker.Invoke(null, e_progress) == false)
                        {
                            doc.Close();
                            e_progress.Failed("Failed because user exit.");
                            tracker?.Invoke(null, e_progress);
                            return await Task.FromResult(false);
                        }
                    }
                }
            }

            //save document
            CrashReporting.Log("WatermarkPdf", "Trying to save. (CanSave())");
            string msg = null;
            if (doc.CanSave(ref msg))
            {
                CrashReporting.Log("WatermarkPdf", "Saving generated pdf file.");
                doc.Save(outfile);

                e_progress.Sucess(e_progress.TotalPages);
                tracker?.Invoke(null, e_progress);
                return await Task.FromResult(true);
            }

            CrashReporting.Log("WatermarkPdf", $"Failed to save. (msg from CanSave(): {msg}).");
            doc.Close();

            e_progress.Failed($"InternalError: {msg}");
            tracker?.Invoke(null, e_progress);
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Generate Pdf document from images.
        /// </summary>
        /// <param name="infile">image files</param>
        /// <param name="outfile">output pdf file</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>pdf generated from images was sucessful or not</returns>
        public static async Task<bool> ImagesToPdfAsync(string[] infile, string outfile, PdfProgressHandler tracker)
        {
            CrashReporting.Log("ImagesToPdf", $"Parameters: ({infile?.Length}, {outfile}, {tracker != null})");

            //-------------------------------------------------------------------------------------//
            //              Opening document and getting reading to perform op                     //
            //-------------------------------------------------------------------------------------//

            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetOtherPdfOutDir(), outfile);

            // Open the file
            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();
            
            CrashReporting.Log("ImagesToPdf", "Checking images.");

            if (infile == null || infile.Length <= 0)
            {
                CrashReporting.Log("ImagesToPdf", "Can't find images.");
                tracker?.Invoke(null, e_progress.Failed("Can't find images."));
                return await Task.FromResult(false);
            }
            e_progress.TotalPages = infile.Length;

            //-------------------------------------------------------------------------------------//
            //              Images to pdf loop                                                     //
            //-------------------------------------------------------------------------------------//

            CrashReporting.Log("ImagesToPdf", "In image to pdf loop.");
            PdfDocument doc = new PdfDocument();
            foreach(string img in infile)
            {
                if(XImage.ExistsFile(img) == false)
                {
                    tracker?.Invoke(null, e_progress.Failed("Can't find images."));
                    return await Task.FromResult(false);
                }
                XImage ximg = XImage.FromFile(img);
                //PdfPage page = new PdfPage();

                PdfPage page = doc.AddPage();

                XGraphics gfx = XGraphics.FromPdfPage(page);
                gfx.DrawImage(ximg, 0, 0);

                e_progress.PagesDone++;
                e_progress.Progress = ((float)e_progress.PagesDone / (float)e_progress.TotalPages) * 100.0f;

                if (tracker != null)
                {
                    if(tracker.Invoke(null, e_progress) == false)
                    {
                        tracker?.Invoke(null, e_progress.Failed("Failed because user exit."));
                        return await Task.FromResult(false);
                    }
                }
            }

            CrashReporting.Log("ImagesToPdf", "Trying to save.");
            string msg = null;
            if(doc.CanSave(ref msg))
            {
                doc.Save(outfile);
                doc.Close();

                tracker?.Invoke(null, e_progress.Sucess(e_progress.TotalPages));
                return await Task.FromResult(true);
            }

            CrashReporting.Log("ImagesToPdf", $"Failed to save. (msg: {msg})");
            tracker?.Invoke(null, e_progress.Failed(msg));
            return await Task.FromResult(false);
        }

        public class PdfInfo
        {
            public string FullPath { get; private set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Subject { get; set; }
            public string Keywords { get; set; }
            public string Creator { get; set; }
            public string Producer { get; private set; }
            public DateTime CreationDate { get; private set; }
            public DateTime ModificationDate { get; private set; }

            public PdfCustomValues CustomValues { get; set; }

            public void Set(string fullpath, PdfDocumentInformation info, PdfCustomValues customvalues)
            {
                this.FullPath = fullpath;
                this.CustomValues = customvalues;
                if(info != null)
                {
                    this.Title = info.Title;
                    this.Author = info.Author;
                    this.Subject = info.Subject;
                    this.Keywords = info.Keywords;
                    this.Creator = info.Creator;
                    this.Producer = info.Producer;
                    this.CreationDate = info.CreationDate;
                    this.ModificationDate = info.ModificationDate;
                }
            }

            public override string ToString()
            {
                return $"(PdfInfo: {FullPath}, {Title})";
            }
        }

        /// <summary>
        /// Read and/or write pdf information.
        /// </summary>
        /// <param name="infile">image files</param>
        /// <param name="outfile">output pdf file</param>
        /// <param name="read">info which will be read from document. (null incase only write)</param>
        /// <param name="write">info which will be written to document and saved at outfile location. (null incase only read)</param>
        /// <param name="tracker">this event will be called each time change in progress happens</param>
        /// <returns>pdf info read/written sucessful or not</returns>
        public static async Task<bool> ModifyPdfInfoAsync(string infile, string outfile, PdfInfo read, PdfInfo write, PdfProgressHandler tracker)
        {
            CrashReporting.Log("ModifyPdfInfo", $"Parameters: ({infile}, {outfile}, {read}, {write}, {tracker != null})");

            //-------------------------------------------------------------------------------------//
            //              Opening document and getting reading to perform op                     //
            //-------------------------------------------------------------------------------------//

            if (outfile != null)
                outfile = Path.Combine(FileSystem.GetOtherPdfOutDir(), outfile);

            // Open the file
            PdfProgressEventArgs e_progress = new PdfProgressEventArgs();

            CrashReporting.Log("ModifyPdfInfo", "Opening input document.");

            if (read == null && write == null)
            {
                tracker?.Invoke(null, e_progress.Failed("Both read and write info can't be null."));
                return await Task.FromResult(false);
            }

            PdfDocument doc;
            try
            {
                doc = PdfReader.Open(infile, PdfDocumentOpenMode.Modify);
            }
            catch (PdfSharp.Pdf.IO.PdfReaderException ex)
            {
                CrashReporting.Log("ModifyPdfInfo", $"Exception while opening input pdf. (msg: {ex.Message})");
                tracker?.Invoke(null, e_progress.Failed(ex.Message));
                return await Task.FromResult(false);
            }
            if (doc == null)
            {
                CrashReporting.Log("ModifyPdfInfo", "While opening input pdf doc retured <null>.");
                tracker?.Invoke(null, e_progress.Failed("Doc object <null>."));
                return await Task.FromResult(false);
            }


            //-------------------------------------------------------------------------------------//
            //              Images to pdf loop                                                     //
            //-------------------------------------------------------------------------------------//

            CrashReporting.Log("ModifyPdfInfo", "Read/Write Info");
            
            if(read != null)
            {
                read.Set(doc.FullPath, doc.Info, doc.CustomValues);
            }

            if(write != null)
            {
                //write info to doc object
                doc.Info.Title = write.Title;
                doc.Info.Author = write.Author;
                doc.Info.Subject = write.Subject;
                doc.Info.Keywords = write.Keywords;
                doc.Info.Creator = write.Creator;
                //doc.Info.CreationDate = write.CreationDate;
                //doc.Info.ModificationDate = write.ModificationDate;

                doc.CustomValues = write.CustomValues;

                CrashReporting.Log("ModifyPdfInfo", "Trying to save.");
                string msg = null;
                if (!doc.CanSave(ref msg))
                {
                    doc.Close();
                    CrashReporting.Log("ModifyPdfInfo", $"Failed to save. (msg: {msg})");
                    tracker?.Invoke(null, e_progress.Failed(msg));
                    return await Task.FromResult(false);
                }

                doc.Save(outfile);
                doc.Close();
            }

            tracker?.Invoke(null, e_progress.SucessNoPages());
            return await Task.FromResult(true);
        }

        public static async Task<bool> ReadPdfInfoAsync(string infile, PdfInfo read, PdfProgressHandler tracker) => await ModifyPdfInfoAsync(infile, null, read, null, tracker);
        public static async Task<bool> WritePdfInfoAsync(string infile, string outfile, PdfInfo write, PdfProgressHandler tracker) => await ModifyPdfInfoAsync(infile, outfile, null, write, tracker);
    }
}
