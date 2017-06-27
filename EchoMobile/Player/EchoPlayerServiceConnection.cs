using Android.Content;
using Android.OS;

namespace Echo.Player
{
    public class EchoPlayerServiceConnection : Java.Lang.Object, IServiceConnection
    {
        private readonly IPlayerInitiator _activity;

        public EchoPlayerServiceConnection(IPlayerInitiator acivity)
        {
            _activity = acivity;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            var mediaPlayerServiceBinder = service as EchoPlayerServiceBinder;
            if (mediaPlayerServiceBinder == null)
                return;
            var binder = (EchoPlayerServiceBinder)service;
            var playerService = binder.GetMediaPlayerService();
            _activity.Binder = binder;
            MainActivity.ServiceBinder = binder;
            playerService.Playing += _activity.OnPlaying;
            playerService.Buffering += _activity.OnBuffering;
            _activity.ServiceConnected();
        }

        public void OnServiceDisconnected(ComponentName name)
        {
        }
    }
}