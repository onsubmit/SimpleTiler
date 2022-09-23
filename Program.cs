namespace SimpleTiler
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref WindowSearchData data);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        
        private delegate bool EnumWindowsProc(IntPtr hWnd, ref WindowSearchData data);

        static void Main()
        {
            Screen screen = GetScreen();

            List<TileableWindow> windows = GetWindows();
            if (!windows.Any())
            {
                return;
            }

            int numCols = (int)Math.Ceiling(Math.Sqrt(windows.Count));
            int numRows = (int)Math.Ceiling((double)windows.Count / numCols);
            int offsetX = screen.Bounds.X;
            int windowWidth = screen.WorkingArea.Width / numCols;
            int windowHeight = screen.WorkingArea.Height / numRows;

            RearrangeWindows(windows, numCols, numRows, offsetX, windowWidth, windowHeight);
        }

        private static HashSet<IntPtr> FindWindows(string wndclass, string title)
        {
            WindowSearchData searchData = new WindowSearchData
            {
                Class = wndclass,
                Title = title,
                Handles = new HashSet<IntPtr>()
            };

            EnumWindows(new EnumWindowsProc(WindowEnumProc), ref searchData);
            return searchData.Handles;
        }

        private static void RearrangeWindows(List<TileableWindow> windows, int numCols, int numRows, int offsetX, int maxWidth, int maxHeight)
        {
            int windowIndex = 0;
            for (int r = 0; r < numRows; r++)
            {
                for (int c = 0; c < numCols; c++)
                {
                    if (windowIndex >= windows.Count)
                    {
                        return;
                    }

                    TileableWindow window = windows[windowIndex++];

                    double aspectRatio = (double)window.Size.Width / window.Size.Height;
                    int width = (int)Math.Round(maxHeight * aspectRatio);
                    int height = (int)Math.Round(maxWidth / aspectRatio);
                    int x = offsetX + c * width + (maxWidth - width);
                    int y = r * maxHeight;

                    Console.WriteLine($"Moving {window} to ({x}, {y}). Resizing to ({width}, {height})");
                    MoveWindow(window.Handle, x, y, width, height, false);
                }
            }
        }

        private static Screen GetScreen()
        {
            int i = 1;
            Screen screen = Screen.PrimaryScreen;
            if (Screen.AllScreens.Length > 1)
            {
                foreach (Screen s in Screen.AllScreens)
                {
                    Console.WriteLine($"{i++}. {(s.Primary ? "Primary" : "Secondary")} @ {s.Bounds}");
                }

                Console.Write($"Found {Screen.AllScreens.Length} screens. Choose screen: ");
                int screenIndex = int.Parse(Console.ReadLine());
                screen = Screen.AllScreens[screenIndex - 1];
            }

            return screen;
        }

        private static bool WindowEnumProc(IntPtr hWnd, ref WindowSearchData searchData)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);

            if (sb.ToString().Equals(searchData.Class))
            {
                sb = new StringBuilder(1024);
                GetWindowText(hWnd, sb, sb.Capacity);

                if (sb.ToString().Equals(searchData.Title))
                {
                    searchData.Handles.Add(hWnd);
                }
            }

            return true;
        }

        private static List<TileableWindow> GetWindows()
        {
            // Find all the Firefox Picture-in-Picture windows
            HashSet<IntPtr> hWnds = FindWindows("MozillaDialogClass", "Picture-in-Picture");

            List<TileableWindow> windows = new List<TileableWindow>();
            foreach (IntPtr hWnd in hWnds)
            {
                RECT rect = new RECT();
                GetWindowRect(hWnd, ref rect);

                if (rect.Left == 0 && rect.Right == 0 && rect.Top == 0 && rect.Bottom == 0)
                {
                    continue;
                }

                Point point = new Point(rect.Left, rect.Top);
                Size size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);

                windows.Add(new TileableWindow()
                {
                    Handle = hWnd,
                    TopLeftCorner = point,
                    Size = size
                });
            }

            return windows;
        }
    }
}
