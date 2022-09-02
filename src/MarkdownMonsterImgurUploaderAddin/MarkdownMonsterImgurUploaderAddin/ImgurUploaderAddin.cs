using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using FontAwesome.WPF;
using MarkdownMonster;
using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderAddin : MarkdownMonsterAddin
    {
        public override Task OnApplicationInitialized(AppModel model)
        {
            this.Id = "ImgurUploaderAddin";
            this.Name = "ImgurUploader Addin";

            var menuItem = new AddInMenuItem(this) { Caption = "ImgurUploader", FontawesomeIcon = FontAwesomeIcon.AddressBook };

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

            return Task.CompletedTask;
        }

        public override Task OnWindowLoaded()
        {
            return Task.CompletedTask;
        }

        public override Task OnExecute(object sender)
        {
            this.Model.Window.Dispatcher.InvokeAsync(
                () =>
                    {
                        var form = new ImgurUploaderWindow { Owner = this.Model.Window };

                        form.ShowDialog();

                        if (!string.IsNullOrEmpty(form.ImgurImage.Url))
                        {
                            this.SetSelection($"![{form.ImgurImage.AlternateText}]({form.ImgurImage.Url})");
                            this.SetEditorFocus();
                            this.RefreshPreview();
                        }

                        // save configuration settings
                        ImgurUploaderConfiguration.Current.Write();
                    });
            
            return Task.CompletedTask;
        }

        public override Task OnExecuteConfiguration(object sender)
        {
            this.Model.Window.OpenTab(Path.Combine(mmApp.Configuration.CommonFolder, "ImgurUploaderAddin.json"));
            
            return Task.CompletedTask;
        }

        public override bool OnCanExecute(object sender)
        {
            return this.Model.IsEditorActive;
        }
    }
}