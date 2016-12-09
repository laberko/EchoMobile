using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Support.Design.Widget;
using Echo.Person;
using HtmlAgilityPack;
using Android.Graphics;
using Echo.Player;
using Echo.Show;
using Plugin.Settings;
using Plugin.Settings.Abstractions;
using XamarinBindings.MaterialProgressBar;

namespace Echo
{
    //common static data and methods
    public static class Common
    {
        public static int DisplayWidth;
        public static FloatingActionButton Fab;
        private static int _httpConnects;                       //active http connections counter (max 10 sessions)
        public static List<PersonItem> PersonList;
        public static List<AbstractContentFactory> BlogContentList;
        public static List<AbstractContentFactory> NewsContentList;
        public static List<AbstractContentFactory> ShowContentList;
        public static List<AbstractContent> ShowHistoryList;
        public static ShowItem[] PlayList;
        public static DateTime[] SelectedDates;
        public static EchoMediaPlayer EchoPlayer;
        public static EchoPlayerServiceBinder ServiceBinder;
        public static readonly string[] ColorPrimary = { "#F44336", "#2196F3", "#4CAF50" };         //red, blue, green
        public static readonly string[] ColorPrimaryDark = { "#D32F2F", "#1976D2", "#388E3C" };
        public static readonly string[] ColorAccent = { "#B71C1C", "#0D47A1", "#1B5E20" };

        public enum PersonType
        {
            Blog,
            Show
        }

        public enum ContentType
        {
            News,
            Blog,
            Show
        }

        #region App Settings
        private static ISettings AppSettings => CrossSettings.Current;

        private const string SetFontKey = "echomobile_font_key";
        private const int SetFontDefault = 18;

        private const string SetFolderKey = "echomobile_folder_key";
        private static readonly string SetFolderDefault = string.Empty;

        private const string SetShowHistorySizeKey = "echomobile_showhistorysize_key";
        private const int SetShowHistorySizeDefault = 20;

        private const string SetBlogHistorySizeKey = "echomobile_bloghistorysize_key";
        private const int SetBlogHistorySizeDefault = 20;


        public static int FontSize
        {
            get
            {
                return AppSettings.GetValueOrDefault(SetFontKey, SetFontDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue(SetFontKey, value);
            }
        }
        public static string FolderSettings
        {
            get
            {
                return AppSettings.GetValueOrDefault(SetFolderKey, SetFolderDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue(SetFolderKey, value);
            }
        }
        public static int ShowHistorySize
        {
            get
            {
                return AppSettings.GetValueOrDefault(SetShowHistorySizeKey, SetShowHistorySizeDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue(SetShowHistorySizeKey, value);
            }
        }
        public static int BlogHistorySize
        {
            get
            {
                return AppSettings.GetValueOrDefault(SetBlogHistorySizeKey, SetBlogHistorySizeDefault);
            }
            set
            {
                AppSettings.AddOrUpdateValue(SetBlogHistorySizeKey, value);
            }
        }

        #endregion

        //download and parse a person's data
        public static async Task<PersonItem> GetPerson(string url, PersonType type)
        {
            var existingPerson = PersonList.FirstOrDefault(e => (e.PersonUrl == url && e.PersonType == type));
            if (existingPerson != null)
                return existingPerson;
            HtmlDocument root;
            HtmlNode rootDiv;
            try
            {
                root = await GetHtml(url);
            }
            catch
            {
                return null;
            }
            if (root == null)
                return null;
            switch (type)
            {
                case PersonType.Blog:
                    rootDiv = root.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "profile min");
                    break;
                case PersonType.Show:
                    rootDiv = root.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "profile");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            var photoDiv = rootDiv?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("foto iblock"));
            var infoDiv = rootDiv?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("aboutperson iblock"));
            var nameNode = infoDiv?.Descendants("h1").FirstOrDefault();
            if (nameNode == null)
                return null;
            var photoNode = photoDiv?.Descendants("img").FirstOrDefault();
            var photoUrl = string.Empty;
            if (photoNode != null)
                photoUrl = "http://echo.msk.ru" + photoNode.Attributes["src"].Value.Replace(@"/avatar2/", @"/");
            var p = new PersonItem(type)
            {
                PersonName = nameNode.InnerText,
                PersonUrl = url,
                PersonPhotoUrl = photoUrl,
                PersonAbout = infoDiv.Descendants("span").FirstOrDefault() != null ? infoDiv.Descendants("span").First().InnerText : string.Empty
            };
            PersonList.Add(p);
            return p;
        }

        //common method to download html string
        public static async Task<HtmlDocument> GetHtml(string url)
        {
            if (string.IsNullOrEmpty(url) || _httpConnects > 10)
                return null;
            try
            {
                _httpConnects++;
                string html;
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(0, 0, 20);
                    html = await client.GetStringAsync(url);
                }
                if (string.IsNullOrEmpty(html)) return null;
                //convert html string to HtmlDocument
                var root = new HtmlDocument();
                root.LoadHtml(html);
                return root;
            }
            catch
            {
                return null;
            }
            finally
            {
                _httpConnects--;
            }
        }

        //common method to download and resize a photo
        public static async Task<Bitmap> GetImage(string url, int widthLimit = 0)
        {
            if (string.IsNullOrEmpty(url) || widthLimit < 0 || _httpConnects > 10)
                return null;
            Bitmap bitmap;
            try
            {
                _httpConnects++;
                byte[] imageBytes;
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(0, 0, 10);
                    imageBytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);
                }
                if (imageBytes == null || imageBytes.Length <= 0)
                    return null;
                bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                //resize bitmap to fit widthLimit
                if (widthLimit == 0)
                    return bitmap;
                var ratio = (float) widthLimit/bitmap.Width;
                bitmap = Bitmap.CreateScaledBitmap(bitmap, (int) Math.Round(ratio*bitmap.Width), (int) Math.Round(ratio*bitmap.Height), true);
            }
            catch
            {
                return null;
            }
            finally
            {
                _httpConnects--;
            }
            return bitmap;
        }


        public static async Task<List<AbstractContent>> UpdateShowHistory(MaterialProgressBar progressBar, string searchUrl,
            List<AbstractContent> currentContent = null)
        {
            //if not user's shows search
            if (currentContent == null)
            {
                if (ShowHistoryList == null)
                    ShowHistoryList = new List<AbstractContent>();
                currentContent = ShowHistoryList.Where(s => (s.ItemType == ContentType.Show && s.ItemRootUrl == searchUrl)).ToList();
            }

            var itemCounter = 0;
            for (var i = 1; ; i++)
            {
                if (itemCounter >= ShowHistorySize || currentContent.Count >= ShowHistorySize)
                    break;
                var pageUrl = searchUrl + @"archive/" + i + ".html";
                HtmlDocument root;
                try
                {
                    root = await GetHtml(pageUrl);
                }
                catch
                {
                    break;
                }
                var rootDiv = root?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "rel");
                if (rootDiv == null)
                    break;
                var findDivs = rootDiv.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("preview iblock"));
                if (findDivs.Count() == 0)
                    break;
                foreach (var showDiv in findDivs)
                {
                    var showAudioUrl = string.Empty;
                    var showSubTitle = string.Empty;
                    string showTitle;
                    DateTime showDateTime;
                    var div = showDiv.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "prevcontent");
                    if (div == null)
                        continue;
                    //date
                    var metaDiv = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "meta");
                    if (metaDiv == null)
                        continue;
                    var timeSpan = metaDiv.Descendants("span").FirstOrDefault(s => s.Attributes.Contains("class") && s.Attributes["class"].Value == "datetime");
                    if (timeSpan == null || !DateTime.TryParse(timeSpan.Attributes["title"].Value, out showDateTime))
                        continue;
                    //audio and text urls
                    var urlNode = metaDiv.Descendants("a").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "view");
                    if (urlNode == null)
                        continue;
                    string showTextUrl = urlNode.GetAttributeValue("href", string.Empty);
                    var mediaDiv = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "mediamenu");
                    var soundUrlNode = mediaDiv?.Descendants("a").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "download iblock");
                    if (soundUrlNode != null)
                        showAudioUrl = soundUrlNode.GetAttributeValue("href", string.Empty);
                    //no audio and no text - not interesting, skip
                    if (string.IsNullOrEmpty(showTextUrl) && string.IsNullOrEmpty(showAudioUrl))
                        continue;

                    //we already have a show with the same audio & date (identical) - stop parsing
                    if (currentContent.Any(s => s.ItemType == ContentType.Show && s.ItemSoundUrl == showAudioUrl && s.ItemDate == showDateTime))
                    {
                        if (currentContent.Count >= ShowHistorySize)
                        {
                            progressBar.Visibility = Android.Views.ViewStates.Gone;
                            return currentContent;
                        }
                        continue;
                    }

                    //title
                    var titleDiv = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "section");
                    if (titleDiv == null)
                        continue;
                    var titleNode = titleDiv.Descendants("a").FirstOrDefault(d => !d.Attributes.Contains("class"));
                    if (titleNode != null)
                        showTitle = titleNode.Descendants("strong").FirstOrDefault().InnerText;
                    else
                    {
                        titleNode = titleDiv.Descendants("a").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "lite");
                        showTitle = titleNode?.Descendants("span").FirstOrDefault().InnerText;
                    }
                    var showRootUrl = titleNode?.GetAttributeValue("href", string.Empty);
                    if (string.IsNullOrEmpty(showTitle))
                        continue;
                    //subtitle
                    var subTitleDiv = div.Descendants("p").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "txt");
                    var subTitleA = subTitleDiv?.Descendants("a").FirstOrDefault(a => a.Attributes.Contains("class") && a.Attributes["class"].Value == "dark");
                    var subTitleNode = subTitleA?.Descendants("strong").FirstOrDefault(a => a.Attributes.Contains("class") && a.Attributes["class"].Value == "title type2");
                    if (subTitleNode != null)
                        showSubTitle = subTitleNode.InnerText;
                    //picture
                    var pictureDiv = div.Descendants("p").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("author type1"));
                    var pictureA = pictureDiv?.Descendants("a").FirstOrDefault(a => a.Attributes.Contains("class") && a.Attributes["class"].Value == "dark");
                    var pictureSpan = pictureA?.Descendants("span").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "photo");
                    var photoImg = pictureSpan?.Descendants("img").FirstOrDefault();
                    var photoUrl = photoImg?.Attributes["src"].Value.Replace(@"/avatar_s2/", @"/");
                    if (!string.IsNullOrEmpty(photoUrl))
                        photoUrl = "http://echo.msk.ru" + photoUrl;
                    currentContent.Add(new ShowItem(ContentType.Show)
                    {
                        ItemId = Guid.NewGuid(),
                        ItemUrl = "http://echo.msk.ru" + showTextUrl,
                        ItemDate = showDateTime,
                        ItemTitle = showTitle,
                        ItemSubTitle = showSubTitle,
                        ItemSoundUrl = showAudioUrl,
                        ItemPictureUrl = photoUrl,
                        ItemRootUrl = !string.IsNullOrEmpty(showRootUrl) ? "http://echo.msk.ru" + showRootUrl : string.Empty
                    });
                    itemCounter++;
                }
            }
            progressBar.Visibility = Android.Views.ViewStates.Gone;
            return currentContent;
        }

    }
}