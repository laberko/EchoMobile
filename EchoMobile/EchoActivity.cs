using System;
using System.Globalization;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Timer = System.Timers.Timer;
using Toolbar = Android.Widget.Toolbar;

namespace Echo
{
    //activity parent containing common methods
    [Activity(Label = "@string/app_name")]
    public class EchoActivity : Activity
    {
        protected DateTime ContentDay;
        private Timer _activityTimer;
        protected RecyclerView ContentRecyclerView;
        

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            if (!DateTime.TryParse(Intent.GetStringExtra("Date"), out ContentDay))
            {
                ContentDay = DateTime.Now.Date;
            }
            SetContentView(Resource.Layout.Main);
            SetActionBar(FindViewById<Toolbar>(Resource.Id.toolbar_top));
            ContentRecyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (_activityTimer == null)
            {
                _activityTimer = new Timer
                {
                    Interval = 5000
                };
                _activityTimer.Elapsed += OnTimer;
            }
            _activityTimer.Start();
        }

        protected virtual void OnTimer(object sender, ElapsedEventArgs e)
        {
        }

        protected override void OnStop()
        {
            base.OnStop();
            _activityTimer?.Dispose();
        }

        //main menu item selected
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Toast.MakeText(this, item.TitleFormatted, ToastLength.Short).Show();
            switch (item.ItemId)
            {
                case Resource.Id.top_menu_news:
                    //StartActivity(typeof(MainActivity));
                    var newsIntent = new Intent(this, typeof(NewsActivity));
                    newsIntent.PutExtra("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    StartActivity(newsIntent);
                    break;
                case Resource.Id.top_menu_blog:
                    //StartActivity(typeof(BlogActivity));
                    var blogIntent = new Intent(this, typeof(BlogActivity));
                    blogIntent.PutExtra("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    StartActivity(blogIntent);
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