using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;

namespace CameraH264ToMp4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string FFmpegPath
        {
            get { return Settings.FFmpegPath; }
            set { Settings.FFmpegPath = value; SaveSettings(Settings); ffmpegPathTB.Text = value; }
        }

        public string OutputFolder
        {
            get { return Settings.OutputFolderPath; }
            set { Settings.OutputFolderPath = value; SaveSettings(Settings); outputFolderTB.Text = value; }
        }

        private IEnumerable<string> inputFolders;

        private Settings Settings;

        public MainWindow()
        {
            InitializeComponent();
            Settings = GetSettings();
            outputFolderTB.Text = Settings.OutputFolderPath;
            ffmpegPathTB.Text = Settings.FFmpegPath;
        }

        private void ChooseInputFolders_Click(object sender, RoutedEventArgs e)
        {
            ShowFileDialog(dialog =>
            {
                dialog.IsFolderPicker = true;
                dialog.Multiselect = true;
            }, dialog => inputFolders = dialog.FileNames.ToList());
        }

        private void ChooseFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            ShowFileDialog(dialog =>
            {
                dialog.InitialDirectory = KnownFolders.Downloads.Path;
                dialog.IsFolderPicker = false;
            }, dialog => FFmpegPath = dialog.FileName);
        }

        private void ChooseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            ShowFileDialog(dialog =>
            {
                dialog.InitialDirectory = Settings.OutputFolderPath;
                dialog.IsFolderPicker = true;
                dialog.Multiselect = false;
            }, dialog => OutputFolder = dialog.FileName);
        }

        private void ShowFileDialog(Action<CommonOpenFileDialog> configure, Action<CommonOpenFileDialog> onResults)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            configure(dialog);
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                onResults(dialog);
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FFmpegPath))
            {
                progressTB.Text = "Can't find ffmpeg.exe";
                return;
            }
            if (string.IsNullOrEmpty(OutputFolder))
            {
                progressTB.Text = "Please choose output folder";
                return;
            }
            if (inputFolders == null || inputFolders.Count() == 0)
            {
                progressTB.Text = "Please choose input folder(s)";
                return;
            }

            var ffmpeg = new FFmpeg(FFmpegPath);

            try
            {
                foreach (var dayFolder in inputFolders)
                {
                    var folderName = Path.GetFileName(dayFolder);
                    var allDayFiles = new List<string>();
                    var hourFolders = Directory.EnumerateDirectories(dayFolder);
                    int folderNr = 1;
                    foreach (var hourFolder in hourFolders)
                    {
                        progressTB.Dispatcher.Invoke(() => { progressTB.Text = $"{folderNr}/{hourFolders.Count()}"; });
                        folderNr++;
                        var hour = Path.GetFileName(hourFolder);
                        var files = Directory.EnumerateFiles(hourFolder).Where(x => Path.GetExtension(x) == ".h264");
                        if (files.Any())
                        {
                            allDayFiles.AddRange(files);

                            var progress = new Progress<double>(p =>
                            {
                                var progress = $"{p}/{files.Count()}";
                                progressTB.Dispatcher.Invoke(() => { progressTB.Text = progress; });
                            });

                            var outputFolderPath = Path.Combine(OutputFolder, folderName);
                            var hourOutputFilePath = Path.Combine(outputFolderPath, $"{folderName}-{hour}.mp4");
                            Directory.CreateDirectory(outputFolderPath);
                            var hourArgs = $"-i \"concat:{string.Join('|', files)}\" -c copy -y -nostdin {hourOutputFilePath}";
                            await ffmpeg.ExecuteMuxAsync(hourArgs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            progressTB.Dispatcher.Invoke(() => { progressTB.Text = "Done"; });
        }

        private void SaveSettings(Settings settings)
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(settings);
                File.WriteAllText("settings.json", jsonString);
            }
            catch (Exception ex)
            {
            }
        }

        private Settings GetSettings()
        {
            try
            {
                var text = File.ReadAllText("settings.json");
                return JsonSerializer.Deserialize<Settings>(text);
            }
            catch (Exception ex)
            {
                return new Settings() { FFmpegPath = "ffmpeg.exe" };
            }
        }

        private void OpenOutputFolderInExplorer_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", OutputFolder);
        }
    }

    public class Settings
    {
        public string FFmpegPath { get; set; }
        public string OutputFolderPath { get; set; }
    }
}
