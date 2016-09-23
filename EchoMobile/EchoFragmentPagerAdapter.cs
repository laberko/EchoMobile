using System;
using System.Collections.Generic;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace Echo
{
    public class EchoFragmentPagerAdapter : FragmentPagerAdapter
    {
        public EchoFragmentPagerAdapter(FragmentManager fm) : base(fm)
        {
            if (Common.FragmentList == null)
            {
                Common.FragmentList = new List<EchoViewPagerFragment>();
            }
        }

        public override int Count => Common.FragmentList.Count;

        public override Fragment GetItem(int position)
        {
            return Common.FragmentList.Count != 0 ? Common.FragmentList[position] : null;
        }

        public override int GetItemPosition(Java.Lang.Object objectValue)
        {
            var fragment = (EchoViewPagerFragment)objectValue;
            return Common.FragmentList.IndexOf(fragment) == Common.CurrentPosition ? PositionNone : PositionUnchanged;
        }

        public static void AddFragmentView(Func<LayoutInflater, ViewGroup, Bundle, View> view)
        {
            Common.FragmentList.Add(new EchoViewPagerFragment(view));
        }
    }
}