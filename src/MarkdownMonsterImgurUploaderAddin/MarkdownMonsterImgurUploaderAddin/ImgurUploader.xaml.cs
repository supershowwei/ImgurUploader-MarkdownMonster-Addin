using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using MarkdownMonster.AddIns;
using Microsoft.Win32;
using Newtonsoft.Json;
using RestSharp;

namespace MarkdownMonsterImgurUploaderAddin
{
    /// <summary>
    ///     ImgurUploader.xaml 的互動邏輯
    /// </summary>
    public partial class ImgurUploader : MetroWindow
    {
        public ImgurUploader(MarkdownMonsterAddin addin)
        {
            this.Addin = addin;
            this.InitializeComponent();
            this.UploadForm.DataContext = new ImgurImage();
        }

        public MarkdownMonsterAddin Addin { get; }

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

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                this.ImageFilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var uploadButton = (Button)sender;
            var imgurImage = (ImgurImage)this.UploadForm.DataContext;

            uploadButton.Content = "Uploading...";
            uploadButton.IsEnabled = false;

            if (File.Exists(imgurImage.FilePath))
            {
                await this.UploadImage(File.ReadAllBytes(imgurImage.FilePath), imgurImage.AlternateText);
            }

            uploadButton.IsEnabled = true;
            uploadButton.Content = "Upload";
        }

        private async void MetroWindow_KeyUp(object sender, KeyEventArgs e)
        {
            var imgurImage = (ImgurImage)this.UploadForm.DataContext;

            if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyUp(Key.V))
            {
                this.UploadButton.Content = "Uploading...";
                this.UploadButton.IsEnabled = false;

                if (Clipboard.GetData("DeviceIndependentBitmap") is MemoryStream ms)
                {
                    var imageBytes = await Task.Run(() => ConvertClipboardDibToPngImageBytes(ms));

                    await this.UploadImage(imageBytes, imgurImage.AlternateText);
                }

                this.UploadButton.IsEnabled = true;
                this.UploadButton.Content = "Upload";
            }
        }

        private async Task UploadImage(byte[] fileBytes, string alternateText)
        {
            try
            {
                var base64File = Convert.ToBase64String(fileBytes);

                var client = new RestClient("https://api.imgur.com");
                var request = new RestRequest("/3/image", Method.POST);
                request.AddHeader("Authorization", "Client-ID 5c8a42b6ed516d3");
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

                this.Addin.SetSelection($"![{alternateText}]({result.Data.Link})");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}