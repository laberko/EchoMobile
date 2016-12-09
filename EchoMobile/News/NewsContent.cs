using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HtmlAgilityPack;
using XamarinBindings.MaterialProgressBar;

namespace Echo.News
{
    //news content for a specific day
    public class NewsContent : AbstractContentFactory, INotifyPropertyChanged
    {
        private readonly MaterialProgressBar _progressBar;

        public NewsContent(DateTime day, MaterialProgressBar progressBar):base(day)
        {
            _progressBar = progressBar;
            GetContent();
        }

        //download and parse news content for the date
        public override sealed async void GetContent()
        {
            var allNewsUrl = ContentDate.Date == DateTime.Now.Date
                ? "http://echo.msk.ru/news/"
                : "http://echo.msk.ru/news/" + ContentDate.Year + "/" + ContentDate.Month + "/" + ContentDate.Day + ".html";
            var allNews = new List<AbstractContent>();
            HtmlDocument root;
            try
            {
                root = await Common.GetHtml(allNewsUrl);
            }
            catch
            {
                return;
            }
            var rootDiv = root?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "rel");
            var findDivs = rootDiv?.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("preview newsblock iblock"));
            if (findDivs == null)
                return;
            foreach (var div in findDivs)
            {
                var headerNode = div.Descendants("h3").FirstOrDefault();
                var urlNode = headerNode?.Descendants("a").FirstOrDefault();
                if (urlNode == null)
                    continue;
                var timeNode = headerNode.Descendants("span").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "datetime");
                if (timeNode == null)
                    continue;
                TimeSpan newsTime;
                if (!TimeSpan.TryParse(timeNode.InnerText, out newsTime))
                    continue;
                allNews.Add(new NewsItem(Common.ContentType.News)
                {
                    ItemId = Guid.NewGuid(),
                    ItemUrl = "http://echo.msk.ru" + urlNode.GetAttributeValue("href", string.Empty),
                    ItemDate = ContentDate.Date.Add(newsTime),
                    ItemTitle = urlNode.InnerText
                });
            }
            //array of new unique news
            var newContent = allNews.Where(news => ContentList.All(n => n.ItemTitle != news.ItemTitle)).ToArray();
            NewItemsCount = newContent.Length;
            if (NewItemsCount == 0)
                return;
            var list = ContentList;
            list.AddRange(newContent);
            //assign News property to raise PropertyChanged
            ContentList = list.OrderByDescending(n => n.ItemDate).ToList();
            _progressBar.Visibility = Android.Views.ViewStates.Gone;
        }
    }
}