using System;
using Android.Support.V4.View;
using Android.Views;

namespace Echo
{
    class EchoViewPageTransformer : Java.Lang.Object, ViewPager.IPageTransformer
    {
        private const float MinScale = 0.9f;
        private const float MinAlpha = 0.5f;

        public void TransformPage(View page, float position)
        {
            var pageWidth = page.Width;
            var pageHeight = page.Height;

            if (position < -1)
                page.Alpha = 0;
            else if (position <= 1)
            {
                var scaleFactor = Math.Max(MinScale, 1 - Math.Abs(position)/2);
                var vertMargin = pageHeight*(1 - scaleFactor)/2;
                var horizMargin = pageWidth*(1 - scaleFactor)/2;

                if (position < 0)
                    page.TranslationX = horizMargin - vertMargin/2;
                else
                    page.TranslationX = -horizMargin + vertMargin/2;

                page.ScaleX = scaleFactor;
                page.ScaleY = scaleFactor;

                page.Alpha = MinAlpha + (scaleFactor - MinScale)/(1 - MinScale)*(1 - MinAlpha);
            }
            else
                page.Alpha = 0;
        }
    }
}