using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HtmlAgilityPack;

namespace Echo.News
{
    //news content for a specific day
    public class NewsContent : INotifyPropertyChanged
    {
        private List<NewsItem> _news;
        public DateTime ContentDate;
        public event PropertyChangedEventHandler PropertyChanged;
        public int NewItemsCount;

        public NewsContent(DateTime day)
        {
            _news = new List<NewsItem>();
            ContentDate = day;
            GetContent();
        }

        //news collection
        public List<NewsItem> News
        {
            get
            {
                return _news;
            }
            private set
            {
                _news = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //raise PropertyChanged event and pass changed property name
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //download and parse news content for the date
        public async void GetContent()
        {
            var allNewsUrl = ContentDate.Date == DateTime.Now.Date
                ? "http://echo.msk.ru/news/"
                : "http://echo.msk.ru/news/" + ContentDate.Year + "/" + ContentDate.Month + "/" + ContentDate.Day + ".html";
            var allNews = new List<NewsItem>();
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
                allNews.Add(new NewsItem
                {
                    NewsId = Guid.NewGuid(),
                    NewsItemUrl = "http://echo.msk.ru" + urlNode.GetAttributeValue("href", string.Empty),
                    NewsDateTime = ContentDate.Date.Add(newsTime),
                    NewsTitle = urlNode.InnerText
                });
            }
            //array of new unique news
            var newContent = allNews.Where(news => News.All(n => n.NewsTitle != news.NewsTitle)).ToArray();
            NewItemsCount = newContent.Length;
            if (NewItemsCount == 0)
                return;
            var list = News;
            list.AddRange(newContent);
            //assign News property to raise PropertyChanged
            News = list.OrderByDescending(n => n.NewsDateTime).ToList();
        }

        //indexer (read only) for accessing a news item
        public NewsItem this[int i] => News.Count == 0 ? null : News[i];
    }
}