using UnityEngine;
using UnityEngine.Windows.Speech;
using Kalos.Main;
using Kalos.Utilities.Debug;
using Kalos.Utilities.General;
using Kalos.Utilities.Interfaces;
using Kalos.Utilities.Interfaces.Empty;
using TMPro;

namespace Kalos.Speech
{
    namespace Recognition
    { 
        public class Kalos_Speech : MonoBehaviour , KAssignable , KMono
        {
            private SpeechListner ca;
            public Kalos_VirtualAssistant VA;
            public TextMeshProUGUI TextMesh;
            public DebugMode DebugMode;
            public AudioSource Source;

            private void Awake()
            {
                ca = new SpeechListner();
                ca.Initialize(VA, TextMesh, DebugMode);
            }

            public void AssignMain(Kalos_VirtualAssistant ar)
            {
                this.VA = ar;
            }
        }

        public class SpeechListner : KAssignable
        {
            public DictationRecognizer SpeechEngine;
            public Kalos_VirtualAssistant Main;
            public TextMeshProUGUI textDisplay;
            public DebugMode DebugMode { get; set; }

            public void Initialize(Kalos_VirtualAssistant v, TextMeshProUGUI text, DebugMode DebugM)
            {
                //Assign values
                this.Main = v;
                this.textDisplay = text;
                this.DebugMode = DebugM;

                //Check to see if there's any microphones or not
                bool hasMicro = false;
                foreach (var device in Microphone.devices){
                    hasMicro = true;
                }

                if (!hasMicro)
                {
                    Debug.LogError("I am sorry. I cannot find any microphone");
                    return;
                }

                if (SpeechEngine != null) {
                    SpeechEngine.Dispose();
                    SpeechEngine = null;
                }

                //Initialize Instance of the Dictation Recognizer;
                try
                {
                    SpeechEngine = new DictationRecognizer();
                    SpeechEngine.DictationResult += SpeechEngine_DictationResult;
                    SpeechEngine.DictationHypothesis += SpeechEngine_DictationHypothesis;
                    SpeechEngine.DictationError += SpeechEngine_DictationError;
                    SpeechEngine.DictationComplete += SpeechEngine_DictationComplete;
                    SpeechEngine.Start();
                    SpeechEngine.AutoSilenceTimeoutSeconds = 5f;
                }

                catch (UnityException f)
                {
                    Debug.LogFormat("Error Data {0}, Help Link {1}, Inner Exeption {2}, HResult {3}", f.Data, f.HelpLink, f.Message,f.HResult);
                    SpeechEngine = new DictationRecognizer();
                    return;
                }
        }

            private void SpeechEngine_DictationResult(string text, ConfidenceLevel confidence)
            {
                if (DebugMode == DebugMode.yes){
                    Debug.Log("Dictation complete. Result : " + text + " , Confidence : " + confidence);
                }
                textDisplay.text = "";
                Main.Speak(text);
            }

            private void SpeechEngine_DictationComplete(DictationCompletionCause cause)
            {
                if (DebugMode == DebugMode.yes){
                    Debug.Log(cause.ToString());
                }
                Initialize(Main, textDisplay,DebugMode);
            }

            private void SpeechEngine_DictationError(string error, int hresult)
            {
                Debug.LogError(error + " on " + hresult);
            }

            private void SpeechEngine_DictationHypothesis(string text)
            {
                if (DebugMode == DebugMode.yes){
                    Debug.Log(text);
                }
                textDisplay.text = text;
            }

            public void AssignMain(Kalos_VirtualAssistant ar)
            {
                this.Main = ar;
            }
        }
    }
}

namespace Kalos.Speech.Localization
{
    public class Kalos_LocalizationModule : KAssignable
    {
        public Kalos_VirtualAssistant Kal;
        //All the KeyCodes
        public static readonly string[] CountryCodes =
        {
            "india",
            "us",
            "germany",
            "france",
            "gujrat"
        };

        //All da LC's
        public static readonly string[] LC_CODES =
        {
            "hi",
            "en-us",
            "de",
            "fr",
            "gu"
        };

        //Current Country
        public static string currentC = "us";
        public static string currentLC = "en-us";

        public static void ChangeCountry(string country)
        {
            //Assign country
            currentC = country;

            //Get Index
            int index = 0;
            for (int i = 0; i < CountryCodes.Length; i++)
            {
                string a = CountryCodes[i];
                if (a == country){
                    index = i;
                    break;
                }
            }

            //Assign Code
            currentLC = LC_CODES[index];
        }

        public static void ChangeCountry(string country, string id)
        {
            //Assign country
            currentC = country;

            //Assign Code
            currentLC = id;
        }

        public void AssignMain(Kalos_VirtualAssistant ar)
        {
            this.Kal = ar;
        }
    }
}

namespace Kalos.Utilities.General
{
    public enum DebugMode
    {
        no,
        yes
    }
}