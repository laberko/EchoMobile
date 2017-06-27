using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Support.V4.View;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo
{
    //listener for ViewPager fragments swipe/switch
    public class EchoViewPageListener : ViewPager.SimpleOnPageChangeListener
    {
        private readonly IMenu _menu;
        private IMenuItem _menuItem;
        private readonly Context _context;
        private readonly Window _window;
        private readonly AppBarLayout _appBar;
        private readonly Toolbar _toolBar;
        private readonly FloatingActionButton _fab;

        public EchoViewPageListener(Context context, IMenu menu, Window window, AppBarLayout appBar, Toolbar toolBar, FloatingActionButton fab)
        {
            _context = context;
            _menu = menu;
            _window = window;
            _appBar = appBar;
            _toolBar = toolBar;
            _fab = fab;
        }

        //change some properties on viewpager selection change according to current position
        public override void OnPageSelected(int position)
        {
            var primaryDarkColor = Color.ParseColor(MainActivity.PrimaryDarkColor[position]);
            var colorPrimary = Color.ParseColor(MainActivity.ColorPrimary[position]);
            var colorAccent = Color.ParseColor(MainActivity.ColorAccent[position]);

            MainActivity.CurrentPosition = position;
            //set default icons
            _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
            _menuItem.SetIcon(Resource.Drawable.news_white);
            _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
            _menuItem.SetIcon(Resource.Drawable.blog_white);
            _menuItem = _menu.FindItem(Resource.Id.top_menu_show);
            _menuItem.SetIcon(Resource.Drawable.show_white);
            _menuItem = _menu.FindItem(Resource.Id.top_menu_online);
            _menuItem.SetIcon(Resource.Drawable.ic_radio_white_48dp);

            if ((int) Build.VERSION.SdkInt > 19)
            {
                _window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                _window.SetNavigationBarColor(primaryDarkColor);
                _window.SetStatusBarColor(primaryDarkColor);
                _fab.BackgroundTintList = ColorStateList.ValueOf(colorPrimary);
            }
            
            _fab.RippleColor = colorAccent;
            _toolBar.SetBackgroundColor(colorPrimary);
            _toolBar.Subtitle = MainActivity.SelectedDates[position].Date == DateTime.Now.Date
                ? _context.Resources.GetString(Resource.String.today)
                : MainActivity.SelectedDates[position].ToString("m");
            _appBar.SetExpanded(true);

            switch (position)
            {
                case 0:
                    _toolBar.Title = _context.Resources.GetText(Resource.String.news);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
                    _menuItem.SetIcon(Resource.Drawable.news_black);
                    _fab.Visibility = ViewStates.Visible;
                    break;
                case 1:
                    _toolBar.Title = _context.Resources.GetText(Resource.String.blog);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
                    _menuItem.SetIcon(Resource.Drawable.blog_black);
                    _fab.Visibility = ViewStates.Visible;
                    break;
                case 2:
                    _toolBar.Title = _context.Resources.GetText(Resource.String.show);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_show);
                    _menuItem.SetIcon(Resource.Drawable.show_black);
                    _fab.Visibility = ViewStates.Visible;
                    break;
                case 3:
                    _toolBar.Title = _context.Resources.GetText(Resource.String.online);
                    _toolBar.Subtitle = null;
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_online);
                    _menuItem.SetIcon(Resource.Drawable.ic_radio_black_48dp);
                    _fab.Visibility = ViewStates.Gone;
                    break;
                default:
                    break;
            }
            base.OnPageSelected(position);
        }
    }
}