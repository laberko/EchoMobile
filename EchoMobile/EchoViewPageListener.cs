using System;
using Android.Content;
using Android.Content.Res;
using Android.Views;
using Android.Support.V4.View;

namespace Echo
{
    public class EchoViewPageListener : ViewPager.SimpleOnPageChangeListener
    {
        private readonly IMenu _menu;
        private IMenuItem _menuItem;
        private readonly Resources _res;

        public EchoViewPageListener(Context context, IMenu menu)
        {
            _res = context.Resources;
            _menu = menu;
        }

        public override void OnPageSelected(int position)
        {
            if (Common.FragmentList.Count == 0) return;
            base.OnPageSelected(position);
            Common.CurrentPosition = position;
            Common.IsSwiping = false;
            //Common.News.ActivityTimer.Stop();
            //Common.Blogs.ActivityTimer.Stop();
            DefaultMenuIcons();
            switch (position)
            {
                case 0:
                    Common.toolbar.Title = _res.GetText(Resource.String.news);
                    Common.toolbar.Subtitle = Common.NewsDay.Date == DateTime.Now.Date ? _res.GetString(Resource.String.today) : Common.NewsDay.ToString("m");
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
                    _menuItem.SetIcon(Resource.Drawable.news_white);
                    //if (Common.News != null)
                    //{
                    //    Common.News.ActivityTimer.Start();
                    //}
                    return;
                case 1:
                    Common.toolbar.Title = _res.GetText(Resource.String.blog);
                    Common.toolbar.Subtitle = Common.BlogDay.Date == DateTime.Now.Date ? _res.GetString(Resource.String.today) : Common.BlogDay.ToString("m");
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
                    _menuItem.SetIcon(Resource.Drawable.blog_white);
                    //if (Common.Blogs != null)
                    //{
                    //    Common.Blogs.ActivityTimer.Start();
                    //}

                    return;
                default:
                    return;
            }
        }

        private void DefaultMenuIcons()
        {
            _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
            _menuItem.SetIcon(Resource.Drawable.news_black);
            _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
            _menuItem.SetIcon(Resource.Drawable.blog_black);
        }
    }
}