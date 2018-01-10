using System.Windows;
using System.Windows.Media;
using FontAwesome.WPF;
using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderAddin : MarkdownMonsterAddin
    {
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
                menuItem.IconImageSource = new ImageSourceConverter().ConvertFromString(
                                                   "pack://application:,,,/MarkdownMonsterImgurUploaderAddin;component/Assets/imgur-uploader_32x32.png")
                                               as ImageSource;
            }
            catch
            {
                // ignored
            }

            this.MenuItems.Add(menuItem);
        }

        public override void OnExecute(object sender)
        {
            new ImgurUploaderWindow(this) { Owner = this.Model.Window }.ShowDialog();
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