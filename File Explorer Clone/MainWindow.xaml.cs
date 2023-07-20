using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;

using File = System.IO.File;

namespace File_Explorer_Clone
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0; // Large icon
        private const uint SHGFI_SMALLICON = 0x1; // Small icon

        private string path = @"C:\";
        private ImageSource cached_FolderIcon = null;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            locationTXT.Text = path;
            GetFolders(path);
            GetFiles(path);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                localDrives.Items.Add(drive.Name);
            }

            localDrives.Text = localDrives.Items[0].ToString();
            path = localDrives.Items[0].ToString();
        }

        private void localDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Remove();

            int index = localDrives.SelectedIndex;
            locationTXT.Text = localDrives.Items[index].ToString();

            GetFolders(locationTXT.Text);
            GetFiles(locationTXT.Text);
        }

        void GetFolders(string path)
        {
            try
            {
                foreach (string folder in Directory.GetDirectories(path))
                {
                    Display(folder);
                }
            }
            catch { /*MessageBox.Show(ex.Message);*/ }
        }

        void GetFiles(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    Display(file);
                }
            }
            catch { /*MessageBox.Show(ex.Message)*/; }
        }

        void Display(string text)
        {
            StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

            Label label = new Label()
            {
                Content = text.Split('\\').Last(),
                FontSize = 15,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            label.MouseDoubleClick += (sender, e) =>
            {
                MainWindow_MouseDoubleClick(sender, e, text.Split('\\').Last());
            };

            try
            {
                if (File.Exists(text))
                {
                    Icon fileIcon = System.Drawing.Icon.ExtractAssociatedIcon(text);
                    ImageSource fileImageSource = Imaging.CreateBitmapSourceFromHIcon(
                        fileIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    stackPanel.Children.Add(new System.Windows.Controls.Image() 
                    { 
                        Source = fileImageSource, 
                        Width = 25, 
                        Height = 25, 
                        Margin = new Thickness(5, 5, 0, 0) 
                    });

                    fileIcon.Dispose();
                }
                if (Directory.Exists(text))
                {
                    if(cached_FolderIcon == null)
                    {
                        SHFILEINFO shinfo = new SHFILEINFO();
                        IntPtr result = SHGetFileInfo(
                            text,
                            0,
                            out shinfo,
                            (uint)Marshal.SizeOf(shinfo),
                            SHGFI_ICON | SHGFI_LARGEICON);

                        if (result != IntPtr.Zero)
                        {
                            Icon folderIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
                            cached_FolderIcon = Imaging.CreateBitmapSourceFromHIcon(
                                folderIcon.Handle,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());

                            folderIcon.Dispose();
                        }
                    }

                    stackPanel.Children.Add(new System.Windows.Controls.Image()
                    {
                        Source = cached_FolderIcon,
                        Width = 25,
                        Height = 25,
                        Margin = new Thickness(5, 5, 0, 0)
                    });

                }
            }
            catch { }

            stackPanel.Children.Add(label);
            AllObjects.Children.Add(stackPanel);
        }

        void Remove()
        {
            List<StackPanel> stacks = AllObjects.Children.OfType<StackPanel>().ToList();
            stacks.ForEach(item => AllObjects.Children.Remove(item));
        }

        private void MainWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e, string folder)
        {
            string path = $"{locationTXT.Text}\\{folder}";

            if (Directory.Exists(path))
            {
                Remove();

                locationTXT.Text = path;

                GetFolders(locationTXT.Text);
                GetFiles(locationTXT.Text);
            }
            else
            {
                Process.Start(new ProcessStartInfo(path));
            }
        }

        private void ChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = locationTXT.Text.Replace(locationTXT.Text.Split('\\').Last(), string.Empty).TrimEnd('\\');

                if (!path.Contains("\\"))
                {
                    path += "\\";
                }

                locationTXT.Text = path;

                Remove();

                GetFolders(locationTXT.Text);
                GetFiles(locationTXT.Text);

            }
            catch
            {
                locationTXT.Text = localDrives.Text;
            }
        }
    }
}
