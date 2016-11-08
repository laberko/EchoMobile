using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Support.Design.Widget;
using Echo.Blog;
using Echo.Show;
using Echo.News;
using Echo.Person;
using HtmlAgilityPack;
using Android.Graphics;
using Echo.Player;

namespace Echo
{
    //common static data and methods
    public static class Common
    {
        public static int DisplayWidth;
        public static FloatingActionButton Fab;
        private static int _httpConnects;                       //active http connections counter (max 10 simultaneous sessions)
        public static List<PersonItem> PersonList;
        public static List<BlogContent> BlogContentList;
        public static List<NewsContent> NewsContentList;
        public static List<ShowContent> ShowContentList;
        public static DateTime[] SelectedDates;
        public static EchoMediaPlayer EchoPlayer;
        public static EchoPlayerServiceBinder ServiceBinder;
        public static readonly string[] ColorPrimary = { "#F44336", "#2196F3", "#4CAF50" };         //red, blue, green
        public static readonly string[] ColorPrimaryDark = { "#D32F2F", "#1976D2", "#388E3C" };
        public static readonly string[] ColorAccent = { "#B71C1C", "#0D47A1", "#1B5E20" };

        //download and parse a person's data
        public static async Task<PersonItem> GetPerson(string url)
        {
            var existingPerson = PersonList.FirstOrDefault(e => e.PersonUrl == url);
            if (existingPerson != null)
                return existingPerson;
            HtmlDocument root;
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
            var rootDiv = url.Contains("blog")
                ? root.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "profile min")
                : root.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "profile");
            var photoDiv = rootDiv?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("foto iblock"));
            var infoDiv = rootDiv?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("aboutperson iblock"));
            var nameNode = infoDiv?.Descendants("h1").FirstOrDefault();
            if (nameNode == null)
                return null;
            var photoNode = photoDiv?.Descendants("img").FirstOrDefault();
            var photoUrl = string.Empty;
            if (photoNode != null)
                photoUrl = "http://echo.msk.ru" + photoNode.Attributes["src"].Value.Replace("avatar2", "avatar");
            var p = new PersonItem
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
                var ratio = (float)widthLimit / bitmap.Width;
                bitmap = Bitmap.CreateScaledBitmap(bitmap, (int)Math.Round(ratio * bitmap.Width),
                    (int)Math.Round(ratio * bitmap.Height), true);
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
    }
}