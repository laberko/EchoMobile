using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Echo.Person;
using XamarinBindings.MaterialProgressBar;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Show
{
    //activity to open a full show item
    [Activity(Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
    public class ShowActivity : AppCompatActivity, View.IOnClickListener, SeekBar.IOnSeekBarChangeListener
    {
        private MediaPlayer _mediaPlayer;
        private SeekBar _seekBar;
        private Handler _seekHandler;
        private ImageButton _button;
        private ShowItem _show;
        private MaterialProgressBar _progressBar;
        private WebView _textWebView;
        private bool _playerReady;
        private bool _personsReady;

        protected override async void OnCreate(Bundle bundle)
        {
            //no collection of daily shows contents
            if (Common.ShowContentList == null)
            {
                Finish();
                return;
            }
            base.OnCreate(bundle);
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

            //find show with id passed to intent
            var content = Common.ShowContentList.FirstOrDefault(c => c.ContentDate.Date == Common.SelectedDates[2].Date)?.Shows;
            _show = content?.FirstOrDefault(s => s.ShowId == Guid.Parse(Intent.GetStringExtra("ID")));
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
                //a handler to repeat player seek bar update
                _seekHandler = new Handler();

                _seekBar = FindViewById<SeekBar>(Resource.Id.seekBar);
                _seekBar.SetOnSeekBarChangeListener(this);

                _button = FindViewById<ImageButton>(Resource.Id.showPlay);
                _button.SetOnClickListener(this);

                _mediaPlayer = new MediaPlayer();
                await _mediaPlayer.SetDataSourceAsync(_show.ShowSoundUrl);
                _mediaPlayer.SetAudioStreamType(Stream.Music);
                _mediaPlayer.Prepared += delegate
                {
                    _seekBar.Max = _mediaPlayer.Duration;
                    //start repeated seek bar update
                    SeekUpdate();
                    if (Intent.GetStringExtra("Action") == "Play")
                    {
                        //start immediately
                        _mediaPlayer.Start();
                        _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black));
                    }
                    else
                        _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.play_black));
                    _playerReady = true;
                    if (_personsReady)
                        _progressBar.Visibility = ViewStates.Gone;
                };
                _mediaPlayer.PrepareAsync();
            }
            else
            {
                //no audio in show - hide player layout
                var playLayout = FindViewById<TableLayout>(Resource.Id.playLayout);
                playLayout.Visibility = ViewStates.Gone;
                _playerReady = true;
                if (_personsReady)
                    _progressBar.Visibility = ViewStates.Gone;
            }

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
            _mediaPlayer?.Stop();
            _mediaPlayer?.Reset();
            _textWebView?.OnPause();
            Finish();
        }

        protected override void OnStop()
        {
            _textWebView?.OnPause();
            base.OnStop();
        }

        protected override void OnResume()
        {
            _textWebView?.OnResume();
            base.OnResume();
        }

        protected override void OnPause()
        {
            _textWebView?.OnPause();
            base.OnPause();
        }

        //player button clicked
        public void OnClick(View v)
        {
            if (v.Id != Resource.Id.showPlay || _mediaPlayer == null) return;
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.play_black));
            }
            else
            {
                _mediaPlayer.Start();
                _button.SetImageDrawable(ContextCompat.GetDrawable(this, Resource.Drawable.pause_black));
            }
        }

        //update seek bar position every 1000 ms
        private void SeekUpdate()
        {
            _seekBar.Progress = _mediaPlayer.CurrentPosition;
            _seekHandler.PostDelayed(SeekUpdate, 1000);
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

        public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
            if (fromUser)
                _mediaPlayer.SeekTo(progress);
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {
        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {
        }
    }
}