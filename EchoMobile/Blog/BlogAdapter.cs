using System;
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
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as BlogViewHolder;
            if ((viewHolder == null) || (Content.BlogCount == 0)) return;
            //viewHolder.Date.Text = Content[position].NewsDateTime.ToString("t");

            //change:
            //viewHolder.Picture.Text = Content[position].BlogAuthor.PersonPhotoUrl;
            viewHolder.Picture.Text = "PIC";

            viewHolder.Author.Text = Content[position].BlogAuthor.PersonName;
            viewHolder.Title.Text = Content[position].BlogTitle;
            viewHolder.Id = Content[position].BlogId.ToString();

        }

        // Return the number of blogs available
        public override int ItemCount => Content?.BlogCount ?? 0;

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}