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
using MarkdownMonster.Windows;
using MarkdownMonsterImgurUploaderAddin.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;

namespace MarkdownMonsterImgurUploaderAddin
{
    public partial class ImgurUploaderWindow : MetroWindow, INotifyPropertyChanged
    {
        private static readonly string DefaultStatusText = "Ready to upload image.";

        private static readonly string AddinDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        
        private bool isUploading;

        private string originalClientId;

        public CommandBase PasteCommand { get; set; }

        public ImgurUploaderWindow()
        {
            this.InitializeComponent();

            var configSchema = new { Api = string.Empty };

            this.ImgurImage = new ImgurImageViewModel
            {
                ClientId = ImgurUploaderConfiguration.Current.LastClientId,
                Api = ImgurUploaderConfiguration.Current.ApiUrl
            };

            
            // Handle Ctrl-V on Form and file Textbox - others tbs aren't affected
            PasteCommand = new CommandBase((s, c) => PasteImageAndUpload(), (s,c)=>true);

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImgurImageViewModel ImgurImage { get; set; }

        public string StatusText => this.isUploading ? "Image uploading ..." : DefaultStatusText;

        public bool IsUploadEnable => !this.isUploading;

        private static byte[] ConvertClipboardImageToPngBytes()
        {
            if (!Clipboard.ContainsImage())
                return null;
                
            var imgSource = Clipboard.GetImage();

            // TODO: probaly should support several image modes here based on a file name extension?
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
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

        private void SaveClientId()
        {
            ImgurUploaderConfiguration.Current.LastClientId = ImgurImage.ClientId;
            this.originalClientId = this.ImgurImage.ClientId;                
        }

        private bool Valid()
        {
            WindowUtilities.FixFocus(this, TextAlternateText);

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
                await Task.Run(() => this.SaveClientId());
            }

            this.SetIsUploading(false);
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PasteImageButton_Click(object sender, RoutedEventArgs e)
        {
            PasteImageAndUpload();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            mmApp.Model.Window.OpenTab(Path.Combine(mmApp.Configuration.CommonFolder, "ImgurUploaderAddin.json"));
        }

        private async void PasteImageAndUpload()
        {
            if (!Clipboard.ContainsImage())
                return;

            this.SetImageFilePath(string.Empty);

            if (!this.Valid()) return;

            this.SetIsUploading(true);


            var imageBytes = ConvertClipboardImageToPngBytes();

            await this.UploadImage(imageBytes);
            this.SaveClientId();

            this.SetIsUploading(false);
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