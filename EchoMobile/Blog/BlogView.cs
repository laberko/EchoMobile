using System;
using System.Linq;
using Android.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.ContentTypes;

namespace Echo.Blog
{
    public class BlogView
    {
        private DateTime _contentDay;
        private readonly BlogAdapter _adapter;
        private readonly BlogContent _content;
        private readonly Context _context;
        private readonly StaggeredGridLayoutManager _layoutManager;

        public BlogView(DateTime day, View view, Context context)
        {
            _context = context;
            _contentDay = day;

            //get existing news from site:
            //find blogContent for selected date
            _content = Common.BlogContentList.Find(b => b.ContentDay.Date == _contentDay.Date);
            if (_content == null)
            {
                //if not found - create new
                _content = new BlogContent(_contentDay);
                Common.BlogContentList.Add(_content);
                _content.GetContent(10);
            }

            _adapter = new BlogAdapter
            {
                Content = _content,
            };
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

        //open full blog on blog card click
        private void OnItemClick(object sender, string id)
        {
            //try
            //{
                var guid = Guid.Parse(id);
                var blog = _content.Blogs.FirstOrDefault(b => b.BlogId == guid);
                if (blog == null) return;
                var blogIntent = new Intent(_context, typeof (BlogActivity));
                blogIntent.PutExtra("Date", blog.BlogDate.Date == DateTime.Now.Date ? string.Empty : blog.BlogDate.ToString("d MMMM"));
                blogIntent.PutExtra("Author", blog.BlogAuthor.PersonId.ToString());
                blogIntent.PutExtra("Title", blog.BlogTitle);
                blogIntent.PutExtra("Text", blog.BlogText);
                _context.StartActivity(blogIntent);
            //}
            //catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException)
            //{
            //    Toast.MakeText(_context, ex.Message, ToastLength.Short);
            //}
        }

        //get content from site
        public void UpdateContent()
        {
            if ((Common.CurrentPosition != 1) || (_content == null) || (_contentDay.Date != DateTime.Now.Date)) return;
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