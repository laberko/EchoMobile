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
        private ImageButton _button;
        private ShowItem _show;
        private MaterialProgressBar _progressBar;
        private WebView _textWebView;
        private bool _playerReady;
        private bool _personsReady;
        public EchoPlayerServiceBinder Binder;
        private EchoPlayerServiceConnection _mediaPlayerServiceConnection;
        private Intent _mediaPlayerServiceIntent;
        private EchoPlayerService _echoPlayerService;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

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
            _button = FindViewById<ImageButton>(Resource.Id.showPlay);

            //find show with id passed to intent
            foreach (var c in Common.ShowContentList)
            {
                _show = c.Shows.FirstOrDefault(s => s.ShowId == Guid.Parse(Intent.GetStringExtra("ID")));
                if (_show != null)
                    break;
            }
            if (_show == null)
            {
                _progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            SupportActionBar.Title = _show.ShowTitle;
            SupportActionBar.Subtitle = _show.ShowDateTime.ToString(_show.ShowDateTime.Date != DateTime.Now.Date ? "d MMMM HH:mm" : "HH:mm");

            //get (sub)title and text in one array
            string[] showContent;
            try
            {
                showContent = await _show.GetShowContent();
            }
            catch
            {
                _progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            if (showContent == null)
            {
                _progressBar.Visibility = ViewStates.Gone;
                Finish();
                return;
            }
            var subTitleTextView = FindViewById<TextView>(Resource.Id.showSubTitle);
            subTitleTextView.Text = string.IsNullOrEmpty(showContent[0]) ? _show.ShowTitle : showContent[0];
            _textWebView = FindViewById<WebView>(Resource.Id.showText);
            _textWebView.SetBackgroundColor(Color.Transparent);
            _textWebView.Settings.JavaScriptEnabled = true;
            _textWebView.Settings.StandardFontFamily = "serif";
            _textWebView.LoadDataWithBaseURL("", showContent[1], "text/html", "UTF-8", "");

            //prepare media player with controls if the show has audio
            if (!string.IsNullOrEmpty(_show.ShowSoundUrl))
            {
                if (_mediaPlayerServiceConnection == null || Binder == null)
                    InitilizeMedia();
                _button.Click += delegate
                {
                    if (Common.EchoPlayer.GetDataSource() != _show.ShowSoundUrl)
                        PlayerService.Show = _show;
                    RunOnUiThread(() => _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black)));
                    PlayerService.PlayPause();
                };
            }
            else
            {
                _playerReady = true;
                if (_personsReady)
                    _progressBar.Visibility = ViewStates.Gone;
            }

            #region PopulatePeopleList
            //fill moderators and guests info in the show item
            if (_show.ShowModerators.Count != _show.ShowModeratorUrls.Count)
            {
                var personList = new List<PersonItem>();
                foreach (var url in _show.ShowModeratorUrls.Distinct())
                {
                    try
                    {
                        personList.Add(await Common.GetPerson(url));
                    }
                    catch
                    {
                        continue;
                    }
                }
                _show.ShowModerators = personList;
            }

            if (_show.ShowGuests.Count != _show.ShowGuestUrls.Count)
            {
                var personList = new List<PersonItem>();
                foreach (var url in _show.ShowGuestUrls.Distinct())
                {
                    try
                    {
                        personList.Add(await Common.GetPerson(url));
                    }
                    catch
                    {
                        continue;
                    }
                }
                _show.ShowGuests = personList;
            }
            //fill the people list with moderators and guests
            PopulatePeopleList(_show.ShowModerators.Union(_show.ShowGuests));
            #endregion

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
            if (Common.EchoPlayer.GetDataSource() != _show.ShowSoundUrl)
                return;
            //update seekbar if this show is currently playing
            _seekBar.Max = PlayerService.Duration;
            _seekBar.Progress = PlayerService.Position;
        }

        public void OnBuffering(object sender, EventArgs e)
        {
            if (Common.EchoPlayer.GetDataSource() != _show.ShowSoundUrl)
                return;
            _seekBar.SecondaryProgress = PlayerService.Buffered;
        }

        private void OnPlaybackStarted(object sender, string url)
        {
            if (url == _show.ShowSoundUrl)
                RunOnUiThread(() => _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black)));
        }

        private void OnPlaybackPaused(object sender, string url)
        {
            if (url == _show.ShowSoundUrl)
                RunOnUiThread(() => _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.play_black)));
        }

        private void InitilizeMedia()
        {
            _mediaPlayerServiceIntent = new Intent(ApplicationContext, typeof(EchoPlayerService));
            //_mediaPlayerServiceConnection invokes ServiceConnected()
            _mediaPlayerServiceConnection = new EchoPlayerServiceConnection(this, _show.ShowId);
            BindService(_mediaPlayerServiceIntent, _mediaPlayerServiceConnection, Bind.WaivePriority);
            StartService(_mediaPlayerServiceIntent);
        }

        public void ServiceConnected()
        {
            var playLayout = FindViewById<TableLayout>(Resource.Id.playLayout);
            try
            {
                Common.EchoPlayer.PlaybackStarted += OnPlaybackStarted;
                Common.EchoPlayer.PlaybackPaused += OnPlaybackPaused;
                if (Intent.GetStringExtra("Action") == "Play")
                    PlayerService.PlayPause();
                _button.SetImageDrawable(!Common.EchoPlayer.IsPlaying
                    || Common.EchoPlayer.GetDataSource() != _show.ShowSoundUrl
                    ? ContextCompat.GetDrawable(this, Resource.Drawable.play_black)
                    : ContextCompat.GetDrawable(this, Resource.Drawable.pause_black));
                //show player layout (hidden by default) and hide progress bar
                _playerReady = true;
                if (_personsReady)
                    _progressBar.Visibility = ViewStates.Gone;
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
            if (!string.IsNullOrEmpty(_show.ShowSoundUrl))
            {
                try
                {
                    UnbindService(_mediaPlayerServiceConnection);
                    PlayerService.Playing -= OnPlaying;
                    PlayerService.Buffering -= OnBuffering;
                    Common.EchoPlayer.PlaybackStarted -= OnPlaybackStarted;
                    Common.EchoPlayer.PlaybackPaused -= OnPlaybackPaused;
                }
                catch
                {
                    Finish();
                }
            }
            base.OnBackPressed();
        }

        //fill the people list with moderators and guests
        private async void PopulatePeopleList(IEnumerable<PersonItem> list)
        {
            var peopleGrid = FindViewById<TableLayout>(Resource.Id.people);
            foreach (var person in list)
            {
                if (person == null)
                    continue;
                var span = false;
                Bitmap photo;
                try
                {
                    if (_show.ShowPicture == null)
                    {
                        _show.ShowPicture = await Common.GetImage(person.PersonPhotoUrl);
                        PlayerService.Cover = _show.ShowPicture;
                    }
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
                    var photoCell = new ImageView(this);
                    photoCell.SetImageBitmap(photo);
                    row.AddView(photoCell);
                    var photoCellParams = (TableRow.LayoutParams)photoCell.LayoutParameters;
                    photoCellParams.SetMargins(8, 8, 8, 8);
                    row.LayoutParameters = photoCellParams;
                }
                else
                    span = true;
                var nameCell = new TextView(this)
                {
                    Text = person.PersonName
                };
                nameCell.SetWidth(0);
                nameCell.SetBackgroundColor(Color.Transparent);
                nameCell.SetTextColor(Color.ParseColor("#000000"));
                if (!string.IsNullOrEmpty(person.PersonAbout))
                    nameCell.Text += (",\n" + person.PersonAbout);
                nameCell.SetTextSize(Android.Util.ComplexUnitType.Sp, 18);
                row.AddView(nameCell);
                var nameCellParams = (TableRow.LayoutParams)nameCell.LayoutParameters;
                nameCellParams.SetMargins(8, 0, 0, 0);
                nameCellParams.Gravity = GravityFlags.CenterVertical;
                if (span)
                    nameCellParams.Span = 2;
                row.LayoutParameters = nameCellParams;
                peopleGrid.AddView(row);
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
            if (Common.EchoPlayer.GetDataSource() == _show.ShowSoundUrl)
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