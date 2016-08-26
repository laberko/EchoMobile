using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Echo.ContentTypes
{
    public class BlogContent
    {
        private readonly List<BlogItem> _blogs;
        public DateTime ContentDay;

        public BlogContent(DateTime day)
        {
            _blogs = new List<BlogItem>();
            ContentDay = day;
            //populate list with dummy items
            AddDummy(10);
        }

        public void AddDummy(int number)
        {
            var random = new Random();
            for (var i = 0; i < number; i++)
            {
                _blogs.Add(new BlogItem
                {
                    BlogId = Guid.NewGuid(),
                    BlogDate = ContentDay.Date,
                    BlogTitle = TextGenerator(1, 10),
                    BlogAuthor = new PersonItem
                    {
                        PersonId = Guid.NewGuid(),
                        PersonName = TextGenerator(1, 2)
                    },
                    BlogText = TextGenerator(random.Next(10), 10)
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

        public int BlogCount => _blogs.Count;

        // Indexer (read only) for accessing a blog item
        public BlogItem this[int i]
        {
            get
            {
                _blogs.Reverse();
                return _blogs[i];
            }
        }
    }
}