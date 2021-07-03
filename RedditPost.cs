using System;

namespace reddit_post_bot
{
    public class RedditPost
    {
        public DateTime postTime;
        public string title;

        public RedditPost(DateTime postTime, string title)
        {
            this.postTime = postTime;
            this.title = title;
        }      
    }
}