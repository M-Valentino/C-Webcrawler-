﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Text;

internal class Web_Crawler
{
    public struct WebPage
    {
        public WebPage(string url, string title)
        {
            Url = url;
            Title = title;
        }

        public string Url { get; }
        public string Title { get; }
        public override string ToString() => $"URL: {Url}, Title: {Title}";
    }

    private HashSet<string> _urlsVisited = new HashSet<string>();
    private Queue<string> _topLevelDomainsToVisit = new Queue<string>();
    private HashSet<WebPage> _finalList = new HashSet<WebPage>();

    private int _animationFrame = 0;

    private void PrintAnimationFrame()
    {
        // Move the cursor up one line
        if (Console.CursorTop > 0)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        // Clear the current line
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);

        switch (_animationFrame)
        {
            case 0:
                Console.WriteLine("-");
                _animationFrame++;
                break;
            case 1:
                Console.WriteLine("\\");
                _animationFrame++;
                break;
            case 2:
                Console.WriteLine("|");
                _animationFrame++;
                break;
            case 3:
                Console.WriteLine("/");
                _animationFrame = 0;
                break;
        }
    }

    private Boolean AddTofinalListOrStop(WebPage page, int numLinks)
    {
        if (_finalList.Count >= numLinks)
        {
            return true;
        }

        _finalList.Add(page);
        return false;
    }

    private async Task Crawl(string startUrl, int numLinks)
    {
        _topLevelDomainsToVisit.Enqueue(startUrl);

        while (_topLevelDomainsToVisit.Count > 0)
        {
            PrintAnimationFrame();
            string url = _topLevelDomainsToVisit.Dequeue();

            if (_urlsVisited.Contains(url))
            {
                continue;
            }

            _urlsVisited.Add(url);

            string webPage = await Fetch(url);
            if (webPage == null)
            {
                continue;
            }

            string title = ExtractTitle(webPage);
            WebPage page = new WebPage(url, title);

            if (AddTofinalListOrStop(page, numLinks))
            {
                return;
            }

            Uri baseUri = new Uri(url);
            var anchorMatches = Regex.Matches(webPage, @"<a\s+href\s*=\s*[""'](https?://[^""']+)[""']");

            foreach (Match match in anchorMatches)
            {
                string absoluteUrl = match.Groups[1].Value;
                if (_urlsVisited.Contains(absoluteUrl))
                {
                    continue;
                }

                _topLevelDomainsToVisit.Enqueue(absoluteUrl);
            }

            var relativeAnchorMatches = Regex.Matches(webPage, @"<a\s+href\s*=\s*[""'](/[^""']+)[""']");
            foreach (Match match in relativeAnchorMatches)
            {
                string relativeUrl = match.Groups[1].Value;
                string absoluteUrl = new Uri(baseUri, relativeUrl).ToString();

                if (_urlsVisited.Contains(absoluteUrl))
                {
                    continue;
                }

                _topLevelDomainsToVisit.Enqueue(absoluteUrl);
            }
        }
    }

    private async Task<string> Fetch(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                string htmlContent = await client.GetStringAsync(url);
                return htmlContent;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }
    }

    private string ExtractTitle(string htmlContent)
    {
        var titleMatch = Regex.Match(htmlContent, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
        return titleMatch.Success ? titleMatch.Groups[1].Value : "No Title Found";
    }

    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run <url> <number of links to craw> [--csv]");
        }
        else
        {
            Web_Crawler crawler = new Web_Crawler();

            await crawler.Crawl(args[0], int.Parse(args[1]));
            Console.WriteLine("Saved " + crawler._finalList.Count + " links.");
            if (args.Length == 2)
            {
                foreach (var link in crawler._finalList)
                {
                    Console.WriteLine(link);
                }
            }
            else if (args[2] == "--csv")
            {
                string filePath = "webpages.csv";
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("url,title");
                foreach (var webPage in crawler._finalList)
                {
                    csv.AppendLine($"{webPage.Url},{webPage.Title}");
                }

                File.WriteAllText(filePath, csv.ToString());
                Console.WriteLine($"CSV file has been written to {filePath}");
            }
            else
            {
                Console.WriteLine($"Invalid 3rd arg provided for exporting to CSV. Printing crawled links: ");
                foreach (var link in crawler._finalList)
                {
                    Console.WriteLine(link);
                }
            }
        }
    }
}