using System;
using System.Timers;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Echo.ContentTypes;

namespace Echo
{
    [Activity(Label = "@string/app_name")]
    public class BlogActivity : EchoActivity
    {
        private RecyclerView.LayoutManager _blogLayoutManager;
        private BlogAdapter _blogAdapter;
        private BlogContent _blogContent;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Title = Resources.GetText(Resource.String.blog);
            GetBlogs();

            //GridLayoutManager – Displays items in a grid
            _blogLayoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);
            //_blogLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Vertical, true);

            // Plug the layout manager into the RecyclerView
            ContentRecyclerView.SetLayoutManager(_blogLayoutManager);

            // Create an adapter for the RecyclerView, and pass it the data set (blogs) to manage:
            _blogAdapter = new BlogAdapter(_blogContent);

            //Register the item click handler with the adapter
            _blogAdapter.ItemClick += OnItemClick;

            //Plug the adapter into the RecyclerView
            ContentRecyclerView.SetAdapter(_blogAdapter);
        }


        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            var menuItem = menu.FindItem(Resource.Id.top_menu_blog);
            menuItem.SetIcon(Resource.Drawable.blog_white);
            return base.OnCreateOptionsMenu(menu);
        }



        //get content from site on timer
        protected override void OnTimer(object sender, ElapsedEventArgs e)
        {
            if ((_blogContent == null) || (ContentDay.Date != DateTime.Now.Date)) return;
            _blogContent.AddDummy(1);
            RunOnUiThread(() => _blogAdapter?.NotifyDataSetChanged());
        }

        //TODO: calendar widget changes _contentDay and _toolbarBottom.Title and calls GetBlogs()
        //get content from list
        private void GetBlogs()
        {
            if (Common.blogContent == null)
            {
                Common.blogContent = new List<BlogContent>();
            }
            var blogContent = Common.blogContent.Find(b => b.ContentDay.Date == ContentDay.Date);
            if (blogContent == null)
            {
                blogContent = new BlogContent(ContentDay);
                Common.blogContent.Add(blogContent);
            }
            _blogContent = blogContent;
            _blogAdapter?.NotifyDataSetChanged();
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