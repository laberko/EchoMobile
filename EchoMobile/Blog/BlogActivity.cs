using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using Echo.BlogHistory;
using Plugin.Connectivity;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Blog
{
    //activity to open a full blog item
    [Activity (Label = "",
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTop,
        ResizeableActivity = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class BlogActivity : AppCompatActivity, View.IOnClickListener
    {
        private EchoWebView _textWebView;
        private AbstractContent _blog;

        protected override async void OnCreate (Bundle bundle)
        {
            SetTheme(MainActivity.Theme);
            base.OnCreate(bundle);

            //no collection of daily blog contents
            if (MainActivity.BlogContentList == null)
            {
                Finish();
                return;
            }
            SetContentView(Resource.Layout.BlogItemView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.ParseColor(MainActivity.ColorPrimary[1]));
            
            
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetNavigationBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[1]));
            Window.SetStatusBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[1]));

            var progressBar = FindViewById<ProgressBar>(Resource.Id.blogsProgress);
            progressBar.ScaleX = 1.5f;
            progressBar.ScaleY = 1.5f;
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(MainActivity.ColorPrimary[1]), PorterDuff.Mode.SrcIn);

            //find blog with id passed to intent
            var content = MainActivity.BlogContentList.FirstOrDefault(c => c.ContentDate.Date == MainActivity.SelectedDates[1].Date)?.ContentList;
            var blogId = Guid.Parse(Intent.GetStringExtra("ID"));
            //activity was called from content list?
            _blog = content?.FirstOrDefault(b => b.ItemId == blogId);
            if (_blog == null)
            {
                //activity was called from person history list?
                var person = MainActivity.PersonList.FirstOrDefault(p =>
                            (p.PersonName == Intent.GetStringExtra("AuthorName") &&
                             p.PersonType == MainActivity.PersonType.Blog));
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
                    _blog.ItemAuthor = await MainActivity.GetPerson(_blog.ItemAuthorUrl, MainActivity.PersonType.Blog);
                    toolbar.Title = _blog.ItemAuthorName;
                }
                catch
                {
                    //close activity on error
                    progressBar.Visibility = ViewStates.Gone;
                    Finish();
                    return;
                }
                toolbar.Title = _blog.ItemAuthor?.PersonName;
            }
            
            if (_blog.ItemDate.Date != DateTime.Now.Date)
                toolbar.Subtitle = _blog.ItemDate.ToString("dddd d MMMM yyyy");

            SetSupportActionBar(toolbar);

            if (!await CheckConnectivity())
                return;

            //get author's picture for the blog
            var pictureView = FindViewById<ImageButton>(Resource.Id.blogPic);
            try
            {
                if (!string.IsNullOrEmpty(_blog.ItemAuthor?.PersonPhotoUrl))
                    pictureView.SetImageBitmap(await _blog.ItemAuthor.GetPersonPhoto(MainActivity.DisplayWidth / 3));
                else if (!string.IsNullOrEmpty(_blog.ItemPictureUrl))
                    pictureView.SetImageBitmap(await MainActivity.GetImage(_blog.ItemPictureUrl, MainActivity.DisplayWidth / 3));
                pictureView.SetOnClickListener(this);
            }
            catch
            {
                pictureView.Visibility = ViewStates.Gone;
            }
            
            //get blog's text
            var titleTextView = FindViewById<EchoTextView>(Resource.Id.blogTitle);
            titleTextView.Setup(_blog.ItemTitle, MainActivity.MainDarkTextColor, TypefaceStyle.Bold, MainActivity.FontSize + 4);
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
            _textWebView = FindViewById<EchoWebView>(Resource.Id.blogText);
            _textWebView.Setup(html);
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