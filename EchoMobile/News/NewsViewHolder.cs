using System;
using Android.Support.V7.Widget;
using Android.Views;

namespace Echo.News
{
    //ViewHolder holds references to the UI components within the CardView 
    public class NewsViewHolder : RecyclerView.ViewHolder
    {
        public EchoTextView Date { get; private set; }
        public EchoTextView Title { get; private set; }
        public string Id;

        //get references to the views defined in the NewsCardView layout
        public NewsViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            //locate and cache view references
            Date = ItemView.FindViewById<EchoTextView>(Resource.Id.newsDate);
            Title = ItemView.FindViewById<EchoTextView>(Resource.Id.newsTitle);

            //detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                listener(Id);
            };
        }
    }
}