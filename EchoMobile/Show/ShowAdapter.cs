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
            viewHolder.Date.Setup(show.ItemDate.ToString("t"), Color.ParseColor(MainActivity.ColorAccent[2]), TypefaceStyle.Bold, MainActivity.FontSize);
            viewHolder.Title.Setup(show.ItemTitle, MainActivity.MainTextColor, TypefaceStyle.Bold, MainActivity.FontSize);

            if (!string.IsNullOrEmpty(show.ItemSubTitle))
                viewHolder.SubTitle.Setup(show.ItemSubTitle, MainActivity.MainTextColor, TypefaceStyle.Bold, MainActivity.FontSize - 4);
            else
                viewHolder.SubTitle.Visibility = ViewStates.Gone;

            if (!string.IsNullOrEmpty(show.ShowModeratorNames))
                viewHolder.Moderators.Setup(show.ShowModeratorNames, MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize - 4);
            else
                viewHolder.Moderators.Visibility = ViewStates.Gone;

            if (!string.IsNullOrEmpty(show.ShowGuestNames))
                viewHolder.Guests.Setup(show.ShowGuestNames, MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize - 4);
            else
                viewHolder.Guests.Visibility = ViewStates.Gone;

            var soundUrl = show.ItemSoundUrl;
            if (!string.IsNullOrEmpty(soundUrl))
            {
                try
                {
                    //the show has audio
                    if (MainActivity.EchoPlayer != null && MainActivity.EchoPlayer.DataSource == show.ItemSoundUrl && MainActivity.EchoPlayer.IsPlaying)
                        viewHolder.ListenButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                            ? ContextCompat.GetDrawable(_context, Resource.Drawable.pause_black)
                            : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_pause_circle_outline_white_48dp));
                    else
                        viewHolder.ListenButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                            ? ContextCompat.GetDrawable(_context, Resource.Drawable.play_black)
                            : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_play_circle_outline_white_48dp));
                    viewHolder.DownloadButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(_context, Resource.Drawable.download_black)
                        : ContextCompat.GetDrawable(_context, Resource.Drawable.download_white));
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