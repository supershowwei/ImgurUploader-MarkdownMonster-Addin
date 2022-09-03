using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MarkdownMonster;
using MarkdownMonster.Windows;
using MarkdownMonsterImgurUploaderAddin.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace MarkdownMonsterImgurUploaderAddin
{
    public partial class ImgurUploaderWindow : INotifyPropertyChanged
    {
        private static readonly string DefaultStatusText = "Ready to upload image.";

        private bool isUploading;

        public ImgurUploaderWindow()
        {
            this.InitializeComponent();

            this.ImgurImage = new ImgurImageViewModel
                                  {
                                      ClientId = ImgurUploaderConfiguration.Current.LastClientId,
                                      Api = ImgurUploaderConfiguration.Current.ApiUrl
                                  };

            this.OpenFileCommand = new CommandBase((s, c) => this.OpenFile(), (s, c) => true);
            this.UploadCommand = new CommandBase(async (s, c) => await this.UploadImage(), (s, c) => true);
            this.PasteCommand = new CommandBase(async (s, c) => await this.PasteImageAndUpload(), (s, c) => true);
            this.CancelCommand = new CommandBase((s, c) => this.Close(), (s, c) => true);
            this.OpenSettingsCommand = new CommandBase((s, c) => this.OpenSettings(), (s, c) => true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand OpenFileCommand { get; set; }
        
        public ICommand UploadCommand { get; set; }

        public ICommand PasteCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        public ICommand OpenSettingsCommand { get; set; }

        public ImgurImageViewModel ImgurImage { get; set; }

        public string StatusText => this.isUploading ? "Image uploading ..." : DefaultStatusText;

        public bool IsUploadEnable => !this.isUploading;

        private static byte[] ConvertClipboardImageToBytes()
        {
            if (!Clipboard.ContainsImage()) return null;

            var imgSource = Clipboard.GetImage();

            // TODO: probaly should support several image modes here based on a file name extension?
            using (var ms = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imgSource));
                encoder.Save(ms);
                ms.Flush();
                ms.Position = 0;
                return ms.ToArray();
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetImageFilePath(string value)
        {
            this.ImgurImage.FilePath = value;
            this.OnPropertyChanged(nameof(this.ImgurImage));
        }

        private void SetIsUploading(bool value)
        {
            this.isUploading = value;
            this.OnPropertyChanged(nameof(this.StatusText));
            this.OnPropertyChanged(nameof(this.IsUploadEnable));
        }

        private bool Valid()
        {
            WindowUtilities.FixFocus(this, this.TextAlternateText);

            if (string.IsNullOrEmpty(this.ImgurImage.ClientId))
            {
                MessageBox.Show(
                    "Please input a valid ClientID.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return false;
            }

            if (!string.IsNullOrEmpty(this.ImgurImage.FilePath) && !File.Exists(this.ImgurImage.FilePath))
            {
                MessageBox.Show("File is not exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                this.SetImageFilePath(openFileDialog.FileName);
            }
        }

        private async Task PasteImageAndUpload()
        {
            if (!Clipboard.ContainsImage()) return;

            this.SetImageFilePath(string.Empty);

            if (!this.Valid()) return;

            this.SetIsUploading(true);

            var imageBytes = ConvertClipboardImageToBytes();

            await this.UploadImage(imageBytes);

            this.SetIsUploading(false);
        }

        private async Task UploadImage()
        {
            if (string.IsNullOrEmpty(this.ImgurImage.FilePath)) return;
            if (!this.Valid()) return;

            this.SetIsUploading(true);

            if (File.Exists(this.ImgurImage.FilePath))
            {
                await this.UploadImage(this.ImgurImage.FilePath);

                ImgurUploaderConfiguration.Current.LastClientId = this.ImgurImage.ClientId;
            }

            this.SetIsUploading(false);
        }

        private void OpenSettings()
        {
            mmApp.Model.Window.OpenTab(Path.Combine(mmApp.Configuration.CommonFolder, "ImgurUploaderAddin.json"));
        }

        private async Task UploadImage(byte[] fileBytes)
        {
            try
            {
                var base64File = Convert.ToBase64String(fileBytes);

                var client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", this.ImgurImage.ClientId);

                var request = new HttpRequestMessage(HttpMethod.Post, this.ImgurImage.Api);

                request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["image"] = base64File });

                var response = await client.SendAsync(request);

                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeAnonymousType(
                    responseContent,
                    new { Data = new { Error = string.Empty, Link = string.Empty }, Success = false, Status = 0 });

                if (!result.Success)
                {
                    MessageBox.Show(result.Data.Error, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    return;
                }

                this.ImgurImage.Url = result.Data.Link;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Task UploadImage(string filePath)
        {
            return this.UploadImage(File.ReadAllBytes(filePath));
        }

        private void OnImgurUploaderFormActivated(object sender, EventArgs e)
        {
            this.DataContext = this;

            mmApp.SetThemeWindowOverride(this);

            this.ImageFilePathTextBox.Focus();
        }
    }
}