using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Graphics;
using Echo.Blog;
using HtmlAgilityPack;
using System.Linq;
using Android.Widget;

//using XamarinBindings.MaterialProgressBar;

namespace Echo.Person
{
    //a single person class
    public class PersonItem
    {
        public string PersonName;
        public string PersonUrl;
        public string PersonPhotoUrl;
        private Bitmap _personPhoto;
        public string PersonAbout;
        public readonly MainActivity.PersonType PersonType;
        public List<AbstractContent> PersonContent;

        public PersonItem(MainActivity.PersonType personType)
        {
            PersonType = personType;
            PersonContent = new List<AbstractContent>();
        }

        //download and resize user picture
        public async Task<Bitmap> GetPersonPhoto(int widthLimit)
        {
            if ((string.IsNullOrEmpty(PersonPhotoUrl)) || (widthLimit < 0))
                return null;
            try
            {
                if (_personPhoto == null)
                {
                    byte[] imageBytes;
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = new TimeSpan(0, 0, 10);
                        imageBytes = await httpClient.GetByteArrayAsync(PersonPhotoUrl).ConfigureAwait(false);
                    }
                    if (imageBytes == null || imageBytes.Length <= 0)
                        return null;
                    _personPhoto = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                }
                if (widthLimit == 0)
                    return _personPhoto;
                var ratio = (float) widthLimit/_personPhoto.Width;
                return Bitmap.CreateScaledBitmap(_personPhoto, (int) Math.Round(ratio*_personPhoto.Width),
                    (int) Math.Round(ratio*_personPhoto.Height), true);
            }
            catch
            {
                return null;
            }
        }

        public async Task GetBlogHistory(ProgressBar progressBar)
        {
            var itemCounter = 0;
            for (var i = 1;; i ++)
            {
                if (itemCounter >= MainActivity.BlogHistorySize || PersonContent.Count >= MainActivity.BlogHistorySize)
                    break;
                var pageUrl = PersonUrl + @"archive/" + i + ".html";
                HtmlDocument root;
                try
                {
                    root = await MainActivity.GetHtml(pageUrl);
                }
                catch
                {
                    break;
                }
                var rootDiv = root?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "blogroll rel");
                if (rootDiv == null)
                    break;
                var findDivs = rootDiv.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "entry");
                if (findDivs.Count() == 0)
                    break;
                foreach (var div in findDivs)
                {
                    var contentDiv = div?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "title");
                    if (contentDiv == null)
                        continue;
                    var contentH = contentDiv.Descendants("h2").FirstOrDefault();
                    var contentUrl = "http://echo.msk.ru" + contentH?.Descendants("a").FirstOrDefault().GetAttributeValue("href", string.Empty);
                    if (PersonContent.Any(c => c.ItemUrl == contentUrl) || contentUrl == "http://echo.msk.ru")
                    {
                        if (PersonContent.Count >= MainActivity.BlogHistorySize)
                        {
                            progressBar.Visibility = Android.Views.ViewStates.Gone;
                            return;
                        }
                        continue;
                    }
                    var contentDate = contentDiv.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "date").InnerText;
                    DateTime blogDate;
                    PersonContent.Add(new BlogItem(MainActivity.ContentType.Blog)
                    {
                        ItemId = Guid.NewGuid(),
                        ItemUrl = "http://echo.msk.ru" + contentH?.Descendants("a").FirstOrDefault().GetAttributeValue("href", string.Empty),
                        ItemDate = DateTime.TryParse(contentDate, out blogDate) ? blogDate : DateTime.Now,
                        ItemTitle = contentH?.Descendants("a").FirstOrDefault().InnerText,
                        ItemPictureUrl = _personPhoto == null ? "http://echo.msk.ru/files/2261876.jpg" : PersonPhotoUrl,
                        ItemAuthorName = PersonName,
                        ItemAuthorUrl = PersonUrl ?? "http://echo.msk.ru/blog/echomsk/"
                    });
                    itemCounter ++;
                }
            }
            progressBar.Visibility = Android.Views.ViewStates.Gone;
        }
    }
}