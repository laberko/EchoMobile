using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;

namespace Echo
{
    public class EchoViewPagerFragment : Fragment
    {
        private readonly Func<LayoutInflater, ViewGroup, Bundle, View> _view;

        public EchoViewPagerFragment()
        {
        }
    
        public EchoViewPagerFragment(Func<LayoutInflater, ViewGroup, Bundle, View> view)
        {
            _view = view;
            //keep the fragment and all its data across screen rotation
            RetainInstance = true;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            try
            {
                base.OnCreateView(inflater, container, savedInstanceState);
                return _view(inflater, container, savedInstanceState);
            }
            catch (Exception ex) when (ex is NullReferenceException)
            {
                FragmentManager.PopBackStack();
                return null;
            }
        }
    }
}