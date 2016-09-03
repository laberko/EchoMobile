using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Widget.Toolbar;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Java.Lang;

namespace Echo
{
    public class GenericFragmentPagerAdapter : FragmentPagerAdapter
    {
        private List<Fragment> _fragmentList;

        public GenericFragmentPagerAdapter(FragmentManager fm) : base(fm)
        {
            if (Common.fragmentList == null)
            {
                Common.fragmentList = new List<Fragment>();
            }
            _fragmentList = Common.fragmentList;
        }

        public override int Count => _fragmentList.Count;

        public override Fragment GetItem(int position)
        {
            return _fragmentList[position];
        }

        public override int GetItemPosition(Java.Lang.Object objectValue)
        {
            return PositionNone;
        }

        //public void AddFragment(GenericViewPagerFragment fragment)
        //{
        //    _fragmentList.Add(fragment);
        //}

        public void AddFragmentView(Func<LayoutInflater, ViewGroup, Bundle, View> view)
        {
            _fragmentList.Add(new GenericViewPagerFragment(view));
            //NotifyDataSetChanged();
        }

    }





    public class ViewPageListenerForActionBar : ViewPager.SimpleOnPageChangeListener
    {
        //private ActionBar _bar;
        private readonly Context _context;
        private readonly Toolbar _toolbar;
        private IMenu _menu;
        private IMenuItem _menuItem;
        private Resources _res;
        public ViewPageListenerForActionBar(Context context, IMenu menu, Toolbar toolbar)
        {
            _res = context.Resources;
            _toolbar = toolbar;
            _menu = menu;
            _context = context;
        }
        public override void OnPageSelected(int position)
        {
            Toast.MakeText(_context, "Position: " + position, ToastLength.Short).Show();
            DefaultMenuIcons();
            switch (position)
            {
                case 0:
                    _toolbar.Title = _res.GetText(Resource.String.news);
                    _toolbar.Subtitle = "Сегодня";
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_news);
                    _menuItem.SetIcon(Resource.Drawable.news_white);
                    return;
                case 1:
                    _toolbar.Title = _res.GetText(Resource.String.blog);
                    _menuItem = _menu.FindItem(Resource.Id.top_menu_blog);
                    _menuItem.SetIcon(Resource.Drawable.blog_white);
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
    //public static class ViewPagerExtensions
    //{
    //    public static ActionBar.Tab GetViewPageTab(this ViewPager viewPager, ActionBar actionBar, string name)
    //    {
    //        var tab = actionBar.NewTab();
    //        tab.SetText(name);
    //        tab.TabSelected += (o, e) =>
    //        {
    //            viewPager.SetCurrentItem(actionBar.SelectedNavigationIndex, false);
    //        };
    //        return tab;
    //    }
    //}
}