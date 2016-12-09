using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;

namespace Echo.Blog
{
    //single blog item
    public class BlogItem : AbstractContent
    {
        public BlogItem(Common.ContentType itemType) : base(itemType)
        {
            
        }
        //download and parse blog text
        public override async Task<string> GetHtml()
        {
            if (!string.IsNullOrEmpty(ItemText))
                return ItemText;
            HtmlDocument blogRoot;
            var mediaHtml = string.Empty;
            try
            {
                blogRoot = await Common.GetHtml(ItemUrl);
            }
            catch
            {
                return null;
            }
            var mediaDiv = blogRoot?.DocumentNode.Descendants().FirstOrDefault(n => (n.ChildAttributes("class").Any()) && (n.Attributes["class"].Value == "multimedia"));
            if (mediaDiv != null)
            {
                foreach (var d in mediaDiv.Descendants("iframe"))
                {
                    var src = d.Attributes["src"].Value;
                    mediaHtml += @"<h1><p><a href=""" + src + @"""><span>ВИДЕО</span></a></p></h1>";
                }
            }
            var textDiv = blogRoot?.DocumentNode.Descendants().FirstOrDefault(n => (n.ChildAttributes("class").Any()) && (n.Attributes["class"].Value == "typical include-relap-widget"));
            if (textDiv == null)
                return null;
            var blogStringBuilder = new StringBuilder();
            //style html and append downloaded divs
            blogStringBuilder.AppendLine(@"<style>img{display: inline; height: auto; max-width: 100%;}</style>");
            blogStringBuilder.AppendLine(@"<style>div{height: auto; max-width: 100%;}</style>");
            blogStringBuilder.AppendLine(@"<style>h1{font-family: sans-serif; font-weight: bold; text-align: center;}</style>");
            blogStringBuilder.AppendLine(@"<style>iframe{height: auto; max-width: 100%;}</style>");
            blogStringBuilder.AppendLine(@"<body>");
            if (!string.IsNullOrEmpty(mediaHtml))
                blogStringBuilder.AppendLine(mediaHtml);
            blogStringBuilder.AppendLine(textDiv.InnerHtml);
            blogStringBuilder.AppendLine(@"<p><a href=""" + ItemUrl + @"""><span>Источник - сайт Эхо Москвы</span></a></p>");
            blogStringBuilder.AppendLine(@"</body>");
            ItemText = blogStringBuilder.ToString().Replace(@"""/", @"""http://echo.msk.ru/");
            return ItemText;
        }
    }
}