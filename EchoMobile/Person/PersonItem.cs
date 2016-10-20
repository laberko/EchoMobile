using System;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Graphics;

namespace Echo.Person
{
    //a single person class
    public class PersonItem
    {
        public string PersonName;
        public string PersonUrl;
        public string PersonPhotoUrl;
        private Bitmap _personPhoto;
        public string PersonAbout;

        //download and resize user picture
        public async Task<Bitmap> GetPersonPhoto(int widthLimit)
        {
            if ((string.IsNullOrEmpty(PersonPhotoUrl)) || (widthLimit <= 0))
                return null;
            Bitmap bitmapResized;
            try
            {
                if (_personPhoto == null)
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromMilliseconds(10000);
                        var imageBytes = await httpClient.GetByteArrayAsync(PersonPhotoUrl).ConfigureAwait(false);
                        if (imageBytes == null || imageBytes.Length <= 0)
                            return null;
                        _personPhoto = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    }
                }
                var ratio = (float)widthLimit / _personPhoto.Width;
                bitmapResized = Bitmap.CreateScaledBitmap(_personPhoto, (int)Math.Round(ratio * _personPhoto.Width), (int)Math.Round(ratio * _personPhoto.Height), true);
            }
            catch
            {
                return null;
            }
            return bitmapResized;
        }
    }
}