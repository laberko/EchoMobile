using System;
using Android.Media;

namespace Echo.Player
{
    public sealed class EchoMediaPlayer : MediaPlayer
    {
        public event EventHandler<string> PlaybackStarted;
        public event EventHandler<string> PlaybackPaused;
        public string DataSource;
        public Guid ShowId;
        private EchoPlayerService _playerService;

        public EchoPlayerService PlayerService
        {
            set
            {
                _playerService = value;
            }
        }

        public override void Start()
        {
            PlaybackStarted?.Invoke(this, DataSource);
            base.Start();
        }

        public override void Pause()
        {
            PlaybackPaused?.Invoke(this, DataSource);
            base.Pause();
        }

        public override void Stop()
        {
            PlaybackPaused?.Invoke(this, DataSource);
            base.Stop();
        }

        public bool Toggle()
        {
            if (_playerService == null)
                return false;
            _playerService.PlayPause();
            return true;
        }
    }
}