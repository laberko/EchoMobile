using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Echo.ContentTypes
{
    public class BlogContent : INotifyPropertyChanged
    {
        private List<BlogItem> _blogs;
        public readonly List<BlogItem> NewContent;
        public DateTime ContentDay;
        public event PropertyChangedEventHandler PropertyChanged;

        public BlogContent(DateTime day)
        {
            Blogs = new List<BlogItem>();
            NewContent = new List<BlogItem>();
            ContentDay = day;
            //populate list with dummy items
            //GetContent(10);
        }

        public List<BlogItem> Blogs
        {
            get
            {
                return _blogs;
            }
            private set
            {
                _blogs = value;
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
            for (var i = 0; i < number; i++)
            {
                var random = new Random();
                NewContent.Add(new BlogItem
                {
                    BlogId = Guid.NewGuid(),
                    BlogDate = ContentDay.Date == DateTime.Now.Date ? DateTime.Now : ContentDay.Date.AddHours(random.Next(23)).AddMinutes(random.Next(59)),
                    BlogTitle = TextGenerator(1, 10),
                    BlogAuthor = Common.PersonList.ElementAt(random.Next(0, Common.PersonList.Count)),
                    BlogText = TextGenerator(random.Next(10, 100), 10)
                });
            }

            if (NewContent.Count == 0) return;
            var list = Blogs;
            list.AddRange(NewContent);
            Blogs = list.Distinct().ToList();
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

        //public int BlogCount => Blogs.Count;

        // Indexer (read only) for accessing a blog item
        public BlogItem this[int i]
        {
            get
            {
                if (Blogs.Count == 0) return null;
                Blogs = Blogs.OrderByDescending(b => b.BlogDate).ToList();
                return Blogs[i];
            }
        }
    }
}