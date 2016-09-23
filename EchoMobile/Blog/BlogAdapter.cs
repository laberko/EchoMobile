using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.ContentTypes;

namespace Echo.Blog
{
    // Adapter to connect the data set (blog) to the RecyclerView
    public class BlogAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public BlogContent Content;

        // Create a new blog CardView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BlogCardView, parent, false);
            var viewHolder = new BlogViewHolder(itemView, OnClick);
            return viewHolder;
        }

        // Fill in the contents of the blog card
        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            Common.IsSwiping = true;
            var viewHolder = holder as BlogViewHolder;
            if ((viewHolder == null) || (Content.Blogs.Count == 0)) return;
            viewHolder.Picture.SetImageBitmap(await Common.GetImageBitmapFromUrlAsync(Content[position].BlogAuthor.PersonPhotoUrl, Common.DisplayWidth / 4));
            viewHolder.Author.Text = Content[position].BlogAuthor.PersonName;
            viewHolder.Author.SetTextColor(Color.ParseColor(Common.colorAccent[1]));
            viewHolder.Title.Text = Content[position].BlogTitle;
            viewHolder.Id = Content[position].BlogId.ToString();
            Common.IsSwiping = false;
        }

        // Return the number of blogs available
        public override int ItemCount => Content?.Blogs.Count ?? 0;

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}