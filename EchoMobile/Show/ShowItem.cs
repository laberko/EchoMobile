using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Echo.Person;
using HtmlAgilityPack;
using System.Linq;
using Android.Graphics;

namespace Echo.Show
{
    //single show item
    public class ShowItem
    {
        public Guid ShowId;
        public string ShowItemUrl;
        public DateTime ShowDateTime;
        public string ShowTitle;
        public string ShowSubTitle;
        public List<PersonItem> ShowModerators;
        public string ShowModeratorNames;
        public List<string> ShowModeratorUrls;
        public List<PersonItem> ShowGuests;
        public string ShowGuestNames;
        public List<string> ShowGuestUrls;
        private string _showText;
        public string ShowSoundUrl;
        public Bitmap ShowPicture;
        public int PlayerPosition;

        public ShowItem()
        {
            ShowModerators = new List<PersonItem>();
            ShowGuests = new List<PersonItem>();
        }

        //get subtitle and text content for a show in one array - invoked in ShowActivity
        public async Task<string[]> GetShowContent()
        {
            //subtitle
            if (!string.IsNullOrEmpty(_showText))
                return new[] { ShowSubTitle, _showText };
            HtmlDocument showRoot;
            try
            {
                showRoot = await Common.GetHtml(ShowItemUrl);
            }
            catch
            {
                return null;
            }
            var showTitleDiv = showRoot?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "title");
            var showSubTitleNode = showTitleDiv?.Descendants("h1").FirstOrDefault();
            ShowSubTitle = showSubTitleNode?.InnerText;
            //text
            var showTextDiv = showRoot?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value.Contains("typical"));
            if (showTextDiv == null)
                return new[] { ShowSubTitle, _showText };
            var showStringBuilder = new StringBuilder();
            showStringBuilder.AppendLine(@"<style>img{display: inline; height: auto; max-width: 100%;}</style>");
            showStringBuilder.AppendLine(@"<style>div{height: auto; max-width: 100%;}</style>");
            showStringBuilder.AppendLine(@"<style>iframe{height: auto; max-width: 100%;}</style>");
            showStringBuilder.AppendLine(@"<style>blockquote{font-weight: bold; font-style: italic; text-align: center; height: auto; max-width: 100%;}</style>");
            showStringBuilder.AppendLine(@"<body>");
            showStringBuilder.AppendLine(showTextDiv.InnerHtml);
            showStringBuilder.AppendLine(@"<p><a href=""" + ShowItemUrl + @"""><span>Источник - сайт Эхо Москвы</span></a></p>");
            showStringBuilder.AppendLine(@"</body>");
            _showText = showStringBuilder.ToString().Replace(@"""/", @"""http://echo.msk.ru/");
            return new[] { ShowSubTitle, _showText };
        }

        public async Task<Bitmap> GetShowPicture()
        {
            if (ShowPicture != null)
                return ShowPicture;
            foreach (var url in ShowGuestUrls.Union(ShowModeratorUrls))
            {
                var person = await Common.GetPerson(url);
                if (person == null)
                    continue;
                var picture = await Common.GetImage(person.PersonPhotoUrl);
                if (picture == null)
                    continue;
                ShowPicture = picture;
                return ShowPicture;
            }
            return null;
        }
    }
}