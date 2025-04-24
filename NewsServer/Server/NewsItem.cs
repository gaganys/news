using System;

namespace NewsServer
{
    public class NewsItem
    {
        public string DocumentId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PublishDate { get; set; }
        public string Category { get; set; }
        public string UserId { get; set; }
    }
}