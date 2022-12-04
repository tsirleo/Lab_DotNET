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
using System.Threading;
using System.Runtime.CompilerServices;
using System.Net;
using Contracts;
using Polly.Retry;
using Polly;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Security.Policy;


namespace WPFApp_Client
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ImageInfo> imgDataCollection = new ObservableCollection<ImageInfo>();
        public EmoType emoType { get; set; } = EmoType.happiness;
        List<string> pathList = new List<string>();

        private CancellationTokenSource cts = new CancellationTokenSource();

        private readonly string apiUrl = "https://localhost:7173/images";
        private const int maxRetries = 5;
        private readonly AsyncRetryPolicy _retryPolicy;

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
        public int idInput { get; set; } = 1;

        public ICommand DropDB { get; private set; }
        public ICommand UploadDB { get; private set; }
        public ICommand UploadData { get; private set; }
        public ICommand DeleteElem { get; private set; }
        public ICommand ProcessImgs { get; private set; }
        public ICommand UploadImage { get; private set; }
        public ICommand Cancellation { get; private set; }
        public ICommand UploadEmotion { get; private set; }
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

        public MainWindow()
        {
            InitializeComponent();
            _retryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(maxRetries, times => TimeSpan.FromMilliseconds(times * 200));
        }


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;

            DropDB = new RelayCommand(_ => { OnDropDB(this); }, CanDropDB);
            UploadDB = new RelayCommand(_ => { OnUploadDB(this); }, CanUploadDB);
            UploadData = new RelayCommand(_ => { OnUploadData(this); }, CanUploadData);
            DeleteElem = new RelayCommand(_ => { OnDeleteElem(this); }, CanDelete);
            ProcessImgs = new RelayCommand(_ => { OnProcessImgs(this); }, CanProcess);
            UploadImage = new RelayCommand(_ => { OnUploadImage(this); }, CanUploadImage);
            Cancellation = new RelayCommand(_ => { OnCancellation(this); }, CanCancel);
            UpdateListBox = new RelayCommand(_ => { OnUpdateListBox(this); }, CanUpdate);
            UploadEmotion = new RelayCommand(_ => { OnUploadEmotion(this); }, CanUploadEmotion);
            ClearOutputFields = new RelayCommand(_ => { OnClearFields(this); }, CanClear);

            LoadDB();

            if (imgDataCollection.Count > 0) { SetFlags(false, false, true, true, true, true, true); }
            else { SetFlags(false, false, false, false, true, false, false); }
            infoblock.Text = DateTime.Now + "\n" + "The application has started.";
        }

        private async void LoadDB()
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    imgDataCollection.Clear();
                    imgDataCollection = await response.Content.ReadFromJsonAsync<ObservableCollection<ImageInfo>>();
                    ImgList.ItemsSource = imgDataCollection;
                    SortByEmotion();

                    infoblock.FontSize = 18;
                    infoblock.Text = DateTime.Now + "\n" + "Data has lodaded from the database.";
                }
                else
                {
                    var code = response.StatusCode;
                    var mess = await response.Content.ReadAsStringAsync();

                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;

                    imgDataCollection = new ObservableCollection<ImageInfo>();
                    ImgList.ItemsSource = imgDataCollection;
                }
            });
        }

        private void SortByEmotion()
        {
            string emotion = emoType.ToString();

            if (imgDataCollection.Count > 0) 
                imgDataCollection = new ObservableCollection<ImageInfo>(imgDataCollection.OrderByDescending(collection => collection.emotionsDict[emotion]));
            ImgList.ItemsSource = imgDataCollection;
        }

        private async void ProcessData(string path, CancellationToken ctn)
        {
            var byteSource = await File.ReadAllBytesAsync(path, ctn);

            var data = (byteSource, path);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                var response = await HttpClientJsonExtensions.PostAsJsonAsync(httpClient, apiUrl, data, ctn);

                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        imgDataCollection.Add(await response.Content.ReadFromJsonAsync<ImageInfo>());
                    }
                    else if (response.StatusCode == HttpStatusCode.NoContent)
                        throw new OperationCanceledException();
                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var code = (int)response.StatusCode;
                        var mess = await response.Content.ReadAsStringAsync();

                        infoblock.FontSize = 14;
                        infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;
                    }
                }
                else
                {
                    var code = (int)response.StatusCode;
                    var mess = await response.Content.ReadAsStringAsync();

                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;
                }
            });
        }

        private void OnProcessImgs(object sender)
        {
            infoblock.FontSize = 18;
            infoblock.Text = DateTime.Now + "\n" + "Data processing has started.";

            progressBar = 0.0;
            CancellationToken ctn = cts.Token;
            pgbarStep = 100.0 / pathList.Count;
            ProgressBar.Foreground = Brushes.Lime;
            imgDataCollection.Clear();

            try
            {
                SetFlags(true);

                foreach (var path in pathList)
                {
                    ProcessData(path, ctn);

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

            await _retryPolicy.ExecuteAsync(async () =>
            {
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.DeleteAsync($"{apiUrl}/{item.imageId}");
                if (response.IsSuccessStatusCode)
                {
                    imgDataCollection.Remove(item);

                    infoblock.FontSize = 18;
                    infoblock.Text = DateTime.Now + "\n" + "The selected item has been deleted.";
                }
                else
                {
                    var code = response.StatusCode;
                    var mess = await response.Content.ReadAsStringAsync();

                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;
                }
            });
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
        }

        private bool CanUploadDB(object sender)
        {
            if (dbFlag)
                return true;

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
            if (updFlag && imgDataCollection.Count > 0)
                return true;
            
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

        private async void OnDropDB(object sender)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    imgDataCollection.Clear();

                    infoblock.FontSize = 18;
                    infoblock.Text = DateTime.Now + "\n" + "All items in DB have been deleted.";
                }
                else
                {
                    var code = response.StatusCode;
                    var mess = await response.Content.ReadAsStringAsync();

                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;
                }
            });
        }

        private bool CanDropDB(object sender)
        {
            if (deleteFlag)
                return true;

            return false;
        }

        private async void OnUploadEmotion(object sender)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{apiUrl}/emotion/{emoType.ToString()}");

                if (response.IsSuccessStatusCode)
                {
                    imgDataCollection.Clear();
                    imgDataCollection = await response.Content.ReadFromJsonAsync<ObservableCollection<ImageInfo>>();
                    ImgList.ItemsSource = imgDataCollection;
                    SortByEmotion();

                    infoblock.FontSize = 18;
                    infoblock.Text = DateTime.Now + "\n" + $"Data has loaded from the database for \"{emoType.ToString()}\".";
                }
                else
                {
                    var code = response.StatusCode;
                    var mess = await response.Content.ReadAsStringAsync();

                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;

                    imgDataCollection = new ObservableCollection<ImageInfo>();
                    ImgList.ItemsSource = imgDataCollection;
                }
            });
        }

        private bool CanUploadEmotion(object sender)
        {
            if (dbFlag)
                return true;

            return false;
        }

        private async void OnUploadImage(object sender)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{apiUrl}/{idInput}");
                if (response.IsSuccessStatusCode)
                {
                    var image = await response.Content.ReadFromJsonAsync<ImageInfo>();
                    imgDataCollection.Clear();
                    imgDataCollection.Add(image);

                    ImgList.ItemsSource = imgDataCollection;

                    infoblock.FontSize = 18;
                    infoblock.Text = DateTime.Now + "\n" + $"Image with id = {idInput} is loaded.";
                }
                else
                {
                    var code = response.StatusCode;
                    var mess = await response.Content.ReadAsStringAsync();

                    infoblock.FontSize = 14;
                    infoblock.Text = DateTime.Now + "\n" + "Request status: " + code + "\n" + "Message: " + mess;

                    imgDataCollection = new ObservableCollection<ImageInfo>();
                    ImgList.ItemsSource = imgDataCollection;
                }
            });
        }

        private bool CanUploadImage(object sender)
        {
            if (dbFlag && idInput > 0) 
                return true;

            return false;
        }
    }
}
