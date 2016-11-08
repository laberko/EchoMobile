using System;
using System.Linq;
using Android.Content;
using Android.OS;
using Echo.Show;

namespace Echo.Player
{
    public class EchoPlayerServiceConnection : Java.Lang.Object, IServiceConnection
    {
        private readonly ShowActivity _activity;
        private readonly ShowItem _show;

        public EchoPlayerServiceConnection(ShowActivity acivity, Guid showId)
        {
            _activity = acivity;
            var content = Common.ShowContentList.FirstOrDefault(c => c.ContentDate.Date == Common.SelectedDates[2].Date)?.Shows;
            _show = content?.FirstOrDefault(s => s.ShowId == showId);
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            var mediaPlayerServiceBinder = service as EchoPlayerServiceBinder;
            if (mediaPlayerServiceBinder == null) return;
            var binder = (EchoPlayerServiceBinder)service;
            var playerService = binder.GetMediaPlayerService();
            _activity.Binder = binder;
            Common.ServiceBinder = binder;

            playerService.Show = _show;
            playerService.Playing += _activity.OnPlaying;
            playerService.Buffering += _activity.OnBuffering;
            _activity.ServiceConnected();
        }

        public void OnServiceDisconnected(ComponentName name)
        {
        }
    }
}