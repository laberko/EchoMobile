using System;

namespace Echo.ContentTypes
{
    public class BlogItem
    {
        public Guid BlogId;
        public DateTime BlogDate;
        public string BlogTitle;
        public PersonItem BlogAuthor;
        public string BlogText;

    }
}