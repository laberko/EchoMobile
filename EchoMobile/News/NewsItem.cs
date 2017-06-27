using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;

namespace Echo.News
{
    //single news item
    public class NewsItem : AbstractContent
    {
        public NewsItem(MainActivity.ContentType itemType) : base(itemType)
        {
            
        }

        //download and parse news text
        public override async Task<string> GetHtml()
        {
            if (!string.IsNullOrEmpty(ItemText))
                return ItemText;
            HtmlDocument newsRoot;
            try
            {
                newsRoot = await MainActivity.GetHtml(ItemUrl);
            }
            catch
            {
                return null;
            }
            var newsRootDiv = newsRoot?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "column");
            var typicalDiv = newsRootDiv?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("typical"));
            var newsStringBuilder = new StringBuilder();
            newsStringBuilder.AppendLine(@"<style>img{display: inline; height: auto; max-width: 100%;}");
            newsStringBuilder.AppendLine(@"div{height: auto; max-width: 100%;}</style>");
            newsStringBuilder.Append("<body text = ");
            newsStringBuilder.Append(MainActivity.WebViewTextColor);
            newsStringBuilder.Append(" link = ");
            newsStringBuilder.Append(MainActivity.WebViewLinkColor);
            newsStringBuilder.AppendLine(">");
            if (typicalDiv != null)
            {
                newsStringBuilder.AppendLine(typicalDiv.InnerHtml);
                newsStringBuilder.AppendLine(@"<p><a href=""" + ItemUrl + @"""><span>Источник - сайт Эхо Москвы</span></a></p>");
            }
            newsStringBuilder.AppendLine(@"</body>");
            ItemText = newsStringBuilder.ToString().Replace(@"""/", @"""http://echo.msk.ru/");
            return ItemText;
        }
    }
}