using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Echo.ContentTypes;
using Toolbar = Android.Widget.Toolbar;

namespace Echo
{
    [Activity(Label = "@string/app_name")]
    public class BlogActivity : Activity
    {
        RecyclerView _blogRecyclerView;
        RecyclerView.LayoutManager _blogLayoutManager;
        private BlogAdapter _blogAdapter;
        private BlogContent _blogContent;
        private DateTime _contentDay;
        private Toolbar _toolbarBottom;
        private LinearLayout _mainContent;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            _toolbarBottom = FindViewById<Toolbar>(Resource.Id.toolbar_bottom);
            _mainContent = FindViewById<LinearLayout>(Resource.Id.main_content);

            if (!DateTime.TryParse(Intent.GetStringExtra("Date"), out _contentDay))
            {
                _contentDay = DateTime.Now.Date;
            }
            GetBlogs();

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetActionBar(toolbar);
            ActionBar.Title = Resources.GetText(Resource.String.blog);

            _blogRecyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);

            // Use the built-in linear layout manager
            //_blogLayoutManager = new LinearLayoutManager(this);
            //other options:
            //GridLayoutManager – Displays items in a grid
            //StaggeredGridLayoutManager – Displays items in a staggered grid, where some items have different heights and widths
            _blogLayoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);
            //_blogLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Vertical, true);


            // Plug the layout manager into the RecyclerView
            _blogRecyclerView.SetLayoutManager(_blogLayoutManager);

            // Create an adapter for the RecyclerView, and pass it the data set (blogs) to manage:
            _blogAdapter = new BlogAdapter(_blogContent);

            //Register the item click handler with the adapter
            _blogAdapter.ItemClick += OnItemClick;

            //Plug the adapter into the RecyclerView
            _blogRecyclerView.SetAdapter(_blogAdapter);

            //var addBlogButton = FindViewById<Button>(Resource.Id.addBlogButton);
            //_timer.Elapsed += delegate
            //{
            //    if (_blogContent == null) return;
            //    _blogContent.AddDummy(1);
            //    //_blogAdapter.NotifyItemInserted(0);
            //    _blogAdapter.NotifyDataSetChanged();
            //    //Calling NotifyItemChanged is significantly more efficient than calling NotifyDataSetChanged
            //};

            UpdateBottom(Resource.Menu.menu_blog);
            AddBlog();

        }

        private async void AddBlog()
        {
            while (true)
            {
                await Task.Delay(5000);
                //only if today selected
                if ((_blogContent == null) || (_contentDay.Date != DateTime.Now.Date)) return;
                _blogContent.AddDummy(1);
                //_blogAdapter.NotifyItemInserted(0);
                _blogAdapter.NotifyDataSetChanged();
                //Calling NotifyItemChanged is significantly more efficient than calling NotifyDataSetChanged
            }
        }

        //TODO: calendar widget changes _contentDay and _toolbarBottom.Title and calls GetBlogs()
        private void GetBlogs()
        {
            if (Common.blogContent == null)
            {
                Common.blogContent = new List<BlogContent>();
            }
            var blogContent = Common.blogContent.Find(b => b.ContentDay.Date == _contentDay.Date);
            if (blogContent == null)
            {
                blogContent = new BlogContent(_contentDay);
                Common.blogContent.Add(blogContent);
            }
            _blogContent = blogContent;
            _blogAdapter?.NotifyDataSetChanged();
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
            _toolbarBottom.Title = $"{Resources.GetText(Resource.String.blog)} ({_contentDay.ToString("m")})";
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
    public class BlogViewHolder : RecyclerView.ViewHolder
    {
        public TextView Author { get; private set; }
        public TextView Date { get; private set; }
        public TextView Title { get; private set; }
        public TextView Id { get; private set; }

        // Get references to the views defined in the BlogCardView layout
        public BlogViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            // Locate and cache view references
            Author = itemView.FindViewById<TextView>(Resource.Id.blogAuthor);
            Date = itemView.FindViewById<TextView>(Resource.Id.blogDate);
            Title = itemView.FindViewById<TextView>(Resource.Id.blogTitle);
            Id = itemView.FindViewById<TextView>(Resource.Id.blogId);

            // Detect user clicks on the item view and report which item was clicked (by position) to the listener
            itemView.Click += (sender, e) => listener(Id.Text);
        }
    }

    // Adapter to connect the data set (blogs) to the RecyclerView
    public class BlogAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        private readonly BlogContent _content;

        public BlogAdapter(BlogContent blogContent)
        {
            _content = blogContent;
        }

        // Create a new blog CardView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BlogCardView, parent, false);
            var viewHolder = new BlogViewHolder(itemView, OnClick);
            return viewHolder;
        }

        // Fill in the contents of the blog card
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as BlogViewHolder;
            if (viewHolder == null) return;
            viewHolder.Author.Text =_content[position].BlogAuthor.PersonName;
            viewHolder.Date.Text = _content[position].BlogDate.ToString("m");
            viewHolder.Title.Text = _content[position].BlogTitle;
            viewHolder.Id.Text = _content[position].BlogId.ToString();
        }

        // Return the number of blogs available
        public override int ItemCount => _content.BlogCount;

        //item click event handler
        void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}