using System;
using System.Collections.Generic;
using System.Text;
using Plugin.FirebaseCrashlytics;

namespace PDF_ToolBox.Misc
{
    public static class CrashReporting
    {
        public static bool IsSupported() => CrossFirebaseCrashlytics.IsSupported;

        public static void Enable() => CrossFirebaseCrashlytics.Current.HandleUncaughtException();
        public static void SetUserId(string userid) => CrossFirebaseCrashlytics.Current.SetUserId(userid);
        public static void Log(string message) => CrossFirebaseCrashlytics.Current.Log(message);
        public static void Log(string functionname, string message) => CrossFirebaseCrashlytics.Current.Log($"{functionname}-> {message}");

        public static void SetKey(string key, string value) => CrossFirebaseCrashlytics.Current.SetCustomKey(key, value);

        
    }
}
