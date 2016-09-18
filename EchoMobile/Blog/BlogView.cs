using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Android.Content;
using Android.Support.V7.Widget;
using Echo.ContentTypes;
using Timer = System.Timers.Timer;

namespace Echo.Blog
{
    public class BlogView : IDisposable
    {
        private DateTime _contentDay;
        private readonly Timer _activityTimer;
        private readonly BlogAdapter _blogAdapter;
        private BlogContent _blogContent;
        private readonly Context _context;

        public BlogView(DateTime day, RecyclerView rView, Context context)
        {
            _context = context;
            _contentDay = day;
            //get existing news from site
            GetBlogs();

            rView.SetLayoutManager(new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical));
            _blogAdapter = new BlogAdapter {Content = _blogContent};
            _blogAdapter.ItemClick += OnItemClick;
            rView.SetAdapter(_blogAdapter);

            _activityTimer = new Timer
            {
                Interval = 5111
            };
            _activityTimer.Elapsed += OnTimer;
            _activityTimer.Start();
        }

        //open full blog on blog card click
        private void OnItemClick(object sender, string id)
        {
            var guid = Guid.Parse(id);
            var blog = _blogContent.Blogs.FirstOrDefault(b => b.BlogId == guid);
            if (blog == null) return;
            var blogIntent = new Intent(_context, typeof (BlogActivity));
            blogIntent.PutExtra("Date", blog.BlogDate.ToString("d MMMM"));
            blogIntent.PutExtra("Author", blog.BlogAuthor.PersonId.ToString());
            blogIntent.PutExtra("Title", blog.BlogTitle);
            blogIntent.PutExtra("Text", blog.BlogText);
            _context.StartActivity(blogIntent);
        }

        //get content from site on timer
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            if ((Common.CurrentPosition != 1) || (_blogContent == null) || (_contentDay.Date != DateTime.Now.Date) || (_blogAdapter == null)) return;
            //ThreadPool.QueueUserWorkItem(o => _newsContent.AddDummy(1));
            _blogContent.AddDummy(1);
            if ((!Common.IsSwiping) && (Common.pagerAdapter!=null) && (Common.FragmentList.Count != 0))
            {
                Common.pagerAdapter.NotifyDataSetChanged();
            }
        }

        //get blogs from site - BlogContent instance
        private void GetBlogs()
        {
            //find blogContent for selected date
            _blogContent = Common.BlogContentList.Find(b => b.ContentDay.Date == _contentDay.Date);
            if (_blogContent != null) return;
            //if not found - create new
            _blogContent = new BlogContent(_contentDay);
            Common.BlogContentList.Add(_blogContent);
        }

        public void Dispose()
        {
            _blogAdapter.Dispose();
            _activityTimer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}