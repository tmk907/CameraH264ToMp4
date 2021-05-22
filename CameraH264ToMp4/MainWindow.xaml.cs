using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string ffmpegPath;
        public string FFmpegPath
        {
            get { return ffmpegPath; }
            set { ffmpegPath = value; ffmpegPathTB.Text = value; }
        }

        private string outputFolder;
        public string OutputFolder
        {
            get { return outputFolder; }
            set { outputFolder = value; outputFolderTB.Text = value; }
        }

        IEnumerable<string> inputFolders;

        public MainWindow()
        {
            InitializeComponent();
            FFmpegPath = @"C:\Users\Public\Programy\FFMpeg\ffmpeg-4.2.1-win64-static\bin\ffmpeg.exe";
            OutputFolder = @"D:\Kamera";
        }

        private void ChooseInputFolders_Click(object sender, RoutedEventArgs e)
        {
            ShowFileDialog(dialog =>
            {
                dialog.InitialDirectory = @"C:\";
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
                dialog.InitialDirectory = @"D:\Kamera";
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
            var ffmpeg = new FFmpeg(FFmpegPath);

            foreach (var dayFolder in inputFolders)
            {
                var folderName = Path.GetFileName(dayFolder);
                //var outputFilePath = Path.Combine(dayFolder, $"{folderName}.mp4");
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
                            System.Diagnostics.Debug.WriteLine($"Progress {p}");
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

                //var args = $"-i \"concat:{string.Join('|', allDayFiles)}\" -c copy -y -nostdin {outputFilePath}";
                //await ffmpeg.ExecuteMuxAsync(args);
            }

            progressTB.Dispatcher.Invoke(() => { progressTB.Text = "Done"; });
        }
    }
}
