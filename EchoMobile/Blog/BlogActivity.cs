using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Widget.Toolbar;

namespace Echo.Blog
{
    //activity to open a full blog item
    [Activity (Label = "@string/app_name", Icon = "@drawable/icon")]
	public class BlogActivity : Activity
    {
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            SetContentView(Resource.Layout.BlogItemView);
            SetActionBar(FindViewById<Toolbar>(Resource.Id.toolbar_top));
            ActionBar.Subtitle = Intent.GetStringExtra("Date");

            var author = Common.PersonList.Find(p => p.PersonId == Guid.Parse(Intent.GetStringExtra("Author")));

            ActionBar.Title = author.PersonName;

            //change:
            var pictureView = FindViewById<TextView>(Resource.Id.blogPic);
            //pictureView.Text = author.PersonPhotoUrl;
            pictureView.Text = "PIC";

            var authorTextView = FindViewById<TextView>(Resource.Id.blogAuthor);
            authorTextView.Text = author.PersonName;
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