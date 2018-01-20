using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MarkdownMonster;
using MarkdownMonster.Windows;
using MarkdownMonsterImgurUploaderAddin.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;

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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImgurImageViewModel ImgurImage { get; set; }

        public string StatusText => this.isUploading ? "Image uploading ..." : DefaultStatusText;

        public bool IsUploadEnable => !this.isUploading;

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

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                this.SetImageFilePath(openFileDialog.FileName);
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            mmApp.Model.Window.OpenTab(Path.Combine(mmApp.Configuration.CommonFolder, "ImgurUploaderAddin.json"));
        }

        private async Task UploadImage(byte[] fileBytes)
        {
            try
            {
                var base64File = Convert.ToBase64String(fileBytes);

                var client = new RestClient(this.ImgurImage.Api);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Client-ID {this.ImgurImage.ClientId}");
                request.AddParameter("image", base64File);

                var response = await client.ExecuteTaskAsync(request);

                var imgurResultSchema = new
                                            {
                                                Data = new { Error = string.Empty, Link = string.Empty },
                                                Success = false,
                                                Status = 0
                                            };

                var result = JsonConvert.DeserializeAnonymousType(response.Content, imgurResultSchema);

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

            this.ImageFilePathTextBox.Focus();
        }
    }
}