using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkdownMonster.AddIns;

namespace MarkdownMonsterImgurUploaderAddin
{
    public class ImgurUploaderConfiguration : BaseAddinConfiguration<ImgurUploaderConfiguration>
    {
        public string ApiUrl { get; set; } = "https://api.imgur.com/3/image";

        public string LastClientId { get; set; }

        public ImgurUploaderConfiguration()
        {
            ConfigurationFilename = "ImgurUploaderAddin.json";
        }
    }
}
