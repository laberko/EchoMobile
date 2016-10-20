using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Echo.Person;
using HtmlAgilityPack;
using System.Linq;

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


        public ShowItem()
        {
            ShowModerators = new List<PersonItem>();
            ShowGuests = new List<PersonItem>();
        }

        //download mp3 file using standard Android DownloadManager
        public void DownloadAudio(Context context)
        {
            var uri = new Uri(ShowSoundUrl);
            var dm = (DownloadManager)Application.Context.GetSystemService(Context.DownloadService);
            var request = new DownloadManager.Request(Android.Net.Uri.Parse(ShowSoundUrl));
            request.SetDestinationInExternalFilesDir(context, Android.OS.Environment.DirectoryMusic, System.IO.Path.GetFileName(uri.LocalPath));
            request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);
            dm.Enqueue(request);
        }

        //open full show item
        public void OpenShowActivity(string action, Context context)
        {
            var showIntent = new Intent(context, typeof(ShowActivity));
            showIntent.SetFlags(ActivityFlags.NewTask);
            showIntent.PutExtra("Action", action);
            showIntent.PutExtra("ID", ShowId.ToString());
            context.StartActivity(showIntent);
        }

        //get subtitle and text content for a show in one array
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
    }
}