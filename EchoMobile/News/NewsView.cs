using System;
using System.Linq;
using Android.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.ContentTypes;

namespace Echo.News
{
    public class NewsView
    {
        private DateTime _contentDay;
        private readonly NewsAdapter _adapter;
        private readonly NewsContent _content;
        private readonly Context _context;
        private readonly StaggeredGridLayoutManager _layoutManager;

        public NewsView(DateTime day, View view, Context context)
        {
            _context = context;
            _contentDay = day;

            //get existing news from site: find newsContent for selected date
            _content = Common.NewsContentList.Find(n => n.ContentDay.Date == _contentDay.Date);
            if (_content == null)
            {
                //if not found - create new
                _content = new NewsContent(_contentDay);
                Common.NewsContentList.Add(_content);
                _content.GetContent(10);
            }

            _adapter = new NewsAdapter { Content = _content };
            _adapter.ItemClick += OnItemClick;

            _layoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);

            var rView = view.FindViewById<RecyclerView>(Resource.Id.recycler_view);
            rView.SetLayoutManager(_layoutManager);
            rView.SetAdapter(_adapter);
            rView.AddOnScrollListener(new EchoRecyclerViewListener());

            var refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            refresher.Refresh += delegate
            {
                UpdateContent();
                refresher.Refreshing = false;
            };

            UpdateView();
        }

        private void OnItemClick(object sender, string id)
        {
            var guid = Guid.Parse(id);
            var news = _content.News.FirstOrDefault(n => n.NewsId == guid);
            if (news == null) return;
            var newsIntent = new Intent(_context, typeof (NewsActivity));
            newsIntent.PutExtra("Date", news.NewsDateTime.Date == DateTime.Now.Date ? _context.Resources.GetString(Resource.String.today) : news.NewsDateTime.ToString("d MMMM"));
            newsIntent.PutExtra("Time", news.NewsDateTime.ToString("HH:mm"));
            newsIntent.PutExtra("Title", news.NewsTitle);
            newsIntent.PutExtra("Text", news.NewsText);
            _context.StartActivity(newsIntent);
        }

        //get content from site
        public void UpdateContent()
        {
            if ((Common.CurrentPosition !=0 ) || (_content == null) || (_contentDay.Date != DateTime.Now.Date)) return;
            _content.GetContent(1);
        }

        public void UpdateView()
        {
            if (Common.IsSwiping) return;
            _adapter?.NotifyItemRangeInserted(0, _content.NewContent.Count);
            _content.NewContent.Clear();
            var firstItem = _layoutManager.FindViewByPosition(1);
            if ((firstItem != null) && (firstItem.IsShown))
            {
                _layoutManager.ScrollToPosition(0);
            }
        }
    }
}