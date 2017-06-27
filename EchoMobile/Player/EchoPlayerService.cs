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
using Android.Widget;
using Echo.Show;
using Java.Lang;
using System.Collections.Generic;
using Android.Media.Session;

namespace Echo.Player
{
    public delegate void StatusChangedEventHandler(object sender, EventArgs e);
    public delegate void BufferingEventHandler(object sender, EventArgs e);
    public delegate void PlayingEventHandler(object sender, EventArgs e);

    [Service]
    public class EchoPlayerService : Service,
        AudioManager.IOnAudioFocusChangeListener,
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
        private MediaSession _mediaSessionCompat;
        private Android.Media.Session.MediaController _mediaControllerCompat;
        private EchoMediaReceiver _mediaReceiver;
        private WifiManager _wifiManager;
        private WifiManager.WifiLock _wifiLock;
        //private ComponentName _remoteComponentName;
        private const int NotificationId = 1;
        public event StatusChangedEventHandler StatusChanged;
        public event PlayingEventHandler Playing;
        public event BufferingEventHandler Buffering;
        private readonly Handler _playingHandler;
        private readonly Runnable _playingHandlerRunnable;
        private IBinder _binder;
        private int _buffered;
        private Bitmap _cover;
        //private Bitmap _compatCover;
        private Notification.Builder _builder;
        private PlaybackState.Builder _stateBuilder;
        private bool _isFocusLost;
        public string SearchPersonName;
        private readonly Dictionary<string, string> _headers;
        private NotificationManager _notificationManager;
        private Notification.Style _notificationStyle;
        private Intent _showIntent;
        private Intent _mainIntent;
        private TimeSpan _span;
        //private readonly int _sdkVersion;

        public EchoPlayerService()
        {
            if (MainActivity.EchoPlayer == null)
                MainActivity.EchoPlayer = new EchoMediaPlayer();
            _mediaPlayer = MainActivity.EchoPlayer;

            //create an instance for a runnable-handler
            _playingHandler = new Handler();

            //create a runnable, restarting itself if the status still is "playing" every second
            _playingHandlerRunnable = new Runnable(() => {
                OnPlaying();
                if (MediaPlayerState == PlaybackStateCode.Playing)
                    _playingHandler.PostDelayed(_playingHandlerRunnable, 1000);
            });

            //on Status changed to PLAYING, start raising the Playing event
            StatusChanged += delegate
            {
                if (MediaPlayerState == PlaybackStateCode.Playing)
                    _playingHandler.PostDelayed(_playingHandlerRunnable, 0);
            };

            _headers = new Dictionary<string, string>
            {
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2"
                }
            };

            //_sdkVersion = (int) Build.VERSION.SdkInt;

            //_notificationManager = (NotificationManager)ApplicationContext.GetSystemService(NotificationService);
            //_notificationStyle = new Notification.MediaStyle();
        }

        private PlaybackStateCode MediaPlayerState
        {
            get
            {
                try
                {
                    return _mediaControllerCompat?.PlaybackState?.State ?? PlaybackStateCode.None;
                }
                catch
                {
                    return PlaybackStateCompat.StateNone;
                }
            }
        }

        private bool IsOnline
        {
            get
            {
                if (Show != null)
                    return Show.ItemSoundUrl == MainActivity.OnlineRadioUrl;
                return true;
            }
        }

        public int Position
        {
            get
            {
                if (_mediaPlayer == null || MediaPlayerState == PlaybackStateCode.Stopped)
                    return 0;
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
                    || (MediaPlayerState != PlaybackStateCode.Playing
                        && MediaPlayerState != PlaybackStateCode.Paused))
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

        private Bitmap Cover
        {
            get
            {
                return _cover ?? BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon);
            }
            set
            {
                _cover = value;
                UpdateMediaMetadataCompat();
            }
        }

        public override void OnCreate()
        {
            base.OnCreate();
            //_compatCover = BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon_white);
            //find audio and notificaton managers
            _audioManager = (AudioManager)GetSystemService(AudioService);
            _wifiManager = (WifiManager)GetSystemService(WifiService);
            _mediaReceiver = new EchoMediaReceiver();
            RegisterReceiver(_mediaReceiver, new IntentFilter(Intent.ActionMediaButton));
            RegisterReceiver(_mediaReceiver, new IntentFilter(Intent.ActionHeadsetPlug));
            RegisterReceiver(_mediaReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            //_remoteComponentName = new ComponentName(PackageName, _mediaReceiver.ComponentName);
            _stateBuilder = new PlaybackState.Builder().SetActions(
                PlaybackState.ActionPause |
                PlaybackState.ActionPlay |
                PlaybackState.ActionPlayPause |
                PlaybackState.ActionSkipToNext |
                PlaybackState.ActionSkipToPrevious |
                PlaybackState.ActionStop);
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
                else if (action.Equals(ActionTogglePlayback))
                {
                    if (MediaPlayerState == PlaybackStateCode.Playing)
                        _mediaControllerCompat.GetTransportControls().Pause();
                    if (MediaPlayerState == PlaybackStateCode.Paused)
                        _mediaControllerCompat.GetTransportControls().Play();
                }
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

        public override bool OnUnbind(Intent intent)
        {
            _mediaSessionCompat?.Release();
            return base.OnUnbind(intent);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Reset();
                _mediaPlayer.Release();
                _mediaPlayer = null;
                MainActivity.EchoPlayer = null;
            }
            _notificationManager?.CancelAll();
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
        }

        private void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlaying()
        {
            if (IsOnline)
                return;
            try
            {
                AddPositionToNotification();
                if (_mediaPlayer.IsPlaying)
                    _notificationManager?.Notify(NotificationId, _builder.Build());
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
            if (MediaPlayerState == PlaybackStateCode.Playing || MediaPlayerState == PlaybackStateCode.Paused)
                duration = mediaPlayer.Duration;
            var newBufferedTime = duration * percent / 100;
            Buffered = newBufferedTime;
        }

        public void OnCompletion(MediaPlayer mediaPlayer)
        {
            Show.ShowPlayerPosition = 0;
            //PlayNext();
            Stop();
        }

        public bool OnError(MediaPlayer mediaPlayer, MediaError what, int extra)
        {
            UpdatePlaybackState(PlaybackStateCode.Error);
            Stop();
            return true;
        }

        public void OnSeekComplete(MediaPlayer mediaPlayer)
        {
            //http://stackoverflow.com/questions/40605675/android-mediaplayer-info-warning-703-0-info-warning-702-0-info-warning
            SystemClock.Sleep(200);
        }

        public async void OnPrepared(MediaPlayer mediaPlayer)
        {
            //prepare next and previous show instances
            if (MainActivity.PlayList == null)
            {
                MainActivity.PlayList = MainActivity.ShowContentList
                    .FirstOrDefault(c => c.ContentDate.Date == MainActivity.SelectedDates[2].Date)?
                    .ContentList.Where(s => !string.IsNullOrEmpty(s.ItemSoundUrl))
                    .OrderBy(s => s.ItemDate).Cast<ShowItem>().ToArray();
            }
            if (MainActivity.PlayList != null && MainActivity.PlayList.Length > 1)
            {
                var currentIndex = Array.IndexOf(MainActivity.PlayList, Show);
                _nextShow = currentIndex == MainActivity.PlayList.Length - 1 || currentIndex == -1
                    ? MainActivity.PlayList[0]
                    : MainActivity.PlayList[currentIndex + 1];
                _previousShow = currentIndex == 0 || currentIndex == -1
                    ? MainActivity.PlayList[MainActivity.PlayList.Length - 1]
                    : MainActivity.PlayList[currentIndex - 1];
            }

            Cover = await Show.GetPicture();

            //mediaplayer is prepared - start track playback
            if (!IsOnline)
                mediaPlayer.SeekTo(Show.ShowPlayerPosition);
            mediaPlayer.Start();
            UpdatePlaybackState(PlaybackStateCode.Playing);

            //update the metadata now that we are playing
            UpdateMediaMetadataCompat();
            StartNotification(true);

            if(IsOnline)
                SystemClock.Sleep(200);

            Show.ShowDuration = Duration;
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
                    showIntent.PutExtra("ID", Show.ItemId.ToString());
                    //var pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0, showIntent, 0);
                    if (_mediaReceiver == null)
                    {
                        _mediaReceiver = new EchoMediaReceiver();
                        RegisterReceiver(_mediaReceiver, new IntentFilter(Intent.ActionMediaButton));
                        RegisterReceiver(_mediaReceiver, new IntentFilter(Intent.ActionHeadsetPlug));
                        RegisterReceiver(_mediaReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
                    }
                    //_remoteComponentName = new ComponentName(PackageName, _mediaReceiver.ComponentName);
                    //_mediaSessionCompat = new MediaSessionCompat(ApplicationContext, "echomobile", _remoteComponentName, pendingIntent);
                    _mediaSessionCompat = new MediaSession(ApplicationContext, "echomobile");
                    _mediaControllerCompat = new Android.Media.Session.MediaController(ApplicationContext, _mediaSessionCompat.SessionToken);
                }
                _mediaSessionCompat.SetCallback(new EchoMediaSessionCallback((EchoPlayerServiceBinder)_binder));
                _mediaSessionCompat.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
                _mediaSessionCompat.Active = true;
            }
            catch
            {
                // ignored
            }
        }

        private void InitializePlayer()
        {
            if (MainActivity.EchoPlayer == null)
                MainActivity.EchoPlayer = new EchoMediaPlayer();
            _mediaPlayer = MainActivity.EchoPlayer;
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
                if (!IsOnline)
                    _mediaPlayer?.SeekTo(position);
            });
        }

        public async void Play()
        {
            if (_mediaSessionCompat == null)
                InitMediaSession();
            if (_mediaPlayer == null || Show.ItemSoundUrl != _mediaPlayer.DataSource)
                InitializePlayer();
            try
            {
                //track not changed
                if (_mediaPlayer != null && Show.ItemSoundUrl == _mediaPlayer.DataSource)
                {
                    //player is on pause, track not changed
                    if (MediaPlayerState == PlaybackStateCode.Paused)
                    {
                        //start again
                        OnPrepared(_mediaPlayer);
                        return;
                    }
                    //player is playing, track not changed
                    if (_mediaPlayer.IsPlaying)
                    {
                        if (Show.ItemSoundUrl == _mediaPlayer.DataSource)
                        {
                            //keep playing
                            UpdatePlaybackState(PlaybackStateCode.Playing);
                            return;
                        }
                    }
                }

                //different track
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                    UpdatePlaybackState(PlaybackStateCode.Stopped);
                }
                _mediaPlayer.Reset();
                _mediaPlayer.SetAudioStreamType(Stream.Music);
                if (!IsOnline)
                {
                    var path = await Show.GetRealSoundUrl();
                    await _mediaPlayer.SetDataSourceAsync(ApplicationContext, Android.Net.Uri.Parse(path), _headers);
                    _mediaPlayer.DataSource = path;
                }
                else
                {
                    await _mediaPlayer.SetDataSourceAsync(ApplicationContext, Android.Net.Uri.Parse(MainActivity.OnlineRadioUrl), _headers);
                    _mediaPlayer.DataSource = MainActivity.OnlineRadioUrl;
                }
                _mediaPlayer.ShowId = Show.ItemId;
                _mediaPlayer.PlayerService = this;
                _mediaPlayer.PrepareAsync();
                UpdatePlaybackState(PlaybackStateCode.Buffering);
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
            catch (System.Exception)
            {
                _mediaPlayer?.Pause();
                Toast.MakeText(this, Resources.GetString(Resource.String.playback_error), ToastLength.Long).Show();
                Stop();
            }
        }

        //toggle play-pause
        public void PlayPause()
        {
            if (Show == null)
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.playback_error), ToastLength.Long).Show();
                return;
            }
            if (_mediaPlayer == null
                || (_mediaPlayer != null && MediaPlayerState == PlaybackStateCode.Paused)
                || (_mediaPlayer != null && MediaPlayerState == PlaybackStateCode.Stopped)
                || (_mediaPlayer != null && Show.ItemSoundUrl != _mediaPlayer.DataSource))
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
                {
                    _mediaPlayer.Pause();
                    Show.ShowPlayerPosition = Position;
                }
                UpdatePlaybackState(PlaybackStateCode.Paused);
                StartNotification(true);
                StopForeground(false);
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
            _audioManager?.AbandonAudioFocus(this);
            UpdatePlaybackState(PlaybackStateCode.Stopped);
            _notificationManager?.CancelAll();
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
            if (Show != null)
                Show.ShowPlayerPosition = lastPosition;
        }

        public void PlayNext()
        {
            //if == 0 - called from OnCompletion
            if (Show.ShowPlayerPosition != 0)
                Show.ShowPlayerPosition = Position;
            StartNotification(false);
            if (_nextShow != null)
                Show = _nextShow;
            Play();
        }

        public void PlayPrevious()
        {
            Show.ShowPlayerPosition = Position;
            StartNotification(false);
            if (_previousShow != null)
                Show = _previousShow;
            Play();
        }

        private void UpdatePlaybackState(PlaybackStateCode state)
        {
            if (_mediaSessionCompat == null || _mediaPlayer == null)
                return;
            try
            {
                _stateBuilder.SetState(state, Position, 1.0f, SystemClock.ElapsedRealtime());
                _mediaSessionCompat.SetPlaybackState(_stateBuilder.Build());
                OnStatusChanged();
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
            _builder = new Notification.Builder(ApplicationContext);
            if (_notificationManager == null)
                _notificationManager = (NotificationManager)ApplicationContext.GetSystemService(NotificationService);
            if (_showIntent == null)
                _showIntent = new Intent(ApplicationContext, typeof(ShowActivity));
            if (_mainIntent == null)
                _mainIntent = new Intent(ApplicationContext, typeof(MainActivity));

            _builder
                .SetSmallIcon(Resource.Drawable.icon_white)
                .SetShowWhen(false)
                .SetOngoing(MediaPlayerState == PlaybackStateCode.Playing)
                .SetVisibility(NotificationVisibility.Public)
                .SetCategory("progress");

            if (!full)
            {
                _notificationStyle = new Notification.MediaStyle();
                _builder.SetStyle(_notificationStyle);
                _builder.SetContentTitle("Эхо Москвы");
                _builder.SetSubText("");
                _builder.SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon));
            }
            else
            {
                //playing recorded show from site
                if (!IsOnline)
                {
                    _showIntent.PutExtra("ID", Show.ItemId.ToString());
                    _showIntent.PutExtra("PersonName", SearchPersonName);

                    //the 1st string
                    _builder.SetContentTitle(Show.ItemTitle);
                    //the 2nd string
                    _builder.SetContentText(Show.ItemDate.ToString("f"));
                    //the 3rd string
                    AddPositionToNotification();

                    _builder.SetContentIntent(PendingIntent.GetActivity(ApplicationContext, 0, _showIntent, PendingIntentFlags.UpdateCurrent));
                    _builder.SetLargeIcon(Cover);
                    _builder.AddAction(GenerateActionCompat(Resource.Drawable.ic_skip_previous_black_48dp, ActionPrevious));
                    _builder.AddAction(MediaPlayerState == PlaybackStateCode.Playing
                        ? GenerateActionCompat(Resource.Drawable.ic_pause_black_48dp, ActionPause)
                        : GenerateActionCompat(Resource.Drawable.ic_play_arrow_black_48dp, ActionPlay));
                    _builder.AddAction(GenerateActionCompat(Resource.Drawable.ic_skip_next_black_48dp, ActionNext));
                    _notificationStyle = new Notification.MediaStyle().SetShowActionsInCompactView(0, 1, 2);
                    _builder.SetStyle(_notificationStyle);
                }
                //playing online stream
                else
                {
                    //the 1st string
                    _builder.SetContentTitle(Show.ItemTitle);
                    //the 2nd string
                    _builder.SetContentText(Show.ItemDate.ToString("D"));
                    //the 3rd string
                    _builder.SetSubText("Эфир онлайн");

                    _builder.SetContentIntent(PendingIntent.GetActivity(ApplicationContext, 0, _mainIntent, PendingIntentFlags.UpdateCurrent));
                    _builder.SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon));
                    _builder.AddAction(MediaPlayerState == PlaybackStateCode.Playing
                        ? GenerateActionCompat(Resource.Drawable.ic_pause_black_48dp, ActionPause)
                        : GenerateActionCompat(Resource.Drawable.ic_play_arrow_black_48dp, ActionPlay));
                    _notificationStyle = new Notification.MediaStyle().SetShowActionsInCompactView(0);
                    _builder.SetStyle(_notificationStyle);
                    Playing?.Invoke(this, EventArgs.Empty);
                }
            }



            MainActivity.EchoNotification = _builder.Build();
            
            _notificationManager.Notify(NotificationId, MainActivity.EchoNotification);
            //NotificationManagerCompat.From(ApplicationContext).Notify(NotificationId, MainActivity.EchoNotification);
            StartForeground(NotificationId, MainActivity.EchoNotification);
        }

        private Notification.Action GenerateActionCompat(int icon, string intentAction, string buttonText = "")
        {
            var serviceIntent = new Intent(ApplicationContext, typeof(EchoPlayerService));
            serviceIntent.SetAction(intentAction);
            var flags = PendingIntentFlags.UpdateCurrent;
            if (intentAction.Equals(ActionStop))
                flags = PendingIntentFlags.CancelCurrent;
            var pendingIntent = PendingIntent.GetService(ApplicationContext, 1, serviceIntent, flags);
            return new Notification.Action.Builder(icon, buttonText, pendingIntent).Build();
        }

        //update the metadata on the lock screen
        private void UpdateMediaMetadataCompat()
        {
            if (_mediaSessionCompat == null)
                return;
            var metaBuilder = new MediaMetadata.Builder();
            if (!IsOnline)
            {
                metaBuilder
                    .PutString(MediaMetadata.MetadataKeyAlbum, "Эхо Москвы")
                    .PutString(MediaMetadata.MetadataKeyArtist, Show.ItemTitle)
                    .PutString(MediaMetadata.MetadataKeyTitle, Show.ItemDate.ToString("f"));
                //metaBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, _sdkVersion > 19 ? Cover : _compatCover);
                metaBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, Cover);
            }
            else
            {
                metaBuilder
                    .PutString(MediaMetadata.MetadataKeyAlbum, "Эхо Москвы")
                    .PutString(MediaMetadata.MetadataKeyArtist, Show.ItemTitle);
                //metaBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, _compatCover);
                metaBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, Cover);
            }
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
                _mediaSessionCompat.Release();
                _mediaSessionCompat.Dispose();
                _mediaSessionCompat = null;
            }
            catch
            {
                // ignored
            }
        }

        private void AddPositionToNotification()
        {
            _span = TimeSpan.FromMilliseconds(Position);
            _builder.SetSubText(_span.Hours > 0
                ? $"{_span.Hours:0}:{_span.Minutes:00}:{_span.Seconds:00}"
                : $"{_span.Minutes:00}:{_span.Seconds:00}");
        }
    }
}