using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;

namespace Echo
{
    [Register("net.laberko.EchoTextView")]
    public class EchoTextView : AppCompatTextView
    {
        public EchoTextView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public EchoTextView(Context context) : base(context)
        {
        }

        public EchoTextView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public EchoTextView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public void Setup(string text, Color textColor, TypefaceStyle textFace, int textSize)
        {
            if (string.IsNullOrEmpty(text))
                text = Resources.GetString(Resource.String.network_error);
            SetTextColor(textColor);
            SetTypeface(null, textFace);
            SetTextSize(ComplexUnitType.Sp, textSize);
            SetBackgroundColor(Color.Transparent);
            Text = text;
            Visibility = ViewStates.Visible;
        }
    }
}