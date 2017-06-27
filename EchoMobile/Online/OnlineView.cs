using System;
using System.Linq;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Support.V4.Content;
using Android.Telephony;
using Android.Util;
using Android.Views;
using Android.Widget;
using Echo.Player;
using Echo.Show;
using Plugin.Connectivity;

namespace Echo.Online
{
    public sealed class OnlineView : IPlayerInitiator, IDisposable
    {
        private readonly Context _context;
        private readonly ImageButton _playRadioButton;
        private readonly LinearLayout _onlineLayout;
        private Intent _mediaPlayerServiceIntent;
        private EchoPlayerServiceConnection _mediaPlayerServiceConnection;
        private EchoPlayerServiceBinder _binder;
        private EchoPlayerService _echoPlayerService;
        private readonly ShowItem _show;
        private static EditText _smsText;
        private readonly SmsSentReceiver _smsSentReceiver;
        private readonly SmsDeliveredReceiver _smsDeliveredReceiver;
        private bool _disposedValue;
        private readonly string[] _appPermissions = {
                Manifest.Permission.SendSms,
                Manifest.Permission.CallPhone
        };

        EchoPlayerServiceBinder IPlayerInitiator.Binder
        {
            get
            {
                return _binder;
            }

            set
            {
                _binder = value;
            }
        }

        private EchoPlayerService PlayerService
        {
            get
            {
                if (_binder == null || _mediaPlayerServiceConnection == null)
                    InitilizeMedia();
                return _echoPlayerService ?? (_echoPlayerService = _binder?.GetMediaPlayerService());
            }
        }

        public OnlineView(View view, Context context)
        {
            _context = context;

            if (MainActivity.EchoPlayer == null)
                MainActivity.EchoPlayer = new EchoMediaPlayer();

            var playRadioText = view.FindViewById<EchoTextView>(Resource.Id.playRadioText);
            playRadioText.Setup(_context.Resources.GetString(Resource.String.listen_radio), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize + 4);
            _playRadioButton = view.FindViewById<ImageButton>(Resource.Id.playRadioButton);
            SetPlayButtonDrawable();
            //dummy online show to pass into Player
            _show = new ShowItem(MainActivity.ContentType.Show)
            {
                ItemSoundUrl = MainActivity.OnlineRadioUrl,
                ItemTitle = _context.Resources.GetString(Resource.String.app_name),
                ItemDate = DateTime.Now
            };
            InitilizeMedia();
            _playRadioButton.Click += OnPlayButtonClick;

            _onlineLayout = view.FindViewById<LinearLayout>(Resource.Id.onlineLayout);

            var voteText = view.FindViewById<EchoTextView>(Resource.Id.voteText);
            voteText.Setup(_context.Resources.GetString(Resource.String.vote), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize + 4);
            var voteButton1 = view.FindViewById<Button>(Resource.Id.buttonVote1);
            voteButton1.SetTextSize(ComplexUnitType.Sp, MainActivity.FontSize + 4);
            voteButton1.SetMinWidth(MainActivity.DisplayWidth / 3);
            voteButton1.Text = "660-06-64";
            voteButton1.Click += delegate
            {
                MakeCall(MainActivity.VoteNumber1);
            };
            var voteButton2 = view.FindViewById<Button>(Resource.Id.buttonVote2);
            voteButton2.SetTextSize(ComplexUnitType.Sp, MainActivity.FontSize + 4);
            voteButton2.SetMinWidth(MainActivity.DisplayWidth / 3);
            voteButton2.Text = "660-06-65";
            voteButton2.Click += delegate
            {
                MakeCall(MainActivity.VoteNumber2);
            };

            var callText = view.FindViewById<EchoTextView>(Resource.Id.callText);
            callText.Setup(_context.Resources.GetString(Resource.String.call), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize + 4);
            var callButton = view.FindViewById<ImageButton>(Resource.Id.callButton);
            callButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                ? ContextCompat.GetDrawable(_context, Resource.Drawable.ic_phone_in_talk_black_48dp)
                : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_phone_in_talk_white_48dp));
            callButton.Click += delegate
            {
                MakeCall(MainActivity.CallNumber);
            };

            var sendSmsText = view.FindViewById<EchoTextView>(Resource.Id.sendSmsText);
            sendSmsText.Setup(_context.Resources.GetString(Resource.String.send_sms), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize + 4);
            var sendSmsButton = view.FindViewById<ImageButton>(Resource.Id.sendSmsButton);
            sendSmsButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                ? ContextCompat.GetDrawable(_context, Resource.Drawable.ic_send_black_48dp)
                : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_send_white_48dp));
            sendSmsButton.Click += OnSendSmsButtonClick;
            _smsText = view.FindViewById<EditText>(Resource.Id.smsText);
            _smsText.LayoutParameters.Height = MainActivity.DisplayWidth / 4;
            _smsSentReceiver = new SmsSentReceiver();
            _smsDeliveredReceiver = new SmsDeliveredReceiver();
            _context.RegisterReceiver(_smsSentReceiver, new IntentFilter("SMS_SENT"));
            _context.RegisterReceiver(_smsDeliveredReceiver, new IntentFilter("SMS_DELIVERED"));

            var callWarningText = view.FindViewById<EchoTextView>(Resource.Id.callWarningText);
            callWarningText.Setup(_context.Resources.GetString(Resource.String.call_warning), MainActivity.MainTextColor, TypefaceStyle.Italic, MainActivity.FontSize);
        }

        private async void OnPlayButtonClick(object sender, EventArgs e)
        {
            try
            {
                if (MainActivity.EchoPlayer == null)
                    InitilizeMedia();
                if (PlayerService == null || await CrossConnectivity.Current.IsRemoteReachable("echo.msk.ru"))
                    return;
                _playRadioButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                    ? ContextCompat.GetDrawable(_context, Resource.Drawable.pause_black)
                    : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_pause_circle_outline_white_48dp));
                //now playing show is different - save its position and replace with current
                if (MainActivity.EchoPlayer != null && MainActivity.EchoPlayer.DataSource != MainActivity.OnlineRadioUrl)
                {
                    if (PlayerService.Show != null)
                        PlayerService.Show.ShowPlayerPosition = PlayerService.Position;
                    PlayerService.Show = _show;
                    PlayerService.SearchPersonName = string.Empty;
                }
                PlayerService.PlayPause();
            }
            catch
            {
                // ignored
            }
        }

        private void InitilizeMedia()
        {
            if (_mediaPlayerServiceIntent == null)
                _mediaPlayerServiceIntent = new Intent(_context.ApplicationContext, typeof(EchoPlayerService));
            //_mediaPlayerServiceConnection invokes ServiceConnected()
            _mediaPlayerServiceConnection = new EchoPlayerServiceConnection(this);
            _context.StartService(_mediaPlayerServiceIntent);
            _context.BindService(_mediaPlayerServiceIntent, _mediaPlayerServiceConnection, Bind.AutoCreate);
        }

        public void ServiceConnected()
        {
            if (MainActivity.EchoPlayer == null)
                return;
            MainActivity.EchoPlayer.PlaybackStarted += OnPlaybackStarted;
            MainActivity.EchoPlayer.PlaybackPaused += OnPlaybackPaused;
            SetPlayButtonDrawable();
        }

        public void OnPlaying(object sender, EventArgs e)
        {
        }

        public void OnBuffering(object sender, EventArgs e)
        {
        }

        private void OnPlaybackStarted(object sender, string url)
        {
            if (url == MainActivity.OnlineRadioUrl)
                _playRadioButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(_context, Resource.Drawable.pause_black)
                        : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_pause_circle_outline_white_48dp));
        }

        private void OnPlaybackPaused(object sender, string url)
        {
            if (url == MainActivity.OnlineRadioUrl || url == null)
                _playRadioButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(_context, Resource.Drawable.play_black)
                        : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_play_circle_outline_white_48dp));
        }

        private void OnSendSmsButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_smsText.Text))
                Toast.MakeText(_context, _context.Resources.GetString(Resource.String.enter_text), ToastLength.Long).Show();
            else
            {
                var intentSent = PendingIntent.GetBroadcast(_context, 0, new Intent("SMS_SENT"), 0);
                var intentDelivered = PendingIntent.GetBroadcast(_context, 0, new Intent("SMS_DELIVERED"), 0);
                //SmsManager.Default.SendTextMessage(MainActivity.SmsNumber, null, _smsText.Text + " " + _username, intentSent, intentDelivered);
                SmsManager.Default.SendTextMessage(MainActivity.SmsNumber, null, _smsText.Text, intentSent, intentDelivered);
            }
        }

        private void SetPlayButtonDrawable()
        {
            if (MainActivity.EchoPlayer.IsPlaying && MainActivity.EchoPlayer.DataSource == MainActivity.OnlineRadioUrl)
                _playRadioButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                    ? ContextCompat.GetDrawable(_context, Resource.Drawable.pause_black)
                    : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_pause_circle_outline_white_48dp));
            else
                _playRadioButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                    ? ContextCompat.GetDrawable(_context, Resource.Drawable.play_black)
                    : ContextCompat.GetDrawable(_context, Resource.Drawable.ic_play_circle_outline_white_48dp));
        }

        //chech if all required permissions satisfied
        public void OnlineEnable()
        {
            var unsatisfiedPermissions = _appPermissions.Count(perm => ContextCompat.CheckSelfPermission(_context, perm) != Permission.Granted);
            if (unsatisfiedPermissions > 0 || _onlineLayout == null)
                return;
            _onlineLayout.Visibility = ViewStates.Visible;
        }

        private void MakeCall(string number)
        {
            var intent = new Intent(Intent.ActionCall, Android.Net.Uri.Parse(number));
            _context.StartActivity(intent);
        }

        [BroadcastReceiver(Exported = true)]
        private class SmsSentReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if ((int) ResultCode == (int) Result.Ok)
                {
                    Toast.MakeText(Application.Context, Application.Context.Resources.GetString(Resource.String.send_ok), ToastLength.Long).Show();
                    _smsText.Text = string.Empty;
                }
                else
                    Toast.MakeText(Application.Context, Application.Context.Resources.GetString(Resource.String.send_error), ToastLength.Long).Show();
            }
        }

        [BroadcastReceiver(Exported = true)]
        private class SmsDeliveredReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if ((int)ResultCode != (int)Result.Ok)
                    Toast.MakeText(Application.Context, Application.Context.Resources.GetString(Resource.String.send_error), ToastLength.Long).Show();
            }
        }

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;
            if (disposing)
            {
                if (MainActivity.EchoPlayer != null)
                {
                    MainActivity.EchoPlayer.PlaybackStarted -= OnPlaybackStarted;
                    MainActivity.EchoPlayer.PlaybackPaused -= OnPlaybackPaused;
                }
                if (_mediaPlayerServiceConnection != null)
                    _context.UnbindService(_mediaPlayerServiceConnection);
                if (_smsDeliveredReceiver != null)
                    _context.UnregisterReceiver(_smsDeliveredReceiver);
                if (_smsSentReceiver != null)
                    _context.UnregisterReceiver(_smsSentReceiver);
            }
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}