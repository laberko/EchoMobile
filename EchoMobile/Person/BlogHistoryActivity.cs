using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.Blog;
using XamarinBindings.MaterialProgressBar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Person
{
    [Activity(Label = "",
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTask,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class BlogHistoryActivity : AppCompatActivity
    {
        private List<AbstractContent> _content;
        private PersonItem _person;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.HistoryView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetSupportActionBar(toolbar);
            if ((int)Build.VERSION.SdkInt > 19)
            {
                Window.SetNavigationBarColor(Color.ParseColor(Common.ColorPrimaryDark[1]));
                Window.SetStatusBarColor(Color.ParseColor(Common.ColorPrimaryDark[1]));
            }
            toolbar.SetBackgroundColor(Color.ParseColor(Common.ColorPrimary[1]));
            SupportActionBar.Subtitle = Resources.GetString(Resource.String.blog);
            var progressBar = FindViewById<MaterialProgressBar>(Resource.Id.historyProgress);
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[1]), PorterDuff.Mode.SrcIn);
            var personUrl = Intent.GetStringExtra("PersonUrl");
            _person = await Common.GetPerson(personUrl, Common.PersonType.Blog);
            if (_person == null)
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            SupportActionBar.Title = _person.PersonName;
            //refresh history content
            await _person.GetBlogHistory(progressBar);
            _content = _person.PersonContent.Where(c => c.ItemType == Common.ContentType.Blog)
                .OrderByDescending(c => c.ItemDate).Take(Common.BlogHistorySize).ToList();

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
    }
}