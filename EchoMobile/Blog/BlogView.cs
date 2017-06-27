using System;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Echo.BlogHistory;

namespace Echo.Blog
{
    //create blogs content and view for a fragment
    public class BlogView
    {
        private DateTime _contentDay;
        private readonly RecyclerView _rView;
        private readonly BlogAdapter _adapter;
        private readonly BlogContent _content;
        private readonly Context _context;
        private readonly StaggeredGridLayoutManager _layoutManager;
        private readonly ProgressBar _progressBar;

        public BlogView(DateTime day, View view, Context context)
        {
            _context = context;
            _contentDay = day;

            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.blogsProgress);
            _progressBar.ScaleX = 1.5f;
            _progressBar.ScaleY = 1.5f;
            _progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(MainActivity.ColorPrimary[1]), PorterDuff.Mode.SrcIn);

            //get existing blogs from collection: find blogContent for selected date
            _content = MainActivity.BlogContentList.FirstOrDefault(b => b.ContentDate.Date == _contentDay.Date) as BlogContent;
            if (_content == null)
            {
                //if not found - create new
                _content = new BlogContent(_contentDay, _progressBar);
                MainActivity.BlogContentList.Add(_content);
            }

            //RecyclerView
            _adapter = new BlogAdapter
            {
                Content = _content,
                HasStableIds = true
            };
            _adapter.ItemClick += OnItemClick;
            _adapter.PictureClick += OnPictureClick;
            _layoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);
            _rView = view.FindViewById<RecyclerView>(Resource.Id.recycler_view);
            _rView.SetPadding(0, 0, 0, MainActivity.DisplayWidth / 4);
            _rView.SetLayoutManager(_layoutManager);
            _rView.SwapAdapter(_adapter, true);
            _adapter.NotifyDataSetChanged();

            _progressBar.Visibility = _layoutManager.ItemCount == 0 ? ViewStates.Visible : ViewStates.Gone;

            //swipe down refresh
            var refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            //refresher.SetFitsSystemWindows(false);
            refresher.Refresh += delegate
            {
                UpdateContent();
                refresher.Refreshing = false;
            };
        }


        private void OnPictureClick(object sender, string personUrl)
        {
            var intent = new Intent(_context, typeof(BlogHistoryActivity));


            //request that the new Activity launches adjacent if possible
            intent.AddFlags(ActivityFlags.LaunchAdjacent);


            //required for adjacent activity mode
            intent.AddFlags(ActivityFlags.NewTask);
            intent.PutExtra("PersonUrl", personUrl);
            _context.StartActivity(intent);
        }

        //open full blog on blog card click
        private void OnItemClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId) || _content.ContentList.FirstOrDefault(n => n.ItemId == itemId) == null)
                return;
            var intent = new Intent(_context, typeof (BlogActivity));


            //request that the new Activity launches adjacent if possible
            intent.AddFlags(ActivityFlags.LaunchAdjacent);


            //required for adjacent activity mode
            intent.AddFlags(ActivityFlags.NewTask);
            intent.PutExtra("ID", id);
            _context.StartActivity(intent);
        }

        //get content from site (invoked on timer by MainActivity)
        public void UpdateContent()
        {
            if (_contentDay.Date == DateTime.Now.Date)
                _content.GetContent();
        }

        //update recyclerview (invoked on property changed in BlogContent by MainActivity)
        public void UpdateView()
        {
            _progressBar.Visibility = ViewStates.Gone;
            if (_content == null || _layoutManager == null || _adapter == null || _rView == null)
                return;
            var hasOffScreenItems = false;
            for (var i = 0; i < _layoutManager.ItemCount; i++)
            {
                var item = _layoutManager.FindViewByPosition(i);
                if (item == null || item.IsShown)
                    continue;
                hasOffScreenItems = true;
                break;
            }
            if (_layoutManager.ItemCount == 0 || _content.NewItemsCount == 0 || !hasOffScreenItems)
                _adapter.NotifyDataSetChanged();
            else
            {
                try
                {
                    var onScreenItem = _layoutManager.FindViewByPosition(5);
                    _adapter.NotifyItemRangeInserted(0, _content.NewItemsCount - 1);
                    if ((onScreenItem != null) && (onScreenItem.IsShown))
                        _layoutManager.ScrollToPosition(0);
                }
                catch
                {
                    _adapter.NotifyDataSetChanged();
                }
            }
        }

        public void HideBar()
        {
            _progressBar.Visibility = ViewStates.Gone;
        }
    }
}