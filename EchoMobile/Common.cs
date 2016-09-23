using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Support.Design.Widget;
using Android.Views;
using Echo.Blog;
using Echo.ContentTypes;
using Echo.News;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo
{
    public static class Common
    {
        public static Toolbar toolbar;
        public static Window window;
        public static int DisplayWidth;
        public static FloatingActionButton fab;
        public static EchoViewPageListener viewPageListener;
        public static int CurrentPosition = 0;
        public static bool IsSwiping;
        public static int NewsNumber = 0;
        public static List<PersonItem> PersonList;
        public static List<BlogContent> BlogContentList;
        public static List<NewsContent> NewsContentList;
        public static List<EchoViewPagerFragment> FragmentList;
        public static NewsView News;
        public static BlogView Blogs;
        public static EchoFragmentPagerAdapter pagerAdapter;
        public static readonly DateTime[] SelectedDates = { DateTime.Now, DateTime.Now };
        public static readonly string[] colorPrimary = { "#F44336", "#2196F3", "#4CAF50" };         //red, blue, green
        public static readonly string[] colorPrimaryDark = { "#D32F2F", "#1976D2", "#388E3C" };
        public static readonly string[] colorAccent = { "#B71C1C", "#0D47A1", "#1B5E20" };

        public static async Task<Bitmap> GetImageBitmapFromUrlAsync(string url, int widthLimit)
        {
            Bitmap bitmapResized;
            //try
            //{
            using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(url);
                    if (imageBytes == null || imageBytes.Length <= 0) return null;
                    var imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    var ratio = (float)widthLimit / imageBitmap.Width;
                    var width = (int)Math.Round(ratio * imageBitmap.Width);
                    var height = (int)Math.Round(ratio * imageBitmap.Height);
                    bitmapResized = Bitmap.CreateScaledBitmap(imageBitmap, width, height, true);
                }
            //}
            //catch (Exception ex) when (ex is WebException)
            //{
            //    Toast.MakeText(context, ex.Message, ToastLength.Short);
            //}


            return bitmapResized;
        }

    }
}