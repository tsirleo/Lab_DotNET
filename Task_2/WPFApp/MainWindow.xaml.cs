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
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Concurrent;
using ModelLib;
using System.Threading;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.VisualBasic;

namespace WPFApp
{
    public partial class MainWindow : Window,  INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ImageData> imgDataCollection = new ObservableCollection<ImageData>();
        public EmoType emoType { get; set; } = EmoType.happiness; 
        private static EmotionDef emotionDef = new EmotionDef();
        private CancellationTokenSource cts = new CancellationTokenSource();
        List<string> pathList = new List<string>();
        private bool cancellationFlag = false;
        private bool processFlag = false;
        private bool clearFlag = false;
        private double pgbar = 0;
        private double pgbarStep;

        public ICommand UploadData { get; private set; }
        public ICommand ProcessImgs { get; private set; }
        public ICommand Cancellation { get; private set; }
        public ICommand UpdateListBox { get; private set; }
        public ICommand ClearOutputFields { get; private set; }

        public double progressBar
        {
            get { return pgbar; }
            set 
            {
                pgbar = value;
                RaisePropertyChanged(nameof(progressBar));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            UploadData = new RelayCommand(_ => { OnUploadData(this); });
            ProcessImgs = new RelayCommand(_ => { OnProcessImgs(this); }, CanProcess);
            Cancellation = new RelayCommand(_ => { OnCancellation(this); }, CanCancel);
            UpdateListBox = new RelayCommand(_ => { OnUpdateListBox(this); }, CanUpdate);
            ClearOutputFields = new RelayCommand(_ => { OnClearFields(this); }, CanClear);
        }

        private async Task ProcessData(string path, CancellationToken ctn)
        {
            var byteSource = await File.ReadAllBytesAsync(path, ctn);
            var resultDict = await emotionDef.ProcessAnImage(byteSource, ctn);
              
            imgDataCollection.Add(new ImageData(System.IO.Path.GetFileName(path), path, resultDict));
        }

        private void SortByEmotion()
        {
            string emotion = emoType.ToString();

            imgDataCollection = new ObservableCollection<ImageData>(imgDataCollection.OrderByDescending(collection => collection.emotionsDict[emotion]));
            ImgList.ItemsSource = imgDataCollection;
        }

        private void OnUploadData(object sender)
        {
            var dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            infoblock.Text = DateTime.Now + "\n" + "Selecting the target directory.";

            if (dlg.ShowDialog(this).GetValueOrDefault())
            {
                imgDataCollection.Clear();
                pathList.Clear();
                infoblock.Text = DateTime.Now + "\n" + "Data has uploaded.";
                var length = Directory.GetFiles(dlg.SelectedPath).Length;
                progressBar = 0.0;
                double step = 100.0 / length;
                ProgressBar.Foreground = Brushes.Lime;

                foreach (var imagePath in System.IO.Directory.GetFiles(dlg.SelectedPath)) 
                { 
                    pathList.Add(imagePath);
                    progressBar += step;
                }

                processFlag = true;
            }
            else
            {
                infoblock.Text = DateTime.Now + "\n" + "The application is in user's waiting state...";
            }
        }

        private async void OnProcessImgs(object sender)
        {
            infoblock.Text = DateTime.Now + "\n" + "Data processing has started.";

            progressBar = 0.0;
            CancellationToken ctn = cts.Token;
            pgbarStep = 100.0 / pathList.Count();
            ProgressBar.Foreground = Brushes.Lime;

            try
            {
                cancellationFlag = true;

                foreach (var path in pathList)
                {
                    await ProcessData(path, ctn);
                    progressBar += pgbarStep;
                }
             
                if (pathList.Count == imgDataCollection.Count)
                {
                    cancellationFlag = false;
                    clearFlag = true;
                    infoblock.Text = DateTime.Now + "\n" + "Data processing is complete.";
                    SortByEmotion();
                    processFlag = false;
                }
            }
            catch (OperationCanceledException)
            {
                infoblock.Text = DateTime.Now + "\n" + "Data processing has been canceled.";
                cts = new CancellationTokenSource();
                ProgressBar.Foreground = Brushes.OrangeRed;
                cancellationFlag = false;
                processFlag = false;
                clearFlag = true;
            }
            catch (Exception excp)
            {
                Console.WriteLine(excp);
            }
        }

        private void OnUpdateListBox(object sender)
        {
            SortByEmotion();
            infoblock.Text = DateTime.Now + "\n" + "Data in ListBox is updated.";
        }

        private bool CanUpdate(object sender)
        {
            if (imgDataCollection.Count > 0)
                return true;
            return false;
        }

        private bool CanProcess(object sender)
        {
            if (processFlag)
                return true;
            return false;
        }

        private void OnCancellation(object sender)
        {
            cts.Cancel();
            cancellationFlag = false;
        }

        private bool CanCancel(object sender)
        {
            if (cancellationFlag)
                return true;
            return false;
        }

        private void OnClearFields(object sender)
        {
            imgDataCollection.Clear();
            infoblock.Text = DateTime.Now + "\n" + "Uploaded data and output fields are cleared.";
            clearFlag = false;
            processFlag = true;
            progressBar = 0;
        }

        private bool CanClear(object sender)
        {
            if (clearFlag)
                return true;
            return false;
        }
    }
}
