using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Support.V4.View;

namespace Echo
{
    public class EchoViewPageListener : ViewPager.SimpleOnPageChangeListener
    {
        private readonly IMenu _menu;
        private IMenuItem _menuItem;
        private readonly Context _context;

        public EchoViewPageListener(Context context, IMenu menu)
        {
            _context = context;
            _menu = menu;
            
        }

        public override void OnPageSelected(int position)
        {
            if (Common.FragmentList.Count == 0) return;
            
            Common.CurrentPosition = position;
            
            DefaultMenuIcons();

            Common.toolbar.SetBackgroundColor(Color.ParseColor(Common.colorPrimary[position]));
            Common.window.SetNavigationBarColor(Color.ParseColor(Common.colorPrimaryDark[position]));
            Common.window.SetStatusBarColor(Color.ParseColor(Common.colorPrimaryDark[position]));
            Common.fab.BackgroundTintList = ColorStateList.ValueOf(Color.ParseColor(Common.colorPrimary[position]));
            Common.fab.SetRippleColor(Color.ParseColor(Common.colorAccent[position]));

            Common.toolbar.Subtitle = Common.SelectedDates[position].Date == DateTime.Now.Date ? _context.Resources.GetString(Resource.String.today) : Common.SelectedDates[position].ToString("m");
            switch (position)
            {
                case 0:
                    Common.toolbar.Title = _context.Resources.GetText(Resource.String.news);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
                    _menuItem.SetIcon(Resource.Drawable.news_white);
                    break;
                case 1:
                    Common.toolbar.Title = _context.Resources.GetText(Resource.String.blog);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
                    _menuItem.SetIcon(Resource.Drawable.blog_white);
                    break;
                default:
                    break;
            }
            Common.IsSwiping = false;
            base.OnPageSelected(position);
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