using System;
using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;

namespace Echo.Show
{
    //RecyclerView adapter to connect the data set (show) to the RecyclerView
    public class ShowAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public ShowContent Content;
        private Context _context;

        //create a new show CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            //inflate the CardView
            _context = parent.Context;
            var itemView = LayoutInflater.From(_context).Inflate(Resource.Layout.ShowCardView, parent, false);
            var viewHolder = new ShowViewHolder(itemView, OnClick);
            return viewHolder;
        }

        //fill in the contents of the show card
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as ShowViewHolder;
            if ((viewHolder == null) || (Content.Shows.Count == 0))
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
            if (!string.IsNullOrEmpty(show.ShowSoundUrl))
            {
                //the show has audio
                viewHolder.ButtonsLayout.Visibility = ViewStates.Visible;
                viewHolder.ButtonsLayout.SetBackgroundColor(Color.Transparent);
                viewHolder.DownloadButton.Click += (sender, args) => show.DownloadAudio(_context);
                //listen button pressed - open show and begin playback
                viewHolder.ListenButton.Click += (sender, args) => show.OpenShowActivity("Play", _context);
            }
            else
            {
                viewHolder.ButtonsLayout.Visibility = ViewStates.Gone;
            }
        }

        //return the number of shows available
        public override int ItemCount => Content?.Shows.Count ?? 0;

        //get stable id based on the item guid
        public override long GetItemId(int position)
        {
            return BitConverter.ToInt64(Content[position].ShowId.ToByteArray(), 8);
        }

        //item click event handler
        private void OnClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}