using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Echo.ContentTypes
{
    public class NewsContent
    {
        private List<NewsItem> _news;
        public DateTime ContentDay;

        public NewsContent(DateTime day)
        {
            _news = new List<NewsItem>();
            ContentDay = day;
            //populate list with dummy items
            AddDummy(10);
        }

        public void AddDummy(int number)
        {
            var random = new Random();
            for (var i = 0; i < number; i++)
            {
                _news.Add(new NewsItem
                {
                    NewsId = Guid.NewGuid(),
                    NewsDateTime = ContentDay.Date==DateTime.Now.Date?DateTime.Now:ContentDay,
                    NewsTitle = TextGenerator(2, 10),
                    NewsText = TextGenerator(random.Next(10), 10)
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

        public int NewsCount => _news.Count;

        // Indexer (read only) for accessing a blog item
        public NewsItem this[int i]
        {
            get
            {
                _news = _news.OrderByDescending(n=>n.NewsDateTime).ToList();
                return _news[i];
            }
        }
    }
}