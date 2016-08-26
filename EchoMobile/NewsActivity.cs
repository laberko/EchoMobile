using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Echo.ContentTypes;
using Toolbar = Android.Widget.Toolbar;

namespace Echo
{
	[Activity (Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon")]
	public class NewsActivity : Activity
	{
        RecyclerView _newsRecyclerView;
        RecyclerView.LayoutManager _newsLayoutManager;
        private NewsAdapter _newsAdapter;
        private NewsContent _newsContent;
        private DateTime _contentDay;
        private Toolbar _toolbarBottom;
        private LinearLayout _mainContent;

        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            SetContentView(Resource.Layout.Main);
            _toolbarBottom = FindViewById<Toolbar>(Resource.Id.toolbar_bottom);
            _mainContent = FindViewById<LinearLayout>(Resource.Id.main_content);

            if (!DateTime.TryParse(Intent.GetStringExtra("Date"), out _contentDay))
            {
                _contentDay = DateTime.Now.Date;
            }
            GetNews();

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetActionBar(toolbar);
            ActionBar.Title = Resources.GetText(Resource.String.news);

            _newsRecyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            _newsLayoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);
            _newsRecyclerView.SetLayoutManager(_newsLayoutManager);
            _newsAdapter = new NewsAdapter(_newsContent);
            _newsRecyclerView.SetAdapter(_newsAdapter);
            _newsAdapter.ItemClick += OnItemClick;

		    UpdateBottom(Resource.Menu.menu_news);
            AddNews();
        }

        private async void AddNews()
        {
            while (true)
            {
                //add fake news
                await Task.Delay(5000);
                //only if current day selected
                if ((_newsContent == null) || (_contentDay.Date!=DateTime.Now.Date)) return;
                _newsContent.AddDummy(1);
                //_blogAdapter.NotifyItemInserted(0);
                _newsAdapter.NotifyDataSetChanged();
                //Calling NotifyItemChanged is significantly more efficient than calling NotifyDataSetChanged
            }
        }

        //TODO: calendar widget changes _contentDay and _toolbarBottom.Title and calls GetNews()
        private void GetNews()
	    {
	        if (Common.newsContent == null)
	        {
	            Common.newsContent = new List<NewsContent>();
	        }
            var newsContent = Common.newsContent.Find(n => n.ContentDay.Date == _contentDay.Date);
            if (newsContent == null)
            {
                newsContent = new NewsContent(_contentDay);
                Common.newsContent.Add(newsContent);
            }
	        _newsContent = newsContent;
	        _newsAdapter?.NotifyDataSetChanged();
	    }

        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //main menu item selected
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Toast.MakeText(this, item.TitleFormatted, ToastLength.Short).Show();
            switch (item.ItemId)
            {
                case Resource.Id.top_menu_news:
                    //StartActivity(typeof(MainActivity));
                    var newsIntent = new Intent(this, typeof(NewsActivity));
                    newsIntent.PutExtra("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    StartActivity(newsIntent);
                    break;
                case Resource.Id.top_menu_blog:
                    //StartActivity(typeof(BlogActivity));
                    var blogIntent = new Intent(this, typeof(BlogActivity));
                    blogIntent.PutExtra("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    StartActivity(blogIntent);
                    break;
                case Resource.Id.top_menu_show:
                    break;
                default:
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        //invoked after main menu item selected
        private void UpdateBottom(int menuId)
	    {
            _toolbarBottom.MenuItemClick += OnBottomMenuItemSelected;
            _mainContent.RemoveView(_toolbarBottom);
            _toolbarBottom.Title = $"{Resources.GetText(Resource.String.news)} ({_contentDay.ToString("m")})";
            _toolbarBottom.Menu.Clear();
            MenuInflater.Inflate(menuId, _toolbarBottom.Menu);
            _mainContent.AddView(_toolbarBottom);
        }

        //bottom menu item selected
        private void OnBottomMenuItemSelected(object sender, Toolbar.MenuItemClickEventArgs e)
	    {
            Toast.MakeText(this, e.Item.TitleFormatted, ToastLength.Short).Show();
	    }

        private void OnItemClick(object sender, string id)
        {
            Toast.MakeText(this, "ID: " + id, ToastLength.Short).Show();
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
            itemView.Click += (sender, e) => listener(Id.Text);
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
        void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }




}


