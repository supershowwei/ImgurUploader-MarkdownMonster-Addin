using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.AddIns;
using MarkdownMonsterImgurUploaderAddin.Helpers;
using MarkdownMonsterImgurUploaderAddin.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;

namespace MarkdownMonsterImgurUploaderAddin
{
    public partial class ImgurUploaderWindow : MetroWindow, INotifyPropertyChanged
    {
        private static readonly string DefaultStatusText = "Ready to upload image.";

        private static readonly string ConfigFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "config.json");

        private bool isUploading;

        public ImgurUploaderWindow()
        {
            this.InitializeComponent();

            var configSchema = new { ClientId = string.Empty };
            var savedConfig = File.Exists(ConfigFilePath)
                                  ? JsonConvert.DeserializeAnonymousType(File.ReadAllText(ConfigFilePath), configSchema)
                                  : configSchema;

            this.ImgurImage = new ImgurImageViewModel { ClientId = savedConfig.ClientId };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImgurImageViewModel ImgurImage { get; set; }

        public string StatusText => this.isUploading ? "Image uploading ..." : DefaultStatusText;

        public bool IsUploadEnable => !this.isUploading;

        private static byte[] ConvertClipboardDibToPngImageBytes(MemoryStream dibStream)
        {
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(ClipboardImageHelper.ImageFromClipboardDib(dibStream));
                encoder.Save(ms);

                return ms.ToArray();
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetIsUploading(bool value)
        {
            this.isUploading = value;
            this.OnPropertyChanged(nameof(this.StatusText));
            this.OnPropertyChanged(nameof(this.IsUploadEnable));
        }

        private void SaveConfiguration()
        {
            try
            {
                File.WriteAllText(
                    ConfigFilePath,
                    JsonConvert.SerializeObject(new { this.ImgurImage.ClientId }, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool Valid()
        {
            if (string.IsNullOrEmpty(this.ImgurImage.ClientId))
            {
                MessageBox.Show(
                    "Please input a valid ClientID.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return false;
            }

            if (!File.Exists(this.ImgurImage.FilePath))
            {
                MessageBox.Show("File is not exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                this.ImgurImage.FilePath = openFileDialog.FileName;
                this.OnPropertyChanged(nameof(this.ImgurImage));
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.Valid()) return;

            this.SetIsUploading(true);

            if (File.Exists(this.ImgurImage.FilePath))
            {
                await this.UploadImage(this.ImgurImage.FilePath);
                await Task.Run(() => this.SaveConfiguration());
            }

            this.SetIsUploading(false);
        }

        private async void ImgurUploaderForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyUp(Key.V)
                && Clipboard.GetData("DeviceIndependentBitmap") is MemoryStream ms)
            {
                if (!this.Valid()) return;

                this.SetIsUploading(true);

                var imageBytes = await Task.Run(() => ConvertClipboardDibToPngImageBytes(ms));

                await this.UploadImage(imageBytes);
                await Task.Run(() => this.SaveConfiguration());

                this.SetIsUploading(false);
            }
        }

        private async Task UploadImage(byte[] fileBytes)
        {
            try
            {
                var base64File = Convert.ToBase64String(fileBytes);

                var client = new RestClient("https://api.imgur.com");
                var request = new RestRequest("/3/image", Method.POST);
                request.AddHeader("Authorization", $"Client-ID {this.ImgurImage.ClientId}");
                request.AddParameter("image", base64File);

                var response = await client.ExecuteTaskAsync(request);
                var schema = new
                                 {
                                     Data = new { Error = string.Empty, Link = string.Empty },
                                     Success = false,
                                     Status = 0
                                 };
                var result = JsonConvert.DeserializeAnonymousType(response.Content, schema);

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

        private void ImgurUploaderForm_Activated(object sender, EventArgs e)
        {
            this.DataContext = this;
            mmApp.SetThemeWindowOverride(this);
        }
    }
}