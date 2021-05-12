﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        string ffmpegPath = "";
        IEnumerable<string> folders;


        public MainWindow()
        {
            InitializeComponent();
            ffmpegPath = @"C:\Users\Public\Programy\FFMpeg\ffmpeg-4.2.1-win64-static\bin\ffmpeg.exe";
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = @"D:\Kamera";
            dialog.IsFolderPicker = true;
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                folders = dialog.FileNames.ToList();

                var ffmpeg = new FFmpeg(ffmpegPath);


                foreach (var dayFolder in folders)
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

                            var progress = new Progress<double>(p=>
                            {
                                System.Diagnostics.Debug.WriteLine($"Progress {p}");
                                var progress = $"{p}/{files.Count()}";
                                progressTB.Dispatcher.Invoke(() => { progressTB.Text = progress; });
                            });

                            var hourOutputFilePath = Path.Combine(dayFolder, $"{folderName}-{hour}.mp4");
                            var hourArgs = $"-i \"concat:{string.Join('|', files)}\" -c copy -y -nostdin {hourOutputFilePath}";
                            await ffmpeg.ExecuteMuxAsync(hourArgs);
                        }
                    }

                    //var args = $"-i \"concat:{string.Join('|', allDayFiles)}\" -c copy -y -nostdin {outputFilePath}";
                    //await ffmpeg.ExecuteMuxAsync(args);
                }
            }
            progressTB.Dispatcher.Invoke(() => { progressTB.Text = "Done"; });
        }

        
        private async Task test()
        {
            var paths = new[] { @"D:\Kamera\2021-05-11\01\00.h264", @"D:\Kamera\2021-05-11\01\01.h264" };
            //var args = FFmpegArgs.BuildArgs(new[] { @"D:\Kamera\2021-05-11\01\00.h264", @"D:\Kamera\2021-05-11\01\01.h264" }, "D:\\Kamera\\f3.mp4");
            var args = $"-i \"concat:{string.Join('|',paths)}\" -c copy -y -nostdin D:\\Kamera\\f3.mp4";
            var ffmpeg = new FFmpeg(ffmpegPath);
            await ffmpeg.ExecuteMuxAsync(args);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = KnownFolders.Downloads.Path;
            dialog.IsFolderPicker = false;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ffmpegPath = dialog.FileName;
            }
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await test();
        }
    }
}