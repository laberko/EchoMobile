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
            if ((string.IsNullOrEmpty(PersonPhotoUrl)) || (widthLimit < 0))
                return null;
            try
            {
                if (_personPhoto == null)
                {
                    byte[] imageBytes;
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = new TimeSpan(0, 0, 10);
                        imageBytes = await httpClient.GetByteArrayAsync(PersonPhotoUrl).ConfigureAwait(false);
                    }
                    if (imageBytes == null || imageBytes.Length <= 0)
                        return null;
                    _personPhoto = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                }
                if (widthLimit == 0)
                    return _personPhoto;
                var ratio = (float) widthLimit/_personPhoto.Width;
                return Bitmap.CreateScaledBitmap(_personPhoto, (int) Math.Round(ratio*_personPhoto.Width),
                    (int) Math.Round(ratio*_personPhoto.Height), true);
            }
            catch
            {
                return null;
            }
        }
    }
}