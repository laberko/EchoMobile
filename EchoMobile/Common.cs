using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Echo.ContentTypes;

namespace Echo
{
    public static class Common
    {
        public static int NewsNumber = 0;
        public static List<BlogContent> blogContent;
        public static List<NewsContent> newsContent;
        public static List<Android.Support.V4.App.Fragment> fragmentList;
        public static NewsView News;
    }

    public static class MenuItems
    {
        
    }
}