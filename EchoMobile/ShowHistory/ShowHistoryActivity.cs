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
using Echo.Person;
using Echo.Show;
using Plugin.Connectivity;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.ShowHistory
{
    [Activity(Label = "",
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTask,
        ResizeableActivity = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class ShowHistoryActivity : AppCompatActivity
    {
        private List<AbstractContent> _content;
        private PersonItem _person;

        protected override async void OnCreate(Bundle bundle)
        {
            SetTheme(MainActivity.Theme);
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.HistoryView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            
            toolbar.SetBackgroundColor(Color.ParseColor(MainActivity.ColorPrimary[2]));
            toolbar.Subtitle = Resources.GetString(Resource.String.show);

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetNavigationBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[2]));
            Window.SetStatusBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[2]));

            var progressBar = FindViewById<ProgressBar>(Resource.Id.historyProgress);
            progressBar.ScaleX = 1.5f;
            progressBar.ScaleY = 1.5f;
            progressBar.Visibility = ViewStates.Visible;
            progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(MainActivity.ColorPrimary[2]), PorterDuff.Mode.SrcIn);

            if (!await CheckConnectivity())
                return;

            //PersonUrl and PersonName are put when searching for a person's shows
            var personUrl = Intent.GetStringExtra("PersonUrl");
            var personName = Intent.GetStringExtra("PersonName");
            //ShowUrl and ShowName are put when searching for the shows with a certain name (ShowUrl = ItemRootUrl)
            var showUrl = Intent.GetStringExtra("ShowUrl");
            var showName = Intent.GetStringExtra("ShowName");

            //if searching for a person's shows
            if (!string.IsNullOrEmpty(personUrl) && !string.IsNullOrEmpty(personName))
            {
                _person = await MainActivity.GetPerson(personUrl, MainActivity.PersonType.Show);
                if (_person == null)
                {
                    progressBar.Visibility = ViewStates.Gone;
                    Finish();
                    return;
                }
                toolbar.Title = personName;
                //refresh history content of the person
                _person.PersonContent = await MainActivity.UpdateShowHistory(progressBar, personUrl, _person.PersonContent);
                _content = _person.PersonContent.Where(c => c.ItemType == MainActivity.ContentType.Show)
                    .OrderByDescending(c => c.ItemDate).Take(MainActivity.ShowHistorySize).ToList();
            }
            //if searching for the shows with a certain name
            else if (!string.IsNullOrEmpty(showUrl) && !string.IsNullOrEmpty(showName))
            {
                toolbar.Title = showName;
                //refresh common history content
                MainActivity.ShowHistoryList = await MainActivity.UpdateShowHistory(progressBar, showUrl);
                _content = MainActivity.ShowHistoryList.Where(c => c.ItemType == MainActivity.ContentType.Show)
                    .OrderByDescending(c => c.ItemDate).Take(MainActivity.ShowHistorySize).ToList();
            }
            else
            {
                progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }

            SetSupportActionBar(toolbar);

            MainActivity.PlayList = _content.Where(c => !string.IsNullOrEmpty(c.ItemSoundUrl)).OrderBy(c => c.ItemDate).Cast<ShowItem>().ToArray();

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