using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace MarkdownMonsterImgurUploaderAddin
{
    /// <summary>
    ///     ImgurUploader.xaml 的互動邏輯
    /// </summary>
    public partial class ImgurUploader : MetroWindow
    {
        public ImgurUploader()
        {
            this.InitializeComponent();
            this.UploadForm.DataContext = new ImgurImage();
        }

        private void OpenFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                this.ImageFilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void UploadButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var a = (ImgurImage)this.UploadForm.DataContext;

            //var base64File = Convert.ToBase64String(File.ReadAllBytes(@"D:\Pictures\20160920_220930.jpg"));

            //var client = new RestClient("https://api.imgur.com");
            //var request = new RestRequest("/3/image", Method.POST);
            //request.AddHeader("Authorization", "Client-ID 5c8a42b6ed516d3");
            //request.AddParameter("image", base64File);

            //var response = client.Execute(request);
            //var schema = new { Data = new { Error = string.Empty, Link = string.Empty }, Success = false, Status = 0 };
            //var result = JsonConvert.DeserializeAnonymousType(response.Content, schema);

            //this.SetSelection($"![test]({result.Data.Link})");
        }
    }
}