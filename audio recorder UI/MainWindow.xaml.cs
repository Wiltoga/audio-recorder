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
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;

namespace audio_recorder_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                ReloadDevices();

                if (System.IO.Directory.Exists(App.dataPath))
                {
                    tb_path.Text = "portable mode";
                    tb_path.IsEnabled = false;
                    btn_browse.IsEnabled = false;
                    btn_openFolder.IsEnabled = false;
                }
                else
                {
                    tb_path.Text = App.Config.SavePath;
                    tb_path.LostFocus += (sender, e) =>
                    {
                        App.Config.SavePath = tb_path.Text;
                        JSONSerializer.Serialize(App.configPath, App.Config);
                    };
                }

                tb_time.Text = App.Config.TimeToRecord.TotalSeconds.ToString();
                tb_time.LostFocus += (sender, e) => App.Config.TimeToRecord = new TimeSpan(0, 0, int.Parse(tb_time.Text));
            }
            catch (Exception e)
            {
                App.logstream.Error(e);
            }
        }

        private void ReloadDevices()
        {
            FillList(lb_devicesOut, "-v devices output");
            FillList(lb_devicesIn, "-v devices input");
        }

        private void FillList(ListBox list, string request)
        {
            list.Items.Clear();
            foreach (var device in App.Client.SendRequest(request).StringData.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] deviceS = device.Split('|'); // id|name
                var cb = new CheckBox()
                {
                    Content = deviceS[0],
                    IsChecked = App.Config.RecordDevices.Exists(i => i == deviceS[1])
                };
                cb.Checked += (sender, e) =>
                {
                    App.Config.RecordDevices.Add(deviceS[1]);
                    JSONSerializer.Serialize(App.configPath, App.Config);
                };
                cb.Unchecked += (sender, e) =>
                {
                    App.Config.RecordDevices.Remove(deviceS[1]);
                    JSONSerializer.Serialize(App.configPath, App.Config);
                };
                list.Items.Add(cb);
            }
        }

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
                        JSONSerializer.Serialize(App.configPath, App.Config);
                        tb_path.Text = App.Config.SavePath;
                    }
                }
                else
                    MessageBox.Show("Can't open a folder browser.", "You must indicate a path manually", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
            catch (Exception ex)
            {
                App.logstream.Log(ex);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(App.Config.SavePath);

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            ReloadRecordingElements();
            if (App.Client.SendRequest("state").StringData == "recording")
            {
                var sb = new StringBuilder("record ");
                foreach (var device in App.Config.RecordDevices)
                    sb.Append($"\"{device}\"");
            }
            else
                App.Client.SendRequest("stop");
        }

        private void ReloadRecordingElements()
        {
            if (App.Client.SendRequest("state").StringData == "recording")
            {
                lb_devicesIn.IsEnabled = false;
                lb_devicesOut.IsEnabled = false;
                tb_path.IsEnabled = false;
                tb_time.IsEnabled = false;
            }
            else
            {
                lb_devicesIn.IsEnabled = false;
                lb_devicesOut.IsEnabled = false;
            }
        }
    }
}