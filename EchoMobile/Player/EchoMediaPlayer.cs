using System;
using Android.Media;

namespace Echo.Player
{
    public sealed class EchoMediaPlayer : MediaPlayer
    {
        public event EventHandler<string> PlaybackStarted;
        public event EventHandler<string> PlaybackPaused;
        private string _dataSource;
        public Guid ShowId;
        private EchoPlayerService _playerService;

        public EchoMediaPlayer()
        {
            SetAudioStreamType(Stream.Music);
        }

        //public EchoMediaPlayer GetPlayer(EchoPlayerService service, string path, Guid showId)
        //{
        //    var player = new EchoMediaPlayer
        //    {
        //        PlayerService = service,
        //        ShowId = showId
        //    };
        //    player.SetDataSource(path);
        //}

        public override void SetDataSource(string path)
        {
            _dataSource = path;
            base.SetDataSource(path);
        }

        public EchoPlayerService PlayerService
        {
            set
            {
                _playerService = value;
            }
        }

        public override void Start()
        {
            PlaybackStarted?.Invoke(this, _dataSource);
            base.Start();
        }

        public override void Pause()
        {
            PlaybackPaused?.Invoke(this, _dataSource);
            base.Pause();
        }

        public override void Stop()
        {
            PlaybackPaused?.Invoke(this, _dataSource);
            base.Stop();
        }

        public string GetDataSource()
        {
            return _dataSource;
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