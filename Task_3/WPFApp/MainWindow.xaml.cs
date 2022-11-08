using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ModelLib;
using System.Threading;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Printing;

namespace WPFApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ImageInfo> imgDataCollection = new ObservableCollection<ImageInfo>();
        public EmoType emoType { get; set; } = EmoType.happiness;
        private static EmotionDef emotionDef = new EmotionDef();
        List<string> pathList = new List<string>();

        private CancellationTokenSource cts = new CancellationTokenSource();
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private HashAlgorithm hashAlg = MD5.Create();

        private bool cancellationFlag;
        private bool processFlag;
        private bool deleteFlag;
        private bool clearFlag;
        private bool uploadFlag;
        private bool updFlag;
        private bool dbFlag;

        private double pgbar = 0;
        private double pgbarStep;
        private int numImgs = 1;

        public ICommand UploadDB { get; private set; }
        public ICommand UploadData { get; private set; }
        public ICommand DeleteElem { get; private set; }
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

        private void SetFlags(bool cancellation = false, bool process = false, bool delete = false, bool clear = false, bool upload = false, bool db = false, bool update = false)
        {
            cancellationFlag = cancellation;
            processFlag = process;
            deleteFlag = delete;
            clearFlag = clear;
            uploadFlag = upload;
            updFlag = update;
            dbFlag = db;
        }

        private byte[] GetHash(byte[] source) => hashAlg.ComputeHash(source); 

        private bool ContainsAllKeys(Dictionary<string, double> dict)
        {
            if ( dict.ContainsKey("anger") && dict.ContainsKey("contempt") && dict.ContainsKey("disgust") && dict.ContainsKey("fear") 
                && dict.ContainsKey("happiness") && dict.ContainsKey("neutral") && dict.ContainsKey("sadness") && dict.ContainsKey("surprise")) 
                    return true;

            return false;
        }

        public MainWindow()
        {
            InitializeComponent();
        }


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            SetFlags(false, false, true, true, true, false, true);
            infoblock.Text = DateTime.Now + "\n" + "The application has started.";
            UploadData = new RelayCommand(_ => { OnUploadData(this); }, CanUploadData);
            UploadDB = new RelayCommand(_ => { OnUploadDB(this); }, CanUploadDB);
            DeleteElem = new RelayCommand(_ => { OnDeleteElem(this); }, CanDelete);
            ProcessImgs = new RelayCommand(_ => { OnProcessImgs(this); }, CanProcess);
            Cancellation = new RelayCommand(_ => { OnCancellation(this); }, CanCancel);
            UpdateListBox = new RelayCommand(_ => { OnUpdateListBox(this); }, CanUpdate);
            ClearOutputFields = new RelayCommand(_ => { OnClearFields(this); }, CanClear);
            LoadDB();
        }

        private async void LoadDB()
        {
            await semaphore.WaitAsync();
            using (var db = new DBContext())
            {
                var images = db.imgInfo.Include(item => item.image).ToList();

                imgDataCollection = new ObservableCollection<ImageInfo>(images);
                ImgList.ItemsSource = imgDataCollection;
            }
            semaphore.Release();

            SortByEmotion();
        }

        private void SortByEmotion()
        {
            string emotion = emoType.ToString();

            imgDataCollection = new ObservableCollection<ImageInfo>(imgDataCollection.OrderByDescending(collection => collection.emotionsDict[emotion]));
            ImgList.ItemsSource = imgDataCollection;
        }

        private bool CheckImgNotExistDB(byte[] hash, byte[] source)
        {
            semaphore.Wait();
            using (var db = new DBContext())
            {
                if (db.imgInfo.Any(x => Equals(x.hashCode, hash)))
                {
                    var query = db.imgInfo.Where(x => Equals(x.hashCode, hash)).Include(item => item.image);

                    if (query.Any(x => Equals(x.image.blob, source)))
                    {
                        semaphore.Release();
                        return false;
                    }
                }

                return true;
            }
        }

        private void SaveProcImgDB(ImageInfo imageData)
        {
            using (var db = new DBContext())
            {
                db.imgInfo.Add(imageData);
                db.SaveChanges();
            }

            semaphore.Release();
        }

        private async Task<ImageInfo> ProcessData(string path, CancellationToken ctn)
        {
            var byteSource = await File.ReadAllBytesAsync(path, ctn);
            var hash = GetHash(byteSource);

            if (CheckImgNotExistDB(hash, byteSource))
            {
                var resultDict = await emotionDef.ProcessAnImage(byteSource, ctn);

                if (resultDict != null && ContainsAllKeys(resultDict))
                {
                    var imgInstance = new ImageInfo(System.IO.Path.GetFileName(path), path, hash, resultDict);
                    imgInstance.SetBlob(byteSource, imgInstance);
                    return imgInstance;
                }
            }

            return null;
        }

        private async void OnProcessImgs(object sender)
        {
            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "Data processing has started.";

            progressBar = 0.0;
            CancellationToken ctn = cts.Token;
            pgbarStep = 100.0 / pathList.Count();
            ProgressBar.Foreground = Brushes.Lime;
            imgDataCollection.Clear();

            try
            {
                SetFlags(true);

                foreach (var path in pathList)
                {
                    var imgInstance = await ProcessData(path, ctn);

                    if (imgInstance != null)
                    {
                        SaveProcImgDB(imgInstance);
                        imgDataCollection.Add(imgInstance);
                    }

                    progressBar += pgbarStep;
                }
             
                if (imgDataCollection.Count != 0)
                {
                    SetFlags(false, false, true, true, true, true, true);
                    infoblock.Text = DateTime.Now + "\n" + "Data processing is complete.";
                    SortByEmotion();
                }
                else 
                {
                    SetFlags(false, false, false, false, true, true, false);
                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Data processing is complete." + "\n" + "All images are already in the databese.";
                }

                ImgList.Focus();
            }
            catch (OperationCanceledException)
            {
                infoblock.Text = DateTime.Now + "\n" + "Data processing has been canceled.";
                cts = new CancellationTokenSource();
                ProgressBar.Foreground = Brushes.OrangeRed;
                SetFlags(false, false, false, true, true, true);
                semaphore.Release();
            }
            catch (Exception excp)
            {
                Console.WriteLine(excp);
            }
        }

        private bool CanProcess(object sender)
        {
            if (processFlag)
                return true;
            return false;
        }

        private void OnUploadData(object sender)
        {
            var dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            imgDataCollection.Clear();

            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "Selecting the target directory.";

            if (dlg.ShowDialog(this).GetValueOrDefault())
            {
                imgDataCollection.Clear();
                pathList.Clear();
                infoblock.Text = DateTime.Now + "\n" + "Data has uploaded.";
                numImgs = Directory.GetFiles(dlg.SelectedPath).Length;
                progressBar = 0.0;
                double step = 100.0 / numImgs;
                ProgressBar.Foreground = Brushes.Lime;

                foreach (var imagePath in Directory.GetFiles(dlg.SelectedPath))
                {
                    pathList.Add(imagePath);
                    progressBar += step;
                }

                SetFlags(false, true, false, false, true, true, false);
            }
            else
            {
                SetFlags(false, false, false, false, true, true, false);
                infoblock.Text = DateTime.Now + "\n" + "The application is in user's waiting state...";
            }
        }

        private bool CanUploadData(object sender)
        {
            if (uploadFlag)
                return true;
            return false;
        }

        private async void OnDeleteElem(object sender)
        {
            var item = ImgList.SelectedItem as ImageInfo;
            if (item == null) return;

            await semaphore.WaitAsync();
            using (var db = new DBContext())
            {
                var img = db.imgInfo
                    .Where(x => Equals(x.hashCode, item.hashCode))
                    .Include(x => x.image)
                    .Where(x => Equals(x.image.blob, item.image.blob))
                    .FirstOrDefault();

                if (img == null)
                {
                    semaphore.Release();
                    return;
                }

                db.imgData.Remove(img.image);
                db.imgInfo.Remove(img);
                db.SaveChanges();
                imgDataCollection.Remove(item);
            }
            semaphore.Release();

            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "The selected item has been deleted.";
        }

        private bool CanDelete(object sender)
        {
            if (deleteFlag && ImgList.SelectedItems.Count > 0)
                return true;
            return false;
        }

        private void OnUploadDB(object sender)
        {
            pathList.Clear();
            LoadDB();
            SetFlags(false, false, true, true, true, false, true);

            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "Data has lodaded from the database.";
        }

        private bool CanUploadDB(object sender)
        {
            using (var db = new DBContext())
            {
                if (dbFlag && db.imgInfo.Any())
                    return true;
            }
            return false;
        }

        private void OnUpdateListBox(object sender)
        {
            SortByEmotion();

            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "Data in ListBox is updated.";
        }

        private bool CanUpdate(object sender)
        {
            using (var db = new DBContext())
            {
                if (updFlag && db.imgInfo.Any())
                    return true;
            }
            return false;
        }

        private void OnCancellation(object sender)
        {
            cts.Cancel();
            SetFlags(false, false, false, true, true, true, false);
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

            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "Uploaded data and output fields are cleared.";

            if (pathList.Count > 0)
                SetFlags(false, true, false, false, true, true, false);
            else
            {
                SetFlags(false, false, false, false, true, true, false);
            }
            progressBar = 0;
        }

        private bool CanClear(object sender)
        {
            if (clearFlag && imgDataCollection.Count > 0)
                return true;
            return false;
        }
    }
}
