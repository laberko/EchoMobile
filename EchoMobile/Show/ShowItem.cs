using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Echo.Person;
using HtmlAgilityPack;
using System.Linq;
using System.Net;
using Android.Graphics;

namespace Echo.Show
{
    //single show item
    public class ShowItem : AbstractContent
    {
        public readonly List<PersonItem> ShowModerators;
        public readonly List<PersonItem> ShowGuests;
        public List<string> ShowModeratorUrls;
        public List<string> ShowGuestUrls;
        public string ShowGuestNames;
        public string ShowModeratorNames;
        public int ShowPlayerPosition;
        public int ShowDuration;

        public ShowItem(MainActivity.ContentType itemType) : base(itemType)
        {
            ShowModerators = new List<PersonItem>();
            ShowModeratorUrls = new List<string>();
            ShowGuests = new List<PersonItem>();
            ShowGuestUrls = new List<string>();
        }

        //try to get a picture for the show
        public async Task<Bitmap> GetPicture(int widthLimit = 0)
        {
            if (ItemPicture != null)
                return ItemPicture;
            Bitmap picture;
            foreach (var url in ShowGuestUrls.Union(ShowModeratorUrls))
            {
                var person = await MainActivity.GetPerson(url, MainActivity.PersonType.Show);
                if (person == null)
                    continue;
                picture = await MainActivity.GetImage(person.PersonPhotoUrl, widthLimit);
                if (picture == null)
                    continue;
                ItemPicture = picture;
                return ItemPicture;
            }
            if (string.IsNullOrEmpty(ItemPictureUrl))
                return null;
            picture = await MainActivity.GetImage(ItemPictureUrl, widthLimit);
            if (picture == null)
                return null;
            ItemPicture = picture;
            return ItemPicture;
        }

        //bypass server redirects
        public Task<string> GetRealSoundUrl()
        {
            return Task.Run(() =>
            {
                var request = (HttpWebRequest)WebRequest.Create(ItemSoundUrl);
                request.AllowAutoRedirect = false;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var location = response.Headers[HttpResponseHeader.Location];
                    if (!string.IsNullOrEmpty(location))
                        ItemSoundUrl = location.Replace(@"http://1.cdn.", @"http://3.cdn.");
                    return ItemSoundUrl;
                }
            });
        }

        //download and parse show text and subtitle
        public override async Task<string> GetHtml()
        {
            if (!string.IsNullOrEmpty(ItemText))
                return ItemText;
            HtmlDocument showRoot;
            try
            {
                showRoot = await MainActivity.GetHtml(ItemUrl);
            }
            catch
            {
                return null;
            }
            //subtitle
            var showTitleDiv = showRoot?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "title");
            var showSubTitleNode = showTitleDiv?.Descendants("h1").FirstOrDefault();
            if (!string.IsNullOrEmpty(showSubTitleNode?.InnerText))
                ItemSubTitle = showSubTitleNode.InnerText;
            //text
            var showTextDiv = showRoot?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value.Contains("typical"));
            if (showTextDiv != null)
            {
                var showStringBuilder = new StringBuilder();
                showStringBuilder.AppendLine(@"<style>img{display: inline; height: auto; max-width: 100%;}");
                showStringBuilder.AppendLine(@"div{height: auto; max-width: 100%;}");
                showStringBuilder.AppendLine(@"iframe{height: auto; max-width: 100%;}");
                showStringBuilder.AppendLine(@"blockquote{font-weight: bold; font-style: italic; text-align: center; height: auto; max-width: 100%;}</style>");
                showStringBuilder.Append("<body text = ");
                showStringBuilder.Append(MainActivity.WebViewTextColor);
                showStringBuilder.Append(" link = ");
                showStringBuilder.Append(MainActivity.WebViewLinkColor);
                showStringBuilder.AppendLine(">");
                showStringBuilder.AppendLine(showTextDiv.InnerHtml);
                showStringBuilder.AppendLine(@"<p><a href=""" + ItemUrl + @"""><span>Источник - сайт Эхо Москвы</span></a></p>");
                showStringBuilder.AppendLine(@"</body>");
                ItemText = showStringBuilder.ToString().Replace(@"""/", @"""http://echo.msk.ru/");
            }
            //update persons' urls
            var personDiv = showRoot?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value.Contains("person "));
            var guestDivs = personDiv?.Descendants("div").Where(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "author iblock").ToList();
            var moderatorsDiv = personDiv?.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value.Contains("lead"));
            var moderatorAs = moderatorsDiv?.Descendants("a").ToList();
            if (guestDivs != null && ShowGuestUrls.Count != guestDivs.Count)
            {
                ShowGuestUrls = new List<string>();
                foreach (var guestDiv in guestDivs)
                {
                    var guestA = guestDiv?.Descendants("a").FirstOrDefault();
                    var url = guestA?.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(url))
                        ShowGuestUrls.Add("http://echo.msk.ru" + url);
                }
            }
            if (moderatorAs != null && ShowModeratorUrls.Count != moderatorAs.Count)
            {
                ShowModeratorUrls = new List<string>();
                foreach (var moderatorA in moderatorAs)
                {
                    var url = moderatorA?.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(url))
                        ShowModeratorUrls.Add("http://echo.msk.ru" + url);
                }
            }
            //update person lists
            if (ShowGuestUrls.Count > 0 && ShowGuests.Count != ShowGuestUrls.Count)
            {
                foreach (var url in ShowGuestUrls.Where(url => (ShowGuests.All(p => p.PersonUrl != url) && ShowModerators.All(p => p.PersonUrl != url))))
                {
                    ShowGuests.Add(await MainActivity.GetPerson(url, MainActivity.PersonType.Show));
                }
            }
            if (ShowModeratorUrls.Count > 0 && ShowModerators.Count != ShowModeratorUrls.Count)
            {
                foreach (var url in ShowModeratorUrls.Where(url => (ShowGuests.All(p => p.PersonUrl != url) && ShowModerators.All(p => p.PersonUrl != url))))
                {
                    ShowModerators.Add(await MainActivity.GetPerson(url, MainActivity.PersonType.Show));
                }
            }
            return ItemText;
        }
    }
}