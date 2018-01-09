using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using FontAwesome.WPF;
using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderAddin : MarkdownMonsterAddin
    {
        private static readonly string IconFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "imgur-uploader.png");

        public ImgurUploaderAddin()
        {
            this.Id = "ImgurUploaderAddin";
            this.Name = "ImgurUploader Addin";
        }

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();

            var menuItem = new AddInMenuItem(this)
                               {
                                   Caption = "ImgurUploader",
                                   FontawesomeIcon = FontAwesomeIcon.Image,
                                   ExecuteConfiguration = null
                               };

            try
            {
                menuItem.IconImageSource = new ImageSourceConverter().ConvertFromString(IconFilePath) as ImageSource;
            }
            catch
            {
                // ignored
            }

            this.MenuItems.Add(menuItem);
        }

        public override void OnExecute(object sender)
        {
            new ImgurUploader(this) { Owner = this.Model.Window }.ShowDialog();
        }

        public override void OnExecuteConfiguration(object sender)
        {
            MessageBox.Show(
                "Configuration from Imgur Addin",
                "Imgur Addin",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public override bool OnCanExecute(object sender)
        {
            return true;
        }
    }
}