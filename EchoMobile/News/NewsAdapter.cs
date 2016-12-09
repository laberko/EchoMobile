using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;

namespace Echo.News
{
    //RecyclerView adapter to connect the data set (news) to the RecyclerView
    public class NewsAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public NewsContent Content;

        // create a new news CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.NewsCardView, parent, false);
            var viewHolder = new NewsViewHolder(itemView, OnClick);
            return viewHolder;
        }

        // fill in the contents of the news card
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as NewsViewHolder;
            if ((viewHolder == null) || (Content.ContentList.Count == 0))
                return;
            var news = Content[position];
            if (news == null)
                return;
            viewHolder.Date.Text = news.ItemDate.ToString("t");
            viewHolder.Date.SetTextColor(Color.ParseColor(Common.ColorAccent[0]));
            viewHolder.Date.SetBackgroundColor(Color.Transparent);
            viewHolder.Date.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            viewHolder.Title.Text = news.ItemTitle;
            viewHolder.Title.SetBackgroundColor(Color.Transparent);
            viewHolder.Title.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            viewHolder.Id = news.ItemId.ToString();
        }

        // return the number of news available
        public override int ItemCount => Content?.ContentList.Count ?? 0;

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