using System;
using System.Collections.Generic;
using Echo.Blog;
using Echo.ContentTypes;
using Echo.News;

namespace Echo
{
    public static class Common
    {
        public static Android.Widget.Toolbar toolbar;
        public static EchoViewPageListener viewPageListener;
        public static int CurrentPosition = 0;
        public static bool IsSwiping;
        public static int NewsNumber = 0;
        public static List<PersonItem> PersonList;
        public static List<BlogContent> BlogContentList;
        public static List<NewsContent> NewsContentList;
        public static List<Android.Support.V4.App.Fragment> FragmentList;
        public static NewsView News;
        public static BlogView Blogs;
        public static EchoFragmentPagerAdapter pagerAdapter;
        public static DateTime NewsDay = DateTime.Now;
        public static DateTime BlogDay = DateTime.Now;
    }
}