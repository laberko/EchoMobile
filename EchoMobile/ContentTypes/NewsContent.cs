using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Echo.ContentTypes
{
    public class NewsContent : INotifyPropertyChanged
    {
        private List<NewsItem> _news;
        public readonly List<NewsItem> NewContent;
        public DateTime ContentDay;
        public event PropertyChangedEventHandler PropertyChanged;

        public NewsContent(DateTime day)
        {
            News = new List<NewsItem>();
            NewContent = new List<NewsItem>();
            ContentDay = day;
            //populate list with dummy items for debug
            //ThreadPool.QueueUserWorkItem(o => AddDummy(10));
            //GetContent(10);
        }

        public List<NewsItem> News
        {
            get
            {
                return _news;
            }
            private set
            {
                _news = value;
                if (!Common.IsSwiping)
                {
                    NotifyPropertyChanged();
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void GetContent(int number)
        {
            //we add dummy content in prototype
            for (var i = 0; i < number; i++)
            {
                var random = new Random();
                Common.NewsNumber++;
                NewContent.Add(new NewsItem
                {
                    NewsId = Guid.NewGuid(),
                    NewsDateTime = ContentDay.Date==DateTime.Now.Date ? DateTime.Now : ContentDay.Date.AddHours(random.Next(23)).AddMinutes(random.Next(59)),
                    NewsTitle = Common.NewsNumber + " " + TextGenerator(2, 10),
                    NewsText = TextGenerator(100, 10)
                });
            }

            if (NewContent.Count == 0) return;
            var list = News;
            list.AddRange(NewContent);
            News = list.Distinct().ToList();
        }

        private static string TextGenerator(int sentenceCount, int wordCount)
        {
            var random = new Random();
            var randWordCount = random.Next(wordCount);
            const string chars = "éöóêåíãøùçõúôûâàïðîëäæýÿ÷ñìèòüáþ¸";
            const string capChars = "ÖÓÊÅÍÃØÙÇÕÔÂÀÏÐÎËÄÆÝß×ÑÌÈÒÁÞ";
            var sb = new StringBuilder();
            for (var i = 0; i < sentenceCount; i++)
            {
                sb.Append(new string(Enumerable.Repeat(capChars, 1).Select(s => s[random.Next(s.Length)]).ToArray()));
                for (var j = 0; j < (randWordCount + 1); j++)
                {
                    sb.Append(new string(Enumerable.Repeat(chars, random.Next(10)).Select(s => s[random.Next(s.Length)]).ToArray()));
                    sb.Append(j == randWordCount ? ". " : " ");
                }
            }
            return sb.ToString();
        }

        //public int NewsCount => News.Count;

        //indexer (read only) for accessing a blog item
        public NewsItem this[int i]
        {
            get
            {
                if (News.Count == 0) return null;
                News = News.OrderByDescending(n => n.NewsDateTime).ToList();
                return News[i];
            }
        }
    }
}