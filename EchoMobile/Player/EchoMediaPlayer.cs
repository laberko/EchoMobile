using System;
using System.Collections.Generic;
using Android.Content;
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
        //private readonly Dictionary<string, string> _headers;

        //public EchoMediaPlayer()
        //{
        //    _headers = new Dictionary<string, string>
        //    {
        //        {
        //            "User-Agent",
        //            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"
        //        }
        //    };
        //}

        //public override void SetDataSource(string path)
        //{
        //    base.SetDataSource(path);
        //    _dataSource = path;
        //}

        //public async void SetDataSourceAsync(Context context, Android.Net.Uri path, Dictionary<string,string> headers)
        //{
        //    await base.SetDataSourceAsync(context, path, headers);
        //    _dataSource = path;
        //}

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

        //public string GetDataSource()
        //{
        //    return _dataSource;
        //}

        public bool Toggle()
        {
            if (_playerService == null)
                return false;
            _playerService.PlayPause();
            return true;
        }
    }
}