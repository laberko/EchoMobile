using Android.Support.V7.Widget;
using Android.Views.Animations;

namespace Echo
{
    public class EchoRecyclerViewListener : RecyclerView.OnScrollListener
    {
        private const int HideThreshold = 20;
        private int _scrolledDistance;
        private bool _controlsVisible = true;

        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            base.OnScrolled(recyclerView, dx, dy);
            if (_scrolledDistance > HideThreshold && _controlsVisible)
            {
                Common.fab?.Animate().TranslationY(Common.fab.Height + Common.fab.Bottom).SetInterpolator(new AccelerateInterpolator(2)).Start();
                _controlsVisible = false;
                _scrolledDistance = 0;
            }
                else if (_scrolledDistance < -HideThreshold && !_controlsVisible)
            {
                Common.fab?.Animate().TranslationY(0).SetInterpolator(new DecelerateInterpolator(2)).Start();
                _controlsVisible = true;
                _scrolledDistance = 0;
            }
            if ((_controlsVisible && dy > 0) || (!_controlsVisible && dy < 0))
            {
                _scrolledDistance += dy;
            }
        }
    }
}