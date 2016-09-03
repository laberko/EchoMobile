using System;
using System.Collections.Generic;
using System.Timers;
using Android.App;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Echo.ContentTypes;

namespace Echo
{
    [Activity (Label = "@string/app_name", Icon = "@drawable/icon")]
	public class NewsActivity : Activity
    {
        private RecyclerView.LayoutManager _newsLayoutManager;
        private NewsAdapter _newsAdapter;
        private NewsContent _newsContent;
        //protected GestureDetector GestDetect;

        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            ActionBar.Title = Resources.GetText(Resource.String.news);
            //GetNews();

            _newsLayoutManager = new StaggeredGridLayoutManager(1, StaggeredGridLayoutManager.Vertical);
            //ContentRecyclerView.SetLayoutManager(_newsLayoutManager);
            _newsAdapter = new NewsAdapter(_newsContent);
            _newsAdapter.ItemClick += OnItemClick;
            //ContentRecyclerView.SetAdapter(_newsAdapter);
            //GestDetect = new GestureDetector(this);
        }

        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            var menuItem = menu.FindItem(Resource.Id.top_menu_news);
            menuItem.SetIcon(Resource.Drawable.news_white);
            return base.OnCreateOptionsMenu(menu);
        }



        //get content from site on timer
        //protected override void OnTimer(object sender, ElapsedEventArgs e)
        //{
        //    if ((_newsContent == null) || (ContentDay.Date != DateTime.Now.Date)) return;
        //    _newsContent.AddDummy(1);
        //    RunOnUiThread(() => _newsAdapter?.NotifyDataSetChanged());
        //}

        //TODO: calendar widget changes _contentDay and _toolbarBottom.Title and calls GetNews()
     //   private void GetNews()
	    //{
	    //    if (Common.newsContent == null)
	    //    {
	    //        Common.newsContent = new List<NewsContent>();
	    //    }
     //       //var newsContent = Common.newsContent.Find(n => n.ContentDay.Date == ContentDay.Date);
     //       if (newsContent == null)
     //       {
     //           //newsContent = new NewsContent(ContentDay);
     //           Common.newsContent.Add(newsContent);
     //       }
	    //    _newsContent = newsContent;
	    //    _newsAdapter?.NotifyDataSetChanged();
	    //}

        private void OnItemClick(object sender, string id)
        {
            Toast.MakeText(this, "ID: " + id, ToastLength.Short).Show();
        }



        //public bool OnDown(MotionEvent e)
        //{
        //    Toast.MakeText(this, "DOWN!", ToastLength.Short).Show();
        //    return false;
        //}
        //public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        //{
        //    Toast.MakeText(this, $"Fling velocity: {velocityX} x {velocityY}", ToastLength.Short).Show();
        //    return true;
        //}
        //public void OnLongPress(MotionEvent e) { }
        //public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        //{
        //    return false;
        //}
        //public void OnShowPress(MotionEvent e) { }
        //public bool OnSingleTapUp(MotionEvent e)
        //{
        //    return false;
        //}

        public override bool OnTouchEvent(MotionEvent e)
        {
            //GestDetect.OnTouchEvent(e);
            Toast.MakeText(this, "OnTouchEvent", ToastLength.Short).Show();
            return base.OnTouchEvent(e);
        }


    }




}