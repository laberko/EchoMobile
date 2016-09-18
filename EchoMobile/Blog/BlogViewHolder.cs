using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Echo.Blog
{
    // Implement the ViewHolder pattern: each ViewHolder holds references
    // to the UI components (TextView) within the CardView 
    // that is displayed in a row of the RecyclerView:
    public class BlogViewHolder : RecyclerView.ViewHolder
    {
        //change:
        public TextView Picture { get; private set; }

        public TextView Author { get; private set; }
        public TextView Title { get; private set; }
        public string Id;

        // Get references to the views defined in the BlogCardView layout
        public BlogViewHolder(View itemView, Action<string> listener) : base(itemView)
        {
            // Locate and cache view references
            Title = itemView.FindViewById<TextView>(Resource.Id.blogTitle);
            Author = itemView.FindViewById<TextView>(Resource.Id.blogAuthor);

            //change:
            Picture = itemView.FindViewById<TextView>(Resource.Id.blogPic);

            // Detect user clicks on the item view and report which item was clicked to the listener
            itemView.Click += (sender, e) =>
            {
                listener(Id);
            };
        }
    }
}