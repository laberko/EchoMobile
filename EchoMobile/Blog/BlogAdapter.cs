using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;

namespace Echo.Blog
{
    //RecyclerView adapter to connect the data set (blog) to the RecyclerView
    public class BlogAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public BlogContent Content;

        // create a new blog CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BlogCardView, parent, false);
            var viewHolder = new BlogViewHolder(itemView, OnClick);
            return viewHolder;
        }

        // fill in the contents of the blog card
        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as BlogViewHolder;
            if ((viewHolder == null) || (Content.Blogs.Count == 0))
                return;
            var blog = Content[position];
            if (blog == null)
                return;
            if (!string.IsNullOrEmpty(blog.BlogImageUrl))
            {
                if (blog.BlogImage == null)
                {
                    try
                    {
                        blog.BlogImage = await Common.GetImage(blog.BlogImageUrl, Common.DisplayWidth / 5);
                    }
                    catch
                    {
                        blog.BlogImage = null;
                    }
                }
                viewHolder.Picture.SetImageBitmap(blog.BlogImage);
            }
            else
                viewHolder.Picture.SetImageBitmap(null);
            viewHolder.Author.Text = blog.BlogAuthorName;
            viewHolder.Author.SetTextColor(Color.ParseColor(Common.ColorAccent[1]));
            viewHolder.Author.SetBackgroundColor(Color.Transparent);
            viewHolder.Title.Text = blog.BlogTitle;
            viewHolder.Title.SetBackgroundColor(Color.Transparent);
            viewHolder.Id = blog.BlogId.ToString();
        }

        //return the number of blogs available
        public override int ItemCount => Content?.Blogs.Count ?? 0;

        //get stable id based on the item guid
        public override long GetItemId(int position)
        {
            return BitConverter.ToInt64(Content[position].BlogId.ToByteArray(), 8);
        }

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}