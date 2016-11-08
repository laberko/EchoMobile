using System;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;

namespace Echo.Show
{
    //RecyclerView adapter to connect the data set (show) to the RecyclerView
    public class ShowAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public event EventHandler<string> DownloadClick;
        public event EventHandler<string> ListenClick;
        public ShowContent Content;
        private Context _context;

        //create a new show CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            //inflate the CardView
            _context = parent.Context;
            var itemView = LayoutInflater.From(_context).Inflate(Resource.Layout.ShowCardView, parent, false);
            var viewHolder = new ShowViewHolder(itemView, OnItemClick, OnDownloadClick, OnListenClick);
            return viewHolder;
        }

        //fill in the contents of the show card
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as ShowViewHolder;
            if ((viewHolder == null) || (ItemCount == 0))
                return;
            var show = Content[position];
            if (show == null)
                return;
            viewHolder.Id = show.ShowId.ToString();
            viewHolder.Date.Text = show.ShowDateTime.ToString("t");
            viewHolder.Date.SetTextColor(Color.ParseColor(Common.ColorAccent[2]));
            viewHolder.Date.SetBackgroundColor(Color.Transparent);
            viewHolder.Title.Text = show.ShowTitle;
            viewHolder.Title.SetBackgroundColor(Color.Transparent);
            if (!string.IsNullOrEmpty(show.ShowModeratorNames))
            {
                viewHolder.Moderators.Visibility = ViewStates.Visible;
                viewHolder.Moderators.SetBackgroundColor(Color.Transparent);
                viewHolder.Moderators.Text = show.ShowModeratorNames;
            }
            else
                viewHolder.Moderators.Visibility = ViewStates.Gone;
            if (!string.IsNullOrEmpty(show.ShowGuestNames))
            {
                viewHolder.Guests.Visibility = ViewStates.Visible;
                viewHolder.Guests.SetBackgroundColor(Color.Transparent);
                viewHolder.Guests.Text = show.ShowGuestNames;
            }
            else
                viewHolder.Guests.Visibility = ViewStates.Gone;
            var soundUrl = show.ShowSoundUrl;
            if (!string.IsNullOrEmpty(soundUrl))
            {
                try
                {
                    //the show has audio
                    if (Common.EchoPlayer != null && Common.EchoPlayer.GetDataSource() == show.ShowSoundUrl &&
                        Common.EchoPlayer.IsPlaying)
                        viewHolder.ListenButton.SetImageDrawable(ContextCompat.GetDrawable(_context,
                            Resource.Drawable.pause_black));
                    else
                        viewHolder.ListenButton.SetImageDrawable(ContextCompat.GetDrawable(_context,
                            Resource.Drawable.play_black));
                    viewHolder.ButtonsLayout.Visibility = ViewStates.Visible;
                    viewHolder.ButtonsLayout.SetBackgroundColor(Color.Transparent);
                }
                catch
                {
                    viewHolder.ButtonsLayout.Visibility = ViewStates.Gone;
                }
            }
            else
                viewHolder.ButtonsLayout.Visibility = ViewStates.Gone;
        }

        //return the number of shows available
        public override int ItemCount => Content?.Shows.Count ?? 0;

        //get stable id based on the item guid
        public override long GetItemId(int position)
        {
            return BitConverter.ToInt64(Content[position].ShowId.ToByteArray(), 8);
        }

        //click event handlers
        private void OnItemClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
        private void OnDownloadClick(string id)
        {
            DownloadClick?.Invoke(this, id);
        }
        private void OnListenClick(string id)
        {
            ListenClick?.Invoke(this, id);
        }
    }
}