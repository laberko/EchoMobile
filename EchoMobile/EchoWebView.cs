using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;

namespace Echo
{
    [Register("net.laberko.EchoWebView")]
    class EchoWebView : WebView
    {
        public EchoWebView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public EchoWebView(Context context) : base(context)
        {
        }

        public EchoWebView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public EchoWebView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public EchoWebView(Context context, IAttributeSet attrs, int defStyleAttr, bool privateBrowsing) : base(context, attrs, defStyleAttr, privateBrowsing)
        {
        }

        public EchoWebView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public void Setup(string html)
        {
            if (string.IsNullOrEmpty(html))
                html = "<body text = #000000><p>" + Resources.GetString(Resource.String.network_error) + "</p></body>";
            Visibility = ViewStates.Visible;
            SetBackgroundColor(Color.Transparent);
            Settings.StandardFontFamily = "serif";
            Settings.DefaultFontSize = MainActivity.FontSize;
            Settings.JavaScriptEnabled = true;
            //replace color in html according to current theme
            var searchString = "<body text = ";
            var startIndex = html.IndexOf(searchString, StringComparison.Ordinal);
            if (startIndex != -1)
                html = html.Replace(html.Substring(startIndex, searchString.Length + 9), searchString + MainActivity.WebViewTextColor);
            searchString = "link = ";
            startIndex = html.IndexOf(searchString, StringComparison.Ordinal);
            if (startIndex != -1)
                html = html.Replace(html.Substring(startIndex, searchString.Length + 9), searchString + MainActivity.WebViewLinkColor);
            LoadDataWithBaseURL("", html, "text/html", "UTF-8", "");
        }
    }
}