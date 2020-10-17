using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Ookii.Dialogs.Wpf;
using System.Windows.Threading;
using Interprocomm;

namespace audio_recorder_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Public Constructors

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                ReloadDevices();
                ReloadRecordingElements();

                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (sender, e) => ReloadRecordingElements();
                timer.Start();

                if (Directory.Exists(App.DataPath))
                {
                    tb_path.Text = "portable mode";
                    tb_path.IsEnabled = false;
                    btn_browse.IsEnabled = false;
                }
                else
                {
                    tb_path.Text = App.Config.SavePath;
                    tb_path.LostFocus += (sender, e) =>
                    {
                        App.Config.SavePath = tb_path.Text;
                        JSONSerializer.Serialize(App.ConfigPath, App.Config);
                        ReloadRecordingElements();
                    };
                }

                tb_time.Text = App.Config.TimeToRecord.TotalSeconds.ToString();
                tb_time.LostFocus += (sender, e) =>
                {
                    try
                    {
                        App.Config.TimeToRecord = TimeSpan.FromSeconds(int.Parse(tb_time.Text));
                        JSONSerializer.Serialize(App.ConfigPath, App.Config);

                        int bitrate = 0;
                        foreach (CheckBox deviceOut in lb_devicesOut.Items)
                            if (deviceOut.IsChecked.Value)
                                bitrate = Math.Max(bitrate, int.Parse(deviceOut.Tag as string));
                        foreach (CheckBox deviceIn in lb_devicesIn.Items)
                            if (deviceIn.IsChecked.Value)
                                bitrate = Math.Max(bitrate, int.Parse(deviceIn.Tag as string));

                        Request resp;
                        if ((resp = App.Client.SendRequest("-xs " + bitrate * App.Config.TimeToRecord.TotalSeconds / 1000)) != null)
                            ShowMessageBox(resp.StringData, "An error occurred", MessageBoxImage.Error);

                        ReloadRecordingElements();
                    }
                    catch (Exception ex) { App.logstream.Error(ex); }
                };

                btn_save.IsEnabled = App.Client.SendRequest("state").StringData == "recording";
                //TODO populate App.Config.RecordDevices if the server was already recording (utiliser "-v record")
            }
            catch (Exception e)
            {
                App.logstream.Error(e);
            }
        }

        #endregion Public Constructors

        #region Private Methods

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
                {
                    var browser_dialog = new VistaFolderBrowserDialog();
                    if (browser_dialog.ShowDialog().Value)
                    {
                        App.Config.SavePath = browser_dialog.SelectedPath;
                        JSONSerializer.Serialize(App.ConfigPath, App.Config);
                        tb_path.Text = App.Config.SavePath;
                    }
                }
                else
                    MessageBox.Show("You must indicate a path manually", "Can't open a folder browser.", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
            catch (Exception ex)
            {
                App.logstream.Log(ex);
            }
        }

        private void FillList(ListBox list, string request)
        {
            list.Items.Clear();
            foreach (var device in App.Client.SendRequest(request).StringData.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] deviceS = device.Split('|'); // id|name|bitrate
                var cb = new CheckBox()
                {
                    Content = deviceS[1],
                    Tag = deviceS[2],
                    IsChecked = App.Config.RecordDevices.Exists(i => i == deviceS[0])
                };
                cb.Checked += (sender, e) =>
                {
                    App.Config.RecordDevices.Add(deviceS[0]);
                    ReloadRecordingElements();
                };
                cb.Unchecked += (sender, e) =>
                {
                    App.Config.RecordDevices.Remove(deviceS[0]);
                    JSONSerializer.Serialize(App.ConfigPath, App.Config);
                    ReloadRecordingElements();
                };
                list.Items.Add(cb);
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = System.Text.RegularExpressions.Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(App.Config.SavePath);

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (App.Config.RecordDevices.Count < 1)
            {
                ShowMessageBox("You must select at least one device", "Can't start recording", MessageBoxImage.Error);
                return;
            }

            Request resp;
            if ((resp = App.Client.SendRequest("state")).StringData == "stopped")
            {
                var sb = new StringBuilder("-r ");
                foreach (var device in App.Config.RecordDevices)
                    sb.Append($"\"{device}\" ");
                if ((resp = App.Client.SendRequest(sb.ToString())) != null)
                    ShowMessageBox(resp.StringData, "An error occurred", MessageBoxImage.Error);
            }
            else if (resp.StringData == "recording")
            {
                if ((resp = App.Client.SendRequest("-sr")) != null)
                    ShowMessageBox(resp.StringData, "An error occurred", MessageBoxImage.Error);
            }
            else
                ShowMessageBox(resp.StringData, "An error occurred", MessageBoxImage.Error);
            ReloadRecordingElements();
        }

        private void ReloadDevices()
        {
            try
            {
                FillList(lb_devicesOut, "-v devices output");
                FillList(lb_devicesIn, "-v devices input");
            }
            catch (Exception ex)
            {
                App.logstream.Error(ex);
            }
        }

        private void ReloadLists_Click(object sender, RoutedEventArgs e) => ReloadDevices();

        private void ReloadRecordingElements()
        {
            try
            {
                Request resp;
                if ((resp = App.Client.SendRequest("state")).StringData == "recording")
                {
                    lb_devicesIn.IsEnabled = false;
                    lb_devicesOut.IsEnabled = false;
                    btn_browse.IsEnabled = false;
                    tb_path.IsEnabled = false;
                    tb_time.IsEnabled = false;
                    btn_record.IsEnabled = false;
                    btn_save.IsEnabled = true;
                    //TODO: Icon
                    btn_record.Content = "Stop";
                }
                else if (resp.StringData == "stopped")
                {
                    lb_devicesIn.IsEnabled = true;
                    lb_devicesOut.IsEnabled = true;
                    if (!Directory.Exists(App.AppPath))
                    {
                        btn_browse.IsEnabled = true;
                        tb_path.IsEnabled = true;
                    }
                    tb_time.IsEnabled = true;
                    btn_reload.IsEnabled = true;
                    btn_save.IsEnabled = false;
                    //TODO: Icon
                    btn_record.Content = "Start";
                }
                else
                {
                    ShowMessageBox(resp.StringData, "An error occurred", MessageBoxImage.Error);
                    return;
                }

                int avgBitrate = 0;
                string[] devices = { };
                if (!(resp = App.Client.SendRequest("-v record")).StringData.Contains("not recording"))
                    devices = resp.StringData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (CheckBox deviceOut in lb_devicesOut.Items)
                {
                    if (devices.Length > 0)
                        deviceOut.IsChecked = Array.Find(devices, device => device.Split('|')[1] == deviceOut.Content.ToString()) != default;
                    if (deviceOut.IsChecked.Value)
                        avgBitrate += int.Parse(deviceOut.Tag as string);
                }
                foreach (CheckBox deviceIn in lb_devicesIn.Items)
                {
                    if (devices.Length > 0)
                        deviceIn.IsChecked = Array.Find(devices, device => device.Split('|')[1] == deviceIn.Content.ToString()) != default;
                    if (deviceIn.IsChecked.Value)
                        avgBitrate += int.Parse(deviceIn.Tag as string);
                }
                if (App.Config.RecordDevices.Count > 0)
                    avgBitrate /= App.Config.RecordDevices.Count;

                tbl_ram.Text = (avgBitrate * App.Config.TimeToRecord.TotalSeconds / (1000 * 1000)).ToString("0.##") + " Mo";
            }
            catch (Exception ex)
            {
                App.logstream.Error(ex);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Request resp;
            if ((resp = App.Client.SendRequest($"-o \"{Path.Combine(App.Config.SavePath, $"audiorecorder_{DateTime.Now:yyyy_MM_dd-H_mm_ss}.wav")}\"")) != null)
                ShowMessageBox(resp.StringData, "An error occurred", MessageBoxImage.Error);
        }

        private void ShowMessageBox(string text, string caption, MessageBoxImage img, MessageBoxButton btn = MessageBoxButton.OK, MessageBoxResult res = MessageBoxResult.OK)
        {
            switch (img)
            {
                case MessageBoxImage.Error:
                    App.logstream.Error(text);
                    break;

                case MessageBoxImage.Warning:
                    App.logstream.Warning(text);
                    break;

                default:
                    App.logstream.Log(text);
                    break;
            }
            MessageBox.Show(text, caption, btn, img, res);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            App.Client.SendRequest("-s");
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Visibility = Visibility.Hidden;
            e.Cancel = true;
        }

        #endregion Private Methods
    }
}