﻿using CloudStreamForms.Core;
using LibVLCSharp.Shared;
using SubtitlesParser.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class VideoPage : ContentPage
	{
		const string PLAY_IMAGE = "netflixPlay.png";//"baseline_play_arrow_white_48dp.png";
		const string PAUSE_IMAGE = "pausePlay.png";//"baseline_pause_white_48dp.png";

		const string LOCKED_IMAGE = "LockLocked1.png";// "wlockLocked.png";
		const string UN_LOCKED_IMAGE = "LockUnlocked1.png";//"wlockUnLocked.png";

		/// <summary>
		/// Given current imdbId respond with the next episode id after loading links
		/// </summary>
		/// <param name="currentEpIMDBId"></param>
		/// <returns></returns>
		public delegate Task<string> LoadLinkForNextEpisode(string currentEpIMDBId, bool loadLinks);
		public delegate bool CanLoadLinks(string currentEpIMDBId);
		public static LoadLinkForNextEpisode loadLinkForNext;
		public static CanLoadLinks canLoad;
		public static EventHandler<bool> OnVideoAppear;


		/// <summary>
		/// If header is correct
		/// </summary>
		public static string loadLinkValidForHeader = "";

		bool isPaused = false;

		MediaPlayer Player { get { return vvideo.MediaPlayer; } set { vvideo.MediaPlayer = value; } }

		static LibVLC _libVLC;
		static MediaPlayer _mediaPlayer;


		/// <summary>
		/// Multiplyes a string
		/// </summary>
		/// <param name="s">String of what you want to multiply</param>
		/// <param name="times">How many times you want to multiply it</param>
		/// <returns></returns>
		public static string MultiplyString(string s, int times)
		{
			return String.Concat(Enumerable.Repeat(s, times));
		}


		/// <summary>
		/// Turn a int to classic score like pacman or spaceinvader, 123 -> 00000123
		/// </summary>
		/// <param name="inp">Input, can be string or int</param>
		/// <param name="maxLetters">How many letters at max</param>
		/// <param name="multiString">What character that will be used before score</param>
		/// <returns></returns>
		public static string ConvertScoreToArcadeScore(object inp, int maxLetters = 8, string multiString = "0")
		{
			string inpS = inp.ToString();
			inpS = MultiplyString(multiString, maxLetters - inpS.Length) + inpS;
			return inpS;
		}

		/// <summary>
		/// 0-1
		/// </summary>
		/// <param name="time"></param>
		public void ChangeTime(double time)
		{
			StartTxt.Text = CloudStreamCore.ConvertTimeToString((GetPlayerLenght() / 1000) * time);
			EndTxt.Text = CloudStreamCore.ConvertTimeToString(((GetPlayerLenght()) / 1000) - (GetPlayerLenght() / 1000) * time);
		}

		/// <summary>
		/// Holds info about the current video
		/// </summary>
		/// 
		[System.Serializable]
		public struct PlayVideo : ICloneable
		{
			public int preferedMirror;
			public long preferedStart;
			public List<string> MirrorUrls;
			public List<string> MirrorNames;
			public bool isDownloadFile;
			public bool isFromIMDB;
			public string downloadFileUrl;

			//public List<string> Subtitles;
			//public List<string> SubtitlesNames; // Requested subtitled to download
			public string name;
			public string descript;
			public int episode; //-1 = null, movie  
			public int season; //-1 = null, movie  
			public bool isSingleMirror;
			public long startPos; // -2 from progress, -1 = from start
			public string episodeId;
			public string headerId;

			public object Clone()
			{
				return this.MemberwiseClone();
			}
		}

		/// <summary>
		/// IF MOVIE, 1 else number of episodes in season
		/// </summary>
		public static int maxEpisodes = 0;
		public static int currentMirrorId = 0;
		//public static int currentSubtitlesId = -2; // -2 = null, -1 = none, 0 = def
		public static PlayVideo currentVideo;

		const string ADD_BEFORE_EPISODE = "\"";
		const string ADD_AFTER_EPISODE = "\"";

		public static List<BasicLink> Mirrors = new List<BasicLink>();

		public static bool IsSeries { get { return !(currentVideo.season <= 0 || currentVideo.episode <= 0); } }
		public static string BeforeAddToName { get { return IsSingleMirror && !currentVideo.isDownloadFile ? AllMirrorsNames[0] : (IsSeries ? ("S" + currentVideo.season + ":E" + currentVideo.episode + " ") : ""); } }
		public static string CurrentDisplayName { get { return BeforeAddToName + (IsSeries ? ADD_BEFORE_EPISODE : "") + currentVideo.name + (IsSeries ? ADD_AFTER_EPISODE : "") + (IsSingleMirror ? "" : (" · " + CurrentMirrorName)); } }

		public static BasicLink CurrentBasicLink { get { return Mirrors[currentMirrorId]; } }
		public static string CurrentMirrorName { get { return CurrentBasicLink.PublicName; } }
		public static string CurrentMirrorUrl { get { return CurrentBasicLink.baseUrl; } }
		//public static string CurrentSubtitles { get { if (currentSubtitlesId == -1) { return ""; } else { return currentVideo.Subtitles[currentSubtitlesId]; } } }

		//public static string CurrentSubtitlesNames { get { if (currentSubtitlesId == -1) { return NONE_SUBTITLES; } else { return currentVideo.SubtitlesNames[currentSubtitlesId]; } } }
		//public static List<string> AllSubtitlesNames { get { var f = new List<string>() { NONE_SUBTITLES }; f.AddRange(currentVideo.SubtitlesNames); return f; } }
		//public static List<string> AllSubtitlesUrls { get { var f = new List<string>() { "" }; f.AddRange(currentVideo.Subtitles); return f; } }

		// public static List<SubtitleItem[]> ParsedSubtitles = new List<SubtitleItem[]>();
		//   public static SubtitleItem[] Subtrack { get { if (currentSubtitlesId == -1) { return null; } else { return ParsedSubtitles[currentSubtitlesId]; } } }
		public static List<string> AllMirrorsNames { get { return Mirrors.Select(t => t.PublicName).ToList(); } }
		public static List<string> AllMirrorsUrls { get { return Mirrors.Select(t => t.baseUrl).ToList(); } }

		string lastUrl = "";
		public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
		{
			try {
				Task task = Task.Factory.StartNew(() => codeBlock());
				task.Wait(timeSpan);
				return task.IsCompleted;
			}
			catch {
				return false;
				// throw ae.InnerExceptions[0];
			}
			finally {
				print("FINISHED EXICUTE");
			}
		}

		public void HandleAudioFocus(object sender, bool e)
		{
			if (!e) {  // HANDLE LOST AUDIO FOCUS
				HandleVideoAction(App.PlayerEventType.Pause);
			}
			else {
				App.ToggleRealFullScreen(true);
			}
		}

		public List<VideoSubtitle> currentSubtitles = new List<VideoSubtitle>();
		public int subtitleIndex = -1;
		readonly object subtitleMutex = new object();
		readonly Dictionary<string, bool> searchingForLang = new Dictionary<string, bool>();
		int subtitleDelay = 0;

		public struct VideoSubtitle
		{
			public SubtitleItem[] subtitles;
			public string name;
		}

		public async Task SubtitleOptions()
		{
			List<string> options = new List<string>();
			if (currentSubtitles.Count > 0) {
				if (subtitleIndex != -1) {
					options.Add($"Change Delay ({subtitleDelay} ms)");
				}
				options.Add("Select Subtitles");
			}
			else {
				options.Add($"Download Subtitles ({Settings.NativeSubLongName})");
			}
			options.Add("Download Subtitles");

			string action = await ActionPopup.DisplayActionSheet("Subtitles", options.ToArray());

			if (action == "Download Subtitles") {
				string subAction = await ActionPopup.DisplayActionSheet("Download Subtitles", subtitleNames);
				if (subAction != "Cancel") {
					int index = subtitleNames.IndexOf(subAction);
					PopulateSubtitle(subtitleShortNames[index], subAction);
				}
			}
			else if (action == "Select Subtitles") {
				List<string> subtitlesList = currentSubtitles.Select(t => t.name).ToList();
				subtitlesList.Insert(0, "None");

				string subAction = await ActionPopup.DisplayActionSheet("Select Subtitles", subtitleIndex + 1, subtitlesList.ToArray());

				if (subAction != "Cancel") {
					int setTo = subtitlesList.IndexOf(subAction) - 1;
					if (setTo != subtitleIndex) {
						subtitleIndex = setTo;
					}
				}
			}
			else if (action.StartsWith("Change Delay")) {
				int del = await ActionPopup.DisplayIntEntry("ms", "Subtitle Delay", 50, false, subtitleDelay.ToString(), "Set Delay");
				if (del != -1) {
					subtitleDelay = del;
				}
			}
			else if (action == $"Download Subtitles ({Settings.NativeSubLongName})") {
				PopulateSubtitle();
			}

		}

		public bool HasSupportForSubtitles() => currentVideo.episodeId.IsClean() && currentVideo.episodeId.StartsWith("tt");

		public void PopulateSubtitle(string lang = "", string name = "")
		{
			if (!HasSupportForSubtitles()) return;

			if (lang == "") {
				lang = Settings.NativeSubShortName;
				name = Settings.NativeSubLongName;
			}

			bool ContainsLang()
			{
				lock (subtitleMutex) {
					return currentSubtitles.Where(t => t.name == name).Count() != 0;
				}
			}

			if (ContainsLang()) {
				App.ShowToast("Subtitles already downloaded"); // THIS SHOULD NEVER HAPPEND
				return;
			}

			if (searchingForLang.ContainsKey(lang)) {
				App.ShowToast("Searching for subtitles");
				return;
			}
			searchingForLang[lang] = true;

			var thread = mainCore.CreateThread(6);
			mainCore.StartThread("PopulateSubtitles", () => {
				try {
					string data = mainCore.DownloadSubtitle(currentVideo.episodeId, lang, false, true);
					if (data.IsClean()) {
						if (!ContainsLang()) {
							lock (subtitleMutex) {
								VideoSubtitle s = new VideoSubtitle {
									name = name,
									subtitles = MainChrome.ParseSubtitles(data).ToArray()
								};
								currentSubtitles.Add(s);
								if (Settings.AutoAddInternalSubtitles) {
									subtitleIndex = currentSubtitles.Count - 1;
								}
							}
							App.ShowToast(name + " subtitles added");
						}
					}
					else {
						App.ShowToast(data == null ? "Connection error" : "No subtitles found");
					}
				}
				finally {
					if (searchingForLang.ContainsKey(lang)) {
						searchingForLang.Remove(lang);
					}
				}
			});
		}

		int currentSubtitleIndexInCurrent = 0;

		string subtitleTextFull; // BOTTOM

		double currentTime = 0;

		string lastSub = "";
		bool isUpdatingSubs = false;
		public int SubtitlesEmptyTime = Settings.SubtitlesEmptyTime;
		public const double SUBTITLE_ADD_TIME = 0;//1000;
		void UpdateSubtitles()
		{
			string comp = subtitleTextFull;
			if (lastSub == comp) {
				return;
			}
			lastSub = comp;

			Device.InvokeOnMainThreadAsync(async () => {
				while (isUpdatingSubs) {
					await Task.Delay(10);
				}
				isUpdatingSubs = true;
				SubtitleTxt1.Text = "";
				SubtitleTxt1BG.Text = "";
				await Task.Delay(SubtitlesEmptyTime);
				SubtitleTxt1.Text = subtitleTextFull;
				SubtitleTxt1BG.Text = subtitleTextFull;
				isUpdatingSubs = false;
			});
		}

		int last = 0;
		bool lastType = false;
		public void SubtitleLoop()
		{
			try {
				if (subtitleIndex == -1 || currentSubtitles[subtitleIndex].subtitles.Length <= 3) {
					// await Task.Delay(50);
					subtitleTextFull = "";
					UpdateSubtitles();
					return;
				}

				bool done = false;
				var Subtrack = currentSubtitles[subtitleIndex].subtitles;
				while (!done) {
					try {
						var track = Subtrack[currentSubtitleIndexInCurrent];
						var _currentTime = currentTime + (SUBTITLE_ADD_TIME + SubtitlesEmptyTime);
						// SET CORRECT TIME
						bool IsTooHigh()
						{
							//  print("::::::>>>>>" + currentSubtitleIndexInCurrent + "|" + currentTime + "<>" + track.fromMilisec);
							return _currentTime < Subtrack[currentSubtitleIndexInCurrent].StartTime + subtitleDelay && currentSubtitleIndexInCurrent > 0;
						}

						bool IsTooLow()
						{
							return _currentTime > Subtrack[currentSubtitleIndexInCurrent + 1].StartTime + subtitleDelay && currentSubtitleIndexInCurrent < Subtrack.Length - 1;
						}

						if (IsTooHigh()) {
							while (IsTooHigh()) {
								currentSubtitleIndexInCurrent--;
							}
							continue;
						}

						if (IsTooLow()) {
							while (IsTooLow()) {
								currentSubtitleIndexInCurrent++;
							}
							continue;
						}


						if (_currentTime < track.EndTime + subtitleDelay && _currentTime > track.StartTime + subtitleDelay) {
							if (last != currentSubtitleIndexInCurrent) {
								var lines = track.Lines;
								if (!Settings.SubtitlesClosedCaptioning) {
									lines = lines.Select(t => CloudStreamCore.RemoveCCFromSubtitles(t)).ToList();
								}

								foreach (var line in lines) {
									print("line:" + line);
								}

								if (lines.Count > 0) {
									subtitleTextFull = "";
									for (int i = 0; i < lines.Count; i++) {
										subtitleTextFull += lines[i] + (i == (lines.Count - 1) ? "" : "\n");
									}
								}
								UpdateSubtitles();
								lastType = false;
							}
						}
						else {
							if (!lastType) {
								subtitleTextFull = "";
								UpdateSubtitles();
								lastType = true;
							}
						}
						done = true;

						last = currentSubtitleIndexInCurrent;
						//  await Task.Delay(30);
					}
					catch {
						done = true;
					}
				}
			}
			catch (Exception) {
			}
		}

		const double lazyLoadOverProcentage = 0.70; // 70% compleated before it loads links
		readonly bool canLazyLoadNextEpisode = false;
		bool hasLazyLoadedNextEpisode = false;
		readonly bool hasLazyLoadSettingTurnedOn = Settings.LazyLoadNextLink;
		public long GetPlayerLenght()
		{
			if (Player == null || !isShown) {
				return -1;
			}
			else {
				return lastPlayerLenght;// Player.Length;
			}
		}

		bool IsNotValid()
		{
			return Player == null || !isShown;
		}

		public bool GetPlayerIsPlaying()
		{
			if (IsNotValid()) return false;
			return Player.IsPlaying; // Player.Time;
		}

		public bool GetPlayerIsSeekable()
		{
			if (IsNotValid()) return false;
			return isSeekeble; // Player.Time;
		}

		public bool GetPlayerIsPauseble()
		{
			if (IsNotValid()) return false;
			return isPausable; // Player.Time;
		}

		public long GetPlayerTime()
		{
			if (Player == null || !isShown) {
				return -1;
			}
			else {
				return lastPlayerTime; // Player.Time;
			}
		}

		long lastPlayerTime = 0;
		long lastPlayerLenght = 0;

		public bool isShowingSkip = false;

		long skipToEnd;

		public void PlayerTimeChanged(long time)
		{
			if (!isShown) return;


			try {
				SubtitleLoop();
			}
			catch (Exception _ex) {
				print("SUBLOOP FAILED:D" + _ex);
			}

			if (Player == null) {
				return;
			}
			if (!isShowingSkip) {
				var _baseSkips = skips.Where(t => t.timestamp < time).Where(t => t.timestamp + t.duration > time);
				if (_baseSkips.Count() > 0) {
					if (!App.currentVideoStatus.shouldSkip) {
						App.currentVideoStatus.shouldSkip = true;
						UpdateVideoStatus();
					}
				}
				else if (App.currentVideoStatus.shouldSkip) {
					App.currentVideoStatus.shouldSkip = false;
					UpdateVideoStatus();
				}


				var skip = _baseSkips.Where(t => !t.hasShown).ToArray();
				if (skip.Length > 0) {
					var _skip = skip[0];
					isShowingSkip = true;

					_ = Device.InvokeOnMainThreadAsync(async () => {
						skipToEnd = _skip.timestamp + _skip.duration;
						SkipSomething.IsEnabled = true;
						SkipSomething.IsVisible = !App.IsPictureInPicture || _skip.forceSkip;
						SkipSomething.Text = _skip.name.ToUpper();
						await Task.Delay(100);

						var skipBounds = SkipSomething.Bounds;
						var fadeToBounds = new Rectangle(skipBounds.X, skipBounds.Y, 2, skipBounds.Height);

						SkipSomething.Layout(fadeToBounds);
						SkipSomething.TranslationX = 0;
						_ = SkipSomething.FadeTo(1, easing: Easing.SinOut);
						_ = SkipSomething.LayoutTo(skipBounds, easing: Easing.SinOut);

						if (!_skip.forceSkip) {


							int delay = Math.Max((int)Math.Min(5000, _skip.duration), 300);
							for (int i = 0; i < delay / 100; i++) {
								if (GetPlayerTime() > skipToEnd || GetPlayerTime() < _skip.timestamp) break;
								await Task.Delay(100);
							}

						}
						else {
							HandleVideoAction(App.PlayerEventType.SkipCurrentChapter);
							await Task.Delay(500);
						}

						_ = SkipSomething.TranslateTo(100, 0, easing: Easing.SinOut);
						_ = SkipSomething.FadeTo(0, easing: Easing.SinOut);

						SkipSomething.IsEnabled = false;

						int index = skips.IndexOf(_skip);
						_skip.hasShown = true;
						skips[index] = _skip;

						await SkipSomething.LayoutTo(fadeToBounds, easing: Easing.SinOut);
						isShowingSkip = false;

						SkipSomething.Layout(skipBounds);
					});
				}
			}

			lastPlayerTime = time;
			double pro = ((double)(time / 1000) / (double)(lastPlayerLenght / 1000));
			if (canLazyLoadNextEpisode && !hasLazyLoadedNextEpisode && hasLazyLoadSettingTurnedOn) {
				if (pro > lazyLoadOverProcentage) {
					hasLazyLoadedNextEpisode = true;
					try {
						loadLinkForNext?.Invoke(currentVideo.episodeId, false);
					}
					catch (Exception) {
					}
				}
			}

			Device.BeginInvokeOnMainThread(() => {
				try {
					if (Player == null) {
						return;
					}
					if (!isShown) return;
					var len = GetPlayerLenght();

					double val = ((double)(time / 1000) / (double)(len / 1000));
					ChangeTime(val);
					currentTime = time;
					VideoSlider.Value = val;
					if (len != -1 && len > 10000 && len <= time + 1000) {
						Navigation.PopModalAsync();
					}
				}
				catch (Exception _ex) {
					print("EROROR WHEN TIME" + _ex);
				}
			});
		}

		readonly VisualElement[] lockElements;
		readonly VisualElement[] settingsElements;

		public static bool IsSingleMirror = false;

		void UpdateAudioDelay(long delay)
		{
			print("SETAUDIO:::: " + delay);

			if (Player == null) return;
			if (GetPlayerLenght() == -1) return;
			if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
			Player.SetAudioDelay(delay * 1000); // MS TO NS
		}

		void ShowVideoToast(string msg)
		{
			Device.BeginInvokeOnMainThread(() => {
				ToastLabel.Text = msg;
				ToastLabel.Opacity = 1;
				ToastLabel.AbortAnimation("FadeTo");
				ToastLabel.FadeTo(0, 700, Easing.SinIn);
			});
		}

		bool canStart = true;

		struct SkipMetadata
		{
			public string name;
			public long timestamp;
			public long duration;
			public bool forceSkip;
			public bool hasShown;
		}

		List<SkipMetadata> skips = new List<SkipMetadata>();

		readonly Func<Task> NextEpisodeClicked;
		public static PlayVideo lastVideo;

		/// <summary>
		/// Subtitles are in full
		/// </summary>
		/// <param name="urls"></param>
		/// <param name="name"></param>
		/// <param name="subtitles"></param>
		public VideoPage(PlayVideo video, int _maxEpisodes = 1)
		{
			try {
				print("Videoplage init");

				lastVideo = (PlayVideo)video.Clone();
				if (!canStart) {
					return;
				}
				canStart = false;

				isShown = true;
				changeFullscreenWhenPop = true;

				currentVideo = video;
				maxEpisodes = _maxEpisodes;
				IsSingleMirror = !video.isFromIMDB && video.isSingleMirror;
				Mirrors = new List<BasicLink>();
				if (currentVideo.MirrorNames != null) {
					for (int i = 0; i < currentVideo.MirrorNames.Count; i++) {
						Mirrors.Add(new BasicLink() {
							name = currentVideo.MirrorNames[i],
							baseUrl = currentVideo.MirrorUrls[i],
						});
					}
				}
				if (currentVideo.isFromIMDB) {
					LinkHolder? holder;
					if ((holder = GetCachedLink(currentVideo.episodeId)) != null) {
						Mirrors = holder.Value.links.Where(t => !t.canNotRunInVideoplayer).ToList();
					}
				}

				Mirrors = Mirrors.OrderBy(t => -t.priority).ToList();

				InitializeComponent();

				int skip = Settings.VideoPlayerSkipTime;
				SkipTime = skip * 1000;
				SkipForward.Text = "+" + skip;
				SkipBack.Text = "-" + skip;

				SkipForwardSmall.Text = skip.ToString();
				SkipBackSmall.Text = skip.ToString();
				print("Videoplage Start 5");

				SkipSomething.Clicked += (o, e) => {
					HandleVideoAction(App.PlayerEventType.SkipCurrentChapter);
				};

				// ======================= SUBTITLE SETUP =======================

				//double base1X = SubtitleTxt1.TranslationX;
				double base1Y = -10;
				SubtitleTxt1.TranslationY = base1Y;
				SubtitleTxt1BG.TranslationY = base1Y;

				bool hasDropshadow = Settings.SubtitlesHasDropShadow;

				string fontFam = App.GetFont(Settings.GlobalSubtitleFont);
				SubtitleTxt1.FontFamily = fontFam;
				SubtitleTxt1BG.FontFamily = fontFam;

				// ======================= END =======================

				LibVLCSharp.Shared.Core.Initialize();
				//NextMirror, NextMirrorBtt, 
				lockElements = new VisualElement[] { ChangeCropBtt, ChangeCrop, BacktoMain, GoBackBtt, EpisodeLabel, RezLabel, PausePlayClickBtt, PausePlayBtt, SkipForward, SkipForwardBtt, SkipForwardImg, SkipForwardSmall, SkipBack, SkipBackBtt, SkipBackImg, SkipBackSmall };
				settingsElements = new VisualElement[] { EpisodesTap, MirrorsTap, DelayTap, GoPipModeTap, SubTap, NextEpisodeTap, };
				VisualElement[] pressIcons = new VisualElement[] { LockTap, EpisodesTap, MirrorsTap, DelayTap, GoPipModeTap, SubTap, NextEpisodeTap };

				void SetIsLocked()
				{
					LockImg.Source = App.GetImageSource(isLocked ? LOCKED_IMAGE : UN_LOCKED_IMAGE);
					LockTxt.TextColor = Color.FromHex(isLocked ? "#516bde" : "#FFFFFF"); // 516bde 617EFF
																						 // LockTap.SetValue(XamEffects.TouchEffect.ColorProperty, Color.FromHex(isLocked ? "#617EFF" : "#FFFFFF"));
					LockTap.SetValue(XamEffects.TouchEffect.ColorProperty, Color.FromHex(isLocked ? "#99acff" : "#FFFFFF"));
					//LockTap.BackgroundColor = isLocked ? new Color(0.6, 0.67, 1, 0.1) : Color.Transparent;
					LockImg.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(isLocked ? MainPage.DARK_BLUE_COLOR : "#FFFFFF")) };
					VideoSlider.InputTransparent = isLocked;

					foreach (var visual in lockElements) {
						visual.IsEnabled = !isLocked;
						visual.IsVisible = !isLocked;
					}

					foreach (var visual in settingsElements) {
						visual.Opacity = isLocked ? 0 : 1;
						visual.IsEnabled = !isLocked;
					}

					if (!isLocked) {
						ShowNextMirror();
					}
				}

				SkipForward.TranslationX = TRANSLATE_START_X;
				SkipForwardImg.Source = App.GetImageSource("netflixSkipForward.png");
				SkipForwardBtt.TranslationX = TRANSLATE_START_X;
				Commands.SetTap(SkipForwardBtt, new Command(() => {
					if (!isShown) return;
					CurrentTap++;
					StartFade();
					HandleVideoAction(App.PlayerEventType.SeekForward);
					lastClick = DateTime.Now;
					SkipFor();
				}));

				SkipBack.TranslationX = -TRANSLATE_START_X;
				SkipBackImg.Source = App.GetImageSource("netflixSkipBack.png");
				SkipBackBtt.TranslationX = -TRANSLATE_START_X;
				Commands.SetTap(SkipBackBtt, new Command(() => {
					if (!isShown) return;
					CurrentTap++;
					StartFade();
					HandleVideoAction(App.PlayerEventType.SeekBack);
					lastClick = DateTime.Now;
					SkipBac();
				}));

				Commands.SetTap(LockTap, new Command(() => {
					isLocked = !isLocked;
					CurrentTap++;
					FadeEverything(false);
					StartFade();
					SetIsLocked();
				}));

				MirrorsTap.IsVisible = !currentVideo.isDownloadFile && AllMirrorsUrls.Count > 1;

				if (Settings.SUBTITLES_INVIDEO_ENABELD) {
					try {
						if (GlobalSubtitlesEnabled && HasSupportForSubtitles()) {
							PopulateSubtitle();
						}
					}
					catch (Exception _ex) {
						print("A_A__A__A:: " + _ex);
					}
				}

				SubTap.IsVisible = HasSupportForSubtitles() && Settings.SUBTITLES_INVIDEO_ENABELD;
				EpisodesTap.IsVisible = false;
				NextEpisodeTap.IsVisible = false;

				if (currentVideo.isDownloadFile) {
					var keys = Download.downloads.Keys;
					foreach (var key in keys) {
						var info = Download.downloads[key].info;
						if (info.source == currentVideo.headerId) {
							if (info.episode == currentVideo.episode + 1 && info.season == currentVideo.season) {
								NextEpisodeTap.IsVisible = true;

								NextEpisodeClicked = async () => {
									await Navigation.PopModalAsync(true);
									Download.PlayDownloadedFile(info, false);
								};

								Commands.SetTap(NextEpisodeTap, new Command(async () => {
									await NextEpisodeClicked();
								}));
								break;
							}
						}
					}
				}
				else {
					if (currentVideo.headerId == loadLinkValidForHeader) {
						if (canLoad?.Invoke(currentVideo.episodeId) ?? false) {
							NextEpisodeTap.IsVisible = true;
							canLazyLoadNextEpisode = true;

							NextEpisodeClicked = async () => {
								await loadLinkForNext?.Invoke(currentVideo.episodeId, true);
							};

							Commands.SetTap(NextEpisodeTap, new Command(async () => {
								await NextEpisodeClicked();
							}));
						}
					}
				}

				Commands.SetTap(MirrorsTap, new Command(async () => {
					string action = await ActionPopup.DisplayActionSheet("Mirrors", AllMirrorsNames.ToArray());//await DisplayActionSheet("Mirrors", "Cancel", null, AllMirrorsNames.ToArray());
					App.ToggleRealFullScreen(true);
					CurrentTap++;
					StartFade();
					print("ACTION = " + action);
					if (action != "Cancel") {
						for (int i = 0; i < AllMirrorsNames.Count; i++) {
							if (AllMirrorsNames[i] == action) {
								print("SELECT MIRR" + action);
								_ = SelectMirror(i);
							}
						}
					}
				}));

				Commands.SetTap(SubTap, new Command(async () => {
					await SubtitleOptions();
				}));

				Commands.SetTap(DelayTap, new Command(async () => {
					int action = await ActionPopup.DisplayIntEntry("ms", "Audio Delay", 50, false, App.GetDelayAudio().ToString(), "Set Delay");//await DisplayActionSheet("Mirrors", "Cancel", null, AllMirrorsNames.ToArray());
					print("MAINACTIONFROM : " + action);
					App.ToggleRealFullScreen(true);
					CurrentTap++;
					StartFade();
					if (action != -1) {
						App.SetAudioDelay(action);
						UpdateAudioDelay(action);
					}
				}));

				// ======================= SETUP ======================= 
				if (_libVLC == null) {
					_libVLC = new LibVLC();
				}
				if (_mediaPlayer == null) {
					_mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true, };
				}

				vvideo.MediaPlayer = _mediaPlayer; // = new VideoView() { MediaPlayer = _mediaPlayer };

				// ========== IMGS ==========
				// SubtitlesImg.Source = App.GetImageSource("netflixSubtitlesCut.png"); //App.GetImageSource("baseline_subtitles_white_48dp.png");
				//MirrosImg.Source = App.GetImageSource("baseline_playlist_play_white_48dp.png");
				AudioImg.Source = App.GetImageSource("AudioVolLow3.png"); // App.GetImageSource("baseline_volume_up_white_48dp.png");
				EpisodesImg.Source = App.GetImageSource("netflixEpisodesCut.png");
				// NextImg.Source = App.GetImageSource("baseline_skip_next_white_48dp.png");
				// BacktoMain.Source = App.GetImageSource("baseline_keyboard_arrow_left_white_48dp.png");
				// NextMirror.Source = App.GetImageSource("baseline_skip_next_white_48dp.png");
				SetIsLocked();
				// LockImg.Source = App.GetImageSource("wlockUnLocked.png");
				//SubtitleImg.Source = App.GetImageSource("outline_subtitles_white_48dp.png");


				//  GradientBottom.Source = App.GetImageSource("gradient.png");
				// DownloadImg.Source = App.GetImageSource("netflixEpisodesCut.png");//App.GetImageSource("round_more_vert_white_48dp.png");

				Player.AudioDevice += (o, e) => {
					HandleVideoAction(App.PlayerEventType.Pause);
				};
				Commands.SetTap(EpisodesTap, new Command(() => {
					//do something
				}));
				Commands.SetTap(ChangeCropBtt, new Command(() => {
					CurrentTap++;
					StartFade();
					Settings.VideoCropType++;
					if (Settings.VideoCropType > MAX_ASPECT_RATIO) {
						Settings.VideoCropType = 0;
					}
					UpdateAspectRatio((AspectRatio)Settings.VideoCropType, true);
				}));
				/*
				Commands.SetTap(NextMirrorBtt, new Command(() => {
					SelectNextMirror();
				}));*/

				//Commands.SetTapParameter(view, someObject);
				// ================================================================================ UI ================================================================================
				PausePlayBtt.Source = PAUSE_IMAGE;//App.GetImageSource(PAUSE_IMAGE);
												  //PausePlayClickBtt.set
				Commands.SetTap(PausePlayClickBtt, new Command(() => {
					//do something
					PausePlayBtt_Clicked(null, EventArgs.Empty);
					print("Hello");
				}));
				Commands.SetTap(GoBackBtt, new Command(() => {
					print("DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAdddddddddddAAAAAAAA");
					HandleVideoAction(App.PlayerEventType.Stop);
				}));

				Commands.SetTap(GoPipModeTap, new Command(() => {
					GoIntoPipMode();
				}));

				void SetIsPausedUI(bool paused)
				{
					Device.BeginInvokeOnMainThread(() => {
						PausePlayBtt.Source = paused ? PLAY_IMAGE : PAUSE_IMAGE;//App.GetImageSource(paused ? PLAY_IMAGE : PAUSE_IMAGE);
						PausePlayBtt.Opacity = 1;
						LoadingCir.IsVisible = false;
						BufferLabel.IsVisible = false;
						isPaused = paused;
						App.currentVideoStatus.isPaused = paused;
						UpdateVideoStatus();

						if (!isPaused) {
							UpdateAudioDelay(App.GetDelayAudio());
						}
					});
				}

				Player.Paused += (o, e) => {
					Device.BeginInvokeOnMainThread(() => {
						SetIsPausedUI(true);
						App.ReleaseAudioFocus();
						//   LoadingCir.IsEnabled = false;
					});
				};

				Player.Opening += (o, e) => {
					App.currentVideoStatus.isLoaded = false;
					UpdateVideoStatus();
				};


				void UpdateIcons()
				{
					Device.BeginInvokeOnMainThread(() => {
						int columCount = 0;
						for (int i = 0; i < pressIcons.Length; i++) {
							if (pressIcons[i].IsVisible) {
								Grid.SetColumn(pressIcons[i], columCount);
								columCount++;
							}
						}
					});
				}

				bool hasAddedSubtitles = false;
				Player.Playing += (o, e) => {
					SetIsPausedUI(false);
					UpdateAspectRatio((AspectRatio)Settings.VideoCropType);

					uint maxWidth = 0;
					uint maxHight = 0;
					//uint maxBitrate = 0;

					foreach (var track in Player.Media.Tracks) {
						switch (track.TrackType) {
							case TrackType.Audio:
								print("Audio track");
								print($"{nameof(track.Data.Audio.Channels)}: {track.Data.Audio.Channels}");
								print($"{nameof(track.Data.Audio.Rate)}: {track.Data.Audio.Rate}");
								break;
							case TrackType.Video:
								if (track.Data.Video.Width > maxWidth) {
									maxWidth = track.Data.Video.Width;
									maxHight = track.Data.Video.Height;
									//maxBitrate = track.Bitrate;
								}
								print("Video track");
								print($"{nameof(track.Data.Video.FrameRateNum)}: {track.Data.Video.FrameRateNum}");
								print($"{nameof(track.Data.Video.FrameRateDen)}: {track.Data.Video.FrameRateDen}");
								print($"{nameof(track.Data.Video.Height)}: {track.Data.Video.Height}");
								print($"{nameof(track.Data.Video.Width)}: {track.Data.Video.Width}");
								//print($"{nameof(track.Data.Video.Width)}: {track.Bitrate}");
								break;
						}
					}
					//+ (maxBitrate == 0 ? "" : $" {App.ConvertBytesToGB(maxBitrate, 1)}Kbps") // bitrate is not established on connect :/
					Device.BeginInvokeOnMainThread(() => {
						RezLabel.Text = maxWidth == 0 ? "" : (($"{maxWidth}x{maxHight}"));
					});

					var slaves = Player.Media.Slaves;
					if (slaves != null && slaves.Length > 0 && !hasAddedSubtitles) {
						for (int i = 0; i < slaves.Length; i++) {
							var slave = slaves[i];
							if (slave.Type == MediaSlaveType.Subtitle) {
								if (slave.Uri.StartsWith("file://") || slave.Uri.StartsWith("content://")) {
									string sub = App.ReadFile(CloudStreamCore.RemoveHtmlUrl(slave.Uri));
									if (sub.IsClean()) {
										hasAddedSubtitles = true;
										int internals = currentSubtitles.Where(t => t.name.StartsWith("Internal")).Count();
										currentSubtitles.Add(new VideoSubtitle() { subtitles = MainChrome.ParseSubtitles(sub).ToArray(), name = $"Internal{ (internals == 0 ? "" : " " + (internals + 1))}" });
										if (Settings.AutoAddInternalSubtitles) {
											subtitleIndex = currentSubtitles.Count - 1;
										}
									}
								}
							}
						}


						/*
                        Task.Run(async () => {
                            await Task.Delay(1000);
                            Device.BeginInvokeOnMainThread(() => {*/
						/*if (_mediaPlayer.SpuCount > 1) {
                            bool succ = _mediaPlayer.SetSpu(0);
                            print(succ);
                        }*/
						/*});
                    });*/
					}
					SubTap.IsVisible = currentSubtitles.Count > 0 || SubTap.IsVisible;
					UpdateIcons();
					// Player.NextFrame(); // GRAY FIX?
					App.currentVideoStatus.isLoaded = true;
					UpdateVideoStatus();

					skips = new List<SkipMetadata>();
					if (Settings.VideoPlayerShowSkip) {
						int chapterCount = Player.ChapterCount;
						if (chapterCount > 0) {
							var cc = Player.FullChapterDescriptions();
							foreach (var t in cc) { // SKIP INTRO
								if (t.Name.IsClean()) {
									var name = t.Name.ToLower();
									if (name.Contains("opening")) {
										skips.Add(new SkipMetadata() { duration = t.Duration, name = "Skip Opening", timestamp = t.TimeOffset });
									}
									else if (name.Contains("ending") || name == "end") {
										string _name = "Skip Credits";
										if (t.Duration + t.TimeOffset + 1000 > Player.Length && NextEpisodeClicked != null) {
											_name = "Next episode";
										}
										skips.Add(new SkipMetadata() { duration = t.Duration, name = _name, timestamp = t.TimeOffset });
									}
								}
							}
							print(cc);
						}

						if (NextEpisodeClicked != null) {
							skips.Add(new SkipMetadata() { duration = 7000, name = "Next episode", timestamp = Player.Length - 7000 }); // 7 sec before quit, but fade after 5
						}

						if (Settings.VideoPlayerSponsorblock) {
							bool autoSkip = Settings.VideoPlayerSponsorblockAutoSkipAds;
							try {
								if (video.headerId.Contains("youtube.com") || video.headerId.Contains("youtu.be")) {
									var id = YouTube.GetYTVideoId(video.headerId);
									if (id != "") {
										var segments = App.GetKey<List<YTSponsorblockVideoSegments>>("Sponsorblock", id, null);
										if (segments != null) {
											foreach (var seg in segments) {
												long start = (long)(seg.segment[0] * 1000);
												long end = (long)(seg.segment[1] * 1000);

												if (seg.category == "sponsor" || seg.category == "selfpromo") {
													skips.Add(new SkipMetadata() { duration = end - start, name = "Skip Ad", timestamp = start, forceSkip = autoSkip });
												}
												else if (seg.category == "intro") {
													skips.Add(new SkipMetadata() { duration = end - start, name = "Skip Intro", timestamp = start });
												}
												else if (seg.category == "outro") {
													skips.Add(new SkipMetadata() { duration = end - start, name = "Skip Outro", timestamp = start });
												}
												else if (seg.category == "music_offtopic") {
													skips.Add(new SkipMetadata() { duration = end - start, name = "Skip Music", timestamp = start });
												}
											}
										}
									}
								}
							}
							catch (Exception) {

							}

						}
					}
					isFirstLoadedMirror = false;

					UpdateAudioDelay(App.GetDelayAudio());
					Device.BeginInvokeOnMainThread(() => {
						HandleVideoAction(App.GainAudioFocus() ? App.PlayerEventType.Play : App.PlayerEventType.Pause);
					});
					//   LoadingCir.IsEnabled = false;
				};
				print("Videoplage Start 12");

				Player.TimeChanged += (o, e) => {
					PlayerTimeChanged(e.Time);
				};

				Player.LengthChanged += (o, e) => {
					lastPlayerLenght = e.Length;
				};

				Player.PausableChanged += (o, e) => {
					isPausable = e.Pausable == 1;
				};

				Player.SeekableChanged += (o, e) => {
					isSeekeble = e.Seekable == 1;
				};

				Player.Buffering += (o, e) => {
					Device.BeginInvokeOnMainThread(() => {
						if (e.Cache == 100) {
							try {
								if (Player != null) {
									SetIsPausedUI(!GetPlayerIsPlaying());
									UpdateAudioDelay(App.GetDelayAudio());
								}
							}
							catch (Exception) {

							}
						}
						else {
							//BufferBar.ProgressTo(e.Cache / 100.0, 50, Easing.Linear);
							BufferLabel.Text = "" + (int)e.Cache + "%";
							BufferLabel.IsVisible = true;
							PausePlayBtt.Opacity = 0;
							LoadingCir.IsVisible = true;
						}
					});

				};

				Player.EncounteredError += (o, e) => {
					error(_libVLC.LastLibVLCError);
					print("ERROR LOADING MDDD: ");
					ErrorWhenLoading();
				};

				_ = SelectMirror(currentVideo.preferedMirror);

				ShowNextMirror();

				GoPipModeTap.IsEnabled = Settings.PictureInPicture && App.FullPictureInPictureSupport;
				GoPipModeTap.IsVisible = GoPipModeTap.IsEnabled;

				UpdateIcons();
				print("Videoplage Start 13");

				App.currentVideoStatus.hasNextEpisode = NextEpisodeClicked != null;
				App.currentVideoStatus.shouldSkip = false;
				UpdateVideoStatus();
			}
			catch (Exception _ex) {
				error("Videoplayer: " + _ex);
			}

			//Player.AddSlave(MediaSlaveType.Subtitle,"") // ADD SUBTITLEs
		}

		bool isPausable = false;
		bool isSeekeble = false;

		void ShowNextMirror()
		{
			/*
			NextMirror.IsVisible = !IsSingleMirror;
			NextMirror.IsEnabled = !IsSingleMirror;
			NextMirrorBtt.IsVisible = !IsSingleMirror;
			NextMirrorBtt.IsEnabled = !IsSingleMirror;*/
		}

		async void ErrorWhenLoading()
		{
			print("ERROR UCCURED");
			if (currentVideo.isSingleMirror || currentVideo.isDownloadFile) {
				ShowVideoToast($"Error Loading");
			}
			else {
				ShowVideoToast($"Error Loading {CurrentBasicLink.name}");
			}
			if (currentVideo.isDownloadFile) {
				App.ShowToast("Error Loading Media");
				MainDispose();
				await Device.InvokeOnMainThreadAsync(async () => {
					App.ToggleRealFullScreen(false);
					await Navigation.PopModalAsync();
				});
			}
			else {
				SelectNextMirror();
			}
		}

		const int MAX_ASPECT_RATIO = 5;
		public enum AspectRatio
		{
			Original = 0,
			Fill = 1,
			BestFit = 2,
			FitScreen = 3,
			_16_9 = 4,
			_4_3 = 5,
		}

		private VideoTrack? GetVideoTrack(MediaPlayer mediaPlayer)
		{
			if (mediaPlayer == null) {
				return null;
			}
			var selectedVideoTrack = mediaPlayer.VideoTrack;
			if (selectedVideoTrack == -1) {
				return null;
			}

			try {
				var videoTrack = mediaPlayer.Media?.Tracks?.FirstOrDefault(t => t.Id == selectedVideoTrack);
				return videoTrack == null ? (VideoTrack?)null : ((MediaTrack)videoTrack).Data.Video;
			}
			catch (Exception) {
				return null;
			}
		}

		private bool IsVideoSwapped(VideoTrack videoTrack)
		{
			var orientation = videoTrack.Orientation;
			return orientation == VideoOrientation.LeftBottom || orientation == VideoOrientation.RightTop;
		}

		private AspectRatio? GetAspectRatio(MediaPlayer mediaPlayer)
		{
			if (mediaPlayer == null) {
				return null;
			}

			var aspectRatio = mediaPlayer.AspectRatio;
			return aspectRatio == null ?
				(mediaPlayer.Scale == 0 ? AspectRatio.BestFit : (mediaPlayer.Scale == 1 ? AspectRatio.Original : AspectRatio.FitScreen)) :
				(aspectRatio == "4:3" ? AspectRatio._4_3 : aspectRatio == "16:9" ? AspectRatio._16_9 : AspectRatio.Fill);
		}

		private void UpdateAspectRatio(AspectRatio? aspectRatio = null, bool showToast = false)
		{
			if (aspectRatio == null) {
				aspectRatio = GetAspectRatio(Player);
			}

			if (showToast) {
				string toast = aspectRatio switch {
					AspectRatio.Original => "Original",
					AspectRatio.Fill => "Fill",
					AspectRatio.BestFit => "Best Fit",
					AspectRatio.FitScreen => "Fit Screen",
					AspectRatio._16_9 => "16:9",
					AspectRatio._4_3 => "4:3",
					_ => "",
				};

				ShowVideoToast(toast);
			}
			//Player
			if (vvideo != null && Player != null) {
				switch (aspectRatio) {
					case AspectRatio.Original:
						Player.AspectRatio = null;
						Player.Scale = 1;
						break;
					case AspectRatio.Fill:
						var videoTrack = GetVideoTrack(Player);
						if (videoTrack == null) {
							break;
						}
						Player.Scale = 0;
						Player.AspectRatio = IsVideoSwapped((VideoTrack)videoTrack) ? $"{vvideo.Height}:{vvideo.Width}" :
							$"{vvideo.Width}:{vvideo.Height}";
						break;
					case AspectRatio.BestFit:
						Player.AspectRatio = null;
						Player.Scale = 0;
						break;
					case AspectRatio.FitScreen:
						videoTrack = GetVideoTrack(Player);
						if (videoTrack == null) {
							break;
						}
						var track = (VideoTrack)videoTrack;
						var videoSwapped = IsVideoSwapped(track);
						var videoWidth = videoSwapped ? track.Height : track.Width;
						var videoHeigth = videoSwapped ? track.Width : track.Height;
						if (track.SarNum != track.SarDen) {
							videoWidth = videoWidth * track.SarNum / track.SarDen;
						}

						var ar = videoWidth / (double)videoHeigth;
						var videoViewWidth = vvideo.Width;
						var videoViewHeight = vvideo.Height;
						var dar = videoViewWidth / videoViewHeight;

						var rawPixelsPerViewPixel = Device.Info.ScalingFactor;
						var displayWidth = videoViewWidth * rawPixelsPerViewPixel;
						var displayHeight = videoViewHeight * rawPixelsPerViewPixel;
						Player.Scale = (float)(dar >= ar ? (displayWidth / videoWidth) : (displayHeight / videoHeigth));
						Player.AspectRatio = null;
						break;
					case AspectRatio._16_9:
						Player.AspectRatio = "16:9";
						Player.Scale = 0;
						break;
					case AspectRatio._4_3:
						Player.AspectRatio = "4:3";
						Player.Scale = 0;
						break;
				}
			}
		}

		// =========================================================================== APP OPEN/CLOSE ===========================================================================


		public static PlayVideo showOnAppearPage;
		public static bool showOnAppear = false;
		public static bool CanReopen = false;
		public void HandleAppResume(object o, EventArgs e)
		{
			App.ToggleRealFullScreen(true);
		}

		public async void HandleAppExit(object o, EventArgs e)
		{
			if (!isShown) return;

			CanReopen = true;
			lastVideo.preferedMirror = currentMirrorId;

			await Navigation.PopModalAsync();
			MainDispose();
		}
		// =========================================================================== HANDLE CONTROLL ===========================================================================

		void GoIntoPipMode()
		{
			//HandleAppPipMode(null, true);
			App.PlatformDep.PictureInPicture();
		}

		public void HandleAction(object o, App.PlayerEventType e)
		{
			HandleVideoAction(e);
		}

		public void HandleAppPipMode(object o, bool e)
		{
			SubtitleTxt1.IsVisible = !e;
			SubtitleTxt1BG.IsVisible = !e;

			AllButtons.IsEnabled = !e;
			AllButtons.IsVisible = !e;
			if (!e) {
				App.ToggleRealFullScreen(true);
			}
		}

		/// <summary>
		/// CALLS ACTION WITH NO UI INTERACTION
		/// </summary>
		/// <param name="eventType"></param>
		async void HandleVideoAction(App.PlayerEventType eventType)
		{
			try {
				if (eventType == App.PlayerEventType.Stop) {
					await Navigation.PopModalAsync();
					return;
				}

				if (Player == null) return;
				if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
				if (GetPlayerLenght() == -1) return;

				switch (eventType) {
					case App.PlayerEventType.Pause:
						if (GetPlayerIsPauseble()) {
							Player.SetPause(true);
						}
						break;
					case App.PlayerEventType.Play:
						if (GetPlayerIsPauseble()) {
							Player.SetPause(false);
						}
						break;
					case App.PlayerEventType.PlayPauseToggle:
						Player.Pause();
						break;
					case App.PlayerEventType.NextMirror:
						SelectNextMirror();
						break;
					case App.PlayerEventType.PrevMirror:
						SelectPrevMirror();
						break;
					case App.PlayerEventType.SeekForward:
						SeekMedia(SkipTime);
						break;
					case App.PlayerEventType.SeekBack:
						SeekMedia(-SkipTime);
						break;
					case App.PlayerEventType.SkipCurrentChapter:
						await SkipCurrentCap();
						break;
					case App.PlayerEventType.NextEpisode:
						if (NextEpisodeClicked != null) {
							await NextEpisodeClicked();
						}
						break;
					default:
						break;
				}
			}
			catch (Exception) {
			}
		}

		async Task SkipCurrentCap()
		{
			var _time = GetPlayerTime();
			if ((Player != null || skipToEnd < 1000) && _time < skipToEnd) {
				var _len = GetPlayerLenght();
				if (_len < skipToEnd + 1000 && NextEpisodeClicked != null) {
					await NextEpisodeClicked();
				}
				else {
					Player.Time = skipToEnd + 1;
				}
			}
		}

		void SeekMedia(long ms)
		{
			if (!isShown) return;
			try {
				if (Player == null) return;
				if (Player.State == VLCState.Error) return;
				if (Player.State == VLCState.Opening) return;
				if (GetPlayerLenght() == -1) return;
				if (!GetPlayerIsSeekable()) return;
			}
			catch (Exception _ex) {
				print("ERRORIN TOUCH: " + _ex);
				return;
			}

			print("SEEK MEDIA to " + ms);
			var newTime = GetPlayerTime() + ms;

			var len = GetPlayerLenght();
			if (newTime > len) {
				newTime = len;
			}
			if (newTime < 0) {
				newTime = 0;
			}
			Player.Time = newTime;

			// SeekSubtitles(newTime);
			PlayerTimeChanged(newTime);
		}


		void SelectNextMirror()
		{
			int _currentMirrorId = currentMirrorId + 1;
			if (_currentMirrorId >= AllMirrorsUrls.Count) {
				_currentMirrorId = 0;
			}
			_ = SelectMirror(_currentMirrorId);
		}

		void SelectPrevMirror()
		{
			int _currentMirrorId = currentMirrorId - 1;
			if (_currentMirrorId < 0) {
				_currentMirrorId = AllMirrorsUrls.Count - 1;
			}
			_ = SelectMirror(_currentMirrorId);
		}

		bool isFirstLoadedMirror = true;
		Media disMedia;
		public async Task SelectMirror(int mirror)
		{
			if (!isShown) return;
			if (AllMirrorsUrls.Count <= mirror || mirror < 0) { // VALIDATE INPUT
				mirror = 0;
			}

			await Task.Delay(1);

			isPausable = false;
			isSeekeble = false;

			List<string> options = new List<string> {
				$"sub-track-id={int.MaxValue}"
			};

			long pos;
			bool startTimeSet = false;
			if (isFirstLoadedMirror) { // THIS IS TO MAKE IT USE DURATION KEY OVER LAST PLAYER POS
				if ((pos = App.GetViewPos(currentVideo.episodeId ?? "")) != -1) { // "tt7886180" "tt7886180"
					long duration = App.GetViewDur(currentVideo.episodeId ?? "");
					var pro = ((double)pos / (double)duration);
					if (pro < 0.9 && pro > 0.05) {
						startTimeSet = true;
						options.Add("start-time=" + (pos / 1000));
					}
				}
			}

			if (currentVideo.isDownloadFile) {
				EpisodeLabel.Text = CurrentDisplayName;
				disMedia = new Media(_libVLC, currentVideo.downloadFileUrl, FromType.FromPath, options.ToArray());
				bool succ = vvideo.MediaPlayer.Play(disMedia);
				return;
			}

			if (lastUrl == AllMirrorsUrls[mirror]) return;

			currentMirrorId = mirror;

			print("MIRROR SELECTED : " + CurrentMirrorUrl);
			if (CurrentMirrorUrl == "" || CurrentMirrorUrl == null) {
				print("ERRPR IN SELECT MIRROR");
				ErrorWhenLoading();
			}
			else {
				Device.BeginInvokeOnMainThread(() => {
					try {
						EpisodeLabel.Text = CurrentDisplayName;
						App.ToggleRealFullScreen(true);
					}
					catch (Exception) { }
				});

				bool Completed = ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(1000), () => {
					try {
						print("PLAY MEDIA " + CurrentMirrorUrl);
						options.Add("http-user-agent=" + USERAGENT);

						if (CurrentBasicLink.referer.IsClean()) {
							options.Add("http-referrer=" + CurrentBasicLink.referer);
						}
						if (!startTimeSet) {
							if (lastPlayerTime > 100 && (CurrentBasicLink.originSite ?? "") != "AnimeFever") {
								options.Add("start-time=" + (lastPlayerTime / 1000));
							}
						}

						disMedia = new Media(_libVLC, CurrentMirrorUrl, FromType.FromLocation, options.ToArray());
						lastUrl = CurrentMirrorUrl;
						if (CurrentBasicLink.IsSeperatedAudioStream) {
							uint prio = 4;
							foreach (var audioStream in CurrentBasicLink.audioStreams.OrderBy(t => -t.prio)) {
								disMedia.AddSlave(MediaSlaveType.Audio, prio, audioStream.url);
								if (prio != 0) {
									prio--;
								}
							}
						}

						if (!isShown) return;
						if (Player == null) return;
						bool succ = vvideo.MediaPlayer.Play(disMedia);
						if (Player.AudioTrackCount > 0) {
							Player.SetAudioTrack(0);
						}

						if (!succ) {
							ErrorWhenLoading();
						}
					}
					catch (Exception _ex) {
						print("EXEPTIOM: " + _ex);
					}
					//
					// Write your time bounded code here
					// 
				});
				print("COMPLEATED::: " + Completed);
			}
		}

		// =========================================================================== END HANDLE CONTROLL ===========================================================================


		public bool ForceReloadOnAppOpen = false;


		void UpdateVideoStatus()
		{
			App.OnVideoStatusChanged?.Invoke(null, EventArgs.Empty);
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			OnVideoAppear?.Invoke(null, true);
			App.currentVideoStatus.isInVideoplayer = true;
			UpdateVideoStatus();

			LibVLCSharp.Shared.Core.Initialize();

			print("ON APPEARING VIDEOPAGE");
			isShown = true;
			App.OnAudioFocusChanged += HandleAudioFocus;
			//App.OnAppNotInForground += HandleAppExit;
			//App.OnAppResume += HandleAppResume;
			App.OnPictureInPictureModeChanged += HandleAppPipMode;
			App.OnRemovePlayAction += HandleAction;

			try { // SETTINGS NOW ALLOWED 
				BrightnessProcentage = App.GetBrightness() * 100;
				print("BRIGHTNESS:::" + BrightnessProcentage + "]]]");
			}
			catch (Exception) {
				canChangeBrightness = false;
			}

			Volume = 100;
			Hide();
			App.LandscapeOrientation();
			App.ToggleRealFullScreen(true);
		}

		public static bool isShown = false;
		public static bool changeFullscreenWhenPop = true;
		protected override void OnDisappearing()
		{
			App.currentVideoStatus.isInVideoplayer = false;
			OnVideoAppear?.Invoke(null, false);

			UpdateVideoStatus();

			isShown = false;
			canStart = true;

			print("ONDIS:::::::");
			try {
				//App.OnAppNotInForground -= HandleAppExit;
				//App.OnAppResume -= HandleAppResume;
				App.OnAudioFocusChanged -= HandleAudioFocus;
				App.OnPictureInPictureModeChanged -= HandleAppPipMode;
				App.OnRemovePlayAction -= HandleAction;

				if (lastPlayerTime > 20 && lastPlayerLenght > 100) {
					string lastId = currentVideo.episodeId ?? currentVideo.episodeId;
					if (lastId != null) {
						long pos = lastPlayerTime;//Last position in media when player exited
						if (pos > -1) {
							App.SetViewPos(lastId, pos);
							print("ViewHistoryTimePos SET TO: " + lastId + "|" + pos);
						}
						long dur = lastPlayerLenght;//	long	Total duration of the media
						if (dur > -1) {
							App.SetViewDur(lastId, dur);
							print("ViewHistoryTimeDur SET TO: " + lastId + "|" + dur);
						}
					}
				}
				App.SaveData();
				MainDispose();

				// App.ShowStatusBar();
				if (changeFullscreenWhenPop) {
					App.NormalOrientation();
					App.ToggleRealFullScreen(false);
				}
				//App.ToggleFullscreen(!Settings.HasStatusBar);
				App.ForceUpdateVideo?.Invoke(null, EventArgs.Empty);

				try {
					//  Player.TryDispose();
				}
				catch (Exception _ex) {
					print("MAIN EX: in video :::: " + _ex);
				}
				print("STOPDIS::");
				// Player.Dispose();
			}
			catch (Exception _ex) {
				print("ERROR IN DISAPEERING" + _ex);
			}



			base.OnDisappearing();
		}

		void MainDispose()
		{
			try {
				if (currentVideo.isDownloadFile) {
					if (disMedia != null) {
						disMedia.Dispose();
					}
					Dispose();
				}
				else {
					_mediaPlayer.Stop();
					Dispose();
				}
			}
			catch (Exception) {

			}

		}

		public void Dispose()
		{
			Thread t = new Thread(() => {
				var mediaPlayer = _mediaPlayer;
				_mediaPlayer = null;
				mediaPlayer?.Dispose();
				_libVLC?.Dispose();
				_libVLC = null;
			}) {
				Name = "DISPOSETHREAD"
			};
			t.Start();
		}
		async void Hide()
		{
			await Task.Delay(100);
			App.HideStatusBar();
		}

		public void PausePlayBtt_Clicked(object sender, EventArgs e)
		{
			if (Player == null) return;
			if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
			if (GetPlayerLenght() == -1) return;
			if (!GetPlayerIsPauseble() && !isPaused) return;

			if (isPaused) { // UNPAUSE
				StartFade();
			}

			//Player.SetPause(true);
			if (!dragingVideo) {
				if (GetPlayerIsPlaying()) {
					Player.SetPause(true);
				}
				else {
					if (App.GainAudioFocus()) {
						Player.SetPause(false);
					}
				}
				//Player.Pause();
			}

		}

		// =========================================================================== LOGIC ====================================================================================================

		bool dragingVideo = false;
		private void VideoSlider_DragStarted(object sender, EventArgs e)
		{
			CurrentTap++;
			if (Player == null) return;
			if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
			if (GetPlayerLenght() == -1) return;
			if (!GetPlayerIsSeekable()) return;
			if (!GetPlayerIsPauseble()) return;

			Player.SetPause(true);
			dragingVideo = true;
			SlideChangedLabel.IsVisible = true;
			SlideChangedLabel.Text = "";
		}

		private void VideoSlider_DragCompleted(object sender, EventArgs e)
		{
			CurrentTap++;
			try {
				if (Player == null) return;
				if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
				if (!GetPlayerIsSeekable()) return;
				if (GetPlayerLenght() == -1) return;

				if (VideoSlider.Value >= 0.995) {
					HandleVideoAction(App.PlayerEventType.Stop);
				}
				else {
					bool skip = Math.Abs(lastTimeChange) > 10000;
					long len = (long)(VideoSlider.Value * GetPlayerLenght());
					if (skip) {
						Player.Time = len; // THIS WILL CAUSE FREEZ WHEN PLAYING CURRUPT VIDEO
										   //SeekSubtitles(len);
					}

					dragingVideo = false;
					SlideChangedLabel.IsVisible = false;
					SlideChangedLabel.Text = "";
					SkiptimeLabel.Text = "";
					StartFade();

					if (App.GainAudioFocus()) {
						HandleVideoAction(App.PlayerEventType.Play);
					}
				}
			}
			catch (Exception _ex) {
				print("ERROR DTAG: " + _ex);
			}
		}

		long lastTimeChange = 0;
		private void VideoSlider_ValueChanged(object sender, ValueChangedEventArgs e)
		{
			if (!isShown) return;
			if (Player == null) return;
			if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
			if (!GetPlayerIsSeekable()) return;
			if (GetPlayerLenght() == -1) return;

			ChangeTime(e.NewValue);
			if (dragingVideo) {
				lastTimeChange = (long)(VideoSlider.Value * GetPlayerLenght()) - GetPlayerTime();
				CurrentTap++;
				SlideChangedLabel.TranslationX = ((e.NewValue - 0.5)) * (VideoSlider.Width - 30); //+ StartTxt.WidthRequest/2;

				//    var time = TimeSpan.FromMilliseconds(timeChange);

				string before = ((Math.Abs(lastTimeChange) < 1000) ? "" : lastTimeChange > 0 ? "+" : "-") + ConvertTimeToString(Math.Abs(lastTimeChange / 1000)); //+ (int)time.Seconds + "s";

				SkiptimeLabel.Text = $"[{ConvertTimeToString(VideoSlider.Value * GetPlayerLenght() / 1000)}]";
				SlideChangedLabel.Text = before;//CloudStreamCore.ConvertTimeToString((timeChange / 1000.0));
			}
		}

		const int TRANSLATE_SKIP_X = 70;
		const int TRANSLATE_START_X = 200;

		async void SkipForAni()
		{
			SkipForwardImg.AbortAnimation("RotateTo");
			SkipForwardImg.AbortAnimation("ScaleTo");
			SkipForwardImg.Rotation = 0;
			_ = SkipForwardImg.ScaleTo(0.9, 100, Easing.SinInOut);
			await SkipForwardImg.RotateTo(90, 100, Easing.SinInOut);
			_ = SkipForwardImg.ScaleTo(1, 100, Easing.SinInOut);
			await SkipForwardImg.RotateTo(0, 100, Easing.SinInOut);
		}

		async void SkipBacAni()
		{
			SkipBackImg.AbortAnimation("RotateTo");
			SkipBackImg.AbortAnimation("ScaleTo");
			SkipBackImg.Rotation = 0;
			_ = SkipBackImg.ScaleTo(0.9, 100, Easing.SinInOut);
			await SkipBackImg.RotateTo(-90, 100, Easing.SinInOut);
			_ = SkipBackImg.ScaleTo(1, 100, Easing.SinInOut);
			await SkipBackImg.RotateTo(0, 100, Easing.SinInOut);
		}

		async void SkipFor()
		{
			SkipForward.AbortAnimation("TranslateTo");
			SkipForAni();

			SkipForward.Opacity = 1;
			SkipForwardSmall.Opacity = 0;
			SkipForward.TranslationX = TRANSLATE_START_X;

			await SkipForward.TranslateTo(TRANSLATE_START_X + TRANSLATE_SKIP_X, SkipForward.TranslationY, 200, Easing.SinOut);

			SkipForward.TranslationX = TRANSLATE_START_X;
			SkipForward.Opacity = 0;
			SkipForwardSmall.Opacity = 1;
		}


		async void SkipBac()
		{
			SkipBack.AbortAnimation("TranslateTo");
			SkipBacAni();

			SkipBack.Opacity = 1;
			SkipBackSmall.Opacity = 0;
			SkipBack.TranslationX = -TRANSLATE_START_X;

			await SkipBack.TranslateTo(-TRANSLATE_START_X - TRANSLATE_SKIP_X, SkipBack.TranslationY, 200, Easing.SinOut);

			SkipBack.TranslationX = -TRANSLATE_START_X;
			SkipBack.Opacity = 0;
			SkipBackSmall.Opacity = 1;
		}


		DateTime lastClick = DateTime.MinValue;

		public static int SkipTime = 10000;
		const float minimumDistance = 1;
		bool isMovingCursor = false;
		bool isMovingHorozontal = false;
		bool isMovingFromLeftSide = false;
		long isMovingStartTime = 0;
		long isMovingSkipTime = 0;


		double _Volume = 100;
		double Volume {
			set {
				_Volume = value; Player.Volume = (int)value;
			}
			get { return _Volume; }
		}

		int maxVol = 100;
		bool canChangeBrightness = true;
		double _BrightnessProcentage = 100;
		double BrightnessProcentage {
			set {
				_BrightnessProcentage = value; /*OverlayBlack.Opacity = 1 - (value / 100.0);*/ App.SetBrightness(value / 100);
			}
			get { return _BrightnessProcentage; }
		}
		TouchTracking.TouchTrackingPoint cursorPosition;
		TouchTracking.TouchTrackingPoint startCursorPosition;

		const uint fadeTime = 100;
		const int timeUntilFade = 3500;
		const bool CAN_FADE_WHEN_PAUSED = false;
		const bool WILL_AUTO_FADE_WHEN_PAUSED = false;
		bool isInAnimation = false;
		async void FadeEverything(bool disable, bool overridePaused = false)
		{
			if (isInAnimation) return;
			double fade = disable ? 0 : 1;

			if (TotalOpasity == fade) return;

			//Device.InvokeOnMainThreadAsync(async () => {
			if (isPaused && !overridePaused && CAN_FADE_WHEN_PAUSED) { // CANT FADE WHEN PAUSED
				return;
			}
			await Task.Delay(100);
			isInAnimation = true;
			print("FADETO: " + disable);
			AllButtons.IsEnabled = !disable;
			TotalOpasity = 1;
			_ = BlackBg.FadeTo(fade * 0.3);

			VideoSliderAndSettings.AbortAnimation("TranslateTo");
			_ = VideoSliderAndSettings.TranslateTo(VideoSliderAndSettings.TranslationX, disable ? 80 : 0, fadeTime, Easing.Linear);
			EpisodeLabel.AbortAnimation("TranslateTo");
			_ = EpisodeLabel.TranslateTo(EpisodeLabel.TranslationX, disable ? -60 : 20, fadeTime, Easing.Linear);

			RezLabel.AbortAnimation("TranslateTo");
			_ = RezLabel.TranslateTo(RezLabel.TranslationX, disable ? -60 : 40, fadeTime, Easing.Linear);

			SubHolder.AbortAnimation("TranslateTo");
			_ = SubHolder.TranslateTo(EpisodeLabel.TranslationX, disable ? 0 : -90, fadeTime, Easing.Linear);


			AllButtons.AbortAnimation("FadeTo");
			_ = AllButtons.FadeTo(fade, fadeTime);

			/*foreach (var item in AllButtons.Children) {
				if (item.ClassId != "NOFADE") {
					item.AbortAnimation("FadeTo");
					item.FadeTo(fade, fadeTime);
				}
			}*/

			await Task.Delay((int)fadeTime);
			isInAnimation = false;

			TotalOpasity = fade;
			//  });

			//    await AllButtons.FadeTo(, fadeTime, Easing.Linear);
		}

		async void StartFade(bool overridePause = false)
		{
			string _currentTap = CurrentTap.ToString();
			await Task.Delay(timeUntilFade);
			if (_currentTap == CurrentTap.ToString()) {
				if (!isPaused || WILL_AUTO_FADE_WHEN_PAUSED) {
					FadeEverything(true, overridePause);
				}
			}
		}

		int CurrentTap = 0;

		bool isLocked = false;

		double TotalOpasity { get; set; } = 1;//AllButtons.Opacity; } }

		DateTime lastRelease = DateTime.MinValue;
		DateTime startPressTime = DateTime.MinValue;

		private void TouchEffect_TouchAction(object sender, TouchTracking.TouchActionEventArgs args)
		{
			if (!isShown) return;
			print("TOUCH ACCTION0");
			bool lockActionOnly = false;

			void CheckLock()
			{
				if (Player == null) {
					lockActionOnly = true; return;
				}
				else if (GetPlayerTime() == -1) {
					lockActionOnly = true; return;
				}
				else if (GetPlayerLenght() == -1) {
					lockActionOnly = true; return;
				}
				else if (Player.State == VLCState.Error) {
					lockActionOnly = true; return;
				}
				else if (Player.State == VLCState.Opening) {
					lockActionOnly = true; return;
				}
				else if (!GetPlayerIsSeekable()) {
					lockActionOnly = true; return;
				}
			}

			try {
				CheckLock();
			}
			catch (Exception _ex) {
				print("ERRORIN TOUCH: " + _ex);
				lockActionOnly = true;
			}

			if (args.Type == TouchTracking.TouchActionType.Pressed || args.Type == TouchTracking.TouchActionType.Moved || args.Type == TouchTracking.TouchActionType.Entered) {
				CurrentTap++;
			}
			else if (args.Type == TouchTracking.TouchActionType.Cancelled || args.Type == TouchTracking.TouchActionType.Exited || args.Type == TouchTracking.TouchActionType.Released) {
				StartFade();
			}

			if (!isShown) return;


			// ========================================== LOCKED LOGIC ==========================================
			if (isLocked || lockActionOnly) {

				if (args.Type == TouchTracking.TouchActionType.Pressed) {
					startPressTime = System.DateTime.Now;
				}

				if (args.Type == TouchTracking.TouchActionType.Released && System.DateTime.Now.Subtract(startPressTime).TotalSeconds < 0.4) {

					if (TotalOpasity == 1) {
						FadeEverything(true);
					}
					else {
						FadeEverything(false);
					}
				}
				return;
			};

			CheckLock();
			if (lockActionOnly) return;

			long playerTime = GetPlayerTime();
			long playerLenght = GetPlayerLenght();
			if (!isShown) return;

			// ========================================== NORMAL LOGIC ==========================================
			if (args.Type == TouchTracking.TouchActionType.Pressed) {

				if (DateTime.Now.Subtract(lastClick).TotalSeconds < 0.25) { // Doubble click
					lastRelease = DateTime.Now;

					bool forward = (TapRec.Width / 2.0 < args.Location.X);
					SeekMedia(SkipTime * (forward ? 1 : -1));
					CurrentTap++;
					StartFade();
					if (forward) {
						SkipFor();
					}
					else {
						SkipBac();
					}
				}
				lastClick = DateTime.Now;

				FadeEverything(false);

				startCursorPosition = args.Location;
				isMovingFromLeftSide = (TapRec.Width / 2.0 > args.Location.X);
				isMovingStartTime = playerTime;
				isMovingSkipTime = 0;
				isMovingCursor = false;
				cursorPosition = args.Location;

				maxVol = Volume >= 100 ? 200 : 100;

			}
			else if (args.Type == TouchTracking.TouchActionType.Moved) {
				print(startCursorPosition.X - args.Location.X);
				if ((minimumDistance < Math.Abs(startCursorPosition.X - args.Location.X) || minimumDistance < Math.Abs(startCursorPosition.X - args.Location.X)) && !isMovingCursor) {
					// STARTED FIRST TIME
					isMovingHorozontal = Math.Abs(startCursorPosition.X - args.Location.X) > Math.Abs(startCursorPosition.Y - args.Location.Y);
					isMovingCursor = true;
				}
				else if (isMovingCursor) { // DRAGINS SKIPING TIME
					if (isMovingHorozontal) {
						double diffX = (args.Location.X - startCursorPosition.X) * 2.0 / TapRec.Width;
						isMovingSkipTime = (long)((playerLenght * (diffX * diffX) / 10) * (diffX < 0 ? -1 : 1)); // EXPONENTIAL SKIP LIKE VLC

						if (isMovingSkipTime + isMovingStartTime > playerLenght) { // SKIP TO END
							isMovingSkipTime = playerLenght - isMovingStartTime;
						}
						else if (isMovingSkipTime + isMovingStartTime < 0) { // SKIP TO FRONT
							isMovingSkipTime = -isMovingStartTime;
						}
						SkiptimeLabel.Text = $"{CloudStreamCore.ConvertTimeToString((isMovingStartTime + isMovingSkipTime) / 1000)} [{ (Math.Abs(isMovingSkipTime) < 1000 ? "" : (isMovingSkipTime > 0 ? "+" : "-"))}{CloudStreamCore.ConvertTimeToString(Math.Abs(isMovingSkipTime / 1000))}]";
					}
					else {
						if (isMovingFromLeftSide) {
							if (canChangeBrightness) {
								BrightnessProcentage -= (args.Location.Y - cursorPosition.Y) / 2.0;
								BrightnessProcentage = Math.Max(Math.Min(BrightnessProcentage, 100), 0); // CLAM
								SkiptimeLabel.Text = $"Brightness {(int)BrightnessProcentage}%";
							}
						}
						else {
							Volume -= (args.Location.Y - cursorPosition.Y) / 2.0;
							Volume = Math.Max(Math.Min(Volume, maxVol), 0); // CLAM
							SkiptimeLabel.Text = $"Volume {(int)Volume}%";
						}
					}

					cursorPosition = args.Location;

				}
			}
			else if (args.Type == TouchTracking.TouchActionType.Released) {
				if (isMovingCursor && isMovingHorozontal && Math.Abs(isMovingSkipTime) > 7000) { // SKIP TIME
																								 //FadeEverything(true);
					SkiptimeLabel.Text = "";

					if (Player != null) {
						SeekMedia(isMovingSkipTime - playerTime + isMovingStartTime);
					}
				}
				else {
					SkiptimeLabel.Text = "";
					isMovingCursor = false;
					if ((DateTime.Now.Subtract(lastClick).TotalSeconds < 0.25) && TotalOpasity == 1 && DateTime.Now.Subtract(lastRelease).TotalSeconds > 0.25) { // FADE WHEN REALEASED
						FadeEverything(true);
					}
				}
			}
			print("LEFT TIGHT " + (TapRec.Width / 2.0 < args.Location.X) + TapRec.Width + "|" + TapRec.X);
			print("TOUCHED::D:A::A" + args.Location.X + "|" + args.Type.ToString());
		}
	}
}