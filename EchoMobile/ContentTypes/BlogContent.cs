using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.ContentTypes
{
    public class BlogContent
    {
        public List<BlogItem> Blogs;
        public DateTime ContentDay;

        public BlogContent(DateTime day)
        {
            Blogs = new List<BlogItem>();
            ContentDay = day;
            //populate list with dummy items
            AddDummy(10);
        }

        public void AddDummy(int number)
        {
            var random = new Random();
            for (var i = 0; i < number; i++)
            {
                Blogs.Add(new BlogItem
                {
                    BlogId = Guid.NewGuid(),
                    BlogDate = ContentDay.Date == DateTime.Now.Date ? DateTime.Now : ContentDay.Date.AddHours(random.Next(23)).AddMinutes(random.Next(59)),
                    BlogTitle = TextGenerator(1, 10),
                    BlogAuthor = Common.PersonList.ElementAt(random.Next(0, Common.PersonList.Count)),
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

        public int BlogCount => Blogs.Count;

        // Indexer (read only) for accessing a blog item
        public BlogItem this[int i]
        {
            get
            {
                Blogs = Blogs.OrderByDescending(b => b.BlogDate).ToList();
                return Blogs[i];
            }
        }
    }
}