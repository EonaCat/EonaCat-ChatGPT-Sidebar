/*
EonaCat ChatGPT Sidebar
Copyright (C) 2023 Jeroen Saey (EonaCat)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License

*/

using EonaCat.Json;
using HtmlAgilityPack;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace EonaCat.ChatGPT.Sidebar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static List<string> chatResponses = new List<string>();
        private const int BorderWidth = 20;

        public MainWindow()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;

            double monitorHeight = SystemParameters.PrimaryScreenHeight;

            Rect desktop = SystemParameters.WorkArea;

            // Get the height of the start bar
            var startBarHeight = SystemParameters.PrimaryScreenHeight - desktop.Height;

            // Calculate the height of the desktop minus the start bar
            var desktopHeightMinusStartBar = desktop.Height - startBarHeight;

            // Set the height and position of the window
            this.Height = desktopHeightMinusStartBar;
            this.Top = startBarHeight / 2;
            this.Left = -this.Width + BorderWidth;

            UpdateStartupButtonIcon();
            InitializeWebviewAsync();
        }

        private async void InitializeWebviewAsync()
        {
            // Wait for the DOMContentLoaded event to fire
            await webView.EnsureCoreWebView2Async();

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(hostName: "EonaCat",
                                           folderPath: Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                           accessKind: CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            webView.Source = new Uri("https://chat.openai.com/");
        }

        private void CoreWebView2_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webView.CoreWebView2.WebMessageReceived += MessageReceived;
        }

        void MessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            String content = args.TryGetWebMessageAsString();
            Debug.WriteLine("Received: " + content);
            if (!string.IsNullOrWhiteSpace(content))
            {
                // We should have an answer
                using (var synth = new SpeechSynthesizer())
                {
                    synth.SpeakCompleted += Synth_SpeakCompleted;
                    synth.Rate = 2;
                    synth.SetOutputToDefaultAudioDevice();
                    synth.Speak(content);
                }
            }
        }

        private void CoreWebView2_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            string text = System.IO.File.ReadAllText("./js/observer.js");
            webView.CoreWebView2.ExecuteScriptAsync(text);
            webView.CoreWebView2.ExecuteScriptAsync("observer(\"p\").all(function(all) {\r\n  window.chrome.webview.postMessage(all[all.length -1].innerText);\r\n        })");
        }

        private void Synth_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            // Do nothing
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);
            Debug.WriteLine("X: " + mousePosition.X + " => " + this.ActualWidth);
            Debug.WriteLine("Y: " + mousePosition.Y + " => " + this.ActualHeight);
            if (mousePosition.X > this.ActualWidth - BorderWidth ||
                mousePosition.Y > this.ActualHeight - BorderWidth)
            {
                // Mouse position is outside bounds
                visible = Sidebar.Visibility.Hidden;
                UpdateVisibility();
            }
            else
            {
                // Mouse position is within bounds
            }
        }

        private void RemoveAddStartupButtonLogic()
        {
            if (CheckStartupFolderForLnkFile("EonaCat ChatGPT Sidebar.lnk"))
            {
                DeleteLnkFileFromStartupFolder("EonaCat ChatGPT Sidebar.lnk");
            }
            else
            {
                CreateLnkFileInStartupFolder("EonaCat ChatGPT Sidebar.lnk");
            }

            UpdateStartupButtonIcon();
        }

        public void CreateLnkFileInStartupFolder(string lnkFileName)
        {
            // Get the path to the current WPF Windows app's executable file
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            // Get the path to the Windows Startup folder
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            // Combine the startup path with the name of the .lnk file to get the full path
            string lnkFilePath = System.IO.Path.Combine(startupPath, lnkFileName);

            // Create a new .lnk file in the Startup folder that points to the current WPF Windows app's executable file
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(lnkFilePath);
            shortcut.TargetPath = appPath;
            shortcut.Save();
        }

        public void DeleteLnkFileFromStartupFolder(string lnkFileName)
        {
            // Get the path to the Windows Startup folder
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            // Combine the startup path with the name of the .lnk file to get the full path
            string lnkFilePath = System.IO.Path.Combine(startupPath, lnkFileName);

            // Check if the .lnk file exists in the Startup folder
            if (System.IO.File.Exists(lnkFilePath))
            {
                // Delete the .lnk file from the Startup folder
                System.IO.File.Delete(lnkFilePath);
            }
        }

        private void UpdateStartupButtonIcon()
        {
            // Check if the .lnk file exists in the Startup folder
            bool lnkFileExists = CheckStartupFolderForLnkFile("EonaCat ChatGPT Sidebar.lnk");
            if (lnkFileExists)
            {
                startup.ToolTip = "Remove from windows startup";
                startup.Source = new BitmapImage(new Uri("windows_on.png", UriKind.Relative));
            }
            else
            {
                startup.ToolTip = "Add to windows startup";
                startup.Source = new BitmapImage(new Uri("windows_off.png", UriKind.Relative));
            }
        }

        public bool CheckStartupFolderForLnkFile(string lnkFileName)
        {
            // Get the path to the Windows Startup folder
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            // Combine the startup path with the name of the .lnk file to get the full path
            string lnkFilePath = System.IO.Path.Combine(startupPath, lnkFileName);

            // Check if the .lnk file exists in the Startup folder
            return System.IO.File.Exists(lnkFilePath);
        }

        private Visibility visible = Sidebar.Visibility.Hidden;

        public double DEFAULT_CHATWINDOW = -10;
        public double DEFAULT_PLAYGROUNDWINDOW = -10;

        private void show_hide_click(object sender, RoutedEventArgs e)
        {
            if (visible == Sidebar.Visibility.Chat)
            {
                visible = Sidebar.Visibility.Hidden;
            }
            else
            {
                visible = Sidebar.Visibility.Chat;
            }
            UpdateVisibility();
        }

        private void UpdateVisibility(double windowPosition = 0)
        {
            if (tabControl.SelectedIndex == 1)
            {
                windowPosition = DEFAULT_PLAYGROUNDWINDOW;
            }

            DoubleAnimation leftAnimation = new DoubleAnimation();
            leftAnimation.From = this.Left;

            if (visible == Sidebar.Visibility.Chat)
            {
                if (windowPosition != 0.0)
                {
                    leftAnimation.To = windowPosition;
                }
                else
                {
                    leftAnimation.To = DEFAULT_CHATWINDOW;
                }
            }
            else if (visible == Sidebar.Visibility.Hidden)
            {
                leftAnimation.To = -this.Width + 20;
            }

            leftAnimation.Duration = TimeSpan.FromSeconds(0.2f);
            this.BeginAnimation(Window.LeftProperty, leftAnimation);
        }

        private async void webView_Loaded(object sender, RoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            await webView2.EnsureCoreWebView2Async();
            await webView3.EnsureCoreWebView2Async();
        }

        private void credits_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://beta.openai.com/account/usage") { UseShellExecute = true });
        }

        private void close_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void startup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RemoveAddStartupButtonLogic();
        }

        private void logo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://EonaCat.com") { UseShellExecute = true });
        }
    }

    public enum Visibility
    {
        Hidden,
        Chat,
        Menu
    }
}