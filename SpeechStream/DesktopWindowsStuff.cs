using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpeechStream
{
    class DesktopWindowsStuff
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd,
            StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop,
            EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        // Define the callback delegate's type.
        private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        // Save window titles and handles in these lists.
        private static List<IntPtr> WindowHandles;
        private static List<string> WindowTitles;

        // Return a list of the desktop windows' handles and titles.
        public static void GetDesktopWindowHandlesAndTitles(
            out List<IntPtr> handles, out List<string> titles)
        {
            WindowHandles = new List<IntPtr>();
            WindowTitles = new List<string>();

            if (!EnumDesktopWindows(IntPtr.Zero, FilterCallback, IntPtr.Zero))
            {
                handles = null;
                titles = null;
            }
            else
            {
                handles = WindowHandles;
                titles = WindowTitles;
            }
        }

        // We use this function to filter windows.
        // This version selects visible windows that have titles.
        private static bool FilterCallback(IntPtr hWnd, int lParam)
        {
            // Get the window's title.
            StringBuilder sb_title = new StringBuilder(1024);
            int length = GetWindowText(hWnd, sb_title, sb_title.Capacity);
            string title = sb_title.ToString();


            // If the window is visible and has a title, save it.
            if (IsWindowVisible(hWnd) && string.IsNullOrEmpty(title) == false)
            {

                WindowHandles.Add(hWnd);                                
                WindowTitles.Add(title);

                // Define path of text file to write
                string path = Environment.CurrentDirectory + "\\Handles.txt";                

                if (!File.Exists(path))
                {
                    // Create a file to write to. 
                    string[] createTextHandle = { title, "|", hWnd.ToString()};
                    File.WriteAllLines(path, createTextHandle, Encoding.UTF8);
                }
                else
                {
                    // This text is always added, making the file longer over time
                    // if it is not deleted.
                    
                    string appendText = title + "|"+ hWnd.ToString() + Environment.NewLine;
                    File.AppendAllText(path, appendText, Encoding.UTF8);
                }

            }
            // Return true to indicate that we
            // should continue enumerating windows.
            return true;
        }
    }
}
