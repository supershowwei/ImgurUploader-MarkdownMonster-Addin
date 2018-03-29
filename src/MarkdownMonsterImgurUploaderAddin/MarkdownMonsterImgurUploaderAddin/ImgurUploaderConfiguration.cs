using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderConfiguration : BaseAddinConfiguration<ImgurUploaderConfiguration>
    {
        public ImgurUploaderConfiguration()
        {
            this.ConfigurationFilename = "ImgurUploaderAddin.json";
        }

        public string ApiUrl { get; set; } = "https://api.imgur.com/3/image";

        public string LastClientId { get; set; }
    }
}