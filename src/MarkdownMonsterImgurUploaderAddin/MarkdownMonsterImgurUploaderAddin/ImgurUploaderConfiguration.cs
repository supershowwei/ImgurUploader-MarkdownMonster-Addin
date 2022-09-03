using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderConfiguration : BaseAddinConfiguration<ImgurUploaderConfiguration>
    {
        private string apiUrl;

        public ImgurUploaderConfiguration()
        {
            this.ConfigurationFilename = "ImgurUploaderAddin.json";
        }

        public string ApiUrl
        {
            get => "https://api.imgur.com/3/image";
            set => this.apiUrl = value;
        }

        public string LastClientId { get; set; }
    }
}