using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Timer = System.Timers.Timer;
using Toolbar = Android.Widget.Toolbar;

namespace Echo
{
    [Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class Activity1 : FragmentActivity
    {
        public DateTime SelectedDay;
        public Toolbar toolbar;
        private ViewPager _pager;
        private GenericFragmentPagerAdapter _pagerAdapter;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SelectedDay = DateTime.Now;
            
            SetContentView(Resource.Layout.Main);
            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            SetActionBar(toolbar);

            _pager = FindViewById<ViewPager>(Resource.Id.pager);
            _pagerAdapter = new GenericFragmentPagerAdapter(SupportFragmentManager);



            _pagerAdapter.AddFragmentView((i, v, b) =>
            {
                if (Common.News != null)
                {
                    Common.News.Dispose();
                }
                var view = i.Inflate(Resource.Layout.tab, v, false);
                var recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recycler_view);
                Common.News = new NewsView(SelectedDay, recyclerView, this, _pagerAdapter);
                return view;
            }
            );

            _pagerAdapter.AddFragmentView((i, v, b) =>
            {
                var view = i.Inflate(Resource.Layout.tab, v, false);
                var sampleTextView = view.FindViewById<TextView>(Resource.Id.textView1);
                sampleTextView.Text = "This is more content";
                return view;
            }
            );

            _pager.Adapter = _pagerAdapter;

        }

        //populate main menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu, menu);
            var listener = new ViewPageListenerForActionBar(this, menu, toolbar);
            _pager.AddOnPageChangeListener(listener);
            listener.OnPageSelected(0);
            return base.OnCreateOptionsMenu(menu);
        }

    }
}