﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AmongUsCapture;
using AmongUsCapture.TextColorLibrary;
using AUCapture_WPF.IPC;
using ControlzEx.Theming;
using NLog;
using NLog.Targets;
using WpfScreenHelper;
using IPCAdapter = AUCapture_WPF.IPC.IPCAdapter;
using IWshRuntimeLibrary;
using NLog.Fluent;
using File = IWshRuntimeLibrary.File;
using URIStartResult = AUCapture_WPF.IPC.URIStartResult;

namespace AUCapture_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly ClientSocket socket = new ClientSocket();
        public static readonly DiscordHandler handler = new DiscordHandler();
        
        public void OnTokenHandler(object sender, StartToken token)
        {
            Settings.conInterface.WriteModuleTextColored("ClientSocket", Color.Cyan,
                $"Attempting to connect to host {Color.LimeGreen.ToTextColor()}{token.Host}{Color.White.ToTextColor()} with connect code {Color.Red.ToTextColor()}{token.ConnectCode}{Color.White.ToTextColor()}");
            socket.Connect(token.Host, token.ConnectCode);
        }

        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string description, string iconPath)
        {
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
 
            shortcut.Description = description;   // The description of the shortcut
            shortcut.IconLocation = iconPath;           // The icon of the shortcut
            shortcut.TargetPath = targetFileLocation;                 // The path of the file that will launch when the shortcut is run
            shortcut.Save();                                    // Save the shortcut
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var args = e.Args;
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncAll;
            ThemeManager.Current.SyncTheme();
             // needs to be the first call in the program to prevent weird bugs
             if (Settings.PersistentSettings.debugConsole)
                 AllocConsole();

            var uriStart = IPCAdapter.getInstance().HandleURIStart(e.Args);
            
            switch (uriStart)
            {
                case URIStartResult.CLOSE:
                    Environment.Exit(0);
                    break;
                case URIStartResult.PARSE:
                    Console.WriteLine($"Starting with args : {e.Args[0]}");
                    break;
                case URIStartResult.CONTINUE:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var r = new Random();
            var goingToPop = r.Next(101) < 5;
            if (!goingToPop)
            {
                if (DateTime.Now.Month == 12)
                {
                    new SplashScreen(Assembly.GetExecutingAssembly(), "SplashScreens\\SplashScreenChristmas.png").Show(true);
                }
                else
                {
                    new SplashScreen(Assembly.GetExecutingAssembly(), "SplashScreens\\SplashScreenNormal.png").Show(true);
                }
                //Console.WriteLine(string.Join(", ",Assembly.GetExecutingAssembly().GetManifestResourceNames())); //Gets all the embedded resources
            }
            else
            {
                new SplashScreen(Assembly.GetExecutingAssembly(), "SplashScreens\\SplashScreenPop.png").Show(true);
                try
                {
                    var req = System.Net.WebRequest.Create(
                        "https://github.com/denverquane/amonguscapture/raw/master/AUCapture-WPF/SplashScreens/popcat.wav");
                    using (Stream stream = req.GetResponse().GetResponseStream())
                    {
                        SoundPlayer myNewSound = new SoundPlayer(stream);
                        myNewSound.Load();
                        myNewSound.Play();
                    }
                }
                catch (Exception errrr)
                {
                    Console.WriteLine("Minor error");
                }
            }
            
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            Settings.conInterface = new WPFLogger(mainWindow);
            IPCAdapter.getInstance().OnToken += OnTokenHandler;
            socket.Init();
            IPCAdapter.getInstance().RegisterMinion();
            mainWindow.Loaded += (sender, args2) =>
            {
                Task.Factory.StartNew(() => GameMemReader.getInstance().RunLoop()); // run loop in background
                if (uriStart == URIStartResult.PARSE) IPCAdapter.getInstance().SendToken(args[0]);
            };
            mainWindow.Closing += (sender, args2) =>
            {
                Environment.Exit(0);
            };
            var logoResource = Application.GetResourceStream(new Uri("/logo/Logo.ico", UriKind.Relative)).Stream; //Load this stream
            if (logoResource is not null)
            {
                var logoPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsCapture", "AmongUsGUI", "Logo.ico");
                FileStream fileStream = System.IO.File.Open(logoPath, FileMode.Create);
                fileStream.Seek(0, SeekOrigin.Begin);
                logoResource.CopyTo(fileStream);
                logoResource.Flush();
            }
            CreateShortcut("AutoMuteUs Capture",
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                GetExecutablePath(),
                "AutoMuteUs Capture Program",
                Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmongUsCapture", "AmongUsGUI", "Logo.ico"));
            

            mainWindow.Show();

        }

        public static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
    }
}
