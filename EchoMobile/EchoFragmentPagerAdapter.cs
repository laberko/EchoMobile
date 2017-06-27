using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace Echo
{
    public class EchoFragmentPagerAdapter : FragmentStatePagerAdapter
    {
        //array of ViewPager fragments
        private readonly EchoViewPagerFragment[] _fragmentList;

        public EchoFragmentPagerAdapter(FragmentManager fm) : base(fm)
        {
            _fragmentList = new EchoViewPagerFragment[4];
        }

        public override int Count => _fragmentList.Length;

        public override Fragment GetItem(int position)
        {
            return _fragmentList[position];
        }

        public override int GetItemPosition(Java.Lang.Object objectValue)
        {
            //update all items on NotifyDatasetChanged
            return PositionNone;
        }

        public override IParcelable SaveState()
        {
            //prevent android from unnecesary recreating fragment
            return null;
        }

        public void AddFragmentView(Func<LayoutInflater, ViewGroup, Bundle, View> view, int index)
        {
            if (view == null)
                return;
            var fragment = new EchoViewPagerFragment(view);
            _fragmentList[index] = fragment;
        }
    }
}