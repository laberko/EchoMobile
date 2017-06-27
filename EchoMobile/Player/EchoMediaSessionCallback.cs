using Android.Media.Session;
using Android.Support.V4.Media.Session;

namespace Echo.Player
{
    public class EchoMediaSessionCallback : MediaSession.Callback
    {
        private readonly EchoPlayerService _service;
        public EchoMediaSessionCallback(EchoPlayerServiceBinder binder)
        {
            _service = binder.GetMediaPlayerService();
        }
        public override void OnPause()
        {
            base.OnPause();
            _service.Pause();
        }
        public override void OnPlay()
        {
            base.OnPlay();
            _service.Play();
        }
        public override void OnSkipToNext()
        {
            base.OnSkipToNext();
            _service.PlayNext();
        }
        public override void OnSkipToPrevious()
        {
            base.OnSkipToPrevious();
            _service.PlayPrevious();
        }
        public override void OnStop()
        {
            base.OnStop();
            _service.Stop();
        }
    }
}