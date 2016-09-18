using System;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Toolbar = Android.Widget.Toolbar;
using Android.Support.Design.Widget;
using Echo.Blog;
using Echo.ContentTypes;
using Echo.News;

namespace Echo
{
    [Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FragmentActivity
    {
        private ViewPager _pager;
        private FloatingActionButton _fab;
        private EventHandler _fabClickHandler;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //add dummy authors
            Common.PersonList = PersonItem.AddDummies();

            SetContentView(Resource.Layout.Main);

            _pager = FindViewById<EchoViewPager>(Resource.Id.pager);
            _pager.OffscreenPageLimit = 2;
            _fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            _fabClickHandler = (sender, args) =>
            {
                var frag = DatePickerFragment.NewInstance(delegate (DateTime date)
                {
                    switch (Common.CurrentPosition)
                    {
                        case 0:
                            Common.NewsDay = date;
                            break;
                        case 1:
                            Common.BlogDay = date;
                            break;
                        default:
                            break;
                    }
                    Common.toolbar.Subtitle = date.Date == DateTime.Now.Date ? Resources.GetString(Resource.String.today) : date.ToString("m");
                    Common.pagerAdapter.NotifyDataSetChanged();
                });
                frag.Show(FragmentManager, DatePickerFragment.TAG);
            };
            _fab.Click += _fabClickHandler;



            Common.toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetActionBar(Common.toolbar);

            if (Common.pagerAdapter == null)
            {
                Common.pagerAdapter = new EchoFragmentPagerAdapter(SupportFragmentManager);
            }
            if (Common.BlogContentList == null)
            {
                Common.BlogContentList = new List<BlogContent>();
            }
            if (Common.NewsContentList == null)
            {
                Common.NewsContentList = new List<NewsContent>();
            }

            //add news fragment to ViewPager adapter
            Common.pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recycler_view);
                if (Common.News != null)
                {
                    Common.News.Dispose();
                }
                Common.News = new NewsView(Common.NewsDay, recyclerView, this);

                return view;
            }
            );

            //add blog fragment to ViewPager adapter
            Common.pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recycler_view);
                if (Common.Blogs != null)
                {
                    Common.Blogs.Dispose();
                }
                Common.Blogs = new BlogView(Common.BlogDay, recyclerView, this);
                
                return view;
            }
            );

            _pager.Adapter = Common.pagerAdapter;
        }


        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            Common.viewPageListener = new EchoViewPageListener(this, menu);
            _pager.AddOnPageChangeListener(Common.viewPageListener);
            Common.viewPageListener.OnPageSelected(0);
            return base.OnCreateOptionsMenu(menu);
        }


        //main menu item selected
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (Common.viewPageListener == null) return base.OnOptionsItemSelected(item);
            switch (item.ItemId)
            {
                case Resource.Id.top_menu_news:
                    _pager.SetCurrentItem(0, true);
                    break;
                case Resource.Id.top_menu_blog:
                    _pager.SetCurrentItem(1, true);
                    break;
                case Resource.Id.top_menu_show:
                    break;
                default:
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        


    }
}