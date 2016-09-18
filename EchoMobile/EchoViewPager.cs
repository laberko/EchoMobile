using Android.Content;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;

namespace Echo
{
    public class EchoViewPager : ViewPager
    {
        public EchoViewPager(Context context):base(context)
        {
        }

        public EchoViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            var action = ev.Action & MotionEventActions.Mask;
            switch (action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Move:
                    Common.IsSwiping = true;
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    Common.IsSwiping = false;
                    break;
                default:
                    break;
            }
            return base.OnTouchEvent(ev);
        }


    }
}