using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using Plugin.Connectivity;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.News
{
    //activity to open a full news item
    [Activity (Label = "",
        Icon = "@drawable/icon",
        ResizeableActivity = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class NewsActivity : AppCompatActivity
    {
        protected async override void OnCreate (Bundle bundle)
        {
            SetTheme(MainActivity.Theme);
            base.OnCreate(bundle);

            //no collection of daily news contents
            if (MainActivity.NewsContentList == null)
            {
                Finish();
                return;
            }
            
            SetContentView(Resource.Layout.NewsItemView);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.ParseColor(MainActivity.ColorPrimary[0]));
            
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetNavigationBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[0]));
            Window.SetStatusBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[0]));

            var progressBar = FindViewById<ProgressBar>(Resource.Id.newsProgress);
            progressBar.ScaleX = 1.5f;
            progressBar.ScaleY = 1.5f;
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(MainActivity.ColorPrimary[0]), PorterDuff.Mode.SrcIn);

            //find news with id passed to intent
            var content = MainActivity.NewsContentList.FirstOrDefault(c => c.ContentDate.Date == MainActivity.SelectedDates[0].Date)?.ContentList;
            var news = content?.FirstOrDefault(n => n.ItemId == Guid.Parse(Intent.GetStringExtra("ID")));
            if (news == null)
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }

            toolbar.Title = news.ItemDate.ToString("HH:mm");
            toolbar.Subtitle = news.ItemDate.Date == DateTime.Now.Date
                ? Resources.GetString(Resource.String.today)
                : news.ItemDate.ToString("dddd d MMMM yyyy");
            SetSupportActionBar(toolbar);

            if (!await CheckConnectivity())
                return;

            var titleTextView = FindViewById<EchoTextView>(Resource.Id.newsTitle);
            titleTextView.Setup(news.ItemTitle, MainActivity.MainDarkTextColor, TypefaceStyle.Bold, MainActivity.FontSize + 4);

            //download html for webview
            string html;
            try
            {
                html = await news.GetHtml();
            }
            catch
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            var textWebView = FindViewById<EchoWebView>(Resource.Id.newsText);
            textWebView.Setup(html);

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

        private async Task<bool> CheckConnectivity()
        {
            if (await CrossConnectivity.Current.IsRemoteReachable("echo.msk.ru"))
                return true;
            var message = "<font color=\"#ffffff\">" + Resources.GetText(Resource.String.network_error) + "</font>";
            Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.main_content),
                Html.FromHtml(message, FromHtmlOptions.ModeLegacy), 60000)
                .SetAction(Resources.GetText(Resource.String.close), v => { OnBackPressed(); })
                .Show();
            return false;
        }

    }
}