using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Android.Widget;
using HtmlAgilityPack;
//using XamarinBindings.MaterialProgressBar;

namespace Echo.Show
{
    //shows content for a specific day
    public class ShowContent : AbstractContentFactory, INotifyPropertyChanged
    {
        private readonly ProgressBar _progressBar;

        public ShowContent(DateTime day, ProgressBar progressBar):base(day)
        {
            _progressBar = progressBar;
            GetContent();
        }

        //download and parse blogs content for the date
        public override sealed async void GetContent()
        {
            var showsUpdated = false;
            var allShows = new List<ShowItem>();
            var newContent = new List<ShowItem>();
            var allShowsUrl = ContentDate.Date == DateTime.Now.Date
                ? "http://echo.msk.ru/schedule/"
                : "http://echo.msk.ru/schedule/" + ContentDate.ToString("yyyy-MM-dd") + ".html";
            HtmlDocument root;
            try
            {
                root = await MainActivity.GetHtml(allShowsUrl);
            }
            catch
            {
                return;
            }
            var rootDiv = root?.DocumentNode.Descendants("div").FirstOrDefault(n => n.Attributes.Contains("class") && n.Attributes["class"].Value == "column");
            var findDivs = rootDiv?.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("programm_info"));
            if (findDivs == null)
                return;
            foreach (var findDiv in findDivs)
            {
                var node = findDiv.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("liveprogrammes"));
                if (node == null)
                    continue;
                var divs = node.Descendants("div");
                foreach (var div in divs)
                {
                    var moderatorNameList = new List<string>();
                    var moderatorUrlList = new List<string>();
                    var guestNameList = new List<string>();
                    var guestUrlList = new List<string>();
                    var showAudioUrl = string.Empty;
                    var showTextUrl = string.Empty;
                    TimeSpan showTime;
                    DateTime showDateTime;
                    //date
                    var timeNode = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("datetime iblock"));
                    if (timeNode == null)
                        continue;
                    if (TimeSpan.TryParse(timeNode.InnerText, out showTime))
                        showDateTime = ContentDate.Date.Add(showTime);
                    else
                        continue;
                    //audio and text urls
                    var contentDiv = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("linkprogramme iblock right"));
                    var contentUrls = contentDiv?.Descendants("a");
                    if (contentUrls != null)
                    {
                        foreach (var url in contentUrls.Select(a => a.GetAttributeValue("href", string.Empty)))
                        {
                            if (url.Contains("programs") && !url.Contains("mmvideo") && !url.Contains("/q.html"))
                                showTextUrl = "http://echo.msk.ru" + url;
                            if (url.Contains("mp3"))
                                showAudioUrl = url;
                        }
                    }
                    //no audio and no text - not interesting, skip
                    if (string.IsNullOrEmpty(showTextUrl) && string.IsNullOrEmpty(showAudioUrl))
                        continue;
                    //we already have a show with the same audio (identical) - skip this one
                    if (allShows.Any(s => !string.IsNullOrEmpty(s.ItemSoundUrl) && s.ItemSoundUrl == showAudioUrl))
                        continue;
                    //title and people
                    var aboutDiv = div.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("aboutprogramme iblock"));
                    var titleDiv = aboutDiv?.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("title"));
                    if (titleDiv == null)
                        continue;
                    var titleNode = titleDiv.Descendants("a").FirstOrDefault();
                    var showTitle = titleNode?.InnerText.Replace("\n\t\t", string.Empty).Replace("  ", string.Empty);
                    if (showTitle == null)
                        continue;
                    var subTitleDiv = aboutDiv.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("notice"));
                    var showRootUrl = titleNode.GetAttributeValue("href", string.Empty);
                    var personsDiv = aboutDiv.Descendants("div").FirstOrDefault(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("persons"));
                    var persons = personsDiv?.Descendants("a");
                    if (persons != null)
                    {
                        foreach (var person in persons)
                        {
                            var personUrl = person.GetAttributeValue("href", string.Empty);
                            if (personUrl.Contains("guests"))
                            {
                                guestUrlList.Add("http://echo.msk.ru" + personUrl);
                                guestNameList.Add(person.Descendants("b").FirstOrDefault()?.InnerText);
                            }
                            if (!personUrl.Contains("contributors"))
                                continue;
                            moderatorUrlList.Add("http://echo.msk.ru" + personUrl);
                            moderatorNameList.Add(person.Descendants("b").FirstOrDefault()?.InnerText);
                        }
                    }
                    var show = new ShowItem(MainActivity.ContentType.Show)
                    {
                        ItemId = Guid.NewGuid(),
                        ItemUrl = showTextUrl,
                        ItemDate = showDateTime,
                        ItemTitle = showTitle,
                        ItemSubTitle = subTitleDiv != null ? subTitleDiv.InnerText.Trim() : string.Empty,
                        ShowModeratorNames = moderatorNameList.Count > 0 ? "Ведущие: " + string.Join(", ", moderatorNameList) : string.Empty,
                        ShowModeratorUrls = moderatorUrlList.Distinct().ToList(),
                        ShowGuestNames = guestNameList.Count > 0 ? "Гости: " + string.Join(", ", guestNameList) : string.Empty,
                        ShowGuestUrls = guestUrlList.Distinct().ToList(),
                        ItemSoundUrl = showAudioUrl,
                        ItemRootUrl = !string.IsNullOrEmpty(showRootUrl) ? "http://echo.msk.ru" + showRootUrl : string.Empty
                    };
                    allShows.Add(show);
                }
            }
            foreach (var show in allShows)
            {
                //fill the collection of new shows and replace modified ones
                var existingShow = ContentList.FirstOrDefault(s => (s.ItemType == MainActivity.ContentType.Show && s.ItemDate == show.ItemDate));
                if (existingShow == null)
                    newContent.Add(show);
                //text or audio was added to an existing show
                else if (existingShow.ItemUrl != show.ItemUrl
                    || (string.IsNullOrEmpty(existingShow.ItemSoundUrl) && !string.IsNullOrEmpty(show.ItemSoundUrl)))
                {
                    ContentList.Remove(existingShow);
                    ContentList.Add(show);
                    showsUpdated = true;
                }
            }
            if (newContent.Count <= 0 && !showsUpdated)
                return;
            var list = ContentList;
            list.AddRange(newContent);
            //assign Shows property to raise PropertyChanged
            ContentList = list.OrderByDescending(b => b.ItemDate).ToList();
            MainActivity.PlayList = ContentList
                .Where(c => (c.ItemType == MainActivity.ContentType.Show && !string.IsNullOrEmpty(c.ItemSoundUrl)))
                .OrderBy(c => c.ItemDate).Cast<ShowItem>().ToArray();
            _progressBar.Visibility = Android.Views.ViewStates.Gone;
        }
    }
}