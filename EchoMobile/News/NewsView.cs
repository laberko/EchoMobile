using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Android.Content;
using Android.Support.V7.Widget;
using Echo.ContentTypes;
using Timer = System.Timers.Timer;

namespace Echo.News
{
    public class NewsView : IDisposable
    {
        private DateTime _contentDay;
        private readonly Timer _activityTimer;
        private readonly NewsAdapter _newsAdapter;
        private NewsContent _newsContent;
        private readonly Context _context;

        public NewsView(DateTime day, RecyclerView rView, Context context)
        {
            _context = context;
            _contentDay = day;
            //get existing news from site
            GetNews();

            rView.SetLayoutManager(new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical));
            _newsAdapter = new NewsAdapter {Content = _newsContent};
            _newsAdapter.ItemClick += OnItemClick;
            rView.SetAdapter(_newsAdapter);

            _activityTimer = new Timer
            {
                Interval = 5000
            };
            _activityTimer.Elapsed += OnTimer;
            _activityTimer.Start();
        }

        private void OnItemClick(object sender, string id)
        {
            var guid = Guid.Parse(id);
            var news = _newsContent.News.FirstOrDefault(n => n.NewsId == guid);
            if (news == null) return;
            var newsIntent = new Intent(_context, typeof (NewsActivity));
            newsIntent.PutExtra("Date", news.NewsDateTime.ToString("d MMMM"));
            newsIntent.PutExtra("Time", news.NewsDateTime.ToString("HH:mm"));
            newsIntent.PutExtra("Title", news.NewsTitle);
            newsIntent.PutExtra("Text", news.NewsText);
            _context.StartActivity(newsIntent);
        }

        //get content from site on timer
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            if ((Common.CurrentPosition !=0 ) || (_newsContent == null) || (_contentDay.Date != DateTime.Now.Date) || (_newsAdapter == null)) return;
            //ThreadPool.QueueUserWorkItem(o => _newsContent.AddDummy(1));
            _newsContent.AddDummy(1);
            if ((!Common.IsSwiping) && (Common.pagerAdapter!=null) && (Common.FragmentList.Count != 0))
            {
                Common.pagerAdapter.NotifyDataSetChanged();
            }
        }

        //get news from site - NewsContent instance
        private void GetNews()
        {
            //find newsContent for selected date
            _newsContent = Common.NewsContentList.Find(n => n.ContentDay.Date == _contentDay.Date);
            if (_newsContent != null) return;
            //if not found - create new
            _newsContent = new NewsContent(_contentDay);
            Common.NewsContentList.Add(_newsContent);
        }

        public void Dispose()
        {
            _newsAdapter.Dispose();
            _activityTimer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}