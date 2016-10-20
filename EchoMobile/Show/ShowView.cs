using System;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Widget;
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

        public ShowView(DateTime day, View view, Context context)
        {
            _context = context;
            _contentDay = day;

            _progressBar = view.FindViewById<MaterialProgressBar>(Resource.Id.showsProgress);
            _progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[2]), PorterDuff.Mode.SrcIn);
            _progressBar.Visibility = ViewStates.Visible;

            //get existing shows from collection: find showContent for selected date
            _content = Common.ShowContentList.FirstOrDefault(s => s.ContentDate.Date == _contentDay.Date);
            if (_content == null)
            {
                //if content not found in collection - create new
                _content = new ShowContent(_contentDay);
                Common.ShowContentList.Add(_content);
            }

            //RecyclerView
            _adapter = new ShowAdapter
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

        //open full show on show card click
        private void OnItemClick(object sender, string id)
        {
            Guid itemId;
            if (!Guid.TryParse(id, out itemId))
                return;
            var item = _content.Shows.FirstOrDefault(n => n.ShowId == itemId);
            item?.OpenShowActivity("Read", _context);
        }

        //get content from site (invoked on timer by MainActivity)
        public void UpdateContent()
        {
            if (_contentDay.Date == DateTime.Now.Date)
                _content.GetContent();
        }

        //update recyclerview (invoked on property changed in ShowContent by MainActivity)
        public void UpdateView()
        {
            if (_layoutManager == null || _adapter == null || _rView == null)
                return;
            _progressBar.Visibility = ViewStates.Invisible;
            try
            {
                var onScreenItem = _layoutManager.FindViewByPosition(5);
                _adapter.NotifyDataSetChanged();
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