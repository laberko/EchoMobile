using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;

namespace Echo.Blog
{
    //RecyclerView adapter to connect the data set (blog) to the RecyclerView
    public class BlogAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> ItemClick;
        public event EventHandler<string> PictureClick;
        public BlogContent Content;

        // create a new blog CardView inside the RecyclerView
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // inflate the CardView
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.BlogCardView, parent, false);
            var viewHolder = new BlogViewHolder(itemView, OnItemClick, OnPictureClick);
            return viewHolder;
        }

        // fill in the contents of the blog card
        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as BlogViewHolder;
            if ((viewHolder == null) || (Content.ContentList.Count == 0))
                return;
            var blog = Content[position];
            if (blog == null)
                return;
            if (!string.IsNullOrEmpty(blog.ItemPictureUrl))
            {
                if (blog.ItemPicture == null)
                {
                    try
                    {
                        blog.ItemPicture = await Common.GetImage(blog.ItemPictureUrl, Common.DisplayWidth / 5);
                    }
                    catch
                    {
                        blog.ItemPicture = null;
                    }
                }
                viewHolder.Picture.SetImageBitmap(blog.ItemPicture);
            }
            else
                viewHolder.Picture.SetImageBitmap(null);
            viewHolder.Author.Text = blog.ItemAuthorName;
            viewHolder.Author.SetTextColor(Color.ParseColor(Common.ColorAccent[1]));
            viewHolder.Author.SetBackgroundColor(Color.Transparent);
            viewHolder.Author.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            viewHolder.Title.Text = blog.ItemTitle;
            viewHolder.Title.SetBackgroundColor(Color.Transparent);
            viewHolder.Title.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            viewHolder.Id = blog.ItemId.ToString();
            viewHolder.AuthorUrl = blog.ItemAuthorUrl;
        }

        //return the number of blogs available
        public override int ItemCount => Content?.ContentList.Count ?? 0;

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
        //item click event handler
        private void OnPictureClick(string personUrl)
        {
            PictureClick?.Invoke(this, personUrl);
        }
    }
}