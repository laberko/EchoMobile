using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.News
{
    //ViewHolder holds references to the UI components within the CardView 
    public class NewsViewHolder : RecyclerView.ViewHolder
    {
        public TextView Date { get; private set; }
        public TextView Title { get; private set; }
        public string Id;

        //get references to the views defined in the NewsCardView layout
        public NewsViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            //locate and cache view references
            Date = itemView.FindViewById<TextView>(Resource.Id.newsDate);
            Title = itemView.FindViewById<TextView>(Resource.Id.newsTitle);

            //detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                listener(Id);
            };
        }
    }
}