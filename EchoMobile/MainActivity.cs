using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Echo.Blog;
using Echo.News;
using Echo.Person;
using Echo.Show;

namespace Echo
{
    [Activity(Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : AppCompatActivity
    {
        private ViewPager _pager;
        private FloatingActionButton _fab;
        private EventHandler _fabClickHandler;
        private Timer _refreshTimer;
        private const int Timeout = 30000;
        private EchoViewPageListener _viewPageListener;
        private EchoFragmentPagerAdapter _pagerAdapter;
        private NewsView _news;
        private BlogView _blogs;
        private ShowView _shows;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //if activity is already living under the top (previously started with different intent) - restart it
            if (!IsTaskRoot)
            {
                var thisIntent = PackageManager.GetLaunchIntentForPackage(PackageName);
                var newIntent = IntentCompat.MakeRestartActivityTask(thisIntent.Component);
                StartActivity(newIntent);
                FinishAffinity();
            }
            
            Common.DisplayWidth = Math.Min(Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);
            Common.SelectedDates = new [] { DateTime.Now, DateTime.Now, DateTime.Now };

            //timer for content update
            _refreshTimer = new Timer
            {
                Interval = Timeout
            };
            _refreshTimer.Elapsed += OnTimer;

            SetContentView(Resource.Layout.Main);

            //collection of people
            if (Common.PersonList == null)
                Common.PersonList = new List<PersonItem>();
            //collections of daily content
            if (Common.BlogContentList == null)
                Common.BlogContentList = new List<BlogContent>();
            if (Common.NewsContentList == null)
                Common.NewsContentList = new List<NewsContent>();
            if (Common.ShowContentList == null)
                Common.ShowContentList = new List<ShowContent>();

            //viewpager adapter
            if (_pagerAdapter == null)
                _pagerAdapter = new EchoFragmentPagerAdapter(SupportFragmentManager);

            //toolbar
            Common.AppBar = FindViewById<AppBarLayout>(Resource.Id.appbar);
            Common.EchoBar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetSupportActionBar(Common.EchoBar);

            //floating action button (calendar)
            _fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            _fabClickHandler = (sender, args) =>
            {
                var frag = EchoDatePicker.NewInstance(delegate (DateTime date)
                {
                    Common.SelectedDates[_viewPageListener.CurrentPosition] = date;
                    Common.EchoBar.Subtitle = date.Date == DateTime.Now.Date
                        ? Resources.GetString(Resource.String.today)
                        : date.ToString("m");
                    _pagerAdapter.NotifyDataSetChanged();
                });
                frag.Show(FragmentManager, frag.Tag);
            };
            _fab.Click += _fabClickHandler;
            Common.Fab = _fab;

                //collection of viewpager fragments
                //add news fragment to ViewPager adapter
                _pagerAdapter.AddFragmentView((i, v, b) =>
                {
                    var view = i.Inflate(Resource.Layout.PagerView, v, false);
                    var selectedDate = Common.SelectedDates[0];
                    _news = new NewsView(selectedDate, view, this);
                    var content = Common.NewsContentList.FirstOrDefault(n => n.ContentDate.Date == selectedDate.Date);
                    if (content != null)
                        content.PropertyChanged += ContentChanged;
                    return view;
                }, 0);
                //add blog fragment to ViewPager adapter
                _pagerAdapter.AddFragmentView((i, v, b) =>
                {
                    var view = i.Inflate(Resource.Layout.PagerView, v, false);
                    var selectedDate = Common.SelectedDates[1];
                    _blogs = new BlogView(selectedDate, view, this);
                    var content = Common.BlogContentList.FirstOrDefault(n => n.ContentDate.Date == selectedDate.Date);
                    if (content != null)
                        content.PropertyChanged += ContentChanged;
                    return view;
                }, 1);
                //add show fragment to ViewPager adapter
                _pagerAdapter.AddFragmentView((i, v, b) =>
                {
                    var view = i.Inflate(Resource.Layout.PagerView, v, false);
                    var selectedDate = Common.SelectedDates[2];
                    _shows = new ShowView(selectedDate, view, this);
                    var content = Common.ShowContentList.FirstOrDefault(n => n.ContentDate.Date == selectedDate.Date);
                    if (content != null)
                        content.PropertyChanged += ContentChanged;
                    return view;
                }, 2);

            //viewpager
            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.OffscreenPageLimit = 2;
            _pager.Adapter = _pagerAdapter;
        }

        protected override void OnPause()
        {
            _refreshTimer?.Stop();
            base.OnPause();
        }

        protected override void OnResume()
        {
            _refreshTimer?.Start();
            _news?.UpdateContent();
            _blogs?.UpdateContent();
            _shows?.UpdateContent();
            base.OnResume();
        }

        protected override void OnStop()
        {
            _refreshTimer?.Stop();
            Common.LastActive = DateTime.Now;
            base.OnStop();
        }

        protected override void OnStart()
        {
            //if date has changed since activity was stopped
            if (Common.LastActive.Date != DateTime.Now.Date)
            {
                Common.SelectedDates = new[] { DateTime.Now, DateTime.Now, DateTime.Now };
                Common.EchoBar.Subtitle = Resources.GetString(Resource.String.today);
                _pagerAdapter.NotifyDataSetChanged();
                Common.LastActive = DateTime.Now;
            }
            base.OnStart();
        }

        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            _viewPageListener = new EchoViewPageListener(this, menu, Window);
            _pager.AddOnPageChangeListener(_viewPageListener);
            _viewPageListener.OnPageSelected(0);
            _pager.SetCurrentItem(0, true);
            return base.OnCreateOptionsMenu(menu);
        }

        //main menu item selected
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (_viewPageListener == null)
                return base.OnOptionsItemSelected(item);
            switch (item.ItemId)
            {
                case Resource.Id.top_menu_news:
                    _pager.SetCurrentItem(0, true);
                    break;
                case Resource.Id.top_menu_blog:
                    _pager.SetCurrentItem(1, true);
                    break;
                case Resource.Id.top_menu_show:
                    _pager.SetCurrentItem(2, true);
                    break;
                default:
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        //update content (only selected fragment will be updated)
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _news?.UpdateContent();
                _blogs?.UpdateContent();
                _shows?.UpdateContent();
            });
        }

        //update fragment view on content change (e has the property name)
        private void ContentChanged(object sender, PropertyChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                switch (e.PropertyName)
                {
                    case "News":
                        _news.UpdateView();
                        break;
                    case "Blogs":
                        _blogs.UpdateView();
                        break;
                    case "Shows":
                        _shows.UpdateView();
                        break;
                    default:
                        break;
                }
            });
        }
    }
}