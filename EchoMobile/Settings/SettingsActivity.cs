using System;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo.Settings
{
    [Activity(Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SettingsActivity : AppCompatActivity
    {
        //private Toolbar _toolBar;
        private Spinner _folderSpinner;
        private Spinner _themeSpinner;
        private Spinner _showSpinner;
        private Spinner _blogSpinner;
        private List<string> _folders;
        private string[] _themes;
        private int[] _searchResults;

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(MainActivity.Theme);
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.SettingsView);

            _folders = new List<string>
            {
                Resources.GetString(Resource.String.music),
                Resources.GetString(Resource.String.downloads),
                Resources.GetString(Resource.String.podcasts),
                Resources.GetString(Resource.String.documents)
            };

            _themes = new[]
            {
                Resources.GetString(Resource.String.light),
                Resources.GetString(Resource.String.dark)
            };

            _searchResults = new[] { 10, 20, 50, 100 };

            var toolBar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolBar.SetBackgroundColor(Color.Transparent);
            toolBar.SetTitle(Resource.String.settings);
            SetSupportActionBar(toolBar);

            var fontText = FindViewById<EchoTextView>(Resource.Id.bigFontText);
            fontText.Setup(Resources.GetString(Resource.String.setfont), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
            var fontSwitch = FindViewById<Switch>(Resource.Id.bigFontSwitch);
            fontSwitch.TextOn = Resources.GetString(Resource.String.yes);
            fontSwitch.TextOff = Resources.GetString(Resource.String.no);
            fontSwitch.Checked = MainActivity.FontSize == 22;
            fontSwitch.CheckedChange += OnFontSwitchCheckedChange;

            var folderText = FindViewById<EchoTextView>(Resource.Id.downloadFolderText);
            folderText.Setup(Resources.GetString(Resource.String.setfolder), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
            _folderSpinner = FindViewById<Spinner>(Resource.Id.downloadFolderSpinner);
            _folderSpinner.ItemSelected += OnFolderSpinnerItemSelected;
            var folderSpinnerAdapter = new ArrayAdapter<string>(this, Resource.Layout.SpinnerItem, _folders);
            _folderSpinner.Adapter = folderSpinnerAdapter;
            if (string.IsNullOrEmpty(MainActivity.FolderSettings))
                MainActivity.FolderSettings = _folders[0];
            _folderSpinner.SetSelection(_folders.IndexOf(MainActivity.FolderSettings), true);

            var themeText = FindViewById<EchoTextView>(Resource.Id.themeText);
            themeText.Setup(Resources.GetString(Resource.String.theme), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
            _themeSpinner = FindViewById<Spinner>(Resource.Id.themeSpinner);
            _themeSpinner.ItemSelected += OnThemeSpinnerItemSelected;
            var themeSpinnerAdapter = new ArrayAdapter<string>(this, Resource.Layout.SpinnerItem, _themes);
            _themeSpinner.Adapter = themeSpinnerAdapter;
            switch (MainActivity.Theme)
            {
                case Resource.Style.MyTheme_Light:
                    _themeSpinner.SetSelection(0, true);
                    break;
                case Resource.Style.MyTheme_Dark:
                    _themeSpinner.SetSelection(1, true);
                    break;
            }

            var showHistoryText = FindViewById<EchoTextView>(Resource.Id.showHistorySizeText);
            showHistoryText.Setup(Resources.GetString(Resource.String.show_history_count), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
            _showSpinner = FindViewById<Spinner>(Resource.Id.showHistorySizeSpinner);
            _showSpinner.ItemSelected += OnShowSpinnerItemSelected;
            var showSpinnerAdapter = new ArrayAdapter<int>(this, Resource.Layout.SpinnerItem, _searchResults);
            _showSpinner.Adapter = showSpinnerAdapter;
            if (MainActivity.ShowHistorySize == 0)
                MainActivity.ShowHistorySize = _searchResults[1];
            _showSpinner.SetSelection(Array.IndexOf(_searchResults, MainActivity.ShowHistorySize), true);

            var blogHistoryText = FindViewById<EchoTextView>(Resource.Id.blogHistorySizeText);
            blogHistoryText.Setup(Resources.GetString(Resource.String.blog_history_count), MainActivity.MainTextColor, TypefaceStyle.Normal, MainActivity.FontSize);
            _blogSpinner = FindViewById<Spinner>(Resource.Id.blogHistorySizeSpinner);
            _blogSpinner.ItemSelected += OnBlogSpinnerItemSelected;
            var blogSpinnerAdapter = new ArrayAdapter<int>(this, Resource.Layout.SpinnerItem, _searchResults);
            _blogSpinner.Adapter = blogSpinnerAdapter;
            if (MainActivity.BlogHistorySize == 0)
                MainActivity.BlogHistorySize = _searchResults[1];
            _blogSpinner.SetSelection(Array.IndexOf(_searchResults, MainActivity.BlogHistorySize), true);
        }

        //theme changed
        private void OnThemeSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var themeChanged = false;
            switch (e.Position)
            {
                case 0:
                    if (MainActivity.Theme != Resource.Style.MyTheme_Light)
                    {
                        MainActivity.Theme = Resource.Style.MyTheme_Light;
                        themeChanged = true;
                    }
                    break;
                case 1:
                    if (MainActivity.Theme != Resource.Style.MyTheme_Dark)
                    {
                        MainActivity.Theme = Resource.Style.MyTheme_Dark;
                        themeChanged = true;
                    }
                    break;
            }
            if (!themeChanged)
                return;
            //MainActivity.IsRestarting = true;
            var thisIntent = PackageManager.GetLaunchIntentForPackage(PackageName);
            var newIntent = IntentCompat.MakeRestartActivityTask(thisIntent.Component);
            //StartActivity(newIntent);
            FinishAffinity();
            StartActivity(newIntent);
        }

        private void OnBlogSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            MainActivity.BlogHistorySize = _searchResults[e.Position];
        }

        private void OnShowSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            MainActivity.ShowHistorySize = _searchResults[e.Position];
        }

        private void OnFontSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            MainActivity.FontSize = e.IsChecked ? 22 : 18;
            Recreate();
        }

        private void OnFolderSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            MainActivity.FolderSettings = _folders[e.Position];
        }

        //populate menu
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.item_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //main menu item selected
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.back)
                OnBackPressed();
            return base.OnOptionsItemSelected(item);
        }
    }
}