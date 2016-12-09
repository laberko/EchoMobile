using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Echo.Person;
using Echo.Player;
using XamarinBindings.MaterialProgressBar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Show
{
    //activity to open a full show item
    [Activity(Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
    public class ShowActivity : AppCompatActivity, SeekBar.IOnSeekBarChangeListener
    {
        private SeekBar _seekBar;
        private ImageButton _playButton;
        private ShowItem _show;
        private MaterialProgressBar _progressBar;
        private WebView _textWebView;
        private bool _playerReady;
        private bool _personsReady;
        public EchoPlayerServiceBinder Binder;
        private EchoPlayerServiceConnection _mediaPlayerServiceConnection;
        private Intent _mediaPlayerServiceIntent;
        private EchoPlayerService _echoPlayerService;
        private string _searchPersonName;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Guid showId;
            string showText;

            //no collection of daily shows contents
            if (Common.ShowContentList == null)
            {
                Finish();
                return;
            }

            SetContentView(Resource.Layout.ShowItemView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.ParseColor(Common.ColorPrimary[2]));
            SetSupportActionBar(toolbar);

            if ((int) Build.VERSION.SdkInt > 19)
            {
                Window.SetNavigationBarColor(Color.ParseColor(Common.ColorPrimaryDark[2]));
                Window.SetStatusBarColor(Color.ParseColor(Common.ColorPrimaryDark[2]));
            }

            _progressBar = FindViewById<MaterialProgressBar>(Resource.Id.showsProgress);
            _progressBar.Visibility = ViewStates.Visible;
            _progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(Common.ColorPrimary[2]), PorterDuff.Mode.SrcIn);
            _seekBar = FindViewById<SeekBar>(Resource.Id.seekBar);
            _seekBar.SetOnSeekBarChangeListener(this);
            _playButton = FindViewById<ImageButton>(Resource.Id.showPlay);

            //was activity called from a person's history list?
            _searchPersonName = Intent.GetStringExtra("PersonName");
            if (!string.IsNullOrEmpty(_searchPersonName))
            {
                var person = Common.PersonList.FirstOrDefault(p => (p.PersonType == Common.PersonType.Show && p.PersonName == _searchPersonName));
                if (person != null && Guid.TryParse(Intent.GetStringExtra("ID"), out showId))
                    _show = person.PersonContent.FirstOrDefault(s => s.ItemId == showId) as ShowItem;
            }
            else if (Guid.TryParse(Intent.GetStringExtra("ID"), out showId))
            {
                //was activity called from search for the shows with a certain name?
                if (Intent.GetBooleanExtra("ShowSearch", false))
                    _show = Common.ShowHistoryList.FirstOrDefault(s => s.ItemId == showId) as ShowItem;
                else
                {
                    //activity was called from MainActivity
                    foreach (var showContent in Common.ShowContentList)
                    {
                        _show = showContent.ContentList.FirstOrDefault(s => (s.ItemType == Common.ContentType.Show && s.ItemId == showId)) as ShowItem;
                        if (_show != null)
                            break;
                    }
                }
            }
            
            if (_show == null)
            {
                //show was not found anywhere
                _progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }

            SupportActionBar.Title = _show.ItemTitle;
            SupportActionBar.Subtitle = _show.ItemDate.ToString(_show.ItemDate.Date != DateTime.Now.Date ? "dddd d MMMM yyyy, HH:mm" : "HH:mm");

            //get (sub)title and text
            try
            {
                showText = await _show.GetHtml();
            }
            catch
            {
                _progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            if (!string.IsNullOrEmpty(showText))
            {
                _textWebView = FindViewById<WebView>(Resource.Id.showText);
                _textWebView.SetBackgroundColor(Color.Transparent);
                _textWebView.Settings.JavaScriptEnabled = true;
                _textWebView.Settings.StandardFontFamily = "serif";
                _textWebView.LoadDataWithBaseURL("", showText, "text/html", "UTF-8", "");
                _textWebView.Settings.DefaultFontSize = Common.FontSize;
            }

            var subTitleTextView = FindViewById<TextView>(Resource.Id.showSubTitle);
            subTitleTextView.Text = string.IsNullOrEmpty(_show.ItemSubTitle) ? _show.ItemTitle : _show.ItemSubTitle;
            subTitleTextView.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize + 4);
            subTitleTextView.SetTextColor(Color.Black);

            if (!string.IsNullOrEmpty(_show.ItemRootUrl))
            {
                var historyButton = FindViewById<ImageButton>(Resource.Id.showHistoryButton);
                historyButton.Visibility = ViewStates.Visible;
                historyButton.Click += OnHistoryButtonClick;
            }

            //prepare media player with controls if the show has audio
            if (!string.IsNullOrEmpty(_show.ItemSoundUrl))
            {
                InitilizeMedia();
                _playButton.Click += OnPlayButtonClick;
                _seekBar.Progress = _show.ShowPlayerPosition;
            }
            else
            {
                _playerReady = true;
                if (_personsReady)
                    _progressBar.Visibility = ViewStates.Gone;
            }

            PopulatePeopleList(_show.ShowModerators);
            PopulatePeopleList(_show.ShowGuests);
        }

        private void OnPlayButtonClick(object sender, EventArgs e)
        {
            _playButton.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black));
            //now playing show is different - save its position and replace with current
            if (Common.EchoPlayer.DataSource != _show.ItemSoundUrl)
            {
                if (PlayerService.Show != null)
                    PlayerService.Show.ShowPlayerPosition = PlayerService.Position;
                PlayerService.Show = _show;
                //activity was started from a person's show search result list - pass search person name to the player service
                PlayerService.SearchPersonName = !string.IsNullOrEmpty(_searchPersonName) ? _searchPersonName : string.Empty;
            }
            PlayerService.PlayPause();
        }

        private void OnHistoryButtonClick(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(ShowHistoryActivity));
            intent.PutExtra("ShowUrl", _show.ItemRootUrl);
            intent.PutExtra("ShowName", _show.ItemTitle);
            StartActivity(intent);
            Finish();
        }

        private EchoPlayerService PlayerService
        {
            get
            {
                if (Binder == null || _mediaPlayerServiceConnection == null)
                    InitilizeMedia();
                return _echoPlayerService ?? (_echoPlayerService = Binder?.GetMediaPlayerService());
            }
        }

        //activity started with different intent
        protected override void OnNewIntent(Intent intent)
        {
            Finish();
            StartActivity(intent);
        }

        public void OnPlaying(object sender, EventArgs e)
        {
            if (Common.EchoPlayer.DataSource != _show.ItemSoundUrl)
                return;
            //update seekbar if this show is currently playing
            _seekBar.Max = PlayerService.Duration;
            _seekBar.Progress = PlayerService.Position;
        }

        public void OnBuffering(object sender, EventArgs e)
        {
            if (_show == null || Common.EchoPlayer == null || Common.EchoPlayer.DataSource != _show.ItemSoundUrl)
                return;
            _seekBar.SecondaryProgress = PlayerService.Buffered;
        }

        private void OnPlaybackStarted(object sender, string url)
        {
            if (_show != null && url == _show.ItemSoundUrl)
                RunOnUiThread(() => _playButton.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black)));
        }

        private void OnPlaybackPaused(object sender, string url)
        {
            if (_show != null && (url == _show.ItemSoundUrl || url == null))
                RunOnUiThread(() => _playButton.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.play_black)));
        }

        private void InitilizeMedia()
        {
            _mediaPlayerServiceIntent = new Intent(ApplicationContext, typeof(EchoPlayerService));
            //_mediaPlayerServiceConnection invokes ServiceConnected()
            _mediaPlayerServiceConnection = new EchoPlayerServiceConnection(this);
            StartService(_mediaPlayerServiceIntent);
            BindService(_mediaPlayerServiceIntent, _mediaPlayerServiceConnection, Bind.AutoCreate);
        }

        public void ServiceConnected()
        {
            var playLayout = FindViewById<TableLayout>(Resource.Id.playLayout);
            try
            {
                Common.EchoPlayer.PlaybackStarted += OnPlaybackStarted;
                Common.EchoPlayer.PlaybackPaused += OnPlaybackPaused;
                if (Intent.GetBooleanExtra("Play", false))
                    OnPlayButtonClick(this, EventArgs.Empty);
                else if (Common.EchoPlayer.IsPlaying && Common.EchoPlayer.DataSource == _show.ItemSoundUrl)
                    _playButton.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black));
                else
                    _playButton.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.play_black));
                //show player layout (hidden by default) and hide progress bar
                _playerReady = true;
                if (_personsReady)
                    _progressBar.Visibility = ViewStates.Gone;
                //set the seek bar parameters if the show playback was already started
                if (_show.ShowDuration != 0)
                {
                    _seekBar.Max = _show.ShowDuration;
                    _seekBar.Progress = _show.ShowPlayerPosition;
                }
                playLayout.Visibility = ViewStates.Visible;
            }
            catch
            {
                Finish();
            }
        }

        //populate menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.item_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //main menu item selected (close button)
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.back)
                OnBackPressed();
            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            Cleanup();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cleanup();
        }

        private void Cleanup()
        {
            if (_show != null && string.IsNullOrEmpty(_show.ItemSoundUrl))
                return;
            try
            {
                if (PlayerService != null)
                {
                    PlayerService.Playing -= OnPlaying;
                    PlayerService.Buffering -= OnBuffering;
                }
                Common.EchoPlayer.PlaybackStarted -= OnPlaybackStarted;
                Common.EchoPlayer.PlaybackPaused -= OnPlaybackPaused;
            }
            catch (Exception)
            {
                Finish();
            }
        }

        //fill the people table with the show moderators and guests
        private async void PopulatePeopleList(IEnumerable<PersonItem> list)
        {
            var peopleTable = FindViewById<TableLayout>(Resource.Id.people);
            foreach (var person in list)
            {
                if (person == null)
                    continue;
                var span = false;
                Bitmap photo;
                try
                {
                    photo = await person.GetPersonPhoto(Common.DisplayWidth/5);
                }
                catch
                {
                    photo = null;
                }
                //create table row for every person
                var row = new TableRow(this);
                if (photo != null)
                {
                    var photoCell = new ImageButton(this);
                    photoCell.SetImageBitmap(photo);
                    photoCell.SetBackgroundColor(Color.Transparent);
                    photoCell.Click += delegate
                    {
                        //open the show history for the selected person 
                        var intent = new Intent(this, typeof(ShowHistoryActivity));
                        intent.PutExtra("PersonUrl", person.PersonUrl);
                        intent.PutExtra("PersonName", person.PersonName);
                        StartActivity(intent);
                        Finish();
                    };
                    row.AddView(photoCell);
                }
                else
                    span = true;
                var nameCell = new TextView(this)
                {
                    Text = person.PersonName
                };
                nameCell.SetWidth(0);
                nameCell.SetBackgroundColor(Color.Transparent);
                nameCell.SetTextColor(Color.Black);
                if (!string.IsNullOrEmpty(person.PersonAbout))
                    nameCell.Text += (",\n" + person.PersonAbout);
                nameCell.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
                row.AddView(nameCell);
                var nameCellParams = (TableRow.LayoutParams)nameCell.LayoutParameters;
                nameCellParams.SetMargins(8, 0, 0, 0);
                nameCellParams.Gravity = GravityFlags.CenterVertical;
                if (span)
                    nameCellParams.Span = 2;
                row.LayoutParameters = nameCellParams;
                peopleTable.AddView(row);
            }
            _personsReady = true;
            if (_playerReady)
                _progressBar.Visibility = ViewStates.Gone;
        }

        //seek bar progress changed by user
        public async void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
            if (!fromUser)
                return;
            if (Common.EchoPlayer.DataSource == _show.ItemSoundUrl)
                await PlayerService.Seek(progress);
            else
                seekBar.Progress = 0;
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {
        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {
        }
    }
}