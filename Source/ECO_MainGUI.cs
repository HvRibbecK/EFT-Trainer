using System;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using EFT_Trainer.Config;

// Original GitHub Thread für ExternalCrosshairOverlay: https://github.com/gmastergreatee/ExternalCrosshairOverlay

namespace EFT_Trainer
{
    public partial class ECO_MainGUI : Window
    {
        //Zwei strings für Mouse Hooks
        public string str1;
        public static ECO_MainGUI str2;

        // global level crosshair window
        OverlayCrosshair crosshairOverlayWindow = new OverlayCrosshair();
        // global level app-config saver/loader
        ConfigSaver configSaver = new ConfigSaver();
        // global low-level keyboard hook
        KeyboardHook kHook;

        List<Process> allRunningProcesses;
        List<string> nonEmptyWindowNames = new List<string>();
        List<Color> crosshairColors = new List<Color>();
        List<string> crosshairColorNames = new List<string>();
        string attachedProcessFilePath = "";
        int offsetX = 0;
        int offsetY = 0;
        float minCrosshairScale = 0.0001F;
        float maxCrosshairScale = 50;

        public int OffsetX
        {
            get
            {
                return offsetX;
            }
            set
            {
                offsetX = value;
                SetOffsets();
            }
        }

        public int OffsetY
        {
            get
            {
                return offsetY;
            }
            set
            {
                offsetY = value;
                SetOffsets();
            }
        }

        bool isAttachedToSomeProcess = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ECO_MainGUI()
        {
            InitializeComponent();
            str2 = this;
            MouseHook.Start();
            MouseHook.MouseAction += new EventHandler(Event);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // loads all the crosshair colors
            LoadColors();
            // load all the processes
            LoadProcesses();
            minCrosshairScale = Convert.ToSingle(sldr_CrosshairScale.Minimum);
            maxCrosshairScale = Convert.ToSingle(sldr_CrosshairScale.Maximum);
            // attaching to event the handler
            crosshairOverlayWindow.AttachedToProcessComplete += AttachingToProcessComplete;
            // display the transparent crosshair window
            crosshairOverlayWindow.Show();
            // initializing all hotkeys
            kHook = new KeyboardHook();
            kHook.KeyCombinationPressed += KHook_KeyCombinationPressed;
        }

        // Hide Crosshair wenn RM gedrückt
        private void Event(object sender, EventArgs e)
        {
            if (str1 == "RM")
            {
                crosshairOverlayWindow.ToggleOverlayOpacity();
                Dispatcher.Invoke(() =>
                {
                    SetTitle();
                });
            }
        }

        // Key pressed abfragen
        private void KHook_KeyCombinationPressed(Key keyPressed)
        {
            if (isAttachedToSomeProcess)
            {
                //Coordination Manual Setup Mode (- Taste)
                if (keyPressed == Key.OemMinus)
                {
                    crosshairOverlayWindow.ToggleOffsetSetupMode();
                }

                //Hide Crosshair (+ Taste)
                if (keyPressed == Key.OemPlus)
                {
                    crosshairOverlayWindow.ToggleOverlayOpacity();
                    Dispatcher.Invoke(() =>
                    {
                        SetTitle();
                    });
                }

                //Hide Crosshair (TAB Taste)
                else if (keyPressed == Key.Tab)
                {
                    crosshairOverlayWindow.ToggleOverlayOpacity();
                    Dispatcher.Invoke(() =>
                    {
                        SetTitle(); 
                    });
                }

                //Hide Crosshair (ESC Taste)
                else if (keyPressed == Key.Escape)
                {
                    crosshairOverlayWindow.ToggleOverlayOpacity();
                    Dispatcher.Invoke(() =>
                    {
                        SetTitle();
                    });
                }

                // Befehle/Methoden für Manual Setup Mode
                if (crosshairOverlayWindow.OffsetSetupMode)
                {
                    if (keyPressed == Key.Up)
                    {
                        OffsetY = crosshairOverlayWindow.MoveCrosshairUp();
                    }
                    else if (keyPressed == Key.Down)
                    {
                        OffsetY = crosshairOverlayWindow.MoveCrosshairDown();
                    }
                    else if (keyPressed == Key.Left)
                    {
                        OffsetX = crosshairOverlayWindow.MoveCrosshairLeft();
                    }
                    else if (keyPressed == Key.Right)
                    {
                        OffsetX = crosshairOverlayWindow.MoveCrosshairRight();
                    }
                    else if (keyPressed == Key.W)
                    {
                        OffsetY = crosshairOverlayWindow.MoveCrosshairUp(true);
                    }
                    else if (keyPressed == Key.S)
                    {
                        OffsetY = crosshairOverlayWindow.MoveCrosshairDown(true);
                    }
                    else if (keyPressed == Key.A)
                    {
                        OffsetX = crosshairOverlayWindow.MoveCrosshairLeft(true);
                    }
                    else if (keyPressed == Key.D)
                    {
                        OffsetX = crosshairOverlayWindow.MoveCrosshairRight(true);
                    }
                }
            }
        }

        //Reload Processes Button
        private void ReloadProcesses_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        // Load Button
        private void LoadSelectedProcess_Click(object sender, RoutedEventArgs e)
        {
            // if selected combo box item isn't empty(empty one is at index -1)
            if (cmb_Processes.SelectedIndex >= 0)
            {
                var process = allRunningProcesses[cmb_Processes.SelectedIndex];
                if (!process.HasExited)
                {
                    crosshairOverlayWindow.AttachToProcess(process);
                }
                else
                {
                    MessageBox.Show("The selected process has exited. Please \"Reload Processes\" and select the process again.", "Process Exited", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        // Set Color Button
        private void ChangeCrosshairColor_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_color.SelectedIndex >= 0)
            {
                crosshairOverlayWindow.SetCrosshairColor = crosshairColors[cmb_color.SelectedIndex];
            }
        }

        // Crosshair Scale Slider
        private void CrosshairScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            crosshairOverlayWindow.SetCrosshairScale((float)e.NewValue);
            SetTitle();
        }

        // Crosshair Opacity Slider
        private void CrosshairTransparency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            crosshairOverlayWindow.SetCrosshairTransparency = (double)e.NewValue;
        }
        
        // Zeige alle Crosshair Settings wenn AttachedToProcess
        private void AttachingToProcessComplete(string processName, string processFilePath)
        {
            Dispatcher.Invoke(() =>
            {
                if (lbl_attachTo.Content.ToString() != processName)
                    lbl_attachTo.Content = processName;
                if (processName != "None")
                {
                    var config = configSaver.GetConfig(processFilePath);

                    // set color
                    cmb_color.SelectedIndex = config.CrosshairColorIndex;
                    if (cmb_color.SelectedIndex >= 0)
                    {
                        crosshairOverlayWindow.SetCrosshairColor = crosshairColors[cmb_color.SelectedIndex];
                    }

                    // set opacity value
                    sldr_Opacity.Value = config.CrosshairOpacity;
                    crosshairOverlayWindow.SetCrosshairTransparency = sldr_Opacity.Value;

                    // set crosshair scale
                    if (config.CrosshairScale >= minCrosshairScale && config.CrosshairScale <= maxCrosshairScale)
                    {
                        crosshairOverlayWindow.SetCrosshairScale(config.CrosshairScale);
                        sldr_CrosshairScale.Value = crosshairOverlayWindow.CrosshairScale;
                    }

                    // set crosshair mode+picture
                    if (crosshairOverlayWindow.SetCrosshairPic(config.CrosshairFileLocation))
                    {
                        var justFileName = (from x in config.CrosshairFileLocation.Split('\\')
                                            where !string.IsNullOrWhiteSpace(x)
                                            select x).LastOrDefault();

                        if (!String.IsNullOrWhiteSpace(justFileName))
                            lbl_crosshair_pic.Content = justFileName;
                        else if (String.IsNullOrWhiteSpace(justFileName))
                            lbl_crosshair_pic.Content = "Default";
                        else
                            lbl_crosshair_pic.Content = "Img_File_With_Invalid_Name";
                        cmb_color.IsEnabled = lbl_crosshair_pic.Content.ToString() == "Default";
                        btn_SetColor.IsEnabled = lbl_crosshair_pic.Content.ToString() == "Default";
                    }

                    // set crosshair offsets
                    OffsetX = config.OffsetX;
                    OffsetY = config.OffsetY;
                    crosshairOverlayWindow.SetCrosshairOffsets(OffsetX, OffsetY);

                    attachedProcessFilePath = processFilePath;
                    isAttachedToSomeProcess = true;
                    btn_saveConfig.IsEnabled = true;

                    // set title
                    SetTitle();
                }
                else
                {
                    attachedProcessFilePath = "";
                    isAttachedToSomeProcess = false;
                    btn_saveConfig.IsEnabled = false;
                }
            });
        }

        // Reset Pic Button
        private void btn_resetPic_Click(object sender, RoutedEventArgs e)
        {
            if (crosshairOverlayWindow.SetCrosshairPic(""))
            {
                lbl_crosshair_pic.Content = "Default";
                cmb_color.IsEnabled = true;
                btn_SetColor.IsEnabled = true;
            }
        }

        // Load Pic Button
        private void btn_loadPic_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                // Set filter for file extension and default file extension
                DefaultExt = ".png",
                Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpeg)|*.jpeg|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*"
            };

            // Display OpenFileDialog by calling ShowDialog method
            var result = dlg.ShowDialog();

            // Get the selected file name
            if (result == true)
            {
                // Open document
                var fileName = dlg.FileName;
                if (crosshairOverlayWindow.SetCrosshairPic(fileName))
                {
                    var justFileName = (from x in fileName.Split('\\')
                                        where !string.IsNullOrWhiteSpace(x)
                                        select x).LastOrDefault();
                    if (!String.IsNullOrWhiteSpace(justFileName))
                        lbl_crosshair_pic.Content = justFileName;
                    else
                        lbl_crosshair_pic.Content = "Img_File_With_Invalid_Name";
                    cmb_color.IsEnabled = false;
                    btn_SetColor.IsEnabled = false;
                }
            }
        }

        // Save Config Button
        private void btn_saveConfig_Click(object sender, RoutedEventArgs e)
        {
            var config = new OverlayConfig()
            {
                ProcessFilePath = attachedProcessFilePath,
                CrosshairColorIndex = cmb_color.SelectedIndex,
                CrosshairFileLocation = crosshairOverlayWindow.CrosshairImagePath,
                CrosshairOpacity = sldr_Opacity.Value,
                CrosshairScale = crosshairOverlayWindow.CrosshairScale,
                OffsetX = OffsetX,
                OffsetY = OffsetY
            };

            configSaver.SaveConfig(config);

            MessageBox.Show("Config saved successfully");
        }

        // Lädt alle Prozesse (zu finden im TaskManager) und ändert den Process Label Text
        private void LoadProcesses()
        {
            nonEmptyWindowNames.Clear();
            lbl_procCount.Content = "Loading...";

            // re-load all the processes in another thread to avoid GUI lag
            var processLoadThread = new Thread(Thread_LoadProcess);
            processLoadThread.Start();
        }

        //Filter für anzuzeigende Prozesse??
        //This method must run in seperate thread!
        private void Thread_LoadProcess()
        {
            // only collect process with a valid window title
            allRunningProcesses = (from process in Process.GetProcesses()
                                   where process.MainWindowHandle != IntPtr.Zero && !String.IsNullOrWhiteSpace(process.MainWindowTitle) && process.MainWindowTitle != "EFT Multifunction Tool by Haze"
                                   select process).ToList();

            // change the gui in accordance with the data collected
            Dispatcher.Invoke(() =>
            {
                // set the combo-box source to list of "Window Titles"
                nonEmptyWindowNames = (from process in allRunningProcesses
                                       select process.MainWindowTitle).ToList();
                cmb_Processes.ItemsSource = nonEmptyWindowNames;
                // update the found process count
                lbl_procCount.Content = allRunningProcesses.Count;
            });
        }

        //Lädt alle Farben für die Color ComboBox
        private void LoadColors()
        {
            // Black color
            crosshairColorNames.Add("Black");
            crosshairColors.Add(Colors.Black);

            // Red color
            crosshairColorNames.Add("Red");
            crosshairColors.Add(Colors.Red);

            // Blue color
            crosshairColorNames.Add("Blue");
            crosshairColors.Add(Colors.Blue);

            // Aqua color
            crosshairColorNames.Add("Aqua");
            crosshairColors.Add(Colors.Aqua);

            // Violet color
            crosshairColorNames.Add("Violet");
            crosshairColors.Add(Colors.Violet);

            // Brown color
            crosshairColorNames.Add("Brown");
            crosshairColors.Add(Colors.Brown);

            // SlateGray color
            crosshairColorNames.Add("SlateGray");
            crosshairColors.Add(Colors.SlateGray);

            // Chocolate color
            crosshairColorNames.Add("Chocolate");
            crosshairColors.Add(Colors.Chocolate);

            // Crimson color
            crosshairColorNames.Add("Crimson");
            crosshairColors.Add(Colors.Crimson);

            // LightGreen color
            crosshairColorNames.Add("LightGreen");
            crosshairColors.Add(Colors.LightGreen);

            // Maroon color
            crosshairColorNames.Add("Maroon");
            crosshairColors.Add(Colors.Maroon);

            // YellowGreen color
            crosshairColorNames.Add("YellowGreen");
            crosshairColors.Add(Colors.YellowGreen);

            cmb_color.ItemsSource = crosshairColorNames;
            cmb_color.SelectedIndex = 0;
        }

        // Zeigt die Offsets (Koordinaten) des Crosshairs im Menü (lbl_offsets)
        private void SetOffsets()
        {
            lbl_offsets.Content = offsetX + ", " + offsetY + " (x, y from center)";
        }

        // Zeigt den Titel des Menüs (Name und Staus des Crosshairs)
        private void SetTitle()
        {
            ECOWindow.Title = "EFT Trainer by Haze (Credits for Crosshair Overlay: gmastergreatee) - " + sldr_CrosshairScale.Value.ToString("n4") + " - [" + (!crosshairOverlayWindow.CrosshairToggled ? "Visible" : "Hidden") + "]";
        }
        
    }
}
