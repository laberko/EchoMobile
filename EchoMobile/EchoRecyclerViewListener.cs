using Android.Support.V7.Widget;
using Android.Views.Animations;

namespace Echo
{
    //listen to RecyclerView scrolls
    public class EchoRecyclerViewListener : RecyclerView.OnScrollListener
    {
        private int _scrolledDistance;
        private bool _controlsVisible = true;

        //hide and show calendar floating action button
        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            base.OnScrolled(recyclerView, dx, dy);
            if (_scrolledDistance > 0 && _controlsVisible)
            {
                Common.Fab?.Animate().TranslationY(Common.Fab.Height + Common.Fab.Bottom).SetInterpolator(new AccelerateInterpolator(2)).Start();
                _controlsVisible = false;
                _scrolledDistance = 0;
            }
                else if (_scrolledDistance < 0 && !_controlsVisible)
            {
                Common.Fab?.Animate().TranslationY(0).SetInterpolator(new DecelerateInterpolator(2)).Start();
                _controlsVisible = true;
                _scrolledDistance = 0;
            }
            if ((_controlsVisible && dy > 0) || (!_controlsVisible && dy < 0))
                _scrolledDistance += dy;
        }
    }
}