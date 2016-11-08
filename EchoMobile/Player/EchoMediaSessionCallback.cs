using Android.Support.V4.Media.Session;

namespace Echo.Player
{
    public class EchoMediaSessionCallback : MediaSessionCompat.Callback
    {
        private readonly EchoPlayerService _service;
        public EchoMediaSessionCallback(EchoPlayerServiceBinder binder)
        {
            _service = binder.GetMediaPlayerService();
        }
        public override void OnPause()
        {
            _service.Pause();
            base.OnPause();
        }
        public override void OnPlay()
        {
            _service.Play();
            base.OnPlay();
        }
        public override void OnSkipToNext()
        {
            _service.PlayNext();
            base.OnSkipToNext();
        }
        public override void OnSkipToPrevious()
        {
            _service.PlayPrevious();
            base.OnSkipToPrevious();
        }
        public override void OnStop()
        {
            _service.Stop();
            base.OnStop();
        }
    }
}