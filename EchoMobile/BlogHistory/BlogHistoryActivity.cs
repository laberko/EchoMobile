using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using Echo.Blog;
using Echo.Person;
using Plugin.Connectivity;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.BlogHistory
{
    [Activity(Label = "",
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTask,
        ResizeableActivity = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class BlogHistoryActivity : AppCompatActivity
    {
        private List<AbstractContent> _content;
        private PersonItem _person;

        protected override async void OnCreate(Bundle bundle)
        {
            SetTheme(MainActivity.Theme);
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.HistoryView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            
            toolbar.Subtitle = Resources.GetString(Resource.String.blog);

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetNavigationBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[1]));
            Window.SetStatusBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[1]));
            toolbar.SetBackgroundColor(Color.ParseColor(MainActivity.ColorPrimary[1]));

            var progressBar = FindViewById<ProgressBar>(Resource.Id.historyProgress);
            progressBar.ScaleX = 1.5f;
            progressBar.ScaleY = 1.5f;
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(MainActivity.ColorPrimary[1]), PorterDuff.Mode.SrcIn);

            var personUrl = Intent.GetStringExtra("PersonUrl");
            _person = await MainActivity.GetPerson(personUrl, MainActivity.PersonType.Blog);
            if (_person == null)
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            toolbar.Title = _person.PersonName;
            SetSupportActionBar(toolbar);

            if (!await CheckConnectivity())
                return;

            //refresh history content
            await _person.GetBlogHistory(progressBar);
            _content = _person.PersonContent.Where(c => c.ItemType == MainActivity.ContentType.Blog)
                .OrderByDescending(c => c.ItemDate).Take(MainActivity.BlogHistorySize).ToList();

            //RecyclerView
            var adapter = new BlogHistoryAdapter
            {
                Content = _content,
                HasStableIds = true
            };
            adapter.ItemClick += OnItemClick;
            var rView = FindViewById<RecyclerView>(Resource.Id.historyRecyclerView);
            rView.SetLayoutManager(new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical));
            rView.SwapAdapter(adapter, true);
            rView.NestedScrollingEnabled = false;
        }

        protected override void OnNewIntent(Intent intent)
        {
            Finish();
            StartActivity(intent);
        }

        private void OnItemClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId) || _content.FirstOrDefault(n => n.ItemId == itemId) == null)
                return;
            var intent = new Intent(this, typeof(BlogActivity));
            intent.PutExtra("ID", id);
            intent.PutExtra("AuthorName", _person.PersonName);
            StartActivity(intent);
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