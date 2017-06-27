using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.Show
{
    //ViewHolder holds references to the UI components within the CardView 
    public class ShowViewHolder : RecyclerView.ViewHolder
    {
        public EchoTextView Date { get; private set; }
        public EchoTextView Title { get; private set; }
        public EchoTextView SubTitle { get; private set; }
        public EchoTextView Moderators { get; private set; }
        public EchoTextView Guests { get; private set; }
        public ImageButton DownloadButton { get; }
        public ImageButton ListenButton { get; }
        public LinearLayoutCompat ButtonsLayout { get; private set; }
        public string Id;

        //get references to the views defined in the ShowCardView layout
        public ShowViewHolder(View itemView, Action<string> onItemClick, Action<string> onDownloadClick, Action<string> onListenClick) : base(itemView)
        {
            //locate and cache view references
            Date = ItemView.FindViewById<EchoTextView>(Resource.Id.showDate);
            Title = ItemView.FindViewById<EchoTextView>(Resource.Id.showTitle);
            SubTitle = ItemView.FindViewById<EchoTextView>(Resource.Id.showSubTitle);
            Moderators = ItemView.FindViewById<EchoTextView>(Resource.Id.showModerators);
            Guests = ItemView.FindViewById<EchoTextView>(Resource.Id.showGuests);
            DownloadButton = ItemView.FindViewById<ImageButton>(Resource.Id.showDownload);
            ListenButton = ItemView.FindViewById<ImageButton>(Resource.Id.showListen);
            ButtonsLayout = ItemView.FindViewById<LinearLayoutCompat>(Resource.Id.showButtons);
            
            //detect user clicks and report which item was clicked to the listeners
            itemView.Click += (sender, e) =>
            {
                onItemClick(Id);
            };
            DownloadButton.Click += (sender, e) =>
            {
                onDownloadClick(Id);
            };
            ListenButton.Click += (sender, e) =>
            {
                onListenClick(Id);
            };
        }
    }
}