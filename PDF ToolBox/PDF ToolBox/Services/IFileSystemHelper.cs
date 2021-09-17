using System;
using System.Collections.Generic;
using System.Text;

namespace PDF_ToolBox.Services
{
    public interface IFileSystemHelper
    {
        string GetAppExternalStorage();
        string GetDeviceId();
    }
}
