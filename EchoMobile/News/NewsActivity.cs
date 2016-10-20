using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using XamarinBindings.MaterialProgressBar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.News
{
    //activity to open a full news item
    [Activity (Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
	public class NewsActivity : AppCompatActivity
    {
        protected async override void OnCreate (Bundle bundle)
        {
            //no collection of daily news contents
            if (Common.NewsContentList == null)
            {
                Finish();
                return;
            }
            base.OnCreate (bundle);
            SetContentView(Resource.Layout.NewsItemView);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.ParseColor(Common.ColorPrimary[0]));
            SetSupportActionBar(toolbar);

            if ((int) Build.VERSION.SdkInt > 19)
            {
                Window.SetNavigationBarColor(Color.ParseColor(Common.ColorPrimaryDark[0]));
                Window.SetStatusBarColor(Color.ParseColor(Common.ColorPrimaryDark[0]));
            }

            var progressBar = FindViewById<MaterialProgressBar>(Resource.Id.newsProgress);
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[0]), PorterDuff.Mode.SrcIn);

            //find news with id passed to intent
            var content = Common.NewsContentList.FirstOrDefault(c => c.ContentDate.Date == Common.SelectedDates[0].Date)?.News;
            var news = content?.FirstOrDefault(n => n.NewsId == Guid.Parse(Intent.GetStringExtra("ID")));
            if (news == null)
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            SupportActionBar.Title = news.NewsDateTime.ToString("HH:mm");
            SupportActionBar.Subtitle = news.NewsDateTime.Date == DateTime.Now.Date
                ? Resources.GetString(Resource.String.today)
                : news.NewsDateTime.ToString("d MMMM");
            var titleTextView = FindViewById<TextView>(Resource.Id.newsTitle);
            titleTextView.Text = news.NewsTitle;

            //download html for webview
            string html;
            try
            {
                html = await news.GetNewsText();
            }
            catch
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            if (html == null)
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            var textWebView = FindViewById<WebView>(Resource.Id.newsText);
            textWebView.SetBackgroundColor(Color.Transparent);
            textWebView.Settings.StandardFontFamily = "serif";
            textWebView.LoadDataWithBaseURL("", html, "text/html", "UTF-8", "");
            progressBar.Visibility = ViewStates.Gone;
        }

        //populate menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.item_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //main menu item selected
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.back)
                OnBackPressed();
            return base.OnOptionsItemSelected(item);
        }
    }
}