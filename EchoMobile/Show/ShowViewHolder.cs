using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.Show
{
    //ViewHolder holds references to the UI components within the CardView 
    public class ShowViewHolder : RecyclerView.ViewHolder
    {
        public TextView Date { get; private set; }
        public TextView Title { get; private set; }
        public TextView Moderators { get; private set; }
        public TextView Guests { get; private set; }
        public ImageButton DownloadButton { get; private set; }
        public ImageButton ListenButton { get; private set; }
        public LinearLayoutCompat ButtonsLayout { get; private set; }
        public string Id;

        //get references to the views defined in the ShowCardView layout
        public ShowViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            //locate and cache view references
            Date = itemView.FindViewById<TextView>(Resource.Id.showDate);
            Title = itemView.FindViewById<TextView>(Resource.Id.showTitle);
            Moderators = itemView.FindViewById<TextView>(Resource.Id.showModerators);
            Guests = itemView.FindViewById<TextView>(Resource.Id.showGuests);
            DownloadButton = ItemView.FindViewById<ImageButton>(Resource.Id.showDownload);
            ListenButton = ItemView.FindViewById<ImageButton>(Resource.Id.showListen);
            ButtonsLayout = ItemView.FindViewById<LinearLayoutCompat>(Resource.Id.showButtons);

            //detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                listener(Id);
            };
        }
    }
}