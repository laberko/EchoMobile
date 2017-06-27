using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Android.Widget;
using HtmlAgilityPack;
//using XamarinBindings.MaterialProgressBar;

namespace Echo.Blog
{
    //blogs content for a specific day
    public class BlogContent : AbstractContentFactory, INotifyPropertyChanged
    {
        private readonly ProgressBar _progressBar;

        public BlogContent(DateTime day, ProgressBar progressBar):base(day)
        {
            _progressBar = progressBar;
            GetContent();
        }

        //download and parse blogs content for the date
        public override sealed async void GetContent()
        {
            var allBlogs = new List<BlogItem>();
            var allBlogsUrl = ContentDate.Date == DateTime.Now.Date
                ? "http://echo.msk.ru/blog/"
                : "http://echo.msk.ru/blog/" + ContentDate.ToString("yyyy-MM-dd") + ".html";
            HtmlDocument root;
            try
            {
                root = await MainActivity.GetHtml(allBlogsUrl);
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
                var authorPhotoUrl = authorImg?.Attributes["src"].Value.Replace(@"/avatar_s2/", "/");
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
                var blog = new BlogItem(MainActivity.ContentType.Blog)
                {
                    ItemId = Guid.NewGuid(),
                    ItemUrl = blogUrl,
                    ItemDate = DateTime.TryParse(blogDateString, out blogDate) ? blogDate : ContentDate,
                    ItemTitle = blogTitle,
                    ItemPictureUrl = authorPhotoUrl == null ? "http://echo.msk.ru/files/2261876.jpg" : "http://echo.msk.ru" + authorPhotoUrl,
                    ItemAuthorName = authorName == null ? "Эхо Москвы" : authorName.InnerText,
                    ItemAuthorUrl = blogAuthorUrl == null ? "http://echo.msk.ru/blog/echomsk/" : "http://echo.msk.ru" + blogAuthorUrl
                };
                allBlogs.Add(blog);
            }
            //array of new unique blogs
            var newContent = allBlogs.Where(blog => ContentList.All(b => b.ItemTitle != blog.ItemTitle)).ToArray();
            NewItemsCount = newContent.Length;
            if (NewItemsCount == 0)
                return;
            var list = ContentList;
            list.AddRange(newContent);
            //assign Blogs property to raise PropertyChanged
            ContentList = list.OrderByDescending(b => b.ItemDate).ToList();
            _progressBar.Visibility = Android.Views.ViewStates.Gone;
        }
    }
}