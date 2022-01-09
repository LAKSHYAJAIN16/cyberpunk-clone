using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Kalos.Main;
using Kalos.Data.Containers;
using Kalos.Speech.Localization;
using Kalos.Language.v1.NLP;
using Kalos.Language.v1.NLU;
using Kalos.Python.Communication;
using Kalos.Python.Objects;
using Kalos.Media.Images;
using Kalos.Media.Calender.Main;
using Kalos.Media.Calender.Creation.Management;
using Kalos.APICalls.Google.Youtube;
using Kalos.Utilities.General;
using Kalos.Utilities.Interfaces;
using Kalos.Utilities.Interfaces.Empty;
using Kalos.Utilities.Debug;
using Kalos.Responses.Main;
using Kalos.Brain.Emotions;
using Kalos.Serialization;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using PexelsDotNetSDK.Api;
using Newtonsoft.Json;
using TMPro;

namespace Kalos.Main
{
    public class Kalos_VirtualAssistant : MonoBehaviour , KMain , KMono
    {
        //Text GO
        public TextMeshProUGUI TextWIndow;

        //Image Player
        public RawImage ImageDisplay;

        //Video Player
        public UnityEngine.Video.VideoPlayer videoPlayer;

        //Audio Player
        public AudioSource AudioPlayer;

        //Textures
        public Texture Clouds, Haze, Clear;

        //Camera vars
        public static Camera cam;
        public static Color defColor;

        //Bool to know if we're playing music
        public static bool playing_music = false;

        //Last spoken dialougue
        private string LastSpokenDialougue = string.Empty;

        //Audio conversion
        private bool downloading = false;
        private const string URL_HEAD = "https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=";
        private const string URL_TAIL = "&tl=";

        /// <summary>
        /// These are modifiable attributes, which can be modified at will
        /// Adding or subtracting a element will influence algorithms.
        /// There is no limit to the values of the following
        /// </summary>
        public static readonly string[] Beginnings =
        {
           "Howdy there, Partner? There she blows",
           "And How are You doing, my fellow Human Friend",
           "Did you listen to Justin Bieber's new song? Its a BOP",
           "Side Step Right Left to My Beat, Get it, Let it, Roll!",
           "Cause I I I'm in the computer tonight!",
           "TOUCHDOWN! Welcome Back!",
           "How are you?"
        };
        public static readonly char[] Punctuation =
        {
           ',',
           ' ',
           '.',
           '?',
           '!',
           ':',
           ';'
         };
        public static readonly string[] StopWords =
        {
            "of",
            "in",
            "therefore",
            "the",
            "after",
            "‘re",
            "herein",
            "namely",
            "whoever",
            "sometime",
            "'m",
            "below",
            "a",
            "an",
            "is",
            "are",
            "so",
            "is",
            "am",
            "was",
            "will",
            "to",
            "do",
            "none"
        };
        public static readonly string[] Subjects =
        {
            "i",
            "me",
            "we",
            "us"
        };
        public static readonly string[] IndirectSubjects =
        {
            "myself",
            "ourself",
            "ourselfs",
            "myselfs",
            "ourselves",
            "myselves"
        };
        public static readonly string[] Quantities =
        {
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "zero"
        };
        public static readonly string[] Recievers =
        {
            "him",
            "her",
            "you",
            "they",
            "it"
        };
        public static readonly string[] IndirectRecievers =
        {
            "yourself",
            "himself",
            "ourself",
            "itself",
            "theirself",
            "theirselves",
            "itselfs",
            "yourselves",
            "himselves",
            "ourselves"
        };
        public static readonly string[] Prepositions =
        {
            "above",
            "up",
            "down",
            "into",
            "by",
            "ontop",
            "on",
            "your",
            "mine"
        };
        public static readonly string[] QuestionWords =
        {
            "where",
            "why",
            "how",
            "what",
            "who"
        };

        //List of Proper nouns
        public static string[] ProperNouns;

        //Language Corpus
        public static LanguageCorpus wordreserve;

        //Sentence Directory
        public static SentenceDirectory SentenceDirectory;

        //Calender
        private Kalos_Calender KC;
        private Kalos_Calender[] calas;

        //COmmunication module
        public Kalos_Communication Communication;

        private void Start()
        {
            //Stuff
            cam = Camera.main;
            defColor = Color.grey;

            if (/*!Kalos_Save.Saved(Application.persistentDataPath + "/Corpus.kalos")*/ true)
            {
                string[] a = { "cream", "love", "play", "eat", "say", "non", "minecraft", "like" };
                int[] b = { 1, 7, 7, 7, 7, 1, 1, 7 };
                wordreserve = new LanguageCorpus();
                wordreserve.PoS = b;
                wordreserve.words = a;
                Kalos_Save.SaveLanguageData(wordreserve, Application.persistentDataPath + "/Corpus.kalos");
            }

            //Proper nouns
            ProperNouns = Kalos_Save.GetTextData(@"C:\Users\GAMING\Desktop\MORE GAMES AAH\The silent slayer\Assets\Scenes\ListOfProperNouns.txt");

            //Begin Conversation
            BeginConversation();
        }

        private void BeginConversation()
        {
            //Define sentence
            string sentence = string.Empty;

            //Get Time of day and add it
            sentence += Kalos_Utils.GetTimeOfDay();

            //Then choose random beginning and add it
            string bg = " " + Beginnings[UnityEngine.Random.Range(0, Beginnings.Length)];
            sentence += bg;

            //Assign last spoken dialougue
            LastSpokenDialougue = sentence;

            //Write sentence and generate audio
            StartCoroutine(WriteSentence(sentence));
            GenerateAudio(sentence);

            //Subscribe to The Delegate
            Communication.OnDataRecieved += OnPythonDataRecieved;
        }

        private void OnPythonDataRecieved(int data, string data_string, object sender){
            KDebug.Log(data_string);
        }

        public void Speak(string text)
        {
            //Then, seperate the words, so that we don't have any punctuation
            string[] bytes = text.Split(' ');

            //Generate reponse
            Kalos_Response rs = new Kalos_Response(LastSpokenDialougue, text, bytes, this, videoPlayer, TextWIndow, AudioPlayer, ImageDisplay);
            rs.AssignMain(this);
            string c = rs.GetResponse();

            //If response is null, search
            if (c == null)
            {
                StartCoroutine(ForceSearchResponse(text));
                c = "Searching";
            }

            //Speak dialougue
            StopAllCoroutines();
            StartCoroutine(WriteSentence(c));
            GenerateAudio(c);

            //If its an audio, well
            if (Kalos_Response.isAudio)
            {
                playing_music = true;
                StartCoroutine(ChangeColor());
            }

            //If it not an audio, well
            if (!Kalos_Response.isAudio)
            {
                playing_music = false;
                cam.backgroundColor = Color.grey;
                StopCoroutine(ChangeColor());
            }

            List<Token> tok = Kalos_NaturalProccesingFramework.ConvertToTokens(text);
            List<Token> fgg = Kalos_NaturalProccesingFramework.FilterStopWords(tok);
            Token[] g = Kalos_NaturalUnderstandingFrameWork.ApplyPartsOfSpeech(fgg);
            Token[] audsiau = Kalos_NaturalUnderstandingFrameWork.IdentifyLivingEntities(g);
            Dictionary<string, Token> hi = Kalos_NaturalUnderstandingFrameWork.CreateRelations(audsiau);

            foreach (KeyValuePair<string, Token> uwu in hi){
                KDebug.Log(uwu.Key);
            }

            bool ga = Kalos_NaturalUnderstandingFrameWork.ProfanityCheck(g);
            KDebug.Log(Kalos_NaturalUnderstandingFrameWork.FindTaggedWords(audsiau));
            Communication.Send(JsonConvert.SerializeObject(new PythonData{
                Message = text,
                Intent = "nlp"
            }));

            if (Input.location.isEnabledByUser) 
            {
                User Lakshya = new User("Lakshya", "13", "Male");
                KC = new Kalos_Calender(Lakshya, this);
                KC.AddEvent(Lakshya, DateTime.Today, "test", "dis just a test", this);
                List<Kalos_Calender> cals = new List<Kalos_Calender>();
                cals.Add(KC);
                calas = cals.ToArray();
                string json = Kalos_Calender_Manager.SimpleToJSONUnity(calas);
                KDebug.Log(json);
                Kalos_Save.SaveString("json", json) ;

                string js = PlayerPrefs.GetString("json");
                calas = Kalos_Calender_Manager.SimpleFromJSONUnity(js);
            }
        }

        public void SpeakDirect(string text)
        {
            StartCoroutine(WriteSentence(text));
            GenerateAudio(text);
        }

        private void UpdateText(string a)
        {
            TextWIndow.text = a;
        }

        private void GenerateAudio(string s)
        {
#pragma warning disable CS0618
            //Get Input string
            string input = WWW.EscapeURL(s);

            //If we are downloading, return.Else just serialize the input
            if (downloading) return;
            StartCoroutine(SerializeAudio(input));

#pragma warning restore CS0618
        }

        private IEnumerator WriteSentence(string s)
        {
            string d = "";
            char[] ch = s.ToCharArray();
            foreach (char c in ch)
            {
                d += c;
                UpdateText(d);
                yield return null;
            }
        }

        private IEnumerator SerializeAudio(string ID)
        {
#pragma warning disable CS0618
            //Set downloading to true
            downloading = true;

            //Get URL
            string URL = URL_HEAD + ID + URL_TAIL + Kalos_LocalizationModule.currentLC;

            //Create Doc and wait for it to load
            WWW www = new WWW(URL);
            yield return www;

            //Get AudioClip
            AudioClip a = www.GetAudioClip(false, true, AudioType.MPEG);
            AudioSource aV = this.transform.GetComponent<AudioSource>();
            aV.clip = a;
            aV.Play();

            //Set downloading to false
            downloading = false;
#pragma warning restore CS0618
        }

        private IEnumerator ForceSearchResponse(string Input)
        {
            //First,open URL
            const string HEAD = "https://www.bing.com/search?q=";
            string url = HEAD + Input.ToLower();
            Application.OpenURL(url);

            //Then, create WebRequest
            UnityWebRequest buffer = UnityWebRequest.Get(url);
            yield return buffer.SendWebRequest();
            UnityEngine.Debug.Log(buffer.downloadHandler.text);
        }

        public static IEnumerator ChangeColor()
        {
            Color[] cols = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };
            while (playing_music)
            {
                yield return new WaitForSeconds(2f);
                cam.backgroundColor = cols[UnityEngine.Random.Range(0, cols.Length)];
            }
        }
    }
}

namespace Kalos.Responses.Main
{
    [Serializable]
    public class Kalos_Response : KResponse
    {
        public string lastSaid, Input;
        public string[] words;
        public UnityEngine.Video.VideoPlayer videoPlayer;
        public RawImage ImageDisplay;
        public AudioSource musicPlayer;
        public Texture cloud, haze, clear;
        public TextMeshProUGUI textDisplay;
        public Kalos_VirtualAssistant Main;
        public static bool isAudio = false;

        //Logoff DLL
        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        //Lock DLL
        [DllImport("user32")]
        public static extern void LockWorkStation();

        //Sleep dill
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        //Gets window
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

        //Sends message to window
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        /// <summary>
        /// Constructor for response
        /// Sets the words, last said word and Text game Object
        /// </summary>
        public Kalos_Response(string ls, string ina, string[] ws, Kalos_VirtualAssistant main, UnityEngine.Video.VideoPlayer vid, TextMeshProUGUI text, AudioSource aud, RawImage image)
        {
            this.lastSaid = ls;
            this.Main = main;
            this.Input = ina;
            this.words = ws;
            this.videoPlayer = vid;
            this.textDisplay = text;
            this.musicPlayer = aud;
            this.cloud = Main.Clouds;
            this.haze = Main.Haze;
            this.clear = Main.Clear;
            this.ImageDisplay = image;
        }

        /// <summary>
        /// Return Response after checking all callbacks
        /// Is called from Main Thread
        /// </summary>
        public string GetResponse()
        {
            //Set Image display to false
            ImageDisplay.gameObject.SetActive(false);

            //First, try to see if we've hard coded it
            string x = ForceCallBackResponse();

            //if we have, return that, else go and search manually
            if (x != null) return x;

            //Get array of Tokens, and filter them
            List<Token> tokens = Kalos_NaturalProccesingFramework.ConvertToTokens(Input);
            List<Token> filteredTokens = Kalos_NaturalProccesingFramework.FilterStopWords(tokens);

            //Convert it to a string array, since its a simple one layered check
            string[] strings = Kalos_Utils.TokensToStringArray(filteredTokens.ToArray());
            words = strings;

            //Try for Tasks
            string task = ForceTaskResponse(strings);

            //If it is a task, return the task, else, search it
            if (task != null) return task;

            return null;
        }

        /// <summary>
        /// Used for 'Chat bot' type behavior
        /// Contains some hard coded prompts which the computer evaluates
        /// </summary>
        public string ForceCallBackResponse()
        {
            //Force hard callbacks.This gives the sort of chatbot feel
            string buffer = Input.ToLower();
            buffer = buffer.Trim(Kalos_VirtualAssistant.Punctuation);

            //Switch buffer
            return buffer switch
            {
                "what your name" => "Its Kalos.With a K",
                "what is your name" => "Its Kalos.With a K",
                "how are you" => "I am fine",
                "where do you live" => "In this computer. Though I quite Like it in here",
                "what is your gender" => "Computer.NOICE",
                "who is the best minecraft player" => "Its Fruitberries.Everyone knows that",
                "what is the worst minecraft version" => "Its 1.8. Litterally the worst.",
                "why did the chicken cross the road" => "It's simple : He was hungry. Ha ha ha ha ha ",
                "when is your birthday" => "Its when you bought this computer",
                "what is your mom's name" => "Idduno, never thought about it",
                "are you smart" => "No. I can't even do 1+1.Is it 11?",
                "are you american" => "Bro.What.",
                "what's a plus b" => "Its an ab.",
                "can computer's laugh" => "Of course. Ha Ha Ha Ha Ha Ha Ha",
                "fine" => "I am Fine too",
                "what america" => "Country",
                "debug"=>"Yes",
                _ => null,
            };
        }

        /// <summary>
        /// Main Function
        /// Cycles through all the threads
        /// </summary>
        public string ForceTaskResponse(string[] tkens)
        {
            //Now loop over and find keywords
            int it = 0;
            foreach (string q in tkens)
            {
                string a = ForceVideoResponse(it, Input);
                string c = ForceMathematicalResponse(it);
                string e = ForceImageResponse(it);
                string f = ForceOpenApplicationResponse(it);
                string g = ForceSystemResponse(it);
                string h = ForceTypeResponse(it);

                if (a != null) return a;
                if (c != null) return c;
                if (e != null) return e;
                if (f != null) return f;
                if (g != null) return g;
                if (h != null) return h;

                it++;
            }

            //If it not a command, google it
            return null;
        }

        /// <summary>
        /// Used for video playing
        /// Calls the video buffer struct
        /// </summary>
        public string ForceVideoResponse(int i, string whole)
        {
            //Get search target
            if (words[i] != "play") return null;
            string faS = whole.ToLower();
            string st = faS.Replace("play", "");

            //Return youtube search
            SearchYoutube(st, 4, YoutubeSearchType.video, 0);
            return "Playing....";
        }

        /// <summary>
        /// Used for Mathematical functions
        /// </summary>
        public string ForceMathematicalResponse(int i)
        {
            try
            {
                //Get operator and numbers
                string o = words[i];
                string a = words[i - 1];
                string b = words[i + 1];

                //Get floating point numbers to apply operators
                float f = float.Parse(a);
                float f1 = float.Parse(b);

                switch (o)
                {
                    case "+":
                        return "Its " + (f + f1).ToString();

                    case "-":
                        return "Its " + (f - f1).ToString();

                    case "times":
                        return "Its " + (f * f1).ToString();

                    case "by":
                        return "Its " + (f / f1).ToString();

                    default:
                        return null;
                }
            }

            catch(IndexOutOfRangeException)
            {
                return null;
            }

            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Stuff like Shutdown, Sleep, etc.
        /// </summary>
        public string ForceSystemResponse(int i)
        {
            string buffer = words[i].ToLower();

            switch (buffer)
            {
                case "shutdown":
                    Process.Start("shutdown", "/s /t 0");
                    return "Shutting Down.Goodbye!";

                case "restart":
                    Process.Start("shutdown", "/r /t 0");
                    return "Restarting.Goodbye!";

                case "logoff":
                    ExitWindowsEx(0, 0);
                    return "Logging off.Goodbye!";

                case "lock":
                    LockWorkStation();
                    return "Locking computer.Goodbye!";

                case "sleep":
                    SetSuspendState(false, true, true);
                    return "Going to sleep.Goodbye!";

                //Dual words
                case "shut":
                    bool a = ContainsSuccessor(i, "down");
                    if (a)
                    {
                        Process.Start("shutdown", "/s /t 0");
                        return "Shutting Down.Goodbye!";
                    }

                    return "What to shut, the door";
            }

            return null;
        }

        /// <summary>
        /// Opens Application
        /// </summary>
        public string ForceOpenApplicationResponse(int i)
        {
            //Get application name
            try
            {
                string appBuffer = words[i + 1].ToLower();

                switch (appBuffer)
                {
                    //Notepad
                    case "notepad":
                        Process.Start(@"C:\Windows\System32\Notepad.exe");
                        return "Opening notepad";

                    case "note":
                        bool a = ContainsSuccessor(i, "pad");
                        if (a)
                        {
                            Process.Start(@"C:\Windows\System32\Notepad.exe");
                            return "Opening notepad";
                        }

                        return "What note?";

                    //File Explorer
                    case "fileexplorer":
                        Process.Start(@"C:\Windows\explorer.exe");
                        return "Opening File Explorer";

                    case "file":
                        bool b = ContainsSuccessor(i, "explorer");
                        if (b)
                        {
                            Process.Start(@"C:\Windows\explorer.exe");
                            return "Opening File Explorer";
                        }

                        return "What file?";

                    //paint
                    case "paint":
                        Process.Start(@"C:\Windows\System32\mspaint.exe");
                        return "Opening MS Paint";

                    case "optifine":
                        Process.Start(@"D:\OptiFine_1.12.2_HD_U_E3.jar");
                        return "Opening Optifine";


                    case "minecraft":
                        bool ba = ContainsSuccessor(i, "launcher");
                        if (ba)
                        {
                            Process.Start(@"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe");
                            return "Opening Minecraft";
                        }
                        return "Open what Minecraft?";

                    //Minecraft Launcher
                    case "minecraftlauncher":
                        Process.Start(@"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe");
                        return "Opening Minecraft";
                }

                return null;
            }

            catch (IndexOutOfRangeException)
            {
                return null;
            }

            catch (Win32Exception)
            {
                return "I am sorry. I am not able to find the requested file.";
            }
        }

        ///<summary>
        ///Types text to notepad file
        ///</summary>
        public string ForceTypeResponse(int i)
        {
            //Gets text to type
            if (words[i] != "type") return null;
            string typeBuffer = words[i].Replace("type", "");

            //Get array of proccesses, i.e the amount of notepad widows opened
            Process[] notepads = Process.GetProcessesByName("notepad");

            //If its length is zero, return 
            if (notepads.Length == 0) return "Where to type, master? No notepad is open.";

            //If it is not, Get desired Notepad
            Process n = notepads[0];
            IntPtr child = FindWindowEx(n.MainWindowHandle, new IntPtr(1), "Edit", null);

            SendMessage(child, 0x000C, 0, typeBuffer);
            SendMessage(child, 0x000c, 1, typeBuffer);
            return typeBuffer;
        }
        ///<summary>
        ///Calls Image APi
        ///</summary>
        public string ForceImageResponse(int i)
        {
            //Get search target
            if (words[i] != "show") return null;
            string faS = Input.ToLower();
            string st = faS.Replace("show", "");

            //Call method
            SearchImage(st);
            return "Displaying.....Just One Second";
        }

        /// <summary>
        /// Checks for Successor and predessesor
        /// Useful for stuff like "shut down" and "shutdown"
        /// </summary>
        public bool ContainsSuccessor(int i, string r)
        {
            try
            {
                bool x = words[i + 2].ToLower() == r;
                return true;
            }

            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        public bool ContainsPredecessor(int i, string r)
        {
            try
            {
                bool x = words[i - 2].ToLower() == r;
                return true;
            }

            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Searches YOutube Video using API
        /// </summary>
        public async void SearchYoutube(string g, long num, YoutubeSearchType f, int index)
        {
            UnityEngine.Debug.Log("Called dis thread");
            string head = string.Empty;

            head = f switch
            {
                YoutubeSearchType.video => "https://www.youtube.com/watch?v=",
                YoutubeSearchType.playlist => "https://www.youtube.com/watch?v=",
                YoutubeSearchType.channels => "https://www.youtube.com/channel/",
                _ => "https://www.youtube.com/watch?v=",
            };

            string fg = await Kalos_YoutubeAPI.SingleQuery(g, num, f, index);
            Application.OpenURL(head + fg);
        }

        /// <summary>
        /// Searches for Image using API
        /// </summary>
        public async void SearchImage(string f)
        {
            string url = await Kalos_Images.SearchForPhotoAsync(f);
            Texture2D tex = await Kalos_Images.ExtractImageAsync(new Uri(url));

            ImageDisplay.gameObject.SetActive(true);
            ImageDisplay.texture = tex;
        }

        /// <summary>
        /// Assigns Main thread
        /// </summary>
        public void AssignMain(Kalos_VirtualAssistant main){
            this.Main = main;
        }
    }
}

namespace Kalos.APICalls.Google
{
    namespace Youtube
    {
        /// <summary>
        /// Uses Youtube API to return a link to the video
        /// </summary>
        public struct Kalos_YoutubeAPI
        {
            //API KEy
            public const string YOUTUBE_APIKEY = "AIzaSyDo2gu4qY86psJMyK8hJncNwFZjc2yqbYc";

            public static async Task<string> SingleQuery(string query, long maxResults, YoutubeSearchType searchType, int searchIndex)
            {
                //Initialize API client with key
                BaseClientService.Initializer client = new BaseClientService.Initializer();
                client.ApiKey = YOUTUBE_APIKEY;
                client.ApplicationName = Application.productName;

                //Create service
                YouTubeService service = new YouTubeService(client);

                //Initialize SearchRequest
                var searchRequest = service.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.MaxResults = maxResults;

                //Await the result
                var response = await searchRequest.ExecuteAsync();

                //Create list, of targets
                List<string> targets = new List<string>();

                //Loop through and assign
                foreach (var result in response.Items)
                {
                    //Add a result according to the type
                    if (result.Id.Kind == "youtube#video" && searchType == YoutubeSearchType.video) targets.Add(result.Id.VideoId);
                    else if (result.Id.Kind == "youtube#channel" && searchType == YoutubeSearchType.channels) targets.Add(result.Id.VideoId);
                    else if (result.Id.Kind == "youtube#playlist" && searchType == YoutubeSearchType.playlist) targets.Add(result.Id.VideoId);
                }

                //Return single result
                return targets[searchIndex];
            }

            public static async Task<string> FirstQuery(string query, long maxResults, YoutubeSearchType searchType)
            {
                //Initialize API client with key
                BaseClientService.Initializer client = new BaseClientService.Initializer();
                client.ApiKey = YOUTUBE_APIKEY;
                client.ApplicationName = Application.productName;

                //Create service
                YouTubeService service = new YouTubeService(client);

                //Initialize SearchRequest
                var searchRequest = service.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.MaxResults = maxResults;

                //Await the result
                var response = await searchRequest.ExecuteAsync();

                //Create list, of targets
                List<string> targets = new List<string>();

                //Loop through and assign
                foreach (var result in response.Items)
                {
                    //Add a result according to the type
                    if (result.Id.Kind == "youtube#video" && searchType == YoutubeSearchType.video) targets.Add(result.Id.VideoId);
                    else if (result.Id.Kind == "youtube#channel" && searchType == YoutubeSearchType.channels) targets.Add(result.Id.VideoId);
                    else if (result.Id.Kind == "youtube#playlist" && searchType == YoutubeSearchType.playlist) targets.Add(result.Id.VideoId);
                }

                //Return single result
                return targets[0];
            }

            public static async Task<List<string>> AllQuerys(string query, long maxResults, YoutubeSearchType searchType)
            {
                //Initialize API client with key
                BaseClientService.Initializer client = new BaseClientService.Initializer();
                client.ApiKey = YOUTUBE_APIKEY;
                client.ApplicationName = Application.productName;

                //Create service
                YouTubeService service = new YouTubeService(client);

                //Initialize SearchRequest
                var searchRequest = service.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.MaxResults = maxResults;

                //Await the result
                var response = await searchRequest.ExecuteAsync();

                //Create list, of targets
                List<string> targets = new List<string>();

                //Loop through and assign
                foreach (var result in response.Items)
                {
                    //Add a result according to the type
                    if (result.Id.Kind == "youtube#video" && searchType == YoutubeSearchType.video) targets.Add(result.Id.VideoId);
                    else if (result.Id.Kind == "youtube#channel" && searchType == YoutubeSearchType.channels) targets.Add(result.Id.VideoId);
                    else if (result.Id.Kind == "youtube#playlist" && searchType == YoutubeSearchType.playlist) targets.Add(result.Id.VideoId);
                }

                //Return single result
                return targets;
            }
        }

        public enum YoutubeSearchType
        {
            video,
            playlist,
            channels
        }
    }
}

namespace Kalos.Language.v1.NLP
{
    public struct Kalos_NaturalProccesingFramework
    {
        /// <summary>
        /// Converts text to tokens
        /// </summary>
        public static List<Token> ConvertToTokens(string c)
        {
            //Make it lower case.
            string a = c.ToLower();

            //Check for special characters, like question, exlamation, etc
            string key = "statement";
            if (a.Contains("?")) key = "question";
            else if (a.Contains("!")) key = "exlamation";

            //Split punctiation.This makes it clear what we are focusing on
            string[] items = a.Split(Kalos_VirtualAssistant.Punctuation);

            //Return tokens for every string
            List<Token> tokenList = new List<Token>();
            foreach (string x in items)
            {
                //Create Token and add it to the list
                string af = x.Trim(Kalos_VirtualAssistant.Punctuation);
                Token y = new Token(af, key);
                tokenList.Add(y);
            }

            return tokenList;
        }

        ///<summary>
        ///Filters stop words like am, a, the in normal language
        ///This optimizes our performance
        ///</summary>        
        public static List<Token> FilterStopWords(List<Token> c)
        {
            //Get stop words
            string[] z = Kalos_VirtualAssistant.StopWords;

            //Cycle through all the Tokens in the List and Remove stop words
            Token[] Tokens = c.ToArray();
            List<Token> returnTokens = new List<Token>();
            bool commited = false;
            foreach (Token x in Tokens)
            {
                commited = false;
                foreach (string st in z)
                {
                    if (x.text.ToLower() == st){
                        commited = true;
                        break;
                    }
                    else if (string.IsNullOrWhiteSpace(x.text)){
                        commited = true;
                        break;
                    }
                    else if (string.IsNullOrEmpty(x.text)){
                        commited = true;
                        break;
                    }
                }

                if (!commited)
                    returnTokens.Add(x);
            }

            return returnTokens;
        }

        /// <summary>
        /// Evaluates Part of Speech
        /// Uses very crude ways, I.E no context, only hard made words
        /// Around 85% accurate so Poggies?
        /// </summary>
        public static SentencePart EvaluatePartOfSpeech(Token w)
        {
            //First, get value of token
            string v = w.text.ToLower();

            //Check if it contains 'ly'. If it does, return adverb
            if (v.Contains("ly")) return SentencePart.adverb;

            //Check for subject, like I, me, stuff
            string[] subs = Kalos_VirtualAssistant.Subjects;
            foreach (string b in subs){
                if (v == b) return SentencePart.subject;
            }

            //Check for Indirect subject, i.e mine, ours. We can infer its about the person
            string[] insubs = Kalos_VirtualAssistant.IndirectSubjects;
            foreach (string c in insubs){
                if (v == c) return SentencePart.subject;
            }

            //Check for quantity. I.E, one, two, three, etc.
            string[] nums = Kalos_VirtualAssistant.Quantities;
            foreach (string d in nums){
                if (v == d) return SentencePart.quantity;
            }

            //Check for reciever, like you, they, him, her
            string[] rec = Kalos_VirtualAssistant.Recievers;
            foreach (string e in rec){
                if (v == e) return SentencePart.reciever;
            }

            //Again, check for INDIRECT reciever, like himself, ourself, yourself, etc.
            string[] inrec = Kalos_VirtualAssistant.IndirectRecievers;
            foreach (string f in inrec){
                if (v == f) return SentencePart.reciever;
            }

            //Check for Preposition, like on, above and stuff
            string[] pr = Kalos_VirtualAssistant.Prepositions;
            foreach (string g in pr){
                if (v == g) return SentencePart.preposition;
            }

            //Check for Quaestion word, like who, what, where
            string[] ques = Kalos_VirtualAssistant.QuestionWords;
            foreach (string h in ques){
                if (v == h) return SentencePart.questionword;
            }

            //If it is not a conventional PoS, return it as unrecognizable
            return SentencePart.unrecognizable;
        }

        /// <summary>
        /// Evaluates Confidence after Lemmatizing token.
        /// Also contains a bias, i.e some bias which we give to the computer
        /// </summary>
        public static ConfidenceLevel EvaluateConfidence(Token cur, float bias)
        {
            //The computer tends to give better answers for verbs than objects.Keep that in mind
            if (cur.partofspeech == SentencePart.objectual) bias *= 1.2f;
            if (cur.partofspeech == SentencePart.verb) bias *= 2f;

            //Some extra stuff
            bias *= 3f;

            //The computer tends to give better answers for LONG words than SHORT ones. So, we do that right here
            int l = cur.text.Length;
            if (l <= 2) bias *= 0.5f;
            else if (l > 2 && l <= 5) bias *= 0.7f;
            else if (l > 5 && l <= 10) bias *= 1f;
            else if (l > 10) bias *= 2f;

            //Return acco54rding to BIAs
            if (bias > 0.9f) return ConfidenceLevel.VeryHigh;
            if (bias > 0.7f) return ConfidenceLevel.High;
            if (bias > 0.5f) return ConfidenceLevel.Average;
            if (bias > 0.3f) return ConfidenceLevel.Poor;
            if (bias <= 0.2f) return ConfidenceLevel.DESPICABLE;

            return ConfidenceLevel.DESPICABLE;
        }

        /// <summary>
        /// Converts nouns, verbs to their base form
        /// Uses Rule Based AND Corpus methods
        /// </summary>
        public static Token LemmatizateToken(Token prev, Token cur, Token suc, LanguageCorpus corpus)
        {
            //Get token to return
            Token Lemma = cur;
            KDebug.Log("Called this to Lemmatize Token");

            //Define bias
            float bias = 0f;

            //Check if the word is already there in the corpus
            int g = 0;
            foreach (string f in corpus.words)
            {
                if (f == cur.text){
                    //Found match in corpus, get assosiated sentencepart
                    int a = corpus.PoS[g];
                    SentencePart sen = Kalos_Utils.ParseSentencePart(a);
                    UnityEngine.Debug.LogFormat("Found match in corpus : Assigning {0} to word {1}", sen.ToString(), f);

                    //Assign it
                    Lemma.partofspeech = sen;
                    return Lemma;
                }

                g++;
            }

            //If the previous word is to, do not change its form, but return it as a word
            if (prev.text == "to") {
                Lemma.partofspeech = SentencePart.verb;
                bias = 0.9f;
            }

            //If the last word was an adverb AND the current word ends with ing, remove the ing
            else if (prev.partofspeech == SentencePart.adverb && cur.text.Contains("ing")) {
                Lemma = CrudeStemToken(cur);
                Lemma.partofspeech = SentencePart.verb;
                bias = 0.9f;
            }

            //If the last word was an adverb AND the current word does not end with ing, remove the ing and give is AS A OBJECT
            else if (prev.partofspeech == SentencePart.adverb) {
                Lemma = CrudeStemToken(cur);
                Lemma.partofspeech = SentencePart.verb;
                bias = 1f;
            }

            //If the last word was not an adverb AND the current value ends with s, give it as a object
            else if (prev.partofspeech != SentencePart.adverb && cur.text.EndsWith("s")) {
                Lemma = CrudeStemToken(cur);
                Lemma.partofspeech = SentencePart.objectual;
                bias = 0.5f;
            }

            //If the last word was a preposition, make it a object
            else if (prev.partofspeech == SentencePart.preposition){
                Lemma.partofspeech = SentencePart.objectual;
                bias = 0.9f;
            }

            //If the last word was the subject, make it a verb
            else if (prev.partofspeech == SentencePart.subject){
                Lemma.partofspeech = SentencePart.verb;
                bias = 0.7f;
            }

            //If it has failed all the above tests, crude Stem it, i.e only following set rules
            Lemma = CrudeStemToken(cur);
            bias = 0.4f;

            //Evaluate the Confidence of the computer regarding this token
            ConfidenceLevel con = EvaluateConfidence(Lemma, bias);
            UnityEngine.Debug.Log("Confidence Level : " + con.ToString());

            //If it is above average, serialize the token to the Corpus
            if (con == ConfidenceLevel.VeryHigh || con == ConfidenceLevel.High || con == ConfidenceLevel.Average){
                UnityEngine.Debug.LogFormat("Confidence is {0}. Saving Word {1} to Corpus", con.ToString(), Lemma.text);
            }

            return Lemma;
        }

        /// <summary>
        /// Crudely Lemmatizes the token
        /// Read this on the standford website.
        /// Most commonly used for plural nouns
        /// </summary>
        public static Token CrudeStemToken(Token cur)
        {
            //Define Stem Token
            Token stem = cur;

            try{
                //Rule no 1:SSES to SS
                if (cur.text.EndsWith("sses"))
                {
                    stem.text.Replace("sses", "ss");
                    stem.partofspeech = SentencePart.objectual;
                    return stem;
                }

                //Rule no 2:IES to Y
                if (cur.text.EndsWith("ies"))
                {
                    stem.text.Replace("ies", "y");
                    stem.partofspeech = SentencePart.objectual;
                    return stem;
                }

                //Rule no 3:S to null
                if (cur.text.EndsWith("s"))
                {
                    stem.text.Remove(stem.text.Length);
                    stem.partofspeech = SentencePart.objectual;
                    return stem;
                }

                //Rule no 4:MENT to null if length of remaining is more than 2
                if (cur.text.EndsWith("ment") && stem.text.Length - 4 >= 2)
                {
                    stem.text.Remove(stem.text.Length - 4);
                    stem.partofspeech = SentencePart.objectual;
                    return stem;
                }

                //Rule no 5:ES to null
                if (cur.text.EndsWith("es"))
                {
                    stem.text.Remove(stem.text.Length);
                    stem.text.Remove(stem.text.Length);
                    stem.partofspeech = SentencePart.objectual;
                    return stem;
                }

                //Rule no 6 : ing to null
                if (cur.text.EndsWith("ing") && stem.text.Length - 3 >= 1)
                {
                    stem.text.Remove(stem.text.Length);
                    stem.text.Remove(stem.text.Length);
                    stem.text.Remove(stem.text.Length);
                    stem.partofspeech = SentencePart.objectual;
                    return stem;
                }

                //if EVERYTHING goes wrong, make it an object
                stem.partofspeech = SentencePart.objectual;
                return stem;
            }

            catch (ArgumentOutOfRangeException) { return cur; }
            catch (IndexOutOfRangeException) { return cur; }
        }
    }

    /// <summary>
    /// Token Struct
    /// VERY VERY VERY VERY IMPORTANT
    /// Stores all the information
    /// </summary>
    [Serializable]
    public struct Token
    {
        public string text;
        public string expression;
        public SentencePart partofspeech;
        public char[] characters;

        //Parts of Speech Objects
        public bool isNoun;
        public bool isSubject;
        public bool isVerb;

        //Reference to anaylsis data
        public AnaylsisData data;
        public string sOriginer;
        public string sAdverb;
        public int iQuantity;

        public Token(string val, string key)
        {
            this.text = val.ToLower();
            this.expression = key.ToLower();
            this.characters = val.ToCharArray();
            this.partofspeech = SentencePart.unrecognizable;
            text.Trim(Kalos_VirtualAssistant.Punctuation);

            isNoun = false;
            isSubject = false;
            isVerb = false;
            data = new AnaylsisData("a");
            sOriginer = string.Empty;
            sAdverb = string.Empty;
            iQuantity = 0;
        }

        public void AssignNoun(){
            isNoun = true;
        }

        public void AssignSubject(){
            isSubject = true;
        }

        public void AssignVerb(){
            isVerb = true;
        }

        public void AssignAnaylsisResult(AnaylsisData d){
            data = d;
            this.sOriginer = d.sOriginer;
            this.sAdverb = d.sAdverb;
            this.iQuantity = d.iQuantity;
        }

        public void AssignPartOfSpeech(SentencePart f)
        {
            this.partofspeech = f;
        }
    }

    public enum SentencePart
    {
        subject,
        objectual,
        questionword,
        reciever,
        quantity,
        adverb,
        preposition,
        verb,
        unrecognizable
    }

    public enum ConfidenceLevel
    {
        VeryHigh,
        High,
        Average,
        Low,
        Poor,
        DESPICABLE
    }
}

namespace Kalos.Language.v1.NLU 
{
    public struct Kalos_NaturalUnderstandingFrameWork
    {
        /// <summary>
        /// Applies PoS to the sentence
        /// Pretty efficient, though takes a lot of ALU
        /// 70% accuracy so pogs?
        /// </summary>
        public static Token[] ApplyPartsOfSpeech(List<Token> i)
        {
            //Convert to an array
            Token[] r = i.ToArray();

            //Make List( to add)
            List<Token> toks = new List<Token>();

            //Loop through each token and assign convential PoS
            int index = 0;
            foreach (Token x in r)
            {
                try
                {
                    //Get token
                    Token returnX = x;

                    //Get PoS
                    SentencePart z = Kalos_NaturalProccesingFramework.EvaluatePartOfSpeech(x);

                    //Assign PoS
                    returnX.partofspeech = z;

                    //Add token to list
                    toks.Add(returnX);

                    //increment our index
                    index++;
                }

                catch (ArgumentOutOfRangeException a) {KDebug.LogError(a.ParamName + " is causing a problem" + a.Message); }
            }

            //Loop through each token and assign UNCONVENTIONAL PoS, like verb, noun, etc
            Token[] newr = toks.ToArray();
            List<Token> newtoks = new List<Token>();

            //Some ints
            int index2 = 0, nons = 0;
            foreach (Token cd in newr)
            {
                //Get token
                Token newX = cd;

                //Check if its an unrecognized one
                if (cd.partofspeech == SentencePart.unrecognizable)
                {
                    //Add our 'nons' field
                    nons++;

                    //Try to lamentize the token
                    try {
                        newX = Kalos_NaturalProccesingFramework.LemmatizateToken(newr[index2 - 1], newr[index2], newr[index2 + 1], Kalos_VirtualAssistant.wordreserve);
                    }

                    //If its the first or last, just crude stem it
                    catch (ArgumentOutOfRangeException) {
                        newX = Kalos_NaturalProccesingFramework.CrudeStemToken(cd);
                    }
                    catch (IndexOutOfRangeException) {
                        newX = Kalos_NaturalProccesingFramework.CrudeStemToken(cd);
                    }
                }

                //Increment index
                index2++;

                //Give it to the List
                newtoks.Add(newX);

                UnityEngine.Debug.Log(newX.text + " : " + newX.partofspeech.ToString());
            }

            return newtoks.ToArray();
        }

        ///<summary>
        ///Returns the sentence type of a particular sentence
        ///</summary>
        public static SentenceType AnaylseSentenceType(Token[] tokens)
        {
            //Make bools
            bool hs = false, hq = false, hv = false, hr = false;

            //Try to get subject, object, etc
            foreach (Token t in tokens){
                switch (t.partofspeech)
                {
                    case SentencePart.subject:
                        hs = true;
                        break;
                    case SentencePart.questionword:
                        hq = true;
                        break;
                    case SentencePart.reciever:
                        hr = true;
                        break;
                    case SentencePart.verb:
                        hv = true;
                        break;
                    default:
                        break;
                }
            }

            //If there a question word and no reciever and subject, call it a query
            if (!hs && hq && !hr) return SentenceType.query;

            //If there is a question word and a a reciever, call it a contextual qursion
            if (hq && hr) return SentenceType.query;

            //If there is a subject, verb, no reciever or question word, call it a statement
            if (!hq && !hr && hs && hv) return SentenceType.statement;

            // if there is no question and subject, call it a command
            if (!hq && !hs) return SentenceType.command;

            return SentenceType.statement;
        }

        /// <summary>
        /// Identifies all living entities
        /// </summary>
        public static Token[] IdentifyLivingEntities(Token[] f)
        {
            foreach (Token item in f)
            {
                bool g = IsNamedEntity(item.text);

                if (g) item.AssignPartOfSpeech(SentencePart.subject);
            }

            return f;
        }

        /// <summary>
        /// Basically it looks for relations in nouns and verbs, etc
        /// Also converts data to dictionary which is SO more better
        /// Removes unnessesary stuff, and compresses data to like half of the stuff
        /// </summary>
        public static Dictionary<string, Token> CreateRelations(Token[] tokens)
        {
            //Make dictionary
            Dictionary<string, Token> imps = new Dictionary<string, Token>();

            //Assign depth(can be changed according to performance)
            const int depth = 5;

            //Loop through all of the tokens
            int index = 0;
            foreach (Token word in tokens)
            {
                //If it a noun, check to see if its a proper noun
                if (word.partofspeech == SentencePart.objectual)
                {
                    //Assign it as anoun
                    word.AssignNoun();

                    //Get Anaylsis Data and assign it
                    AnaylsisData data = AnalyseSingleToken(word, depth, index, word.partofspeech, tokens);
                    word.AssignAnaylsisResult(data);

                    //Add it to our dictionary
                    imps.Add(word.text, word);
                }

                //If it a subject, just add it no questions asked
                if (word.partofspeech == SentencePart.subject){
                    word.AssignSubject();
                    AnaylsisData data = AnalyseSingleToken(word, depth, index, word.partofspeech, tokens);
                    word.AssignAnaylsisResult(data);
                    imps.Add(word.text, word);
                }

                //If it is a verb, just add it to our dictionary
                if (word.partofspeech == SentencePart.verb){
                    word.AssignVerb();
                    AnaylsisData data = AnalyseSingleToken(word, depth, index, word.partofspeech, tokens);
                    word.AssignAnaylsisResult(data);
                    imps.Add(word.text, word);
                }

                index++;
            }

            return imps;
        }

        /// <summary>
        /// Loops through Array and finds relation
        /// Actual Meat of the NLU
        /// </summary>
        public static AnaylsisData AnalyseSingleToken(Token tok, int depth, int index, SentencePart partOfSpeech, Token[] tokens)
        {
            //Check if it is a noun or verb
            bool noun = partOfSpeech == SentencePart.subject || partOfSpeech == SentencePart.subject;
            bool verb = partOfSpeech == SentencePart.verb;

            //Define a quantity, originer and adverb
            Token quantity = new Token();
            Token originer = new Token();
            Token adverb = new Token();
            bool a = false, q = false, o = false, e = false;

            //Loop through for relations
            for (int i = 0; i < depth; i++)
            {
                //try just in case its the first or last
                try{
                    //Get Tokens infront AND behind
                    Token Infront = tokens[index + i];
                    Token Behind = tokens[index - 1];

                    //If we are a noun, check for quantity. Else, check for originer AND adverb
                    if (noun){
                        if (Behind.partofspeech == SentencePart.quantity){
                            quantity = Behind;
                            q = true;
                            break;
                        }
                    }

                    if (verb){
                        if (Behind.partofspeech == SentencePart.subject || Behind.partofspeech == SentencePart.reciever){
                            originer = Behind;
                            o = true;
                        }

                        if (Behind.partofspeech == SentencePart.objectual && i > depth / 2){
                            e = true;
                        }

                        if (Behind.partofspeech == SentencePart.adverb){
                            adverb = Behind;
                            a = true;
                        }

                        if (Infront.partofspeech == SentencePart.adverb){
                            adverb = Infront;
                            a = true;
                        }
                    }
                }

                catch (ArgumentOutOfRangeException) { }
                catch (IndexOutOfRangeException) { }
            }

            //Assign values to our new Anaylsis Data
            AnaylsisData data = new AnaylsisData("normal");

            //If we are a verb but have no originer , return an error
            if (verb && !o){
                data.isVerb = true;
                data.vType = VerbType.error;
                data.sOriginer = "NAN DATS AN ERROR";
                return data;
            }

            //If an OBJECT is doing an action, i.e the clouds danced, return an error, since the computer doesn't understand personification
            if (verb && e){
                data.isVerb = true;
                data.vType = VerbType.objectAction;
                data.sOriginer = "NAN DATS AN ERROR";
            }

            //If we are a verb AND have a originer but no adverb, return a adverb less verb
            if (verb && o && !a){
                data.isVerb = true;
                data.vType = VerbType.withoutAdverb;
                data.Originer = originer;
                data.sOriginer = originer.text.ToLower();
                return data;
            }

            //If we are a verb AND have a originer AND adverb, return a adverb verb
            if (verb && o && a){
                data.isVerb = true;
                data.vType = VerbType.withAdverb;
                data.Originer = originer;
                data.Adverb = adverb;
                data.sAdverb = adverb.text.ToLower();
                data.sOriginer = originer.text.ToLower();
                return data;
            }

            //If we are a proper noun with quantity, return error
            if (noun && q && tok.isSubject){
                data.isSubject = true;
                data.iQuantity = int.Parse(quantity.text);
                data.Quantity = quantity;
                data.nType = NounType.Error;
                return data;
            }

            //If we are a proper noun without quantity, return subject
            if (noun && !q && tok.isSubject){
                data.isSubject = true;
                data.iQuantity = 1;
                data.nType = NounType.Subject;
                return data;
            }

            //If we are a common noun with quantity, return object with quantity
            if (noun && q && tok.isNoun){
                data.isNoun = true;
                data.iQuantity = int.Parse(quantity.text);
                data.Quantity = quantity;
                data.nType = NounType.ObjectWithQuantity;
                return data;
            }

            //If we are a common noun without quantity, return object with quantity
            if (noun && !q && tok.isNoun){
                data.isNoun = true;
                data.iQuantity = 1;
                data.nType = NounType.ObjectWithoutQuantity;
                return data;
            }

            return new AnaylsisData("BUB THIS AIN'T POSSIBLE YA BEAT DA SYSTEM I WANT A REFUND");
        }

        ///<summary>
        ///The Finale
        ///Returns the computer's anaylsis
        ///Pretty pogs ngl
        ///</summary>
        public static string AnaylseResults(Token[] finalTokens, Dictionary<string, Token> Directory, string text)
        {
            //Get an array of relations
            string[] relations = {};

            foreach (KeyValuePair<string, Token> id in Directory)
            {
                //Get Token
                Token tok = id.Value;

                //If it is a verb, find the noun in our dictionary
                if (tok.isVerb)
                {
                    //Check if we contain that key. If we don't , return an error
                    bool con = Directory.ContainsKey(tok.sOriginer);
                    if (!con)
                        return "I am sorry.I have encountered an error.Error code CS2032 : ObjectNotDefined Exeption";

                    //Else if we do, Get the token with that key
                    else if (con){
                        Token originer = Directory[tok.sOriginer];
                    }
                }
            }

            return "WTF";
        }

        ///<summary>
        ///Finds tagged words, like burnt, sad, etc
        ///</summary>
        public static string FindTaggedWords(Token[] finalTokens)
        {
            foreach (Token s in finalTokens)
            {
                string word = s.text;

                switch (word)
                {
                    case "burn":
                        return "What sir? Who got burnt?";

                    case "dead":
                        return "I am sorry to hear this";

                    case "born":
                        return "Congrats Sir!";

                    case "sad":
                        return "Who was sad sir? And why?";
                        
                    default:
                        break;
                }
            }

            return "I did not get anything";
        }

        /// <summary>
        /// Returns if it can find the name in the text file
        /// </summary>
        public static bool IsNamedEntity(string i)
        {
            //Get the string array which contains the proper nouns
            string[] propernouns = Kalos_VirtualAssistant.ProperNouns;
            string f = i.ToUpper();

            //Loop through the list and return if its true
            foreach (string proper in propernouns) {
                if (f == proper){
                    UnityEngine.Debug.Log(f + " is a proper noun");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Exactly what you think it is
        /// </summary>
        public static bool ProfanityCheck(Token[] toks){
            string[] fg = Kalos_Save.GetTextData(@"C:\Users\GAMING\Desktop\MORE GAMES AAH\The silent slayer\Assets\Scenes\FlaggedWords.txt");

            foreach (Token tok in toks)
            {
                foreach (string f in fg)
                {
                    if (tok.text == f) return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class AnaylsisData
    {
        //Just stores data in container :L
        public bool isVerb;
        public bool isNoun;
        public bool isSubject;

        //Noun vars
        public Token Quantity;
        public NounType nType;
        public int iQuantity;

        //Verb vars
        public Token Originer;
        public Token Adverb;
        public VerbType vType;
        public string sOriginer;
        public string sAdverb;

        public AnaylsisData(string f)
        {
            //Defaults all values
            isVerb = false;
            isNoun = false;
            isSubject = false;
            Quantity = new Token();
            Originer = new Token();
            Adverb = new Token();
            nType = NounType.Error;
            vType = VerbType.error;
            iQuantity = 0;
            sOriginer = string.Empty;
            sAdverb = string.Empty;
        }
    }

    public enum VerbType
    {
        withAdverb,
        withoutAdverb,
        objectAction,
        error
    }

    public enum NounType
    {
        ObjectWithQuantity,
        ObjectWithoutQuantity,
        Subject,
        Error
    }

    public enum SentenceType
    {
        command,
        statement,
        query,
        contextualquestion
    }
}

namespace Kalos.Serialization
{
    [Serializable]
    public struct Kalos_Save
    {
        /// <summary>
        /// Saves Language Data to a 64 bit binary file
        /// </summary>
        public static void SaveLanguageData(LanguageCorpus ld, string path)
        {
            //Create File stream
            FileStream fs = new FileStream(path, FileMode.Create);

            //Create Binary Formatter
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, ld);

            //Close the Data stream. VERY IMPORTANT
            fs.Close();
        }
        
        /// <summary>
        /// Returns text Data as a new string array
        /// </summary>
        public static string[] GetTextData(string path)
        {
            return File.ReadAllLines(path);
        }

        /// <summary>
        /// Returns a new instance of a language corpus
        /// </summary>
        public static LanguageCorpus GetLanguageData(string path)
        {
            //Create File Stream
            FileStream fs = new FileStream(path, FileMode.Open);

            //Create Binary Formatter
            BinaryFormatter bf = new BinaryFormatter();

            //Get Data
            LanguageCorpus ld = bf.Deserialize(fs) as LanguageCorpus;

            //Close the Data stream. VERY IMPORTANT
            fs.Close();
            return ld;
        }

        public static void SaveDirectory(SentenceDirectory f, string path)
        {
            //Create File stream
            FileStream fs = new FileStream(path, FileMode.Create);

            //Create Binary Formatter
            BinaryFormatter bf = new BinaryFormatter();

            //Serialize bDictionary
            BDirectory fa = new BDirectory();
            fa.JSON = JsonUtility.ToJson(fa);
            bf.Serialize(fs, fa);

            //Close the Data stream. VERY IMPORTANT
            fs.Close();
        }

        public static SentenceDirectory GetDirectory(string path)
        {
            //Create File Stream
            FileStream fs = new FileStream(path, FileMode.Open);

            //Create Binary Formatter
            BinaryFormatter bf = new BinaryFormatter();

            //Get Data
            BDirectory ld = bf.Deserialize(fs) as BDirectory;

            fs.Close();
            return JsonUtility.FromJson<SentenceDirectory>(ld.JSON);
        }

        /// <summary>
        /// Checks if a file has been saved on that path
        /// </summary>
        public static bool Saved(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// PlayerPrefsStuff
        /// </summary>
        public static void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }
        public static void SaveFloat(string key,float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }
        public static void SaveInt(string key, int value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public static string GetString(string key)
        {
            return PlayerPrefs.GetString(key);
        }
    }

    [Serializable]
    public struct Kalos_Write
    {
        /// <summary>
        /// Saves data in our 64 bit corpus
        /// </summary>
        public static void SaveToCorpus(LanguageCorpus co, string word, int value)
        {
            List<string> f = new List<string>();
            List<int> g = new List<int>();

            //First, assign the lkists with the elements
            foreach (string y in co.words)
            {
                f.Add(y);
            }
            foreach (int x in co.PoS)
            {
                g.Add(x);
            }

            //Add our values
            f.Add(word);
            g.Add(value);

            //Now, set our Language corpus's values to that
            co.words = f.ToArray();
            co.PoS = g.ToArray();
        }
    }
}

namespace Kalos.Data
{
    namespace Containers
    {
        [Serializable]
        public class LanguageCorpus
        {
            //Use parallel array system to keep track of the PoS of each word
            public string[] words;
            public int[] PoS;
        }

        [Serializable]
        public class SentenceDirectory
        {
            public List<string> Sentences;
            public List<string> Subjects;
        }

        [Serializable]
        public class BDirectory
        {
            public string JSON;
        }
    }

    namespace Structures
    {
    }
}

namespace Kalos.Utilities
{
    namespace Personalization
    {
        namespace General
        {
            public struct Kalos_Personalization
            {
                public static OperatingSystem Operating_System()
                {
                    return Environment.OSVersion;
                }

                public static float FPS()
                {
                    return 1 / Time.smoothDeltaTime;
                }

                public static string Username()
                {
                    return Environment.MachineName;
                }

                public static string CommandLine()
                {
                    return Environment.StackTrace;
                }
            }
        }

        namespace CPU
        {
            public struct Kalos_CPU
            {
                public static string Time
                {
                    get{
                        PerformanceCounter counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        return counter.NextValue().ToString();
                    }
                }

                public static string Ram
                {
                    get
                    {
                        PerformanceCounter counter = new PerformanceCounter("Memory", "Available MBytes");
                        KDebug.Log(counter.NextValue());
                        return counter.NextValue().ToString();
                    }
                }

                public static string Usage
                {
                    get
                    {
                        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        return cpuCounter.NextValue().ToString();
                    }
                }
            }
        }
    }

    namespace General
    {
        [Serializable]
        public struct Kalos_Utils
        {
            public static SentencePart ParseSentencePart(int i)
            {
                return i switch
                {
                    0 => SentencePart.subject,
                    1 => SentencePart.objectual,
                    2 => SentencePart.questionword,
                    3 => SentencePart.reciever,
                    5 => SentencePart.adverb,
                    4 => SentencePart.quantity,
                    6 => SentencePart.preposition,
                    7 => SentencePart.verb,
                    8 => SentencePart.unrecognizable,
                    _ => SentencePart.unrecognizable,
                };
            }

            public static int SentencePartToInt(SentencePart i)
            {
                switch (i)
                {
                    case SentencePart.subject:
                        return 0;

                    case SentencePart.objectual:
                        return 1;

                    case SentencePart.reciever:
                        return 2;

                    case SentencePart.quantity:
                        return 3;

                    case SentencePart.adverb:
                        return 4;

                    case SentencePart.preposition:
                        return 5;

                    case SentencePart.verb:
                        return 6;

                    case SentencePart.unrecognizable:
                        return 7;

                    case SentencePart.questionword:
                        return 8;

                    default:
                        return 7;
                }
            }

            public static string GetTimeOfDay()
            {
                //Check for time
                DateTime currentTime = DateTime.Now;
                int hour = currentTime.Hour;

                //Check for hour
                if (hour <= 12) return "Good Morning!";
                else if (hour > 12 || hour <= 15) return "Good Afternoon!";
                else
                    return "Good Evening!";
            }

            public static List<object> ToList(object[] f)
            {
                List<object> s = new List<object>();

                foreach (object h in f)
                {
                    s.Add(h);
                }

                return s;
            }

            public static string[] TokensToStringArray(Token[] tokens)
            {
                List<string> strings = new List<string>();
                foreach (Token t in tokens)
                {
                    strings.Add(t.text);
                }

                return strings.ToArray();
            }

            public static string Spoonacular_nAPIKEYURL
            {
                get
                {
                    string returnstr = "&apiKey=f5ecd666879b46b3847f42ca4fd5ba50";
                    return returnstr;
                }
            }

            public static string Spoonacular_qAPIKEYURL
            {
                get
                {
                    string returnstr = "?apiKey=f5ecd666879b46b3847f42ca4fd5ba50";
                    return returnstr;
                }
            }
            
            public static string Happi_APIKEYURL
            {
                get{
                    return "?apikey=81eff7GH9osDT9YLUpvgzLhJ9muQ38zM9PW8FThdGUexys1SI4wVMrWC";
                }
            }

            public static string Musixmatch_APIKEYURL
            {
                get
                {
                    return "&apikey=47115ac1bbaf7a88ded391ab70111802";
                }
            }
        }
    }

    namespace Debug
    {
        public struct KDebug
        {
            public static void Log(object o)
            {
                UnityEngine.Debug.Log(o);
            }

            public static void Log(object o, UnityEngine.Object context)
            {
                UnityEngine.Debug.Log(o, context);
            }

            public static void LogError(object o)
            {
                UnityEngine.Debug.LogError(o);
            }

            public static void LogError(object o, UnityEngine.Object context)
            {
                UnityEngine.Debug.LogError(o, context);
            }

            public static void LogWarning(object o)
            {
                UnityEngine.Debug.LogWarning(o);
            }

            public static void LogWarning(object o, UnityEngine.Object context)
            {
                UnityEngine.Debug.LogWarning(o, context);
            }

            public static void LogFormat(string format, params object[] args)
            {
                UnityEngine.Debug.LogFormat(format, args);
            }
        }
    }
}

namespace Kalos.Media.Images
{
#pragma warning disable CS1998
    [Serializable]
    public struct Kalos_Images
    {
        public const string PIXEL_API_KEY = "563492ad6f917000010000011afb4b275f884ac4a4442212898e0925";

        public static async Task<string> SearchForPhotoAsync(string topic)
        {
            //Create PexelClient
            var pixelClient = new PexelsClient(PIXEL_API_KEY);

            //Wait for result
            var result = await pixelClient.SearchPhotosAsync(topic, "", "small");

            //Get Photo and result
            var reqPhoto = result.photos[0];
            var source = reqPhoto.source;

            //Get URL
            var url = source.tiny;
            return url;
        }

        public static async Task<Texture2D> ExtractImageAsync(Uri url)
        { 
            //Create web client
            WebClient cl = new WebClient();

            //Create Unique Seed Id
            string f = (100 * UnityEngine.Random.value).ToString();
            string path = string.Format(@"C:\Users\GAMING\Desktop\Tests\KALOS_{0}.jpeg", f);

            //Download file
            cl.DownloadFile(url, path);

            //Return Texture
            Texture2D tex = null;
            byte[] fileData;

            fileData = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            return tex;
        }

        public static void DeleteAllImages()
        {
            // Delete all files in a directory    
            string[] files = Directory.GetFiles(@"C:\Users\GAMING\Desktop\Tests");
            foreach (string file in files)
            {
                File.Delete(file);
                Console.WriteLine($"{file} is deleted.");
            }
        }

        public static void DeleteAllWebCamImages()
        {
            // Delete all files in a directory    
            string[] files = Directory.GetFiles(@"C:\Users\GAMING\Desktop\Tests\FacialData\TempImages");
            foreach (string file in files)
            {
                File.Delete(file);
                Console.WriteLine($"{file} is deleted.");
            }
        }
    }

#pragma warning restore CS1998
}