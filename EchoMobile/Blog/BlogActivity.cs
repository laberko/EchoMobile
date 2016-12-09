using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Echo.Person;
using XamarinBindings.MaterialProgressBar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Blog
{
    //activity to open a full blog item
    [Activity (Label = "",
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class BlogActivity : AppCompatActivity, View.IOnClickListener
    {
        private WebView _textWebView;
        private AbstractContent _blog;

        protected override async void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            //no collection of daily blog contents
            if (Common.BlogContentList == null)
            {
                Finish();
                return;
            }
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
            var content = Common.BlogContentList.FirstOrDefault(c => c.ContentDate.Date == Common.SelectedDates[1].Date)?.ContentList;
            var blogId = Guid.Parse(Intent.GetStringExtra("ID"));
            //activity was called from content list?
            _blog = content?.FirstOrDefault(b => b.ItemId == blogId);
            if (_blog == null)
            {
                //activity was called from person history list?
                var person = Common.PersonList.FirstOrDefault(p =>
                            (p.PersonName == Intent.GetStringExtra("AuthorName") &&
                             p.PersonType == Common.PersonType.Blog));
                if (person != null)
                    _blog = person.PersonContent.FirstOrDefault(b => b.ItemId == blogId);
                if (_blog == null)
                {
                    //blog was not found anywhere
                    progressBar.Visibility = ViewStates.Gone;
                    Finish();
                    return;
                }
            }
            //get blog author
            if (!string.IsNullOrEmpty(_blog.ItemAuthorUrl))
            {
                try
                {
                    _blog.ItemAuthor = await Common.GetPerson(_blog.ItemAuthorUrl, Common.PersonType.Blog);
                    SupportActionBar.Title = _blog.ItemAuthorName;
                }
                catch
                {
                    //close activity on error
                    progressBar.Visibility = ViewStates.Gone;
                    Finish();
                    return;
                }
                SupportActionBar.Title = _blog.ItemAuthor?.PersonName;
            }
            
            if (_blog.ItemDate.Date != DateTime.Now.Date)
                SupportActionBar.Subtitle = _blog.ItemDate.ToString("dddd d MMMM yyyy");

            //get author's picture for the blog
            var pictureView = FindViewById<ImageButton>(Resource.Id.blogPic);
            try
            {
                if (!string.IsNullOrEmpty(_blog.ItemAuthor?.PersonPhotoUrl))
                    pictureView.SetImageBitmap(await _blog.ItemAuthor.GetPersonPhoto(Common.DisplayWidth / 3));
                else if (!string.IsNullOrEmpty(_blog.ItemPictureUrl))
                    pictureView.SetImageBitmap(await Common.GetImage(_blog.ItemPictureUrl, Common.DisplayWidth / 3));
                pictureView.SetOnClickListener(this);
            }
            catch
            {
                pictureView.Visibility = ViewStates.Gone;
            }
            
            //get blog's text (html)
            var titleTextView = FindViewById<TextView>(Resource.Id.blogTitle);
            titleTextView.Text = _blog.ItemTitle;
            titleTextView.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize + 4);
            titleTextView.SetTextColor(Color.Black);
            var html = string.Empty;
            try
            {
                html = await _blog.GetHtml();
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
            _textWebView.Settings.DefaultFontSize = Common.FontSize;
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

        //on author avatar click
        public void OnClick(View v)
        {
            var intent = new Intent(this, typeof(BlogHistoryActivity));
            intent.PutExtra("PersonUrl", _blog.ItemAuthorUrl);
            StartActivity(intent);
            Finish();
        }
    }
}