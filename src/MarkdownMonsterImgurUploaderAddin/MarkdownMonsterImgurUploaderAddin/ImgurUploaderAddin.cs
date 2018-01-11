using System.IO;
using System.Reflection;
using System.Windows.Media;
using FontAwesome.WPF;
using MarkdownMonster;
using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderAddin : MarkdownMonsterAddin
    {
        private static readonly string AddinDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ImgurUploaderAddin()
        {
            this.Id = "ImgurUploaderAddin";
            this.Name = "ImgurUploader Addin";
        }

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();

            var menuItem =
                new AddInMenuItem(this) { Caption = "ImgurUploader", FontawesomeIcon = FontAwesomeIcon.Image };

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
            var form = new ImgurUploaderWindow { Owner = this.Model.Window };

            form.ShowDialog();

            if (!string.IsNullOrEmpty(form.ImgurImage.Url))
            {
                this.SetSelection($"![{form.ImgurImage.AlternateText}]({form.ImgurImage.Url})");
                this.SetEditorFocus();
                this.RefreshPreview();
            }
        }

        public override void OnExecuteConfiguration(object sender)
        {
            this.Model.Window.OpenTab(Path.Combine(AddinDirectory, "config.json"));
        }

        public override bool OnCanExecute(object sender)
        {
            return this.Model.IsEditorActive;
        }
    }
}