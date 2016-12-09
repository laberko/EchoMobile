using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.Blog
{
    //ViewHolder holds references to the UI components within the CardView 
    public class BlogViewHolder : RecyclerView.ViewHolder
    {
        public ImageButton Picture { get; }
        public TextView Author { get; }
        public TextView Title { get; private set; }
        public string Id;
        public string AuthorUrl;

        //get references to the views defined in the BlogCardView layout
        public BlogViewHolder(View itemView, Action<string> itemClickListener, Action<string> pictureClickListener) : base(itemView)
        {
            //locate and cache view references
            Title = ItemView.FindViewById<TextView>(Resource.Id.blogTitle);
            Author = ItemView.FindViewById<TextView>(Resource.Id.blogAuthor);
            Picture = ItemView.FindViewById<ImageButton>(Resource.Id.blogCardPic);

            //detect user clicks on the item view and report which item was clicked to the listener
            Picture.Click += (sender, e) =>
            {
                pictureClickListener(AuthorUrl);
            };

            itemView.Click += (sender, e) =>
            {
                itemClickListener(Id);
            };
        }
    }
}