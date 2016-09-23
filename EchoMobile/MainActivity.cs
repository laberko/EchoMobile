using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Echo.Blog;
using Echo.ContentTypes;
using Echo.News;

namespace Echo
{
    [Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : AppCompatActivity
    {
        private EchoViewPager _pager;
        private FloatingActionButton _fab;
        private EventHandler _fabClickHandler;
        private Timer _refreshTimer;
        private const int Timeout = 10000;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Common.window = Window;
            Common.DisplayWidth = Resources.DisplayMetrics.WidthPixels;

            _refreshTimer = new Timer
            {
                Interval = Timeout
            };
            _refreshTimer.Elapsed += OnTimer;
            _refreshTimer.Start();

            //add dummy authors - for debug
            Common.PersonList = PersonItem.AddDummies();

            SetContentView(Resource.Layout.Main);

            //collections of daily content
            if (Common.BlogContentList == null)
            {
                Common.BlogContentList = new List<BlogContent>();
            }
            if (Common.NewsContentList == null)
            {
                Common.NewsContentList = new List<NewsContent>();
            }

            //viewpager
            _pager = FindViewById<EchoViewPager>(Resource.Id.pager);

            //floating action button (calendar)
            _fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            _fabClickHandler = (sender, args) =>
            {
                var frag = DatePickerFragment.NewInstance(delegate (DateTime date)
                {
                    Common.SelectedDates[Common.CurrentPosition] = date;
                    Common.toolbar.Subtitle = date.Date == DateTime.Now.Date ? Resources.GetString(Resource.String.today) : date.ToString("m");
                    Common.pagerAdapter.NotifyDataSetChanged();
                });
                frag.Show(FragmentManager, DatePickerFragment.TAG);
            };
            _fab.Click += _fabClickHandler;
            Common.fab = _fab;

            //toolbar
            Common.toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetSupportActionBar(Common.toolbar);

            //viewpager adapter
            if (Common.pagerAdapter == null)
            {
                Common.pagerAdapter = new EchoFragmentPagerAdapter(SupportFragmentManager);
            }

            //add news fragment to ViewPager adapter
            EchoFragmentPagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var selectedDate = Common.SelectedDates[0];
                Common.News = new NewsView(selectedDate, view, this);
                //find content created by NewsView constructor and subscribe to its changes
                Common.NewsContentList.Find(n => n.ContentDay.Date == selectedDate.Date).PropertyChanged += ContentChanged;
                return view;
            });

            //add blog fragment to ViewPager adapter
            EchoFragmentPagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var selectedDate = Common.SelectedDates[1];
                Common.Blogs = new BlogView(selectedDate, view, this);
                Common.BlogContentList.Find(n => n.ContentDay.Date == selectedDate.Date).PropertyChanged += ContentChanged;
                return view;
            });

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

        private static void OnTimer(object sender, ElapsedEventArgs e)
        {
            Common.News.UpdateContent();
            Common.Blogs.UpdateContent();
        }

        //e has property name
        private void ContentChanged(object sender, PropertyChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                switch (e.PropertyName)
                {
                    case "News":
                        Common.News.UpdateView();
                        break;
                    case "Blogs":
                        Common.Blogs.UpdateView();
                        break;
                    default:
                        break;
                }
            });

        }
    }
}