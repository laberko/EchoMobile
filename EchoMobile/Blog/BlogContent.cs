using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HtmlAgilityPack;

namespace Echo.Blog
{
    //blogs content for a specific day
    public class BlogContent : INotifyPropertyChanged
    {
        private List<BlogItem> _blogs;
        public DateTime ContentDate;
        public event PropertyChangedEventHandler PropertyChanged;
        public int NewItemsCount;

        public BlogContent(DateTime day)
        {
            Blogs = new List<BlogItem>();
            ContentDate = day;
            GetContent();
        }

        //blogs collection
        public List<BlogItem> Blogs
        {
            get
            {
                return _blogs;
            }
            private set
            {
                _blogs = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //raise PropertyChanged event and pass changed property name
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //download and parse blogs content for the date
        public async void GetContent()
        {
            var allBlogs = new List<BlogItem>();
            var allBlogsUrl = ContentDate.Date == DateTime.Now.Date
                ? "http://echo.msk.ru/blog/"
                : "http://echo.msk.ru/blog/" + ContentDate.ToString("yyyy-MM-dd") + ".html";
            HtmlDocument root;
            try
            {
                root = await Common.GetHtml(allBlogsUrl);
            }
            catch
            {
                return;
            }
            var rootDiv = root?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "rel");
            var findDivs = rootDiv?.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("preview iblock"));
            if (findDivs == null)
                return;
            foreach (var div in findDivs)
            {
                DateTime blogDate;
                var contentDiv = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "prevcontent");
                if (contentDiv == null)
                    continue;
                var authorDiv = contentDiv.Descendants("p").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "author type2");
                var authorHref = authorDiv?.Descendants("a").FirstOrDefault();
                var authorPhotoSpan = authorHref?.Descendants("span").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "photo");
                var authorImg = authorPhotoSpan?.Descendants("img").FirstOrDefault();
                var authorPhotoUrl = authorImg?.Attributes["src"].Value.Replace("avatar_s2", "avatar");
                var authorNameSpan = authorHref?.Descendants("span").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "about");
                var authorName = authorNameSpan?.Descendants("strong").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "name");
                var blogAuthorUrl = authorDiv?.Descendants("a").FirstOrDefault().GetAttributeValue("href", string.Empty);
                var headerDiv = contentDiv.Descendants("p").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "txt");
                var blogTitle = headerDiv?.Descendants().FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "title type1")?.InnerText;
                if (blogTitle == null)
                    continue;
                var urlHref = headerDiv.Descendants("a").FirstOrDefault();
                if (urlHref == null)
                    continue;
                var blogUrl = "http://echo.msk.ru" + urlHref.GetAttributeValue("href", string.Empty);
                var metaDiv = contentDiv.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "meta");
                var dateSpan = metaDiv?.Descendants("span").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "datetime");
                var blogDateString = dateSpan?.GetAttributeValue("title", string.Empty);
                var blog = new BlogItem
                {
                    BlogId = Guid.NewGuid(),
                    BlogItemUrl = blogUrl,
                    BlogDate = DateTime.TryParse(blogDateString, out blogDate) ? blogDate : ContentDate,
                    BlogTitle = blogTitle,
                    BlogImageUrl = authorPhotoUrl == null ? "http://echo.msk.ru/files/avatar/2261876.jpg" : "http://echo.msk.ru" + authorPhotoUrl,
                    BlogAuthorName = authorName == null ? "Эхо Москвы" : authorName.InnerText,
                    BlogAuthorUrl = blogAuthorUrl == null ? "http://echo.msk.ru/blog/echomsk/" : "http://echo.msk.ru" + blogAuthorUrl
                };
                allBlogs.Add(blog);
            }
            //array of new unique blogs
            var newContent = allBlogs.Where(blog => Blogs.All(b => b.BlogTitle != blog.BlogTitle)).ToArray();
            NewItemsCount = newContent.Length;
            if (NewItemsCount == 0)
                return;
            var list = Blogs;
            list.AddRange(newContent);
            //assign Blogs property to raise PropertyChanged
            Blogs = list.OrderByDescending(b => b.BlogDate).ToList();
        }

        //indexer (read only) for accessing a blog item
        public BlogItem this[int i] => Blogs.Count == 0 ? null : Blogs[i];
    }
}