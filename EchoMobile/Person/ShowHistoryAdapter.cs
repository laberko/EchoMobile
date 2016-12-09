using System;
using System.Collections.Generic;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Echo.Show;

namespace Echo.Person
{
    //RecyclerView adapter to connect the data set (blog) to the RecyclerView
    public class ShowHistoryAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public List<AbstractContent> Content;

        // create a new blog CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ShowHistoryCardView, parent, false);
            var viewHolder = new ShowHistoryViewHolder(itemView, OnItemClick);
            return viewHolder;
        }

        // fill in the contents of the blog card
        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as ShowHistoryViewHolder;
            if ((viewHolder == null) || (Content.Count == 0))
                return;
            var show = Content[position] as ShowItem;
            if (show == null)
                return;
            if (!string.IsNullOrEmpty(show.ItemPictureUrl))
            {
                var picture = await show.GetPicture(Common.DisplayWidth/5);
                if (picture != null)
                {
                    viewHolder.Picture.SetImageBitmap(picture);
                    viewHolder.Picture.SetBackgroundColor(Color.Transparent);
                }
                else
                    viewHolder.Picture.Visibility = ViewStates.Gone;
            }
            else
                viewHolder.Picture.Visibility = ViewStates.Gone;
            viewHolder.Date.Text = show.ItemDate.ToString("f");
            viewHolder.Date.SetTextColor(Color.ParseColor(Common.ColorAccent[2]));
            viewHolder.Date.SetBackgroundColor(Color.Transparent);
            viewHolder.Date.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            viewHolder.Title.Text = show.ItemTitle;
            viewHolder.Title.SetBackgroundColor(Color.Transparent);
            viewHolder.Title.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            if (!string.IsNullOrEmpty(show.ItemSubTitle))
            {
                viewHolder.SubTitle.Text = show.ItemSubTitle;
                viewHolder.SubTitle.SetBackgroundColor(Color.Transparent);
                viewHolder.SubTitle.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize - 4);
            }
            else
                viewHolder.SubTitle.Visibility = ViewStates.Gone;
            viewHolder.Id = show.ItemId.ToString();
        }

        //return the number of blogs available
        public override int ItemCount => Content?.Count ?? 0;

        //get stable id based on the item guid
        public override long GetItemId(int position)
        {
            return BitConverter.ToInt64(Content[position].ItemId.ToByteArray(), 8);
        }

        //item click event handler
        private void OnItemClick(string id)
        {
            ItemClick?.Invoke(this, id);
        }
    }
}