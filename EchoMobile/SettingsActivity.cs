using System;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Echo
{
    [Activity(Label = "",
        Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SettingsActivity : AppCompatActivity
    {
        private Spinner _folderSpinner;
        private Spinner _showSpinner;
        private Spinner _blogSpinner;
        private List<string> _folders;
        private int[] _searchResults;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.SettingsView);

            _folders = new List<string>
            {
                Resources.GetString(Resource.String.music),
                Resources.GetString(Resource.String.downloads),
                Resources.GetString(Resource.String.podcasts)
            };
            if ((int)Build.VERSION.SdkInt >= 19)
                _folders.Add(Resources.GetString(Resource.String.documents));

            _searchResults = new[] { 10, 20, 50, 100 };

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_top);
            toolbar.SetBackgroundColor(Color.Transparent);
            toolbar.SetTitle(Resource.String.settings);
            SetSupportActionBar(toolbar);

            var fontText = FindViewById<TextView>(Resource.Id.bigFontText);
            fontText.Text = Resources.GetString(Resource.String.setfont);
            fontText.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            fontText.SetTextColor(Color.Black);
            var fontSwitch = FindViewById<Switch>(Resource.Id.bigFontSwitch);
            fontSwitch.TextOn = Resources.GetString(Resource.String.yes);
            fontSwitch.TextOff = Resources.GetString(Resource.String.no);
            fontSwitch.Checked = Common.FontSize == 22;
            fontSwitch.CheckedChange += OnFontSwitchCheckedChange;

            var folderText = FindViewById<TextView>(Resource.Id.downloadFolderText);
            folderText.Text = Resources.GetString(Resource.String.setfolder);
            folderText.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            folderText.SetTextColor(Color.Black);
            _folderSpinner = FindViewById<Spinner>(Resource.Id.downloadFolderSpinner);
            _folderSpinner.ItemSelected += OnFolderSpinnerItemSelected;
            var folderSpinnerAdapter = new ArrayAdapter<string>(this, Resource.Layout.SpinnerItem, _folders);
            _folderSpinner.Adapter = folderSpinnerAdapter;
            if (string.IsNullOrEmpty(Common.FolderSettings))
                Common.FolderSettings = _folders[0];
            _folderSpinner.SetSelection(_folders.IndexOf(Common.FolderSettings), true);

            var showHistoryText = FindViewById<TextView>(Resource.Id.showHistorySizeText);
            showHistoryText.Text = Resources.GetString(Resource.String.show_history_count);
            showHistoryText.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            showHistoryText.SetTextColor(Color.Black);
            _showSpinner = FindViewById<Spinner>(Resource.Id.showHistorySizeSpinner);
            _showSpinner.ItemSelected += OnShowSpinnerItemSelected;
            var showSpinnerAdapter = new ArrayAdapter<int>(this, Resource.Layout.SpinnerItem, _searchResults);
            _showSpinner.Adapter = showSpinnerAdapter;
            if (Common.ShowHistorySize == 0)
                Common.ShowHistorySize = _searchResults[1];
            _showSpinner.SetSelection(Array.IndexOf(_searchResults, Common.ShowHistorySize), true);

            var blogHistoryText = FindViewById<TextView>(Resource.Id.blogHistorySizeText);
            blogHistoryText.Text = Resources.GetString(Resource.String.blog_history_count);
            blogHistoryText.SetTextSize(Android.Util.ComplexUnitType.Sp, Common.FontSize);
            blogHistoryText.SetTextColor(Color.Black);
            _blogSpinner = FindViewById<Spinner>(Resource.Id.blogHistorySizeSpinner);
            _blogSpinner.ItemSelected += OnBlogSpinnerItemSelected;
            var blogSpinnerAdapter = new ArrayAdapter<int>(this, Resource.Layout.SpinnerItem, _searchResults);
            _blogSpinner.Adapter = blogSpinnerAdapter;
            if (Common.BlogHistorySize == 0)
                Common.BlogHistorySize = _searchResults[1];
            _blogSpinner.SetSelection(Array.IndexOf(_searchResults, Common.BlogHistorySize), true);
        }

        private void OnBlogSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Common.BlogHistorySize = _searchResults[e.Position];
        }

        private void OnShowSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Common.ShowHistorySize = _searchResults[e.Position];
        }

        private void OnFontSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Common.FontSize = e.IsChecked ? 22 : 18;
            Recreate();
        }

        private void OnFolderSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Common.FolderSettings = _folders[e.Position];
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