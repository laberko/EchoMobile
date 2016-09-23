using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Blog
{
    //activity to open a full blog item
    [Activity (Label = "@string/app_name", Icon = "@drawable/icon")]
	public class BlogActivity : AppCompatActivity
    {
        protected override async void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            SetContentView(Resource.Layout.BlogItemView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);

            toolbar.SetBackgroundColor(Color.ParseColor(Common.colorPrimary[1]));
            Window.SetNavigationBarColor(Color.ParseColor(Common.colorPrimaryDark[1]));
            Window.SetStatusBarColor(Color.ParseColor(Common.colorPrimaryDark[1]));

            SetSupportActionBar(toolbar);
            SupportActionBar.Subtitle = Intent.GetStringExtra("Date");

            var author = Common.PersonList.Find(p => p.PersonId == Guid.Parse(Intent.GetStringExtra("Author")));

            SupportActionBar.Title = author.PersonName;

            var pictureView = FindViewById<ImageView>(Resource.Id.blogPic);
            pictureView.SetImageBitmap(await Common.GetImageBitmapFromUrlAsync(author.PersonPhotoUrl, Common.DisplayWidth / 3));

            var titleTextView = FindViewById<TextView>(Resource.Id.blogTitle);
            titleTextView.Text = Intent.GetStringExtra("Title");
            var mainTextView = FindViewById<TextView>(Resource.Id.blogText);
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