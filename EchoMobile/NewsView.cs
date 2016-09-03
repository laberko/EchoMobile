using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Echo.ContentTypes;
using Timer = System.Timers.Timer;
using Toolbar = Android.Widget.Toolbar;

namespace Echo
{
    public class NewsView : IDisposable
    {
        public DateTime ContentDay;
        public Timer _activityTimer;
        public RecyclerView ContentRecyclerView;
        private NewsAdapter _newsAdapter;
        private NewsContent _newsContent;
        private Context _context;
        private GenericFragmentPagerAdapter _pagerAdapter;

        public NewsView(DateTime day, RecyclerView rView, Context context, GenericFragmentPagerAdapter pagerAdapter)
        {
            _context = context;
            _pagerAdapter = pagerAdapter;
            ContentDay = day;
            ContentRecyclerView = rView;
            GetNews();
            ContentRecyclerView.SetLayoutManager(new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical));
            _newsAdapter = new NewsAdapter(_newsContent);
            _newsAdapter.ItemClick += OnItemClick;
            ContentRecyclerView.SetAdapter(_newsAdapter);

            _activityTimer = new Timer
            {
                Interval = 5000
            };
            _activityTimer.Elapsed += OnTimer;
            _activityTimer.Start();
        }

        private void OnItemClick(object sender, string id)
        {
            Toast.MakeText(_context, "ID: " + id, ToastLength.Short).Show();

        }

        //get content from site on timer
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            if ((_newsContent == null) || (ContentDay.Date != DateTime.Now.Date)) return;
            _newsContent.AddDummy(1);
            _pagerAdapter.NotifyDataSetChanged();
            //_newsAdapter?.NotifyDataSetChanged();
        }


        private void GetNews()
        {
            if (Common.newsContent == null)
            {
                Common.newsContent = new List<NewsContent>();
            }
            var newsContent = Common.newsContent.Find(n => n.ContentDay.Date == ContentDay.Date);
            if (newsContent == null)
            {
                newsContent = new NewsContent(ContentDay);
                Common.newsContent.Add(newsContent);
            }
            _newsContent = newsContent;
            //_newsAdapter?.NotifyDataSetChanged();
        }

        public void Dispose()
        {
            _activityTimer.Dispose();
            _newsAdapter.Dispose();
            GC.SuppressFinalize(this);
        }



    }




    // Implement the ViewHolder pattern: each ViewHolder holds references
    // to the UI components (TextView) within the CardView 
    // that is displayed in a row of the RecyclerView:
    public class NewsViewHolder : RecyclerView.ViewHolder
    {
        public TextView Date { get; private set; }
        public TextView Title { get; private set; }
        public TextView Id { get; private set; }

        // Get references to the views defined in the NewsCardView layout
        public NewsViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            // Locate and cache view references
            Date = itemView.FindViewById<TextView>(Resource.Id.newsDate);
            Title = itemView.FindViewById<TextView>(Resource.Id.newsTitle);
            Id = itemView.FindViewById<TextView>(Resource.Id.newsId);

            // Detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                listener(Id.Text);
            };

        }

    }

    // Adapter to connect the data set (news) to the RecyclerView
    public class NewsAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        private readonly NewsContent _content;


        public NewsAdapter(NewsContent newsContent)
        {
            _content = newsContent;
        }

        // Create a new news CardView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.NewsCardView, parent, false);
            var viewHolder = new NewsViewHolder(itemView, OnClick);
            return viewHolder;
        }

        // Fill in the contents of the news card
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as NewsViewHolder;
            if (viewHolder == null) return;
            viewHolder.Date.Text = _content[position].NewsDateTime.ToString("t");
            viewHolder.Title.Text = _content[position].NewsTitle;
            viewHolder.Id.Text = _content[position].NewsId.ToString();
        }

        // Return the number of news available
        public override int ItemCount => _content.NewsCount;

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);

        }


    }



}