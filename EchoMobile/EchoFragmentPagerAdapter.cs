using System;
using System.Collections.Generic;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Echo
{
    public class EchoFragmentPagerAdapter : FragmentPagerAdapter
    {
        private readonly List<Fragment> _fragmentList;
        private readonly FragmentManager _fm;

        public EchoFragmentPagerAdapter(FragmentManager fm) : base(fm)
        {
            if (Common.FragmentList == null)
            {
                Common.FragmentList = new List<Fragment>();
            }
            _fragmentList = Common.FragmentList;
            _fm = fm;
        }

        public override int Count => _fragmentList.Count;

        public override Fragment GetItem(int position)
        {
            return _fragmentList.Count != 0 ? _fragmentList[position] : null;
        }

        public override int GetItemPosition(Java.Lang.Object objectValue)
        {
            //var fragment = (Fragment) objectValue;
            //var fragments = _fm.Fragments;
            //return fragments.Contains(fragment) ? PositionNone : PositionUnchanged;
            return PositionNone;
        }

        public void AddFragmentView(Func<LayoutInflater, ViewGroup, Bundle, View> view)
        {
            _fragmentList.Add(new EchoViewPagerFragment(view));
        }
    }
}