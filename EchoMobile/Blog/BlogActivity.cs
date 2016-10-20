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

namespace Echo.Blog
{
    //activity to open a full blog item
    [Activity (Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
	public class BlogActivity : AppCompatActivity
    {
        private WebView _textWebView;

        protected override async void OnCreate (Bundle bundle)
        {
            //no collection of daily blog contents
            if (Common.BlogContentList == null)
            {
                Finish();
                return;
            }
            base.OnCreate (bundle);
            SetContentView(Resource.Layout.BlogItemView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.ParseColor(Common.ColorPrimary[1]));
            SetSupportActionBar(toolbar);

            if ((int) Build.VERSION.SdkInt > 19)
            {
                Window.SetNavigationBarColor(Color.ParseColor(Common.ColorPrimaryDark[1]));
                Window.SetStatusBarColor(Color.ParseColor(Common.ColorPrimaryDark[1]));
            }

            var progressBar = FindViewById<MaterialProgressBar>(Resource.Id.blogsProgress);
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[1]), PorterDuff.Mode.SrcIn);

            //find blog with id passed to intent
            var content = Common.BlogContentList.FirstOrDefault(c => c.ContentDate.Date == Common.SelectedDates[1].Date)?.Blogs;
            var blog = content?.FirstOrDefault(b => b.BlogId == Guid.Parse(Intent.GetStringExtra("ID")));
            if (blog == null)
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            //get blog author
            if (blog.BlogAuthor == null && !string.IsNullOrEmpty(blog.BlogAuthorUrl))
            {
                try
                {
                    blog.BlogAuthor = await Common.GetPerson(blog.BlogAuthorUrl);
                }
                catch
                {
                    //close activity on error
                    progressBar.Visibility = ViewStates.Gone;
                    Finish();
                    return;
                }
                SupportActionBar.Title = blog.BlogAuthor?.PersonName;
            }
            else
                SupportActionBar.Title = blog.BlogAuthorName;
            
            if (blog.BlogDate.Date != DateTime.Now.Date)
                SupportActionBar.Subtitle = blog.BlogDate.ToString("d MMMM");

            //get author's picture for the blog
            var pictureView = FindViewById<ImageView>(Resource.Id.blogPic);
            try
            {
                if (!string.IsNullOrEmpty(blog.BlogAuthor?.PersonPhotoUrl))
                    pictureView.SetImageBitmap(await blog.BlogAuthor.GetPersonPhoto(Common.DisplayWidth/3));
                else if (!string.IsNullOrEmpty(blog.BlogImageUrl))
                    pictureView.SetImageBitmap(await Common.GetImage(Common.DisplayWidth/3, blog.BlogImageUrl));
            }
            catch
            {
                pictureView.Visibility = ViewStates.Gone;
            }

            //get blog's text (html)
            var titleTextView = FindViewById<TextView>(Resource.Id.blogTitle);
            titleTextView.Text = blog.BlogTitle;
            var html = string.Empty;
            try
            {
                html = await blog.GetBlogHtml();
            }
            catch
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
            }
            if (string.IsNullOrEmpty(html))
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
            }
            _textWebView = FindViewById<WebView>(Resource.Id.blogText);
            _textWebView.SetBackgroundColor(Color.Transparent);
            _textWebView.Settings.JavaScriptEnabled = true;
            _textWebView.Settings.StandardFontFamily = "serif";
            _textWebView.LoadDataWithBaseURL("", html, "text/html", "UTF-8", "");
            progressBar.Visibility = ViewStates.Gone;
        }

        //populate menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.item_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //main menu item selected (back button)
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.back)
                OnBackPressed();
            return base.OnOptionsItemSelected(item);
        }

        //webview may contain youtube embedded player - we need to pause it
        public override void OnBackPressed()
        {
            _textWebView?.OnPause();
            Finish();
        }

        protected override void OnPause()
        {
            _textWebView?.OnPause();
            base.OnPause();
        }

        protected override void OnResume()
        {
            _textWebView?.OnResume();
            base.OnResume();
        }

        protected override void OnStop()
        {
            _textWebView?.OnPause();
            base.OnStop();
        }
    }
}