using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.Person
{
    //ViewHolder holds references to the UI components within the CardView 
    public class ShowHistoryViewHolder : RecyclerView.ViewHolder
    {
        public TextView Date { get; private set; }
        public TextView Title { get; private set; }
        public TextView SubTitle { get; private set; }
        public ImageView Picture { get; }
        public string Id;

        //get references to the views defined in the CardView layout
        public ShowHistoryViewHolder(View itemView, Action<string> itemClickListener) : base(itemView)
        {
            //locate and cache view references
            Date = ItemView.FindViewById<TextView>(Resource.Id.contentDate);
            Title = ItemView.FindViewById<TextView>(Resource.Id.contentTitle);
            SubTitle = ItemView.FindViewById<TextView>(Resource.Id.contentSubTitle);
            Picture = ItemView.FindViewById<ImageView>(Resource.Id.showPic);

            //detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                itemClickListener(Id);
            };
        }
    }
}