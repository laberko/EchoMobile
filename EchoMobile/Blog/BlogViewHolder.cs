using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.Blog
{
    //ViewHolder holds references to the UI components within the CardView 
    public class BlogViewHolder : RecyclerView.ViewHolder
    {
        public ImageView Picture { get; private set; }
        public TextView Author { get; private set; }
        public TextView Title { get; private set; }
        public string Id;

        //get references to the views defined in the BlogCardView layout
        public BlogViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            //locate and cache view references
            Title = itemView.FindViewById<TextView>(Resource.Id.blogTitle);
            Author = itemView.FindViewById<TextView>(Resource.Id.blogAuthor);
            Picture = itemView.FindViewById<ImageView>(Resource.Id.blogCardPic);

            //detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                listener(Id);
            };
        }
    }
}