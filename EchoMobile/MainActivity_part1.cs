using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Views;
using Android.Support.V4.Content;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Echo.Blog;
using Echo.News;
using Echo.Online;
using Echo.Person;
using Echo.Player;
using Echo.Show;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Android.Runtime;
using Android.Text;
using Echo.Settings;
using Plugin.Connectivity;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo
{
    [Activity(Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop,
        ResizeableActivity = true,
        AlwaysRetainTaskState = true)]
    public partial class MainActivity : AppCompatActivity
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
        private OnlineView _online;
        private AppBarLayout _appBar;
        private Toolbar _toolBar;
        private DateTime _lastActive;

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Theme);
            base.OnCreate(bundle);

            //if activity is already living under the top (previously started with different intent) - restart it
            if (!IsTaskRoot)
            {
                try
                {
                    StopService(new Intent(ApplicationContext, typeof (EchoPlayerService)));
                    Cleanup();
                }
                catch
                {
                    // ignored
                }
                var thisIntent = PackageManager.GetLaunchIntentForPackage(PackageName);
                var newIntent = IntentCompat.MakeRestartActivityTask(thisIntent.Component);
                StartActivity(newIntent);
                FinishAffinity();
            }

            MobileCenter.Start("5e142fe1-d7d1-4e79-96be-ccd83296239f", typeof(Analytics), typeof(Crashes));

            //get screen width
            DisplayWidth = Math.Min(Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);

            //dates for the main fragments
            SelectedDates = new [] { DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now };

            //timer for content update
            _refreshTimer = new Timer
            {
                Interval = Timeout
            };
            _refreshTimer.Elapsed += OnTimer;

            SetContentView(Resource.Layout.Main);

            //toolbar
            _appBar = FindViewById<AppBarLayout>(Resource.Id.appbar);
            _toolBar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetSupportActionBar(_toolBar);
            //ActionBar.SetDisplayShowTitleEnabled(true);

            //collection of people
            if (PersonList == null)
                PersonList = new List<PersonItem>();
            //collections of daily content
            if (BlogContentList == null)
                BlogContentList = new List<AbstractContentFactory>();
            if (NewsContentList == null)
                NewsContentList = new List<AbstractContentFactory>();
            if (ShowContentList == null)
                ShowContentList = new List<AbstractContentFactory>();
            //mediaplayer
            if (EchoPlayer == null)
                EchoPlayer = new EchoMediaPlayer();
            //viewpager adapter
            if (_pagerAdapter == null)
                _pagerAdapter = new EchoFragmentPagerAdapter(SupportFragmentManager);

            //floating action button (calendar)
            _fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            _fabClickHandler = delegate
            {
                var frag = EchoDatePicker.NewInstance(delegate(DateTime date)
                {
                    SelectedDates[CurrentPosition] = date;
                    _toolBar.Subtitle = date.Date == DateTime.Now.Date
                        ? Resources.GetString(Resource.String.today)
                        : date.ToString("m");
                    _pagerAdapter.NotifyDataSetChanged();
                    _appBar.SetExpanded(true);
                });
                frag.Show(FragmentManager, frag.Tag);
            };
            _fab.Click += _fabClickHandler;

            //collection of viewpager fragments
            //add news fragment to ViewPager adapter
            _pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var selectedDate = SelectedDates[0];
                _news = new NewsView(selectedDate, view, this);
                var content = NewsContentList.FirstOrDefault(n => n.ContentDate.Date == selectedDate.Date);
                if (content != null)
                    content.PropertyChanged += OnContentChanged;
                return view;
            }, 0);
            //add blog fragment to ViewPager adapter
            _pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var selectedDate = SelectedDates[1];
                _blogs = new BlogView(selectedDate, view, this);
                var content = BlogContentList.FirstOrDefault(n => n.ContentDate.Date == selectedDate.Date);
                if (content != null)
                    content.PropertyChanged += OnContentChanged;
                return view;
            }, 1);
            //add show fragment to ViewPager adapter
            _pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.PagerView, v, false);
                var selectedDate = SelectedDates[2];
                _shows = new ShowView(selectedDate, view, this);
                var content = ShowContentList.FirstOrDefault(n => n.ContentDate.Date == selectedDate.Date);
                if (content != null)
                    content.PropertyChanged += OnContentChanged;
                return view;
            }, 2);
            //add online fragment to ViewPager adapter
            _pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.OnlineView, v, false);
                view.SetFitsSystemWindows(false);
                _online = new OnlineView(view, this);
                return view;
            }, 3);

            //viewpager
            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pager.OffscreenPageLimit = 3;
            _pager.Adapter = _pagerAdapter;
            _pager.SetPageTransformer(true, new EchoViewPageTransformer());

            //refresh RecyclerView on Play/Pause (update buttons)
            EchoPlayer.PlaybackStarted += delegate
            {
                //RunOnUiThread(() => _shows.UpdateViewOnPlay());
                _shows.UpdateViewOnPlay();
            };
            EchoPlayer.PlaybackPaused += delegate
            {
                //RunOnUiThread(() => _shows.UpdateViewOnPlay());
                _shows.UpdateViewOnPlay();
            };

            //request necessary permissions
            var appPermissions = new[]
            {
                Manifest.Permission.Internet,
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.MediaContentControl,
                Manifest.Permission.WakeLock,
                Manifest.Permission.SendSms,
                Manifest.Permission.GetAccounts,
                Manifest.Permission.CallPhone,
                Manifest.Permission.ModifyAudioSettings
            };
            var requiredPermissions = appPermissions.Where(perm => ContextCompat.CheckSelfPermission(this, perm) != Permission.Granted).ToArray();
            if (requiredPermissions.Length > 0)
                ActivityCompat.RequestPermissions(this, requiredPermissions, 101);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            _online?.OnlineEnable();
        }

        protected override void OnStart()
        {
            base.OnStart();
            //if date has changed since activity was stopped
            if (_lastActive.Date != DateTime.Now.Date)
            {
                SelectedDates = new[] { DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now };
                _toolBar.Subtitle = Resources.GetString(Resource.String.today);
                _lastActive = DateTime.Now;
            }
            _pagerAdapter.NotifyDataSetChanged();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            _refreshTimer?.Start();
            if (await CheckConnectivity())
                RunOnUiThread(() =>
                {
                    _news?.UpdateContent();
                    _blogs?.UpdateContent();
                    _shows?.UpdateContent();
                    _online?.OnlineEnable();
                });
            PlayList = _shows?.PlayList;
        }

        protected override void OnPause()
        {
            _online?.Dispose();
            base.OnPause();
            _refreshTimer?.Stop();
        }

        protected override void OnStop()
        {
            base.OnStop();
            _refreshTimer?.Stop();
            _lastActive = DateTime.Now;
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            try
            {
                FinishAffinity();
                System.Environment.Exit(0);
            }
            catch
            {
                // ignored
            }
        }


        protected override void OnDestroy()
        {
            _online?.Dispose();
            base.OnDestroy();
            Cleanup();
        }

        private void Cleanup()
        {
            try
            {
                _refreshTimer?.Stop();
                if (ServiceBinder?.GetMediaPlayerService() != null)
                {
                    ServiceBinder.GetMediaPlayerService().Stop();
                    ServiceBinder.GetMediaPlayerService().OnDestroy();
                    ServiceBinder = null;
                }
                if (EchoPlayer == null)
                    return;
                EchoPlayer.Reset();
                EchoPlayer.Release();
                EchoPlayer = null;
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
            _viewPageListener = new EchoViewPageListener(this, menu, Window, _appBar, _toolBar, _fab);
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
                case Resource.Id.top_menu_online:
                    _pager.SetCurrentItem(3, true);
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


        //update content
        private async void OnTimer(object sender, ElapsedEventArgs e)
        {
            if (await CheckConnectivity())
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

        private async Task<bool> CheckConnectivity()
        {
            if (await CrossConnectivity.Current.IsRemoteReachable("echo.msk.ru"))
                return true;
            var message = "<font color=\"#ffffff\">" + Resources.GetText(Resource.String.network_error) + "</font>";
            Snackbar.Make(_pager, Html.FromHtml(message, FromHtmlOptions.ModeLegacy), 10000)
                .SetAction(Resources.GetText(Resource.String.exit), v => { OnBackPressed(); })
                .Show();
            return false;
        }
    }
}