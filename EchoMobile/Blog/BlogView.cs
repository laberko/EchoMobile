using System;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using XamarinBindings.MaterialProgressBar;

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
        private readonly MaterialProgressBar _progressBar;

        public BlogView(DateTime day, View view, Context context)
        {
            _context = context;
            _contentDay = day;

            _progressBar = view.FindViewById<MaterialProgressBar>(Resource.Id.blogsProgress);
            _progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[1]), PorterDuff.Mode.SrcIn);
            _progressBar.Visibility = ViewStates.Visible;

            //get existing blogs from collection: find blogContent for selected date
            _content = Common.BlogContentList.FirstOrDefault(b => b.ContentDate.Date == _contentDay.Date);
            if (_content == null)
            {
                //if not found - create new
                _content = new BlogContent(_contentDay);
                Common.BlogContentList.Add(_content);
            }

            //RecyclerView
            _adapter = new BlogAdapter
            {
                Content = _content,
                HasStableIds = true
            };
            _adapter.ItemClick += OnItemClick;
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

        //open full blog on blog card click
        private void OnItemClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId) || _content.Blogs.FirstOrDefault(n => n.BlogId == itemId) == null)
                return;
            var intent = new Intent(_context, typeof (BlogActivity));
            intent.PutExtra("ID", id);
            _context.StartActivity(intent);
        }

        //get content from site (invoked on timer by MainActivity)
        public void UpdateContent()
        {
            if (_contentDay.Date == DateTime.Now.Date)
                _content?.GetContent();
        }

        //update recyclerview (invoked on property changed in BlogContent by MainActivity)
        public void UpdateView()
        {
            if (_content == null || _layoutManager == null || _adapter == null || _rView == null)
                return;
            _progressBar.Visibility = ViewStates.Invisible;
            if (_layoutManager.ItemCount == 0 || _content.NewItemsCount == 0)
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
                    _rView.GetRecycledViewPool().Clear();
                    _adapter.NotifyDataSetChanged();
                }
            }
        }
    }
}