using System;
using System.Collections.Generic;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.Person;

namespace Echo.BlogHistory
{
    //RecyclerView adapter to connect the data set (blog) to the RecyclerView
    public class BlogHistoryAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public List<AbstractContent> Content;

        // create a new blog CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BlogHistoryCardView, parent, false);
            var viewHolder = new BlogHistoryViewHolder(itemView, OnClick);
            return viewHolder;
        }

        // fill in the contents of the blog card
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as BlogHistoryViewHolder;
            if ((viewHolder == null) || (Content.Count == 0))
                return;
            var blog = Content[position];
            if (blog == null)
                return;

            viewHolder.Date.Setup(blog.ItemDate.ToString("d MMMM yyyy"), Color.ParseColor(MainActivity.ColorAccent[1]),
                TypefaceStyle.Bold, MainActivity.FontSize);
            viewHolder.Title.Setup(blog.ItemTitle, MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
            viewHolder.Id = blog.ItemId.ToString();
        }

        //return the number of blogs available
        public override int ItemCount => Content?.Count ?? 0;

        //get stable id based on the item guid
        public override long GetItemId(int position)
        {
            return BitConverter.ToInt64(Content[position].ItemId.ToByteArray(), 8);
        }

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}