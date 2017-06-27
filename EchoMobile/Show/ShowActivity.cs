using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using Echo.Person;
using Echo.Player;
using Echo.ShowHistory;
using Plugin.Connectivity;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Show
{
    //activity to open a full show item
    [Activity(Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ResizeableActivity = true,
        LaunchMode = LaunchMode.SingleTop)]
    public class ShowActivity : AppCompatActivity, SeekBar.IOnSeekBarChangeListener, IPlayerInitiator
    {
        private SeekBar _seekBar;
        private ImageButton _playButton;
        private ShowItem _show;
        private ProgressBar _progressBar;
        private EchoWebView _textWebView;
        private bool _playerReady;
        private bool _personsReady;
        private EchoPlayerServiceBinder _binder;
        private EchoPlayerServiceConnection _mediaPlayerServiceConnection;
        private Intent _mediaPlayerServiceIntent;
        private EchoPlayerService _echoPlayerService;
        private string _searchPersonName;

        protected override async void OnCreate(Bundle bundle)
        {
            SetTheme(MainActivity.Theme);
            base.OnCreate(bundle);

            Guid showId;
            string showText;

            //no collection of daily shows contents
            if (MainActivity.ShowContentList == null)
            {
                Finish();
                return;
            }

            SetContentView(Resource.Layout.ShowItemView);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.ParseColor(MainActivity.ColorPrimary[2]));
            
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetNavigationBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[2]));
            Window.SetStatusBarColor(Color.ParseColor(MainActivity.PrimaryDarkColor[2]));

            _progressBar = FindViewById<ProgressBar>(Resource.Id.showsProgress);
            _progressBar.ScaleX = 1.5f;
            _progressBar.ScaleY = 1.5f;
            _progressBar.Visibility = ViewStates.Visible;
            _progressBar.IndeterminateDrawable.SetColorFilter(Color.ParseColor(MainActivity.ColorPrimary[2]), PorterDuff.Mode.SrcIn);
            _seekBar = FindViewById<SeekBar>(Resource.Id.seekBar);
            _seekBar.SetOnSeekBarChangeListener(this);
            _playButton = FindViewById<ImageButton>(Resource.Id.showPlay);

            //was activity called from a person's history list?
            _searchPersonName = Intent.GetStringExtra("PersonName");
            if (!string.IsNullOrEmpty(_searchPersonName))
            {
                var person = MainActivity.PersonList.FirstOrDefault(p => (p.PersonType == MainActivity.PersonType.Show && p.PersonName == _searchPersonName));
                if (person != null && Guid.TryParse(Intent.GetStringExtra("ID"), out showId))
                    _show = person.PersonContent.FirstOrDefault(s => s.ItemId == showId) as ShowItem;
            }
            else if (Guid.TryParse(Intent.GetStringExtra("ID"), out showId))
            {
                //was activity called from search for the shows with a certain name?
                if (Intent.GetBooleanExtra("ShowSearch", false))
                    _show = MainActivity.ShowHistoryList.FirstOrDefault(s => s.ItemId == showId) as ShowItem;
                else
                {
                    //activity was called from MainActivity
                    foreach (var showContent in MainActivity.ShowContentList)
                    {
                        _show = showContent.ContentList.FirstOrDefault(s => (s.ItemType == MainActivity.ContentType.Show && s.ItemId == showId)) as ShowItem;
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

            toolbar.Title = _show.ItemTitle;
            toolbar.Subtitle = _show.ItemDate.ToString(_show.ItemDate.Date != DateTime.Now.Date ? "dddd d MMMM yyyy, HH:mm" : "HH:mm");
            SetSupportActionBar(toolbar);

            if (!await CheckConnectivity())
                return;

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
                _textWebView = FindViewById<EchoWebView>(Resource.Id.showText);
                _textWebView.Setup(showText);
            }

            var subTitleTextView = FindViewById<EchoTextView>(Resource.Id.showSubTitle);
            subTitleTextView.Setup(string.IsNullOrEmpty(_show.ItemSubTitle) ? _show.ItemTitle : _show.ItemSubTitle,
                MainActivity.MainDarkTextColor, TypefaceStyle.Bold, MainActivity.FontSize + 4);

            if (!string.IsNullOrEmpty(_show.ItemRootUrl))
            {
                var historyButton = FindViewById<ImageButton>(Resource.Id.showHistoryButton);
                historyButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(this, Resource.Drawable.ic_search_black_48dp)
                        : ContextCompat.GetDrawable(this, Resource.Drawable.ic_search_white_48dp));
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

        private async void OnPlayButtonClick(object sender, EventArgs e)
        {
            try
            {
                if (PlayerService == null || !await CheckConnectivity())
                    return;
                _playButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                    ? ContextCompat.GetDrawable(this, Resource.Drawable.pause_black)
                    : ContextCompat.GetDrawable(this, Resource.Drawable.ic_pause_circle_outline_white_48dp));
                //now playing show is different - save its position and replace with current
                if (MainActivity.EchoPlayer.DataSource != _show.ItemSoundUrl)
                {
                    if (PlayerService.Show != null)
                        PlayerService.Show.ShowPlayerPosition = PlayerService.Position;
                    PlayerService.Show = _show;
                    //activity was started from a person's show search result list - pass search person name to the player service
                    PlayerService.SearchPersonName = !string.IsNullOrEmpty(_searchPersonName)
                        ? _searchPersonName
                        : string.Empty;
                }
                PlayerService.PlayPause();
            }
            catch
            {
                // ignored
            }
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
                if (_binder == null || _mediaPlayerServiceConnection == null)
                    InitilizeMedia();
                return _echoPlayerService ?? (_echoPlayerService = _binder?.GetMediaPlayerService());
            }
        }

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

        //activity started with different intent
        protected override void OnNewIntent(Intent intent)
        {
            Finish();
            StartActivity(intent);
        }

        public void OnPlaying(object sender, EventArgs e)
        {
            if (MainActivity.EchoPlayer.DataSource != _show.ItemSoundUrl)
                return;
            //update seekbar if this show is currently playing
            _seekBar.Max = PlayerService.Duration;
            _seekBar.Progress = PlayerService.Position;
        }

        public void OnBuffering(object sender, EventArgs e)
        {
            if (_show == null || MainActivity.EchoPlayer == null || MainActivity.EchoPlayer.DataSource != _show.ItemSoundUrl)
                return;
            _seekBar.SecondaryProgress = PlayerService.Buffered;
        }

        private void OnPlaybackStarted(object sender, string url)
        {
            if (_show != null && url == _show.ItemSoundUrl)
                RunOnUiThread(() =>
                _playButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(this, Resource.Drawable.pause_black)
                        : ContextCompat.GetDrawable(this, Resource.Drawable.ic_pause_circle_outline_white_48dp)));
        }

        private void OnPlaybackPaused(object sender, string url)
        {
            if (_show != null && (url == _show.ItemSoundUrl || url == null))
                RunOnUiThread(() =>
                _playButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(this, Resource.Drawable.play_black)
                        : ContextCompat.GetDrawable(this, Resource.Drawable.ic_play_circle_outline_white_48dp)));
        }

        private void InitilizeMedia()
        {
            if (MainActivity.EchoPlayer == null)
                MainActivity.EchoPlayer = new EchoMediaPlayer();
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
                MainActivity.EchoPlayer.PlaybackStarted += OnPlaybackStarted;
                MainActivity.EchoPlayer.PlaybackPaused += OnPlaybackPaused;
                if (Intent.GetBooleanExtra("Play", false))
                    OnPlayButtonClick(this, EventArgs.Empty);
                else if (MainActivity.EchoPlayer.IsPlaying && MainActivity.EchoPlayer.DataSource == _show.ItemSoundUrl)
                    _playButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(this, Resource.Drawable.pause_black)
                        : ContextCompat.GetDrawable(this, Resource.Drawable.ic_pause_circle_outline_white_48dp));
                else
                    _playButton.SetImageDrawable(MainActivity.Theme == Resource.Style.MyTheme_Light
                        ? ContextCompat.GetDrawable(this, Resource.Drawable.play_black)
                        : ContextCompat.GetDrawable(this, Resource.Drawable.ic_play_circle_outline_white_48dp));
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
                MainActivity.EchoPlayer.PlaybackStarted -= OnPlaybackStarted;
                MainActivity.EchoPlayer.PlaybackPaused -= OnPlaybackPaused;
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
                    photo = await person.GetPersonPhoto(MainActivity.DisplayWidth/5);
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
                var nameCell = new EchoTextView(this);
                nameCell.SetWidth(0);
                var personText = person.PersonName;
                if (!string.IsNullOrEmpty(person.PersonAbout))
                    personText += (",\n" + person.PersonAbout);
                nameCell.Setup(personText, MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
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
            if (MainActivity.EchoPlayer.DataSource == _show.ItemSoundUrl)
                await PlayerService.Seek(progress);
            _show.ShowPlayerPosition = progress;
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {
        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {
        }

        private async Task<bool> CheckConnectivity()
        {
            if (await CrossConnectivity.Current.IsRemoteReachable("echo.msk.ru"))
                return true;
            var message = "<font color=\"#ffffff\">" + Resources.GetText(Resource.String.network_error) + "</font>";
            Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.main_content),
                Html.FromHtml(message, FromHtmlOptions.ModeLegacy), 60000)
                .SetAction(Resources.GetText(Resource.String.close), v => { OnBackPressed(); })
                .Show();
            return false;
        }
    }
}