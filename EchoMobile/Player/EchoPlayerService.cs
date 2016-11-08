using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Echo.Show;
using Java.Lang;

namespace Echo.Player
{
    public delegate void StatusChangedEventHandler(object sender, EventArgs e);
    public delegate void BufferingEventHandler(object sender, EventArgs e);
    public delegate void PlayingEventHandler(object sender, EventArgs e);

    [Service]
    public class EchoPlayerService : Service, AudioManager.IOnAudioFocusChangeListener,
    MediaPlayer.IOnBufferingUpdateListener,
    MediaPlayer.IOnCompletionListener,
    MediaPlayer.IOnErrorListener,
    MediaPlayer.IOnPreparedListener,
    MediaPlayer.IOnSeekCompleteListener
    {
        //Actions
        public const string ActionPlay = "net.laberko.action.PLAY";
        public const string ActionPause = "net.laberko.action.PAUSE";
        public const string ActionStop = "net.laberko.action.STOP";
        public const string ActionTogglePlayback = "net.laberko.action.TOGGLEPLAYBACK";
        public const string ActionNext = "net.laberko.action.NEXT";
        public const string ActionPrevious = "net.laberko.action.PREVIOUS";

        private EchoMediaPlayer _mediaPlayer;
        private AudioManager _audioManager;
        public ShowItem Show;
        private ShowItem _nextShow;
        private ShowItem _previousShow;
        private MediaSessionCompat _mediaSessionCompat;
        private MediaControllerCompat _mediaControllerCompat;
        private WifiManager _wifiManager;
        private WifiManager.WifiLock _wifiLock;
        private ComponentName _remoteComponentName;
        private const int NotificationId = 1;
        public event StatusChangedEventHandler StatusChanged;
        public event PlayingEventHandler Playing;
        public event BufferingEventHandler Buffering;
        private readonly Handler _playingHandler;
        private readonly Runnable _playingHandlerRunnable;
        private IBinder _binder;
        private int _buffered;
        private Bitmap _cover;
        private Bitmap _compatCover;
        private NotificationCompat.Builder _builder;
        private bool _isFocusLost;

        public EchoPlayerService()
        {
            if (Common.EchoPlayer == null)
                Common.EchoPlayer = new EchoMediaPlayer();
            _mediaPlayer = Common.EchoPlayer;

            //create an instance for a runnable-handler
            _playingHandler = new Handler();

            //create a runnable, restarting itself if the status still is "playing" every second
            _playingHandlerRunnable = new Runnable(() => {
                OnPlaying();
                if (MediaPlayerState == PlaybackStateCompat.StatePlaying)
                    _playingHandler.PostDelayed(_playingHandlerRunnable, 1000);
            });

            //on Status changed to PLAYING, start raising the Playing event
            StatusChanged += delegate
            {
                if (MediaPlayerState == PlaybackStateCompat.StatePlaying)
                    _playingHandler.PostDelayed(_playingHandlerRunnable, 0);
            };
        }

        private int MediaPlayerState
        {
            get
            {
                try
                {
                    return _mediaControllerCompat?.PlaybackState?.State ?? PlaybackStateCompat.StateNone;
                }
                catch
                {
                    return PlaybackStateCompat.StateNone;
                }
            }
        }

        public int Position
        {
            get
            {
                if (_mediaPlayer == null
                    || (MediaPlayerState != PlaybackStateCompat.StatePlaying
                        && MediaPlayerState != PlaybackStateCompat.StatePaused))
                    return -1;
                try
                {
                    return _mediaPlayer.CurrentPosition;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int Duration
        {
            get
            {
                if (_mediaPlayer == null
                    || (MediaPlayerState != PlaybackStateCompat.StatePlaying
                        && MediaPlayerState != PlaybackStateCompat.StatePaused))
                    return 0;
                try
                {
                    return _mediaPlayer.Duration;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int Buffered
        {
            get
            {
                return _mediaPlayer == null ? 0 : _buffered;
            }
            private set
            {
                _buffered = value;
                OnBuffering(EventArgs.Empty);
            }
        }

        public Bitmap Cover
        {
            private get
            {
                return _cover ?? BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon);
            }
            set
            {
                _cover = value;
                if (MediaPlayerState != PlaybackStateCompat.StatePlaying || _mediaPlayer.GetDataSource() != Show.ShowSoundUrl)
                    return;
                //cover belongs to currently playing track - update notification
                StartNotification(true);
                UpdateMediaMetadataCompat();
            }
        }

        public override void OnCreate()
        {
            base.OnCreate();
            _compatCover = BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon_white);
            //find audio and notificaton managers
            _audioManager = (AudioManager)GetSystemService(AudioService);
            _wifiManager = (WifiManager)GetSystemService(WifiService);
            _remoteComponentName = new ComponentName(PackageName, new EchoMediaButtonReceiver().ComponentName);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            try
            {
                if (intent.Action == null)
                    return StartCommandResult.Sticky;
                var action = intent.Action;
                if (action.Equals(ActionPlay))
                    _mediaControllerCompat.GetTransportControls().Play();
                else if (action.Equals(ActionPause))
                    _mediaControllerCompat.GetTransportControls().Pause();
                else if (action.Equals(ActionPrevious))
                    _mediaControllerCompat.GetTransportControls().SkipToPrevious();
                else if (action.Equals(ActionNext))
                    _mediaControllerCompat.GetTransportControls().SkipToNext();
                else if (action.Equals(ActionStop))
                    _mediaControllerCompat.GetTransportControls().Stop();
            }
            catch
            {
                OnDestroy();
            }
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            _binder = new EchoPlayerServiceBinder(this);
            return _binder;
        }

        public override void OnDestroy()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Reset();
                _mediaPlayer.Release();
                _mediaPlayer = null;
                Common.EchoPlayer = null;
            }
            NotificationManagerCompat.From(ApplicationContext).CancelAll();
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
            base.OnDestroy();
        }

        private void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlaying()
        {
            try
            {
                _builder.SetProgress(Duration, Position, false);
                if (Position > 0 && Show != null)
                    Show.PlayerPosition = Position;
                if (_mediaPlayer.IsPlaying)
                    NotificationManagerCompat.From(ApplicationContext).Notify(NotificationId, _builder.Build());
                Playing?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }

        private void OnBuffering(EventArgs e)
        {
            Buffering?.Invoke(this, e);
        }

        public void OnBufferingUpdate(MediaPlayer mediaPlayer, int percent)
        {
            var duration = 0;
            if (MediaPlayerState == PlaybackStateCompat.StatePlaying || MediaPlayerState == PlaybackStateCompat.StatePaused)
                duration = mediaPlayer.Duration;
            var newBufferedTime = duration * percent / 100;
            Buffered = newBufferedTime;
        }

        public void OnCompletion(MediaPlayer mediaPlayer)
        {
            PlayNext();
        }

        public bool OnError(MediaPlayer mediaPlayer, MediaError what, int extra)
        {
            UpdatePlaybackState(PlaybackStateCompat.StateError);
            Stop();
            return true;
        }

        public void OnSeekComplete(MediaPlayer mediaPlayer)
        {
        }

        public async void OnPrepared(MediaPlayer mediaPlayer)
        {
            //mediaplayer is prepared - start track playback
            mediaPlayer.SeekTo(Show.PlayerPosition);
            mediaPlayer.Start();
            UpdatePlaybackState(PlaybackStateCompat.StatePlaying);
            Cover = await Show.GetShowPicture();

            //prepare next and previous show instance
            var content = Common.ShowContentList
                .FirstOrDefault(c => c.ContentDate.Date == Common.SelectedDates[2].Date)?
                .Shows.Where(s => !string.IsNullOrEmpty(s.ShowSoundUrl))
                .OrderBy(s => s.ShowDateTime).ToArray();
            if (content == null)
                return;
            var currentIndex = Array.IndexOf(content, Show);
            _nextShow = currentIndex == content.Length - 1 || currentIndex == -1
                ? content[0]
                : content[currentIndex + 1];
            _previousShow = currentIndex == 0 || currentIndex == -1
                ? content[content.Length - 1]
                : content[currentIndex - 1];

            StartNotification(true);
            //update the metadata now that we are playing
            UpdateMediaMetadataCompat();

        }

        //change volume on audio focus change
        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (_isFocusLost)
                    {
                        if (_mediaPlayer != null && !_mediaPlayer.IsPlaying)
                            Play();
                        //turn volume up
                        _mediaPlayer?.SetVolume(1.0f, 1.0f);
                    }
                    _isFocusLost = false;
                    break;
                case AudioFocus.Loss:
                case AudioFocus.LossTransient:
                    //we have lost focus - pause
                    if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
                    {
                        _isFocusLost = true;
                        Pause();
                    }
                    break;
                case AudioFocus.LossTransientCanDuck:
                    //we have lost focus but should play at a muted 20% volume
                    if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
                    {
                        _isFocusLost = true;
                        //turn volume down
                        _mediaPlayer.SetVolume(.2f, .2f);
                    }
                    break;
            }
        }

        private void InitMediaSession()
        {
            try
            {
                if (_mediaSessionCompat == null)
                {
                    var showIntent = new Intent(ApplicationContext, typeof(ShowActivity));
                    showIntent.PutExtra("ID", Show.ShowId.ToString());
                    var pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0, showIntent, 0);
                    _remoteComponentName = new ComponentName(PackageName, new EchoMediaButtonReceiver().ComponentName);
                    _mediaSessionCompat = new MediaSessionCompat(ApplicationContext, "echomobile", _remoteComponentName, pendingIntent);
                    _mediaControllerCompat = new MediaControllerCompat(ApplicationContext, _mediaSessionCompat.SessionToken);
                }
                _mediaSessionCompat.Active = true;
                _mediaSessionCompat.SetCallback(new EchoMediaSessionCallback((EchoPlayerServiceBinder)_binder));
                _mediaSessionCompat.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
            }
            catch
            {
                // ignored
            }
        }

        private void InitializePlayer()
        {
            if (Common.EchoPlayer == null)
                Common.EchoPlayer = new EchoMediaPlayer();
            _mediaPlayer = Common.EchoPlayer;
            //wake mode will be partial to keep the CPU still running under lock screen
            _mediaPlayer.SetWakeMode(ApplicationContext, WakeLockFlags.Partial);
            _mediaPlayer.SetOnBufferingUpdateListener(this);
            _mediaPlayer.SetOnCompletionListener(this);
            _mediaPlayer.SetOnErrorListener(this);
            _mediaPlayer.SetOnPreparedListener(this);
        }

        public async Task Seek(int position)
        {
            await Task.Run(() =>
            {
                _mediaPlayer?.SeekTo(position);
            });
        }

        public void Play()
        {
            if (_mediaSessionCompat == null)
                InitMediaSession();
            if (_mediaPlayer == null || Show.ShowSoundUrl != _mediaPlayer.GetDataSource())
                InitializePlayer();
            try
            {
                //player is on pause, track not changed
                if (_mediaPlayer != null && MediaPlayerState == PlaybackStateCompat.StatePaused && Show.ShowSoundUrl == _mediaPlayer.GetDataSource())
                {
                        //start again
                        OnPrepared(_mediaPlayer);
                        return;
                }

                //player is playing, track not changed
                if (_mediaPlayer != null && _mediaPlayer.IsPlaying && Show.ShowSoundUrl == _mediaPlayer.GetDataSource())
                {
                    if (Show.ShowSoundUrl == _mediaPlayer.GetDataSource())
                    {
                        //keep playing
                        UpdatePlaybackState(PlaybackStateCompat.StatePlaying);
                        return;
                    }
                }

                //different track
                _mediaPlayer.Stop();
                UpdatePlaybackState(PlaybackStateCompat.StateStopped);
                _mediaPlayer.Reset();
                _mediaPlayer.SetDataSource(Show.ShowSoundUrl);
                _mediaPlayer.ShowId = Show.ShowId;
                _mediaPlayer.PlayerService = this;
                _mediaPlayer.PrepareAsync();
                UpdatePlaybackState(PlaybackStateCompat.StateBuffering);
                _audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
                _isFocusLost = false;
                //lock the wifi so we can still stream under lock screen
                if (_wifiLock == null)
                    _wifiLock = _wifiManager.CreateWifiLock(WifiMode.Full, "echomobile_wifi_lock");
                _wifiLock.Acquire();
            }
            catch (IllegalStateException)
            {
            }
            catch
            {
                UpdatePlaybackState(PlaybackStateCompat.StateStopped);
                _mediaPlayer?.Reset();
                _mediaPlayer?.Release();
                _mediaPlayer = null;
            }
        }

        //toggle play-pause
        public void PlayPause()
        {
            if (_mediaPlayer == null
                || (_mediaPlayer != null && MediaPlayerState == PlaybackStateCompat.StatePaused)
                || (_mediaPlayer != null && MediaPlayerState == PlaybackStateCompat.StateStopped)
                || (_mediaPlayer != null && Show.ShowSoundUrl != _mediaPlayer.GetDataSource()))
                Play();
            else
                Pause();
        }

        public void Pause()
        {
            try
            {
                if (_mediaPlayer == null)
                    return;
                if (_mediaPlayer.IsPlaying)
                    _mediaPlayer.Pause();
                UpdatePlaybackState(PlaybackStateCompat.StatePaused);
            }
            catch
            {
                Stop();
            }
        }

        public void Stop()
        {
            var lastPosition = Position;
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                    _mediaPlayer.Stop();
                _mediaPlayer.Reset();
            }
            _audioManager.AbandonAudioFocus(this);
            UpdatePlaybackState(PlaybackStateCompat.StateStopped);
            NotificationManagerCompat.From(ApplicationContext).CancelAll();
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
            Show.PlayerPosition = lastPosition;
        }

        public void PlayNext()
        {
            StartNotification(false);
            _mediaPlayer.Stop();
            Show.PlayerPosition = 0;
            Show = _nextShow;
            Play();
        }

        public void PlayPrevious()
        {
            StartNotification(false);
            _mediaPlayer.Stop();
            Show.PlayerPosition = 0;
            Show = _previousShow;
            Play();
        }

        private void UpdatePlaybackState(int state)
        {
            if (_mediaSessionCompat == null || _mediaPlayer == null)
                return;
            try
            {
                var stateBuilder = new PlaybackStateCompat.Builder()
                    .SetActions(
                        PlaybackStateCompat.ActionPause |
                        PlaybackStateCompat.ActionPlay |
                        PlaybackStateCompat.ActionPlayPause |
                        PlaybackStateCompat.ActionSkipToNext |
                        PlaybackStateCompat.ActionSkipToPrevious |
                        PlaybackStateCompat.ActionStop
                    )
                    .SetState(state, Position, 1.0f, SystemClock.ElapsedRealtime());
                _mediaSessionCompat.SetPlaybackState(stateBuilder.Build());
                //used for backwards compatibility
                if ((int)Build.VERSION.SdkInt < 21)
                {
                    if (_mediaSessionCompat.RemoteControlClient != null && _mediaSessionCompat.RemoteControlClient.Equals(typeof(RemoteControlClient)))
                    {
                        var remoteControlClient = (RemoteControlClient)_mediaSessionCompat.RemoteControlClient;
                        const RemoteControlFlags flags = RemoteControlFlags.Play
                                                         | RemoteControlFlags.Pause
                                                         | RemoteControlFlags.PlayPause
                                                         | RemoteControlFlags.Previous
                                                         | RemoteControlFlags.Next
                                                         | RemoteControlFlags.Stop;
                        remoteControlClient.SetTransportControlFlags(flags);
                    }
                }
                OnStatusChanged();
                if (state == PlaybackStateCompat.StatePlaying || state == PlaybackStateCompat.StatePaused)
                    StartNotification(true);
            }
            catch
            {
                // ignored
            }
        }

        private void StartNotification(bool full)
        {
            if (_mediaSessionCompat == null || Show == null)
                return;
            var showIntent = new Intent(ApplicationContext, typeof(ShowActivity));
            showIntent.PutExtra("ID", Show.ShowId.ToString());
            var pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0, showIntent, PendingIntentFlags.UpdateCurrent);
            _builder = new NotificationCompat.Builder(ApplicationContext)
                .SetContentTitle(Show.ShowTitle)
                .SetContentText(Show.ShowDateTime.ToString("f"))
                .SetContentInfo("Эхо Москвы")
                .SetSmallIcon(Resource.Drawable.icon_white)
                .SetContentIntent(pendingIntent)
                .SetShowWhen(false)
                .SetOngoing(MediaPlayerState == PlaybackStateCompat.StatePlaying)
                .SetVisibility(NotificationCompat.VisibilityPublic);
            if (full)
            {
                _builder.SetProgress(Duration, 0, false);
                if ((int) Build.VERSION.SdkInt > 19)
                {
                    _builder.SetLargeIcon(Cover);
                    _builder.AddAction(GenerateActionCompat(Resource.Drawable.ic_skip_previous_black_48dp,
                        ActionPrevious));
                    _builder.AddAction(MediaPlayerState == PlaybackStateCompat.StatePlaying
                        ? GenerateActionCompat(Resource.Drawable.ic_pause_circle_outline_black_48dp, ActionPause)
                        : GenerateActionCompat(Resource.Drawable.ic_play_circle_outline_black_48dp, ActionPlay));
                    _builder.AddAction(GenerateActionCompat(Resource.Drawable.ic_skip_next_black_48dp, ActionNext));
                }
                else
                {
                    _builder.SetLargeIcon(_compatCover);
                    _builder.AddAction(GenerateActionCompat(Resource.Drawable.ic_skip_previous_white_48dp,
                        ActionPrevious));
                    _builder.AddAction(MediaPlayerState == PlaybackStateCompat.StatePlaying
                        ? GenerateActionCompat(Resource.Drawable.ic_pause_circle_outline_white_48dp, ActionPause)
                        : GenerateActionCompat(Resource.Drawable.ic_play_circle_outline_white_48dp, ActionPlay));
                    _builder.AddAction(GenerateActionCompat(Resource.Drawable.ic_skip_next_white_48dp, ActionNext));
                }
            }
            NotificationManagerCompat.From(ApplicationContext).Notify(NotificationId, _builder.Build());
        }

        private NotificationCompat.Action GenerateActionCompat(int icon, string intentAction)
        {
            var serviceIntent = new Intent(ApplicationContext, typeof(EchoPlayerService));
            serviceIntent.SetAction(intentAction);
            var flags = PendingIntentFlags.UpdateCurrent;
            if (intentAction.Equals(ActionStop))
                flags = PendingIntentFlags.CancelCurrent;
            var pendingIntent = PendingIntent.GetService(ApplicationContext, 1, serviceIntent, flags);
            return new NotificationCompat.Action.Builder(icon, string.Empty, pendingIntent).Build();
        }

        //update the metadata on the lock screen
        private void UpdateMediaMetadataCompat()
        {
            if (_mediaSessionCompat == null)
                return;
            var metaBuilder = new MediaMetadataCompat.Builder();
            metaBuilder
                .PutString(MediaMetadata.MetadataKeyAlbum, "Эхо Москвы")
                .PutString(MediaMetadata.MetadataKeyArtist, Show.ShowTitle)
                .PutString(MediaMetadata.MetadataKeyTitle, Show.ShowDateTime.ToString("f"));
            metaBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, (int) Build.VERSION.SdkInt > 19 ? Cover : _compatCover);
            _mediaSessionCompat.SetMetadata(metaBuilder.Build());
        }

        //this will release the wifi lock if it is no longer needed
        private void ReleaseWifiLock()
        {
            if (_wifiLock == null)
                return;
            _wifiLock.Release();
            _wifiLock = null;
        }

        private void UnregisterMediaSessionCompat()
        {
            try
            {
                if (_mediaSessionCompat == null)
                    return;
                _mediaSessionCompat.Dispose();
                _mediaSessionCompat = null;
            }
            catch
            {
                // ignored
            }
        }
    }
}