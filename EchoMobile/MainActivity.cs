using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using Android.App;
using Android.Content;
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
using Echo.Player;
using Echo.Show;

namespace Echo
{
    [Activity(Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop,
        AlwaysRetainTaskState = true)]
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
        private AppBarLayout _appBar;
        private Toolbar _toolBar;
        private DateTime _lastActive;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //if activity is already living under the top (previously started with different intent) - restart it
            if (!IsTaskRoot)
            {
                Common.ServiceBinder?.GetMediaPlayerService().Stop();
                try
                {
                    StopService(new Intent(ApplicationContext, typeof (EchoPlayerService)));
                }
                catch
                {
                    // ignored
                }
                Common.EchoPlayer = null;
                Common.ServiceBinder = null;
                var thisIntent = PackageManager.GetLaunchIntentForPackage(PackageName);
                var newIntent = IntentCompat.MakeRestartActivityTask(thisIntent.Component);
                StartActivity(newIntent);
                FinishAffinity();
            }

            //get screen width
            Common.DisplayWidth = Math.Min(Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);

            //dates for the main fragments
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
                Common.BlogContentList = new List<AbstractContentFactory>();
            if (Common.NewsContentList == null)
                Common.NewsContentList = new List<AbstractContentFactory>();
            if (Common.ShowContentList == null)
                Common.ShowContentList = new List<AbstractContentFactory>();
            //mediaplayer
            if (Common.EchoPlayer == null)
                Common.EchoPlayer = new EchoMediaPlayer();
            //viewpager adapter
            if (_pagerAdapter == null)
                _pagerAdapter = new EchoFragmentPagerAdapter(SupportFragmentManager);

            //toolbar
            _appBar = FindViewById<AppBarLayout>(Resource.Id.appbar);
            _toolBar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetSupportActionBar(_toolBar);

            //floating action button (calendar)
            _fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            _fabClickHandler = (sender, args) =>
            {
                var frag = EchoDatePicker.NewInstance(delegate (DateTime date)
                {
                    Common.SelectedDates[_viewPageListener.CurrentPosition] = date;
                    _toolBar.Subtitle = date.Date == DateTime.Now.Date
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
                    content.PropertyChanged += OnContentChanged;
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
                    content.PropertyChanged += OnContentChanged;
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
                    content.PropertyChanged += OnContentChanged;
                return view;
            }, 2);

            //viewpager
            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.OffscreenPageLimit = 2;
            _pager.Adapter = _pagerAdapter;

            //refresh RecyclerView on Play/Pause
            Common.EchoPlayer.PlaybackStarted += delegate
            {
                RunOnUiThread(() => _shows.UpdateViewOnPlay());
            };
            Common.EchoPlayer.PlaybackPaused += delegate
            {
                RunOnUiThread(() => _shows.UpdateViewOnPlay());
            };
        }

        protected override void OnPause()
        {
            base.OnPause();
            _refreshTimer?.Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _refreshTimer?.Start();
            _news?.UpdateContent();
            _blogs?.UpdateContent();
            _shows?.UpdateContent();
            Common.PlayList = _shows?.PlayList;
        }

        protected override void OnStop()
        {
            base.OnStop();
            _refreshTimer?.Stop();
            _lastActive = DateTime.Now;
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (_lastActive.Date == DateTime.Now.Date)
                return;
            //if date has changed since activity was stopped
            Common.SelectedDates = new[] { DateTime.Now, DateTime.Now, DateTime.Now };
            _toolBar.Subtitle = Resources.GetString(Resource.String.today);
            _pagerAdapter.NotifyDataSetChanged();
            _lastActive = DateTime.Now;
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            _pagerAdapter.NotifyDataSetChanged();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            try
            {
                if (Common.ServiceBinder != null && Common.ServiceBinder.GetMediaPlayerService() != null)
                {
                    Common.ServiceBinder.GetMediaPlayerService().Stop();
                    Common.ServiceBinder.GetMediaPlayerService().OnDestroy();
                    Common.ServiceBinder = null;
                }
                if (Common.EchoPlayer == null)
                    return;
                Common.EchoPlayer.Reset();
                Common.EchoPlayer.Release();
                Common.EchoPlayer = null;
            }
            catch
            {
                // ignored
            }
        }

        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            _viewPageListener = new EchoViewPageListener(this, menu, Window, _appBar, _toolBar);
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
                case Resource.Id.top_menu_settings:
                    StartActivity(new Intent(this, typeof (SettingsActivity)));
                    break;
                case Resource.Id.top_menu_about:
                    var openMarket = new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=" + PackageName));
                    openMarket.AddFlags(ActivityFlags.NoHistory | ActivityFlags.NewDocument | ActivityFlags.MultipleTask);
                    try
                    {
                        StartActivity(openMarket);
                    }
                    catch (ActivityNotFoundException)
                    {
                        StartActivity(new Intent(Intent.ActionView, 
                            Android.Net.Uri.Parse("http://play.google.com/store/apps/details?id=" + PackageName)));
                    }
                    break;
                default:
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            try
            {
                if (Common.ServiceBinder != null && Common.ServiceBinder.GetMediaPlayerService() != null)
                {
                    Common.ServiceBinder.GetMediaPlayerService().Stop();
                    Common.ServiceBinder.GetMediaPlayerService().OnDestroy();
                    Common.ServiceBinder = null;
                }
                if (Common.EchoPlayer == null)
                    return;
                Common.EchoPlayer.Reset();
                Common.EchoPlayer.Release();
                Common.EchoPlayer = null;
            }
            catch
            {
                // ignored
            }
        }

        //update content
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _news.UpdateContent();
                _news.HideBar();
                _blogs.UpdateContent();
                _blogs.HideBar();
                _shows.UpdateContent();
                _shows.HideBar();
            });
        }

        //update fragment view on content change (e has the property name)
        private void OnContentChanged(object sender, PropertyChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                switch (e.PropertyName)
                {
                    case "NewsContent":
                        _news.UpdateView();
                        break;
                    case "BlogContent":
                        _blogs.UpdateView();
                        break;
                    case "ShowContent":
                        _shows.UpdateView();
                        break;
                    default:
                        break;
                }
            });
        }
    }
}