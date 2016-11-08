using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Android.Graphics;
using Echo.Person;
using HtmlAgilityPack;

namespace Echo.Show
{
    //shows content for a specific day
    public class ShowContent : INotifyPropertyChanged
    {
        private List<ShowItem> _shows;
        public DateTime ContentDate;
        public event PropertyChangedEventHandler PropertyChanged;

        public ShowContent(DateTime day)
        {
            _shows = new List<ShowItem>();
            ContentDate = day;
            GetContent();
        }

        //shows collection
        public List<ShowItem> Shows
        {
            get
            {
                return _shows;
            }
            private set
            {
                _shows = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //raise PropertyChanged event and pass changed property name
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //download and parse blogs content for the date
        public async void GetContent()
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
                root = await Common.GetHtml(allShowsUrl);
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
                    var showSubTitle = string.Empty;
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
                    //audio and text
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
                    if (allShows.Any(s => !string.IsNullOrEmpty(s.ShowSoundUrl) && s.ShowSoundUrl == showAudioUrl))
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
                    var show = new ShowItem
                    {
                        ShowId = Guid.NewGuid(),
                        ShowItemUrl = showTextUrl,
                        ShowDateTime = showDateTime,
                        ShowTitle = showTitle,
                        ShowSubTitle = showSubTitle,
                        ShowModeratorNames = moderatorNameList.Count > 0 ? "Ведущие: " + string.Join(", ", moderatorNameList) : string.Empty,
                        ShowModeratorUrls = moderatorUrlList,
                        ShowGuestNames = guestNameList.Count > 0 ? "Гости: " + string.Join(", ", guestNameList) : string.Empty,
                        ShowGuestUrls = guestUrlList,
                        ShowSoundUrl = showAudioUrl
                    };
                    allShows.Add(show);
                }
            }
            foreach (var show in allShows)
            {
                //fill the collection of new shows and replace modified ones
                var existingShow = Shows.FirstOrDefault(s => s.ShowDateTime == show.ShowDateTime);
                if (existingShow == null)
                    newContent.Add(show);
                else if (existingShow.ShowItemUrl != show.ShowItemUrl || existingShow.ShowSoundUrl != show.ShowSoundUrl)
                {
                    Shows.Remove(existingShow);
                    Shows.Add(show);
                    showsUpdated = true;
                }
            }
            if (newContent.Count <= 0 && !showsUpdated)
                return;
            var list = Shows;
            list.AddRange(newContent);
            //assign Shows property to raise PropertyChanged
            Shows = list.OrderByDescending(b => b.ShowDateTime).ToList();
        }

        //indexer (read only) for accessing a show item
        public ShowItem this[int i] => Shows.Count == 0 ? null : Shows[i];
    }
}