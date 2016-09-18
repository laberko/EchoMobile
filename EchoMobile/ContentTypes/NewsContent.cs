using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Echo.ContentTypes
{
    public class NewsContent
    {
        public List<NewsItem> News;
        public DateTime ContentDay;

        public NewsContent(DateTime day)
        {
            News = new List<NewsItem>();
            ContentDay = day;
            //populate list with dummy items for debug
            //ThreadPool.QueueUserWorkItem(o => AddDummy(10));
            AddDummy(10);
        }

        public void AddDummy(int number)
        {
            for (var i = 0; i < number; i++)
            {
                var random = new Random();
                Common.NewsNumber++;
                News.Add(new NewsItem
                {
                    NewsId = Guid.NewGuid(),
                    NewsDateTime = ContentDay.Date==DateTime.Now.Date?DateTime.Now : ContentDay.Date.AddHours(random.Next(23)).AddMinutes(random.Next(59)),
                    NewsTitle = Common.NewsNumber + TextGenerator(2, 10),
                    NewsText = TextGenerator(100, 10)
                });
            }
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

        public int NewsCount => News.Count;

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