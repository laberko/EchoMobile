using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Widget.Toolbar;

namespace Echo.News
{
    [Activity (Label = "@string/app_name", Icon = "@drawable/icon")]
	public class NewsActivity : Activity
    {
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            SetContentView(Resource.Layout.NewsItemView);
            SetActionBar(FindViewById<Toolbar>(Resource.Id.toolbar_top));
            ActionBar.Title = Intent.GetStringExtra("Time");
            ActionBar.Subtitle = Intent.GetStringExtra("Date");
            var titleTextView = FindViewById<TextView>(Resource.Id.newsTitle);
            titleTextView.Text = Intent.GetStringExtra("Title");
            var mainTextView = FindViewById<TextView>(Resource.Id.newsText);
            mainTextView.Text = Intent.GetStringExtra("Text");
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