using System;
using System.Threading.Tasks;
using Android.Graphics;
using Echo.Person;

namespace Echo
{
    public abstract class AbstractContent
    {
        public readonly MainActivity.ContentType ItemType;
        public Guid ItemId;
        public string ItemUrl;
        public string ItemSoundUrl;
        public PersonItem ItemAuthor;
        public string ItemAuthorName;
        public string ItemAuthorUrl;
        public Bitmap ItemPicture;
        public string ItemPictureUrl;
        public string ItemTitle;
        public string ItemSubTitle;
        protected string ItemText;
        public DateTime ItemDate;
        public string ItemRootUrl;

        protected AbstractContent(MainActivity.ContentType itemType)
        {
            ItemType = itemType;
        }

        public abstract Task<string> GetHtml();
    }
}