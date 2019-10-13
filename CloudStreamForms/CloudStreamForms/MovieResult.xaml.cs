﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.Main;
using System.Reflection;
using System.Collections.ObjectModel;
using CloudStreamForms.Models;
using Xamarin.Essentials;
using CloudStreamForms;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MovieResult : ContentPage
    {

        public Poster mainPoster;
        public string trailerUrl = "";
        List<ImageButton> recBtts = new List<ImageButton>();
        public static List<Movie> lastMovie;
        List<Poster> RecomendedPosters { set { currentMovie.title.recomended = value; } get { return currentMovie.title.recomended; } }  //= new List<Poster>();

        bool loadedTitle = false;
        int currentSeason = 0;
        //ListView episodeView;
        public const int heightRequestPerEpisode = 120;
        public const int heightRequestAddEpisode = 40;
        public const int heightRequestAddEpisodeAndroid = 0;

        bool isMovie = false;
        Movie currentMovie = new Movie();
        bool isDub = true;
        bool RunningWindows { get { return DeviceInfo.Platform == DevicePlatform.UWP; } }
        string CurrentMalLink
        {
            get {

                try {
                    string s = currentMovie.title.MALData.seasonData[currentSeason].malUrl;
                    if (s != "https://myanimelist.net") {
                        return s;
                    }
                    else {
                        return "";
                    }
                }
                catch (Exception) {
                    return "";
                }
            }
        }


        List<Episode> currentEpisodes { set { currentMovie.episodes = value; } get { return currentMovie.episodes; } }

        protected override bool OnBackButtonPressed()
        {
            if (lastMovie != null) {
                if (lastMovie.Count > 0) {
                    activeMovie = lastMovie[lastMovie.Count - 1];
                    lastMovie.RemoveAt(lastMovie.Count - 1);
                }
            }
            //Navigation.PopModalAsync();
            // return true;
            return base.OnBackButtonPressed();
        }

        void SetRows()
        {
            MainThread.BeginInvokeOnMainThread(() => {

                //  Grid.SetRow(RowSeason, SeasonPicker.IsVisible ? 3 - ((DubPicker.IsVisible ? 0 : 1) + (MALBtt.IsVisible ? 0 : 1)) : 0);
                Grid.SetRow(RowDub, SeasonPicker.IsVisible ? (1 - (MALBtt.IsVisible ? 0 : 1)) : 0);
                Grid.SetRow(RowMal, MALBtt.IsVisible ? 0 : 0);

                // MALBtt.IsVisible = MALBtt.IsVisible;
            });

        }
        public MainEpisodeView epView;
        public MovieResult()
        {

            InitializeComponent();

            mainPoster = Search.mainPoster;

            Gradient.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png");

            //NameLabel.Text = activeMovie.title.name;
            NameLabel.Text = mainPoster.name;
            RatingLabel.Text = mainPoster.year;

            titleLoaded += MovieResult_titleLoaded;
            trailerLoaded += MovieResult_trailerLoaded;
            epsiodesLoaded += MovieResult_epsiodesLoaded;


            // TrailerBtt.Clicked += TrailerBtt_Clicked;
            Gradient.Clicked += TrailerBtt_Clicked;
            linkAdded += MovieResult_linkAdded;
            linksProbablyDone += MovieResult_linksProbablyDone;
            movie123FishingDone += MovieResult_movie123FishingDone;
            //  myEpisodeResultCollection;

            //  FakePlayBtt.Source = ImageSource.FromUri(new System.Uri("https://m.media-amazon.com/images/M/MV5BMjEyNzQ0MjE2OF5BMl5BanBnXkFtZTcwMTkyNjE5Ng@@._V1_CR0,60,640,360_AL_UX477_CR0,0,477,268_AL_.jpg"));
            //FakePlayBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.playBtt.png");
            BackgroundColor = Color.Black;
            #region notUsed
            /*
            DataTemplate dataTemplate = new ListViewDataTemplateSelector();
            
            episodeView = new ListView {
                //VerticalOptions = LayoutOptions.Start,
                //HorizontalOptions = LayoutOptions.FillAndExpand,
                // TranslationY = heightRequestAddEpisode / 2f,
                // Source of data items.
                ItemsSource = MyEpisodeResultCollection,

                // Define template for displaying each item.
                // (Argument of DataTemplate constructor is called for 
                //      each item; it must return a Cell derivative.)
                ItemTemplate = dataTemplate, /*new DataTemplate(() => {

                    // Create views with bindings for displaying each property.
                    Label nameLabel = new Label();
                    Label desLabel = new Label();
                    nameLabel.SetBinding(Label.TextProperty, "Title");
                    desLabel.SetBinding(Label.TextProperty, "Description");
                    desLabel.FontSize = nameLabel.FontSize / 1.2f;
                    
                    desLabel.TextColor = Color.Gray;
                    nameLabel.TranslationX = 5;
                    desLabel.TranslationX = 5;
                    ProgressBar progressBar = new ProgressBar();
                    progressBar.IsVisible = false;
                    progressBar.SetBinding(ProgressBar.ProgressProperty, "Progress");

                    Picker linkPicker = new Picker();
                    // linkPicker.Items.Add("Mirror 1");
                    //  linkPicker.Items.Add("Mirror 2");
                    // linkPicker.Items.Add("Mirror 3");
                    linkPicker.SetBinding(Picker.ItemsSourceProperty, "Mirros");


                    Picker subPicker = new Picker();
                    subPicker.SetBinding(Picker.ItemsSourceProperty, "Subtitles");

                    
                    //   subPicker.Items.Add("English");
                    //   subPicker.Items.Add("Swedish");

                    Picker exePicker = new Picker();
                    exePicker.Items.Add("Play");
                    exePicker.Items.Add("Download");
                    exePicker.Items.Add("Copy Link");
                    exePicker.Items.Add("Copy Subtitle Link");

                    // Button playBtt = new Button() { Text="Play" };

                    Grid grid = new Grid();
                    grid.Children.Add(linkPicker);
                    grid.Children.Add(subPicker);
                    grid.Children.Add(exePicker);
                    //   grid.Children.Add(playBtt);

                    Grid.SetColumn(subPicker, 1);
                    Grid.SetColumn(exePicker, 2);

                    grid.SetBinding(Grid.IsVisibleProperty, "EpVis");

                    try {
                        exePicker.SelectedIndex = 0;
                        subPicker.SelectedIndex = 0;
                        linkPicker.SelectedIndex = 0;
                    }
                    catch (Exception) {

                    }
                    //grid.IsVisible = true;
                    //    Grid.SetColumn(playBtt, 3);

                    //nameLabel.SetBinding(Label.d, "Extra");


                    // Return an assembled ViewCell.
                    return new ViewCell {
                        View = new StackLayout {
                            // Padding = new Thickness(0, 5),
                            HeightRequest = heightRequestPerEpisode,
                            MinimumHeightRequest = heightRequestPerEpisode,
                            Orientation = StackOrientation.Horizontal,
                            VerticalOptions = LayoutOptions.Start,
                            Children =
                            {
                                //boxView,
                                new StackLayout
                                {
                                    VerticalOptions = LayoutOptions.Start,
                                    Spacing = 0,
                                    Children =
                                    {
                                        nameLabel,
                                        desLabel,
                                        grid,
                                        progressBar,
                                        //birthdayLabel
                                    }
                                }
                            }
                        }
                    };
                })    star/

            };
            episodeView.ItemTapped += ListView_ItemTapped;
       //     episodeView.HeightRequest = 0;
            episodeView.MinimumHeightRequest = 0;
            episodeView.VerticalOptions = LayoutOptions.Start;
            episodeView.HasUnevenRows = true;*/
            #endregion
            MALBtt.IsVisible = false;
            epView = new MainEpisodeView();
            BindingContext = epView;

            // listView.HeightRequest = 100;
            // starPng.Source = 
            //MGRID.Children.Add(listView);
            //Grid.SetRow(listView, 6);
            //  EpisodeView = listView;
            /*
            this.Content = new ScrollView {
                Content = new StackLayout() {
                    Children =
                    {
                        XGRID,
                    MGRID,
                   // episodeView,
                   SLay,
                    RText,
                    MScroll,
                    }
                }
            };
            */
            //  Grid.SetRow(RowSeason, 0);
            episodeView.ItemAppearing += EpisodeView_ItemAppearing;
            SizeChanged += MainPage_SizeChanged;
            Grid.SetRow(RowDub, 0);
            Grid.SetRow(RowMal, 0);
            episodeView.HeightRequest = 0;

            //episodeView.HeightRequest = 10000;
            // print(mainPoster.name + "|" + mainPoster.url + "|" + mainPoster.year);
            GetImdbTitle(mainPoster);
            //  episodeView.HeightRequest = 0;
            //  AbsoluteLayout.SetLayoutFlags(episodeView, AbsoluteLayoutFlags.PositionProportional);
            //   AbsoluteLayout.SetLayoutBounds(episodeView, new Rectangle(0f, 0f, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        }

        private void MainPage_SizeChanged(object sender, EventArgs e)
        {
            WaitScale();
        }
        List<Grid> grids = new List<Grid>();
        List<ProgressBar> progressBars = new List<ProgressBar>();
        public void AddEpisode(EpisodeResult episodeResult)
        {
            if (episodeResult.Rating != "") {
                episodeResult.Title += " | ★ " + episodeResult.Rating;
            }

            if (episodeResult.PosterUrl == "") {
                if (activeMovie.title.posterUrl != "") {
                    episodeResult.PosterUrl = activeMovie.title.trailers.FirstOrDefault().posterUrl ?? "";
                }
            }
            epView.MyEpisodeResultCollection.Add(episodeResult);
        }
        public void ClearEpisodes()
        {
            episodeView.ItemsSource = null;
            epView.MyEpisodeResultCollection.Clear();
            episodeView.ItemsSource = epView.MyEpisodeResultCollection;
            episodeView.HeightRequest = 0;
            totalHeight = 0;
            counter = 0;
            play_btts = new List<Image>();
            grids = new List<Grid>();
            progressBars = new List<ProgressBar>();
            //  grids.Clear();
            // gridsSize.Clear();
        }
        double totalHeight = 0;
        int counter = 0;

        private void ViewCell_SizeChanged(object sender, EventArgs e)
        {
            if (sender is Grid) {
                Grid grid = (Grid)sender;
                EpisodeResult episodeResult = ((EpisodeResult)((Grid)sender).BindingContext);

                // ProgressBar progressBar = (ProgressBar)( (Grid)((Grid)((Grid)grid.Children.ElementAt(0)).Children.ElementAt(0)).Children.ElementAt(2)).Children.ElementAt(0);
                //  progressBars = new List<ProgressBar>(epView.MyEpisodeResultCollection.Count);
                if (counter < epView.MyEpisodeResultCollection.Count) {
                    grids.Add(grid);

                }
                // print(">>>>> cc" + counter + "/" +);
                counter++;
                //  if (counter >= (epView.MyEpisodeResultCollection.Count)) {
                WaitScale();
                //}
            }
        }


        async void WaitScale()
        {
            await Task.Delay(30);
            totalHeight = 0;
            for (int i = 0; i < grids.Count; i++) {
                Grid grid = grids[i];
                totalHeight += grid.Height;
                totalHeight += grid.Margin.Top;
                totalHeight += grid.Margin.Bottom;
            }

            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = totalHeight + 50);
        }

        private void EpisodeView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            // print("SPACING;: " + RText.Y + "|" + RText.AnchorY + "|" + RText.TranslationY + "|" + episodeView.Height + "|" + e.ItemIndex + "|" + episodeView.Y + "|" + episodeView.TranslationY + "|" + episodeView.Bounds.Height);
            //  placeIt.IsVisible = true;
            //  episodeView.Footer = placeIt;
            //placeIt.IsVisible = false;

            //  SLay.HeightRequest = 100000;
            //  print("A---- :" + episodeView.RowHeight * epView.MyEpisodeResultCollection.Count + "--: " + episodeView.);
            //  SLay.HeightRequest = episodeView.RowHeight * epView.MyEpisodeResultCollection.Count;
        }



        private void MovieResult_movie123FishingDone(object sender, Movie e)
        {
            if (!SameAsActiveMovie()) return;
            currentMovie = e;
        }

        bool SameAsActiveMovie()
        {
            //print(currentMovie.title.id + " || " + activeMovie.title.id);
            return currentMovie.title.id == activeMovie.title.id;
        }

        private void MovieResult_linksProbablyDone(object sender, Episode e)
        {
            /*
            foreach (var item in currentEpisodes) {
                if (item.name == e.name) {
                    List<Link> links = e.links.OrderBy(l => -l.priority).ToList();
                    for (int i = 0; i < links.Count; i++) {
                        print(links[i].name + " | " + links[i].url);
                    }
                    try {
                        PlayVLCWithSingleUrl(links[0].url);

                    }
                    catch (Exception) {

                    }
                    return;
                }
            }*/

        }


        private void MovieResult_linkAdded(object sender, int e)
        {
            if (!SameAsActiveMovie()) return;

            if (currentMovie.episodes[0].name + currentMovie.episodes[0].description == activeMovie.episodes[0].name + activeMovie.episodes[0].description && epView.MyEpisodeResultCollection.Count > 0) {

            }
            else {
                return;
            }
            MainThread.BeginInvokeOnMainThread(() => {

                currentMovie = activeMovie;

                if (currentEpisodes != null) {
                    for (int i = 0; i < currentEpisodes.Count; i++) {
                        if (currentEpisodes[i].links != null) {

                            if (currentEpisodes[i].links.Count > 0) {

                                List<Link> links = currentEpisodes[i].links;
                                try {
                                    links = links.OrderBy(l => -l.priority).ToList();

                                }
                                catch (Exception) {

                                }

                                epView.MyEpisodeResultCollection[i].epVis = true;
                                List<string> mirrors = new List<string>();
                                List<string> mirrorsUrls = new List<string>();
                                int mirrorCounter = 0;
                                // myEpisodeResultCollection[i].Mirros.Clear();
                                for (int f = 0; f < links.Count; f++) {
                                    try {
                                        Link link = links[f];

                                        if (CheckIfURLIsValid(link.url)) {
                                            string name = link.name;
                                            if (name.Contains("[MIRRORCOUNTER]")) {
                                                mirrorCounter++;
                                                name = name.Replace("[MIRRORCOUNTER]", mirrorCounter.ToString());
                                            }
                                            mirrors.Add(name);
                                            mirrorsUrls.Add(link.url);
                                            //    myEpisodeResultCollection[i].Mirros.Add(currentEpisodes[i].links[f].name);
                                        }
                                    }
                                    catch (Exception) {

                                    }
                                }

                                if (mirrors.Count > epView.MyEpisodeResultCollection[i].mirros.Count) {
                                    //EpisodeResult epRes = epView.MyEpisodeResultCollection[i];
                                    epView.MyEpisodeResultCollection[i].mirrosUrls = mirrorsUrls;
                                    epView.MyEpisodeResultCollection[i].epVis = mirrors.Count > 0;
                                    epView.MyEpisodeResultCollection[i].mirros = mirrors;// = new EpisodeResult() { mirros = mirrors, Description = epRes.Description, epVis = mirrors.Count > 0, Id = epRes.Id, mirrosUrls = mirrorsUrls, PosterUrl = epRes.PosterUrl, progress = epRes.progress, Rating = epRes.Rating, subtitles = epRes.subtitles, Title = epRes.Title };
                                }
                            }
                        }
                    }
                }
            });
            //print(e + "|" + activeMovie.episodes[1].maxProgress);
        }

        private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (!SameAsActiveMovie()) return;

            EpisodeResult episodeResult = ((MainEpisodeView)((ListView)sender).BindingContext).MyEpisodeResultCollection[e.ItemIndex];


        }

        private void TrailerBtt_Clicked(object sender, EventArgs e)
        {
            if (trailerUrl != null) {
                if (trailerUrl != "") {
                    PlayVLCWithSingleUrl(trailerUrl, currentMovie.title.name + " - Trailer");
                }
            }
        }


        private void MovieResult_titleLoaded(object sender, Movie e)
        {
            if (loadedTitle) return;

            loadedTitle = true;
            isMovie = (e.title.movieType == MovieType.Movie || e.title.movieType == MovieType.AnimeMovie);
            currentMovie = e;
            print("Title loded" + " | " + mainPoster.name);
            MainThread.BeginInvokeOnMainThread(() => {
                try {
                    string souceUrl = e.title.trailers.First().posterUrl;
                    if (CheckIfURLIsValid(souceUrl)) {

                        TrailerBtt.Source = souceUrl;
                    }
                    else {
                        TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png");
                    }
                }
                catch (Exception) {
                    TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png");
                }

                if (!RunningWindows) {
                    //Gradient.IsVisible = false;
                }

                string extra = "";
                bool haveSeasons = e.title.seasons != 0;

                if (haveSeasons) {
                    extra = e.title.seasons + " Season" + (e.title.seasons == 1 ? "" : "s") + " | ";
                }

                string rYear = mainPoster.year;
                if (rYear == null || rYear == "") {
                    rYear = e.title.year;
                }
                RatingLabel.Text = rYear + " | " + e.title.runtime + " | " + extra + "★ " + e.title.rating;
                DescriptionLabel.Text = e.title.description.Replace("\\u0027", "\'");

                // ---------------------------- SEASONS ----------------------------

                // currentMovie.title.movieType == MovieType.Anime;

                SeasonPicker.IsVisible = haveSeasons;
                SeasonPicker.SelectedIndexChanged += SeasonPicker_SelectedIndexChanged;
                DubPicker.SelectedIndexChanged += DubPicker_SelectedIndexChanged;
                if (haveSeasons) {
                    SeasonPicker.Items.Clear();
                    for (int i = 1; i <= e.title.seasons; i++) {
                        SeasonPicker.Items.Add("Season " + i);
                    }
                    SeasonPicker.SelectedIndex = 0;
                    currentSeason = 1;
                    GetImdbEpisodes(currentSeason);
                }
                else {
                    currentSeason = 0; // MOVIES
                    GetImdbEpisodes();
                }

                // ---------------------------- RECOMMENDATIONS ----------------------------
                foreach (var item in Recommendations.Children) { // SETUP
                    Grid.SetColumn(item, 0);
                    Grid.SetRow(item, 0);
                }
                Recommendations.Children.Clear();
                for (int i = 0; i < RecomendedPosters.Count; i++) {
                    Poster p = e.title.recomended[i];
                    if (CheckIfURLIsValid(p.posterUrl)) {
                        ImageButton imageButton = new ImageButton() { HeightRequest = 100, WidthRequest = 65, Source = p.posterUrl, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start };
                        recBtts.Add(imageButton);
                        Recommendations.Children.Add(recBtts[i]);
                    }
                }
                RecomendationLoaded.IsVisible = false;
                for (int i = 0; i < recBtts.Count; i++) { // --- RECOMMENDATIONS CLICKED -----
                    recBtts[i].Clicked += (o, _e) => {
                        for (int z = 0; z < recBtts.Count; z++) {
                            if (((ImageButton)o).Id == recBtts[z].Id) {
                                if (lastMovie == null) {
                                    lastMovie = new List<Movie>();
                                }
                                lastMovie.Add(activeMovie);
                                Search.mainPoster = RecomendedPosters[z];
                                Page p = new MovieResult();// { mainPoster = mainPoster };
                                Navigation.PushModalAsync(p);
                            }
                        }
                    };
                }

                for (int i = 0; i < Recommendations.Children.Count; i++) { // GRID
                    Grid.SetColumn(Recommendations.Children[i], i);
                }

                SetRows();
            });



        }

        private void DubPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            try {

                isDub = "Dub" == DubPicker.Items[DubPicker.SelectedIndex];
                SetDubExist();
            }
            catch (Exception) {

            }
        }

        private void SeasonPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearEpisodes();
            currentSeason = SeasonPicker.SelectedIndex + 1;
            GetImdbEpisodes(currentSeason);
            // myEpisodeResultCollection.Clear();
        }

        private void MovieResult_epsiodesLoaded(object sender, List<Episode> e)
        {
            if (!SameAsActiveMovie()) return;
            print("episodes loaded");

            currentMovie = activeMovie;

            currentMovie.episodes = e;
            MainThread.BeginInvokeOnMainThread(() => {
                currentEpisodes = e;
                ClearEpisodes();
                bool isLocalMovie = false;
                bool isAnime = currentMovie.title.movieType == MovieType.Anime;


                if (currentMovie.title.movieType != MovieType.Movie && currentMovie.title.movieType != MovieType.AnimeMovie) {
                    if (currentMovie.title.movieType != MovieType.Anime) {
                        for (int i = 0; i < currentEpisodes.Count; i++) {
                            AddEpisode(new EpisodeResult() { Title = (i + 1) + ". " + currentEpisodes[i].name, Id = i, Description = currentEpisodes[i].description.Replace("\n", "").Replace("  ", ""), PosterUrl = currentEpisodes[i].posterUrl, Rating = currentEpisodes[i].rating, Progress = 0, epVis = false, subtitles = new List<string>() { "None" }, mirros = new List<string>() });

                        }
                    }
                }
                else {
                    AddEpisode(new EpisodeResult() { Title = currentMovie.title.name, Description = currentMovie.title.description, Id = 0, PosterUrl = "", Progress = 0, Rating = "", epVis = false, subtitles = new List<string>() { "None" }, mirros = new List<string>() });
                    isLocalMovie = true;
                }

                //episodeView.HeightRequest = myEpisodeResultCollection.Count * heightRequestPerEpisode + (RunningWindows ? heightRequestAddEpisode : heightRequestAddEpisodeAndroid);

                DubPicker.Items.Clear();

                if (isAnime) {
                    bool dubExists = false;
                    bool subExists = false;
                    try {

                        for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                            MALSeason ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q];

                            if (ms.dubExists) {
                                dubExists = true;
                            }
                            if (ms.subExists) {
                                subExists = true;
                            }
                        }
                    }
                    catch (Exception) {

                    }

                    isDub = dubExists;

                    if (dubExists) {
                        DubPicker.Items.Add("Dub");
                        print("BBBBBBBBBBBBBBBBBB");
                    }
                    if (subExists) {
                        DubPicker.Items.Add("Sub");
                        print("AAAAAAAAAAAAAAA");
                    }
                    if (DubPicker.Items.Count > 0) {
                        DubPicker.SelectedIndex = 0;
                    }
                    SetDubExist();
                }
                else {

                }

                DubPicker.IsVisible = DubPicker.Items.Count > 0;
                print(DubPicker.IsVisible + "ENABLED");
                MALBtt.IsVisible = CurrentMalLink != "";
                SetRows();

            });
        }

        void SetDubExist()
        {
            if (!SameAsActiveMovie()) return;

            // string dstring = "";
            List<string> baseUrls = GetAllEpsFromAnime(currentMovie, currentSeason, isDub);

            if (baseUrls.Count > 0) {

                TempThred tempThred = new TempThred();
                tempThred.typeId = 6; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        int max = 0;
                        activeMovie.title.MALData.currentActiveMaxEpsPerSeason = new List<int>();

                        for (int i = 0; i < baseUrls.Count; i++) {
                            string dstring = baseUrls[i]; dstring = dstring.Replace("-dub", "") + (isDub ? "-dub" : "");
                            string d = DownloadString("https://www9.gogoanime.io/category/" + dstring);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            string subMax = FindHTML(d, "class=\"active\" ep_start = \'", ">");
                            string maxEp = FindHTML(subMax, "ep_end = \'", "\'");//FindHTML(d, "<a href=\"#\" class=\"active\" ep_start = \'0\' ep_end = \'", "\'");
                            print(i + "MAXEP" + maxEp);
                            print(baseUrls[i]);

                            max += int.Parse(maxEp);
                            activeMovie.title.MALData.currentActiveMaxEpsPerSeason.Add(int.Parse(maxEp));
                        }

                        MainThread.BeginInvokeOnMainThread(() => {
                            ClearEpisodes();
                            for (int i = 0; i < max; i++) {
                                try {
                                    AddEpisode(new EpisodeResult() { Title = (i + 1) + ". " + currentEpisodes[i].name, Id = i, Description = currentEpisodes[i].description.Replace("\n", "").Replace("  ", ""), PosterUrl = currentEpisodes[i].posterUrl, Rating = currentEpisodes[i].rating, Progress = 0, epVis = false, subtitles = new List<string>() { "None" }, mirros = new List<string>() });

                                }
                                catch (Exception) {
                                    AddEpisode(new EpisodeResult() { Title = (i + 1) + ". " + "Episode #" + (i + 1), Id = i, Description = "", PosterUrl = "", Rating = "", Progress = 0, epVis = false, subtitles = new List<string>() { "None" }, mirros = new List<string>() });

                                }
                            }
                            //episodeView.HeightRequest = myEpisodeResultCollection.Count * heightRequestPerEpisode + heightRequestAddEpisode;
                            SetRows();
                        });


                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "Gogoanime Download";
                tempThred.Thread.Start();


            }
        }


        private void MovieResult_trailerLoaded(object sender, string e)
        {
            if (!SameAsActiveMovie()) return;

            trailerUrl = e;
            /*
            MainThread.BeginInvokeOnMainThread(() => {
                Trailer trailer = activeMovie.title.trailers.First();
                trailerUrl = trailer.url;
                print(trailer.posterUrl);
                TrailerBtt.Source = trailer.posterUrl;//ImageSource.FromUri(new System.Uri(trailer.posterUrl));

            });*/

        }



        private void IMDb_Clicked(object sender, EventArgs e)
        {
            if (!SameAsActiveMovie()) return;
            OpenBrowser("https://www.imdb.com/title/" + mainPoster.url);
        }
        private void MAL_Clicked(object sender, EventArgs e)
        {
            if (!SameAsActiveMovie()) return;
            OpenBrowser(CurrentMalLink);
        }

        List<Image> play_btts = new List<Image>();

        private void Image_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {

            Image image = ((Image)sender);

            if (play_btts.Where(t => t.Id == image.Id).Count() == 0) {
                play_btts.Add(image);
                print("----->> " + image.Source);
                image.Source = ImageSource.FromResource("CloudStreamForms.Resource.play_png.png");
                image.Scale = 0.4f;
            }

        }

        async void SetProgress(int sec, int sender)
        {
            EpisodeResult ep = epView.MyEpisodeResultCollection[sender];

            ep.loadedLinks = false;

            double add = 0;
            for (int i = 0; i < 100 * sec; i++) {
                await Task.Delay(10);
                add = ((double)i / (double)sec) / (double)100;
                MainThread.BeginInvokeOnMainThread(() => {

                    //  ep.Progress = add;
                });

                print(add + "|" + ep.Progress);
            }
            ep.loadedLinks = true;
        }

        private void ImageButton_Clicked(object sender, EventArgs e) // LOAD
        {
            if (!SameAsActiveMovie()) return;
            //print(activeMovie.title.movies123MetaData.seasonData.Count);
            // activeMovie = currentMovie;
            //ProgressBar progressBar = (ProgressBar)((Grid)((Grid)((ImageButton)sender).Children.ElementAt(0)).Children.ElementAt(0)).Children.ElementAt(2)).Children.ElementAt(0);

            EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            if (episodeResult.loadedLinks) {
                print("OPEN : " + episodeResult.Title);
                if (!CheckIfURLIsValid(episodeResult.loadResult.url)) {
                    try {
                        LoadResult cl = episodeResult.loadResult;

                        episodeResult.loadResult = new LoadResult() { url = episodeResult.mirrosUrls[0], loadSelection = cl.loadSelection, subtitleUrl = cl.subtitleUrl };
                    }
                    catch (Exception) {
                    }
                 
                }

                if (CheckIfURLIsValid(episodeResult.loadResult.url)) {
                        PlayVLCWithSingleUrl(episodeResult.loadResult.url, episodeResult.Title);
                    }
                    else {
                        // VALID URL ERROR
                    }

                
            }
            else {
                if (progressBars[episodeResult.Id].Progress == 0) {
                    GetEpisodeLink(isMovie ? -1 : (episodeResult.Id + 1), currentSeason, isDub: isDub);
              
                    Device.InvokeOnMainThreadAsync(async () => {
                        progressBars[episodeResult.Id].Progress = 0.01f;
                        progressBars[episodeResult.Id].IsVisible = true;
                        await progressBars[episodeResult.Id].ProgressTo(1, 3000, Easing.SinIn);
                        progressBars[episodeResult.Id].Progress = 1;
                        episodeResult.loadedLinks = true;
                    });
                }

                // SetProgress(3, ((EpisodeResult)((ImageButton)sender).BindingContext).Id);

            }
            //MainThread.BeginInvokeOnMainThread(() => {
            //   epView.MyEpisodeResultCollection[((EpisodeResult)((ImageButton)sender).BindingContext).Id].Progress = 1;

        
            // print(epView.MyEpisodeResultCollection[((EpisodeResult)((ImageButton)sender).BindingContext).Id].Progress + "-->><<");
            //});
            episodeView.SelectedItem = null;
        }

        private void ProgressBar_BindingContextChanged(object sender, EventArgs e)
        {
            ProgressBar pBar = (ProgressBar)sender;
            if (pBar.BindingContext is EpisodeResult) {
                int id = ((EpisodeResult)pBar.BindingContext).Id;
                if (id >= progressBars.Count) {
                    progressBars.Add(pBar);
                }
                else {
                    progressBars[id] = pBar;
                }
            }
        }
        public double MaxWidthRequest { set; get; } = 10;
        private void ScrollView_SizeChanged(object sender, EventArgs e)
        {
            MaxWidthRequest = ((ScrollView)sender).ContentSize.Width;
        }
    }

    public class MainEpisodeView
    {
        public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set; get; }
        public MainEpisodeView()
        {
            MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
        }
    }
}

public class ListViewDataTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return new DataTemplate(() => {
            //print(((EpisodeResult)item).Title + " ------------------->>>>>>>");
            EpisodeResult result = (EpisodeResult)item;
            Label nameLabel = new Label();
            Label desLabel = new Label();
            nameLabel.SetBinding(Label.TextProperty, "Title");
            desLabel.SetBinding(Label.TextProperty, "Description");
            desLabel.FontSize = nameLabel.FontSize / 1.2f;

            desLabel.TextColor = Color.Gray;
            nameLabel.TranslationX = 5;
            desLabel.TranslationX = 5;

            ProgressBar progressBar = new ProgressBar();
            progressBar.IsVisible = false;
            progressBar.SetBinding(ProgressBar.ProgressProperty, "Progress");

            Picker linkPicker = new Picker();
            // linkPicker.Items.Add("Mirror 1");
            //  linkPicker.Items.Add("Mirror 2");
            // linkPicker.Items.Add("Mirror 3");
            linkPicker.SetBinding(Picker.ItemsSourceProperty, "Mirros");


            Picker subPicker = new Picker();
            subPicker.SetBinding(Picker.ItemsSourceProperty, "Subtitles");

            //   subPicker.Items.Add("English");
            //   subPicker.Items.Add("Swedish");

            Picker exePicker = new Picker();
            exePicker.Items.Add("Play");
            exePicker.Items.Add("Download");
            exePicker.Items.Add("Copy Link");
            exePicker.Items.Add("Copy Subtitle Link");

            // Button playBtt = new Button() { Text="Play" };

            Grid grid = new Grid();
            grid.Children.Add(linkPicker);
            grid.Children.Add(subPicker);
            grid.Children.Add(exePicker);
            //   grid.Children.Add(playBtt);

            if (GLOBAL_SUBTITLES_ENABLED) {
                Grid.SetColumn(subPicker, 1);
                if (PLAY_SELECT_ENABLED) {
                    Grid.SetColumn(exePicker, 2);
                }
            }
            else {
                if (PLAY_SELECT_ENABLED) {

                    Grid.SetColumn(exePicker, 1);
                }
            }
            exePicker.IsEnabled = PLAY_SELECT_ENABLED;
            exePicker.IsVisible = PLAY_SELECT_ENABLED;
            subPicker.IsEnabled = GLOBAL_SUBTITLES_ENABLED;
            subPicker.IsVisible = GLOBAL_SUBTITLES_ENABLED;

            grid.SetBinding(Grid.IsVisibleProperty, "EpVis");

            try {
                exePicker.SelectedIndex = 0;
                subPicker.SelectedIndex = 0;
                linkPicker.SelectedIndex = 0;
            }
            catch (Exception) {

            }
            if (!PLAY_SELECT_ENABLED) { // NOT FINAL, REMOVE THIS 

                Button copyBtt = new Button() { Text = "Copy Link" };
                copyBtt.Clicked += (o, e) => {
                    if (linkPicker.SelectedIndex != -1) {
                        try {
                            Clipboard.SetTextAsync(result.mirrosUrls[linkPicker.SelectedIndex]);

                        }
                        catch (Exception) {

                        }
                    }
                };
                grid.Children.Add(copyBtt);

                Grid.SetColumn(copyBtt, 1);
            }


            grid.WidthRequest = 1000000;

            linkPicker.SelectedIndexChanged += (o, e) => {
                LoadResult cl = result.loadResult;
                result.loadResult = new LoadResult() { url = result.mirrosUrls[linkPicker.SelectedIndex], loadSelection = cl.loadSelection, subtitleUrl = cl.subtitleUrl };
            };

            exePicker.SelectedIndexChanged += (o, e) => {
                LoadResult cl = result.loadResult;
                LoadSelection[] loadSelections = { LoadSelection.Play, LoadSelection.Download, LoadSelection.CopyLink, LoadSelection.CopySubtitleLink };
                LoadSelection loadSelection = loadSelections[exePicker.SelectedIndex];
                result.loadResult = new LoadResult() { url = cl.url, loadSelection = loadSelection, subtitleUrl = cl.subtitleUrl };
            };

            subPicker.SelectedIndexChanged += (o, e) => {
                LoadResult cl = result.loadResult;
                result.loadResult = new LoadResult() { url = cl.url, loadSelection = cl.loadSelection, subtitleUrl = subPicker.SelectedIndex == 0 ? "" : result.subtitlesUrls[subPicker.SelectedIndex - 1] };
            };


            //grid.IsVisible = true;
            //    Grid.SetColumn(playBtt, 3);

            //nameLabel.SetBinding(Label.d, "Extra");
            /*
            Label birthdayLabel = new Label();
            birthdayLabel.SetBinding(Label.TextProperty,
                new Binding("Birthday", BindingMode.OneWay,
                    null, null, "Born {0:d}"));

            BoxView boxView = new BoxView();
            boxView.SetBinding(BoxView.ColorProperty, "FavoriteColor");*/

            // Return an assembled ViewCell.
            return new ViewCell {
                View = new StackLayout {
                    // Padding = new Thickness(0, 5),
                    //  HeightRequest = MovieResult.heightRequestPerEpisode,
                    //MinimumHeightRequest = MovieResult.heightRequestPerEpisode,
                    //  Orientation = StackOrientation.Horizontal,
                    // VerticalOptions = LayoutOptions.Start,
                    Children =
                {
                                //boxView,
                                new StackLayout
                                {
                                    Padding= new Thickness (0,10),
                                    VerticalOptions = LayoutOptions.FillAndExpand,
                                    HorizontalOptions = LayoutOptions.FillAndExpand,
                                    Spacing = 10,
                                    Children =
                                    {
                                        nameLabel,
                                        desLabel,
                                        grid,
                                        progressBar,
                                        //birthdayLabel
                                    }
                                }
                            }
                }
            };
        });
    }
}