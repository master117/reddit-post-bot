using System.Threading;
using System.Linq;
using System.Net;
using System;
using System.IO;
using RedditSharp;
using RedditSharp.Things;
using Imgur.API.Authentication;
using System.Threading.Tasks;
using System.Net.Http;
using Imgur.API.Endpoints;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace reddit_post_bot
{
    public static class Program
    {
        // RNG for image to psot selection
        private static Random rng = new Random();

        // Supported image extensions
        private static readonly string[] extensions = { ".jpg", ".jpeg", ".png", ".gif" };
        
        public static async Task Main(string[] args)
        {
            Console.WriteLine(DateTime.Now + ": Started reddit-post-bot");
            // Load all config data
            string user = System.IO.File.ReadAllText("user.txt");
            string pw = System.IO.File.ReadAllText("pw.txt");
            string clientid = System.IO.File.ReadAllText("clientid.txt");
            string secret = System.IO.File.ReadAllText("secret.txt");
            string imgurclientid = System.IO.File.ReadAllText("imgurclientid.txt");
            string imgursecret = System.IO.File.ReadAllText("imgursecret.txt");
            string targetReddit = System.IO.File.ReadAllText("targetreddit.txt");

            // Initialize imgur connection
            var apiClient = new ApiClient(imgurclientid, imgursecret);
            var httpClient = new HttpClient();
            var oAuth2Endpoint = new OAuth2Endpoint(apiClient, httpClient);
            var imageEndpoint = new ImageEndpoint(apiClient, httpClient);

            // Check if an imgur refresh token ist stored, it can be used to login,
            // otherwise manually generate a new one
            Console.WriteLine(DateTime.Now + ": Checking for imgur refresh token...");
            while (!System.IO.File.Exists("imgurrefreshtoken.txt") || System.IO.File.ReadAllText("imgurrefreshtoken.txt") == "")
            {
                var authUrl = oAuth2Endpoint.GetAuthorizationUrl();
                Console.WriteLine(authUrl);
                OpenBrowser(authUrl);
                Console.WriteLine("Please allow the application in the opened browserwindow, " +
                "then check the opened url for a refreshtoken and put it in imgurrefreshtoken.txt. Press any key to continue.");
                Console.ReadKey();
            }

            // Load token, save new refresh token
            var refreshtoken = System.IO.File.ReadAllText("imgurrefreshtoken.txt");
            var token = await oAuth2Endpoint.GetTokenAsync(refreshtoken);
            await System.IO.File.WriteAllTextAsync("imgurrefreshtoken.txt", token.RefreshToken);
            Console.WriteLine(DateTime.Now + ": Successfully connected to imgur as: " + token.AccountUsername);

            // Load images to be posted
            List<RedditPost> postList = deserializePostList();
            while (true)
            {
                if (postList.Count == 0 || postList[postList.Count - 1].postTime.AddDays(1) < DateTime.Now)
                {
                    Console.WriteLine("");
                    try
                    {
                        // Filter list of files to exclude already posted files and to files having the correct extension
                        var files = Directory.EnumerateFiles("posts");
                        List<string> possiblefiles = files.Where(
                            x => !postList.Any(y => x == y.title)
                            && extensions.Any(y => x.EndsWith(y))).ToList();
                        // Check if we still have files to post
                        if (possiblefiles.Count > 0)
                        {
                            // Shuffle
                            possiblefiles.Shuffle();
                            // Upload first image from shuffled list
                            using (var fileStream = File.OpenRead(possiblefiles[0]))
                            {
                                Console.WriteLine(DateTime.Now + ": Uploading: " + possiblefiles[0]);
                                var imageUpload = await imageEndpoint.UploadImageAsync(fileStream);
                                Console.WriteLine(DateTime.Now + ": Uploaded: " + possiblefiles[0]);

                                Console.WriteLine(DateTime.Now + ": Posting: " + possiblefiles[0]);
                                // Post imagelink to reddit
                                PostToReddit(user, pw, clientid, secret, targetReddit, "Daily post: #" + (postList.Count + 1), imageUpload.Link);
                                Console.WriteLine(DateTime.Now + ": Posted: 'Daily post: #" + (postList.Count + 1) + "'");

                                // Mark as posted
                                postList.Add(new RedditPost(DateTime.Now, possiblefiles[0]));
                                serializePostList(postList);
                            }
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now + ": All Files have been posted!");
                            Console.ReadKey();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(DateTime.Now + ": " + e);
                    }
                }

                Console.Write("\r" + DateTime.Now + ": Last post was less than a day ago, sleeping for 1 minute.");
                Thread.Sleep(60 * 1000);
            }
        }

        // Serializes the list of posts
        public static void serializePostList(List<RedditPost> tempList)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter file = File.CreateText("postlist.json"))
                serializer.Serialize(file, tempList);
        }

        // Deserializes the list of posts
        public static List<RedditPost> deserializePostList()
        {
            if (!File.Exists("postlist.json"))
                return new List<RedditPost>();

            List<RedditPost> tempList;
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader file = File.OpenText("postlist.json"))
                tempList = (List<RedditPost>)serializer.Deserialize(file, typeof(List<RedditPost>));

            return tempList;
        }

        // Posts an image to reddit
        public static async void PostToReddit(string user, string pw, string clientid, string secret, string subredditName, string title, string url)
        {
            var webAgent = new BotWebAgent(user, pw, clientid, secret, "http://localhost:8080");
            //This will check if the access token is about to expire before each request and automatically request a new one for you
            //"false" means that it will NOT load the logged in user profile so reddit.User will be null
            var reddit = new Reddit(webAgent, false);
            var subreddit = await reddit.GetSubredditAsync(subredditName);
            await subreddit.SubscribeAsync();
            await subreddit.SubmitPostAsync(title, url);
        }

        // Open the default borswer on the given url, used to request a new imgur token
        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        // Shuffles a list
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}