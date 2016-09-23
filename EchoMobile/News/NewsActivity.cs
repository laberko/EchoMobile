using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.News
{
    [Activity (Label = "@string/app_name", Icon = "@drawable/icon")]
	public class NewsActivity : AppCompatActivity
    {
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            SetContentView(Resource.Layout.NewsItemView);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);

            toolbar.SetBackgroundColor(Color.ParseColor(Common.colorPrimary[0]));
            Window.SetNavigationBarColor(Color.ParseColor(Common.colorPrimaryDark[0]));
            Window.SetStatusBarColor(Color.ParseColor(Common.colorPrimaryDark[0]));

            SetSupportActionBar(toolbar);
            SupportActionBar.Title = Intent.GetStringExtra("Time");
            SupportActionBar.Subtitle = Intent.GetStringExtra("Date");
            var titleTextView = FindViewById<TextView>(Resource.Id.newsTitle);
            titleTextView.Text = Intent.GetStringExtra("Title");
            var mainTextView = FindViewById<TextView>(Resource.Id.newsText);
            mainTextView.Text = Intent.GetStringExtra("Text");


            //var attrs = Theme.ObtainStyledAttributes(new[] { Android.Resource.Attribute.ActionBarSize });
            //var toolbarHeight = attrs.GetDimension(0, 0);
            //attrs.Recycle();

            //var scrollView = FindViewById<ScrollView>(Resource.Id.scroll);
            //scrollView.ViewTreeObserver.ScrollChanged += (sender, args) =>
            //{
            //    var y = scrollView.ScrollY;
            //    if (y >= toolbarHeight && toolbar.IsShown)
            //        toolbar.Visibility = ViewStates.Gone;
            //    else if (y == 0 && !toolbar.IsShown)
            //        toolbar.Visibility = ViewStates.Visible;
            //};

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
            {
                OnBackPressed();
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}