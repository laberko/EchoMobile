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
            var show = Content[position] as ShowItem;
            if (show == null)
                return;
            viewHolder.Id = show.ItemId.ToString();
            viewHolder.Date.Text = show.ItemDate.ToString("t");
            viewHolder.Date.SetTextColor(Color.ParseColor(Common.ColorAccent[2]));
            viewHolder.Date.SetBackgroundColor(Color.Transparent);
            viewHolder.Date.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            viewHolder.Title.Text = show.ItemTitle;
            viewHolder.Title.SetBackgroundColor(Color.Transparent);
            viewHolder.Title.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            if (!string.IsNullOrEmpty(show.ShowModeratorNames))
            {
                viewHolder.Moderators.Visibility = ViewStates.Visible;
                viewHolder.Moderators.SetBackgroundColor(Color.Transparent);
                viewHolder.Moderators.Text = show.ShowModeratorNames;
                viewHolder.Moderators.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize - 4);
            }
            else
                viewHolder.Moderators.Visibility = ViewStates.Gone;
            if (!string.IsNullOrEmpty(show.ShowGuestNames))
            {
                viewHolder.Guests.Visibility = ViewStates.Visible;
                viewHolder.Guests.SetBackgroundColor(Color.Transparent);
                viewHolder.Guests.Text = show.ShowGuestNames;
                viewHolder.Guests.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize - 4);
            }
            else
                viewHolder.Guests.Visibility = ViewStates.Gone;

            var soundUrl = show.ItemSoundUrl;
            if (!string.IsNullOrEmpty(soundUrl))
            {
                try
                {
                    //the show has audio
                    if (Common.EchoPlayer != null && Common.EchoPlayer.DataSource == show.ItemSoundUrl && Common.EchoPlayer.IsPlaying)
                        viewHolder.ListenButton.SetImageDrawable(ContextCompat.GetDrawable(_context, Resource.Drawable.pause_black));
                    else
                        viewHolder.ListenButton.SetImageDrawable(ContextCompat.GetDrawable(_context, Resource.Drawable.play_black));
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
        public override int ItemCount => Content?.ContentList.Count ?? 0;

        //get stable id based on the item guid
        public override long GetItemId(int position)
        {
            return BitConverter.ToInt64(Content[position].ItemId.ToByteArray(), 8);
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