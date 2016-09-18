using System;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.ContentTypes;

namespace Echo.News
{
    // Adapter to connect the data set (news) to the RecyclerView
    public class NewsAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public NewsContent Content;

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
            if ((viewHolder == null) || (Content.NewsCount == 0)) return;
            viewHolder.Date.Text = Content[position].NewsDateTime.ToString("t");
            viewHolder.Title.Text = Content[position].NewsTitle;
            viewHolder.Id = Content[position].NewsId.ToString();

        }

        // Return the number of news available
        public override int ItemCount => Content?.NewsCount ?? 0;

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}