using System;
using System.Linq;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using XamarinBindings.MaterialProgressBar;

namespace Echo.Show
{
    //create shows content and view for a fragment
    public class ShowView
    {
        private DateTime _contentDay;
        private readonly RecyclerView _rView;
        private readonly ShowAdapter _adapter;
        private readonly ShowContent _content;
        private readonly Context _context;
        private readonly StaggeredGridLayoutManager _layoutManager;
        private readonly MaterialProgressBar _progressBar;
        private const string Permissions = Manifest.Permission.WriteExternalStorage;


        public ShowView(DateTime day, View view, Context context)
        {
            if (ContextCompat.CheckSelfPermission((AppCompatActivity)context, Permissions) != Permission.Granted)
                ActivityCompat.RequestPermissions((AppCompatActivity)context, new[] { Permissions }, 0);
            _context = context;
            _contentDay = day;
            _progressBar = view.FindViewById<MaterialProgressBar>(Resource.Id.showsProgress);
            _progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[2]), PorterDuff.Mode.SrcIn);
            _progressBar.Visibility = ViewStates.Visible;

            //get existing shows from collection: find showContent for selected date
            _content = Common.ShowContentList.FirstOrDefault(s => s.ContentDate.Date == _contentDay.Date) as ShowContent;
            if (_content == null)
            {
                //if content not found in collection - create new
                _content = new ShowContent(_contentDay, _progressBar);
                Common.ShowContentList.Add(_content);
            }

            //RecyclerView
            _adapter = new ShowAdapter
            {
                Content = _content,
                HasStableIds = true
            };
            _adapter.ItemClick += OnItemClick;
            _adapter.DownloadClick += OnDownloadClick;
            _adapter.ListenClick += OnListenClick;
            _layoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);
            _rView = view.FindViewById<RecyclerView>(Resource.Id.recycler_view);
            _rView.SetLayoutManager(_layoutManager);
            _rView.AddOnScrollListener(new EchoRecyclerViewListener());
            _rView.SwapAdapter(_adapter, true);
            _adapter.NotifyDataSetChanged();

            //swipe down refresh
            var refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            refresher.Refresh += delegate
            {
                UpdateContent();
                refresher.Refreshing = false;
            };
        }

        public ShowItem[] PlayList
        {
            get
            {
                return _content.ContentList
                    .Where(c => (c.ItemType == Common.ContentType.Show && !string.IsNullOrEmpty(c.ItemSoundUrl)))
                    .OrderBy(c => c.ItemDate).Cast<ShowItem>().ToArray();
            }
        }

        //open full show on show card click
        private void OnItemClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId))
                return;
            var showIntent = new Intent(_context, typeof(ShowActivity));
            showIntent.PutExtra("ID", id);
            _context.StartActivity(showIntent);
        }

        //download button pressed - download mp3 file using Android DownloadManager
        private void OnDownloadClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId))
                return;
            var item = _content.ContentList.FirstOrDefault(n => n.ItemId == itemId);
            if (string.IsNullOrEmpty(item?.ItemSoundUrl))
                return;
            try
            {
                using (var dm = (DownloadManager) Application.Context.GetSystemService(Context.DownloadService))
                using (var request = new DownloadManager.Request(Android.Net.Uri.Parse(item.ItemSoundUrl)))
                {
                    if (ContextCompat.CheckSelfPermission(_context, Permissions) == Permission.Granted)
                    {
                        var uri = new Uri(item.ItemSoundUrl);
                        if (Common.FolderSettings == _context.Resources.GetString(Resource.String.music) || Common.FolderSettings == string.Empty)
                            request.SetDestinationInExternalPublicDir(Android.OS.Environment.DirectoryMusic,
                                System.IO.Path.GetFileName(uri.LocalPath));
                        else if (Common.FolderSettings == _context.Resources.GetString(Resource.String.downloads))
                            request.SetDestinationInExternalPublicDir(Android.OS.Environment.DirectoryDownloads,
                                System.IO.Path.GetFileName(uri.LocalPath));
                        else if (Common.FolderSettings == _context.Resources.GetString(Resource.String.documents))
                            request.SetDestinationInExternalPublicDir(Android.OS.Environment.DirectoryDocuments,
                                System.IO.Path.GetFileName(uri.LocalPath));
                        else if (Common.FolderSettings == _context.Resources.GetString(Resource.String.podcasts))
                            request.SetDestinationInExternalPublicDir(Android.OS.Environment.DirectoryPodcasts,
                                System.IO.Path.GetFileName(uri.LocalPath));
                        request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);
                        dm.Enqueue(request);
                    }
                    else
                        ActivityCompat.RequestPermissions((AppCompatActivity)_context, new[] { Permissions }, 0);
                }
            }
            catch
            {
                // ignored
            }
        }

        //listen button pressed - open show and begin playback
        private void OnListenClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId))
                return;
            //this show audio is playing now - no need to open full view, just toggle playback
            if (Common.EchoPlayer != null && Common.EchoPlayer.ShowId == itemId && Common.EchoPlayer.Toggle())
                return;
            var showIntent = new Intent(_context, typeof(ShowActivity));
            //extra Play means start playback immediately
            showIntent.PutExtra("Play", true);
            showIntent.PutExtra("ID", id);
            _context.StartActivity(showIntent);
        }

        //get content from site (invoked on timer by MainActivity) for current day only
        public void UpdateContent()
        {
            if (_contentDay.Date == DateTime.Now.Date)
                _content.GetContent();
        }

        //update recyclerview (invoked on property changed in ShowContent by MainActivity)
        public void UpdateView()
        {
            _progressBar.Visibility = ViewStates.Gone;
            if (_layoutManager == null || _adapter == null || _rView == null)
                return;
            try
            {
                var onScreenItem = _layoutManager.FindViewByPosition(5);
                _adapter.NotifyDataSetChanged();
                if (onScreenItem != null && onScreenItem.IsShown)
                    _layoutManager.ScrollToPosition(0);
            }
            catch
            {
                _adapter.NotifyDataSetChanged();
            }
        }

        public void UpdateViewOnPlay()
        {
            _adapter.NotifyDataSetChanged();
        }

        public void HideBar()
        {
            _progressBar.Visibility = ViewStates.Gone;
        }
    }
}