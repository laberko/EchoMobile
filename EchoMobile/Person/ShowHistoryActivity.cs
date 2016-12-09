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
using Echo.Show;
using XamarinBindings.MaterialProgressBar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Person
{
    [Activity(Label = "",
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTask,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class ShowHistoryActivity : AppCompatActivity
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
                Window.SetNavigationBarColor(Color.ParseColor(Common.ColorPrimaryDark[2]));
                Window.SetStatusBarColor(Color.ParseColor(Common.ColorPrimaryDark[2]));
            }
            toolbar.SetBackgroundColor(Color.ParseColor(Common.ColorPrimary[2]));
            SupportActionBar.Subtitle = Resources.GetString(Resource.String.show);
            var progressBar = FindViewById<MaterialProgressBar>(Resource.Id.historyProgress);
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[2]), PorterDuff.Mode.SrcIn);

            //PersonUrl and PersonName are put when searching for a person's shows
            var personUrl = Intent.GetStringExtra("PersonUrl");
            var personName = Intent.GetStringExtra("PersonName");
            //ShowUrl and ShowName are put when searching for the shows with a certain name (ShowUrl = ItemRootUrl)
            var showUrl = Intent.GetStringExtra("ShowUrl");
            var showName = Intent.GetStringExtra("ShowName");

            //if searching for a person's shows
            if (!string.IsNullOrEmpty(personUrl) && !string.IsNullOrEmpty(personName))
            {
                _person = await Common.GetPerson(personUrl, Common.PersonType.Show);
                if (_person == null)
                {
                    progressBar.Visibility = ViewStates.Gone;
                    Finish();
                    return;
                }
                SupportActionBar.Title = personName;
                //refresh history content of the person
                _person.PersonContent = await Common.UpdateShowHistory(progressBar, personUrl, _person.PersonContent);
                _content = _person.PersonContent.Where(c => c.ItemType == Common.ContentType.Show)
                    .OrderByDescending(c => c.ItemDate).Take(Common.ShowHistorySize).ToList();
            }
            //if searching for the shows with a certain name
            else if (!string.IsNullOrEmpty(showUrl) && !string.IsNullOrEmpty(showName))
            {
                SupportActionBar.Title = showName;
                //refresh common history content
                Common.ShowHistoryList = await Common.UpdateShowHistory(progressBar, showUrl);
                _content = Common.ShowHistoryList.Where(c => c.ItemType == Common.ContentType.Show)
                    .OrderByDescending(c => c.ItemDate).Take(Common.ShowHistorySize).ToList();
            }
            else
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }

            Common.PlayList = _content.Where(c => !string.IsNullOrEmpty(c.ItemSoundUrl)).OrderBy(c => c.ItemDate).Cast<ShowItem>().ToArray();

            //RecyclerView
            var adapter = new ShowHistoryAdapter
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
            var intent = new Intent(this, typeof(ShowActivity));
            intent.PutExtra("ID", id);
            //if searching for a person's shows
            if (_person != null)
                intent.PutExtra("PersonName", _person.PersonName);
            else
            //if searching for the shows with a certain name
                intent.PutExtra("ShowSearch", true);
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