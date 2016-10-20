using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Support.V4.View;
using Android.Views.Animations;

namespace Echo
{
    //listener for ViewPager fragments swipe/switch
    public class EchoViewPageListener : ViewPager.SimpleOnPageChangeListener
    {
        public int CurrentPosition;
        private readonly IMenu _menu;
        private IMenuItem _menuItem;
        private readonly Context _context;
        private readonly Window _window;
        private readonly DecelerateInterpolator _fabInterpolator;

        public EchoViewPageListener(Context context, IMenu menu, Window window)
        {
            //default position is news fragment
            CurrentPosition = 0;
            _context = context;
            _menu = menu;
            _window = window;
            _fabInterpolator = new DecelerateInterpolator(2);
        }

        //change some properties on viewpager selection change
        public override void OnPageSelected(int position)
        {
            CurrentPosition = position;
            //set default icons
            _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
            _menuItem.SetIcon(Resource.Drawable.news_black);
            _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
            _menuItem.SetIcon(Resource.Drawable.blog_black);
            _menuItem = _menu.FindItem(Resource.Id.top_menu_show);
            _menuItem.SetIcon(Resource.Drawable.show_black);
            if ((int) Build.VERSION.SdkInt > 19)
            {
                _window.SetNavigationBarColor(Color.ParseColor(Common.ColorPrimaryDark[position]));
                _window.SetStatusBarColor(Color.ParseColor(Common.ColorPrimaryDark[position]));
            }
            Common.Fab.BackgroundTintList = ColorStateList.ValueOf(Color.ParseColor(Common.ColorPrimary[position]));
            Common.Fab.SetRippleColor(Color.ParseColor(Common.ColorAccent[position]));
            //restore floating button position
            Common.Fab.Animate().TranslationY(0).SetInterpolator(_fabInterpolator).Start();
            Common.EchoBar.SetBackgroundColor(Color.ParseColor(Common.ColorPrimary[position]));
            Common.EchoBar.Subtitle = Common.SelectedDates[position].Date == DateTime.Now.Date
                ? _context.Resources.GetString(Resource.String.today)
                : Common.SelectedDates[position].ToString("m");
            Common.AppBar.SetExpanded(true);
            switch (position)
            {
                case 0:
                    Common.EchoBar.Title = _context.Resources.GetText(Resource.String.news);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
                    _menuItem.SetIcon(Resource.Drawable.news_white);
                    break;
                case 1:
                    Common.EchoBar.Title = _context.Resources.GetText(Resource.String.blog);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
                    _menuItem.SetIcon(Resource.Drawable.blog_white);
                    break;
                case 2:
                    Common.EchoBar.Title = _context.Resources.GetText(Resource.String.show);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_show);
                    _menuItem.SetIcon(Resource.Drawable.show_white);
                    break;
                default:
                    break;
            }
            base.OnPageSelected(position);
        }
    }
}