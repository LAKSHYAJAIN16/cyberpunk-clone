using UnityEngine;
using UnityEngine.UI;
using Kalos.Data.Containers;
using Kalos.Main;
using Kalos.Media.Images;
using Kalos.Face.Recognition;
using Kalos.Face.Objects;
using Kalos.Utilities.General;
using Kalos.Utilities.Debug;
using Kalos.Utilities.Interfaces;
using Kalos.Utilities.Interfaces.Empty;
using Kalos.Utilities.EscapeKeys;
using System;
using System.Security;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Kalos.Face.Retrievation
{
    public class Kalos_Face : MonoBehaviour , KFace, KAssignable , KPythonExecuter , KMono
    {
        //Kalos Main
        public Kalos_VirtualAssistant Main;

        //Define Callrate and path
        public float CallRate = 2f;
        public string PATH = "";

        //Display
        public RawImage RawImage;
        public bool DisplayFace = false;

        //Texture
        WebCamTexture tx;

        //Face Mode
        public FaceMode Mode = FaceMode.call;

        //Debug
        public DebugMode debugMode;

        // Start is called before the first frame update
        void Start()
        {
            tx = new WebCamTexture();
            tx.Play();
            Kalos_FaceRecognition FaceRecog = new Kalos_FaceRecognition();
            Kalos_FaceRecognition.DEBUG_MODE = debugMode;
            FaceRecog.AssignMain(Main);
            InvokeRepeating(nameof(RecognizeFace), CallRate, CallRate);

            if (!DisplayFace)
                RawImage.gameObject.SetActive(false);
        }

        void Update(){
            if (DisplayFace)
                RawImage.texture = tx;
        }

        public void RecognizeFace()
        {
            StartCoroutine(GetSnap());
        }

        private void OnApplicationQuit(){
            Kalos_Images.DeleteAllWebCamImages();
        }

        ///<summary>
        ///Returns WebCamTexture
        ///</summary>
        public IEnumerator GetSnap()
        {
            //Wait for end of frame(debug)
            yield return new WaitForEndOfFrame();

            //Create texture
            Texture2D p = new Texture2D(tx.width, tx.height);

            //Serialize Bytes
            p.SetPixels32(tx.GetPixels32());
            p.Apply();

            byte[] bytes = p.EncodeToPNG();
            RawImage.texture = p;

            //Get Random name
            string name = "Kalos_" + UnityEngine.Random.Range(69, 420).ToString() + ".png";

            //Get Path
            string fPath = string.Format(@"{0}\{1}", PATH, name);

            //Write File
            File.WriteAllBytes(fPath, bytes);

            //Run Python Script(Async)
            //RunPython(pythonScript, ExecuteMethod.withoutWindow, true);
            Kalos_FaceRecognition.AnayliseFacialData("GOGS NOICE");
        }

        /*public void RunPython(PythonScript script, ExecuteMethod method, bool a)
        {
            //Get String
            string f = "69 Noice";

            //Pass it in to our Recognition Module
            Kalos_FaceRecognition.AnayliseFacialData(f);
        }*/

        public void AssignMain(Kalos_VirtualAssistant ar){
            this.Main = ar;
        }

        public IEnumerator CallFaceMethod()
        {
            while (Mode == FaceMode.call){
                yield return new WaitForSeconds(CallRate);
                StartCoroutine(GetSnap());
            }
        }

        public void SwitchMode(FaceMode mode)
        {
            this.Mode = mode;

            if (mode == FaceMode.call)
                CallFaceMethod();
        }
    }


    public enum FaceMode
    {
        call,
        no
    }
}

namespace Kalos.Face.Recognition
{
    public struct Kalos_FaceRecognition : KCloneable , KAssignable , KFace
    {
        //Paths
        public const string PATH = @"C:\Users\GAMING\Desktop\Tests\FacialData\Profiles";
        public const string LOGPATH = @"C:\Users\GAMING\Desktop\Tests\FacialData\Log.txt";

        //DebugMode
        public static DebugMode DEBUG_MODE;

        //Kalos Main Module
        public static Kalos_VirtualAssistant Main; 

        /// <summary>
        /// Main Method
        /// Anaylses Result and Calls all subMethods
        /// </summary>
        public static void AnayliseFacialData(string data)
        {
            //Get Facial Data
            CoreFacialData Face = ParseFacialData(data);

            //If it is null, return
            if (Face == null) return;

            //Create Profile
            User us = new User("Lakshya", "13", "Male");
            FacialData FData = ConvertToFacialData(Face, us);

            //Compare Faces
            AnaylsisResult result = CompareFaces(FData);

            //Throw new SuccessCall back!
            if (result.Match == null){
                FaceSuccessCallback callback = new FaceSuccessCallback(result);
            }
            else{
                FaceSuccessCallback faceSuccessCallback = new FaceSuccessCallback(result, result.Match);
            }
        }

        /// <summary>
        /// Parses the Python FacialData into a c# class
        /// Poggies?
        /// </summary>
        public static CoreFacialData ParseFacialData(string data)
        {
            try
            {
                //Array of non excepted chars
                char[] NOTEXCEPTED = new char[] { '[', ']', ' ', '(', ')' };
                
                //Create New Facial Data
                CoreFacialData FData = new CoreFacialData();

                //Trim the data of all the not excepted chars
                data = data.Replace("]]", "");
                data.Replace("[[", "");
                data = data.Trim(NOTEXCEPTED);

                //Loop through and Get All da values
                string[] nons = data.Split(',');

                foreach (string item in nons)
                {
                    if (string.IsNullOrEmpty(item)){
                        WriteToLog("No User");
                        return null;
                    }

                    //Parse it to a float
                    float val = float.Parse(item);

                    //Add it to the Facial Data List
                    FData.measurements.Add(val);
                }

                return FData;
            }

            catch (FormatException){

                //There is no user. Write it to the Log
                if(DEBUG_MODE==DebugMode.yes)
                    KDebug.Log("Error. No Human Detected");

                WriteToLog("No User");
                return null;
            }
        }

        /// <summary>
        /// Compares faces and gives what the computer has to do
        /// </summary>
        public static AnaylsisResult CompareFaces(FacialData FaceData)
        {
            //Check if We already have made it, by checking the already existing Files
            string[] profiles = Directory.GetFiles(PATH);

            //If it's length is 0, return it as a new profile, because its the first user
            if (profiles.Length == 0){
                SerializeData(FaceData);
                WriteToLog(FaceData.User);
                return new AnaylsisResult(true, FaceData.CoreData, "N.A", Result.CreateNewFile);
            }

            //Else, Deserialize All of The Objects
            List<Match> Matches = new List<Match>();
            foreach (string it in profiles){
                //Get Match
                Match match = AnaylseSingleProfile(it, FaceData, DEBUG_MODE);

                //Add Match
                Matches.Add(match);
            }

            //Check which was the best Match
            Match best = GetBestMatch(Matches.ToArray(), 75f);

            //If the Best Match was not found, create new file
            if (best.Percent == 0f){
                SerializeData(FaceData);
                WriteToLog(FaceData.User);
                return new AnaylsisResult(true, FaceData.CoreData, "N.A", Result.CreateNewFile);
            }

            //Assign Match
            FaceData.Match = best;

            //Assign credentials
            FaceData.User = best.ComparedFaceData.User;
            FaceData.ID = best.ComparedFaceData.ID;

            //Write it to Memmory
            WriteToLog(FaceData.User);

            //Return Anaylsis Result
            return new AnaylsisResult(false, FaceData.CoreData, best.Path, Result.ExistsSoRead, best);
        }

        /// <summary>
        /// Converts Raw Data to Standarized Data to Read
        /// </summary>
        public static FacialData ConvertToFacialData(CoreFacialData data, User user)
        {
            return new FacialData(){
                CoreData = data,
                User = user,
                ID = UnityEngine.Random.Range(69, 420)
            };
        }

        /// <summary>
        /// Gives a percentage of how much it matches the data
        /// Around 75% Accuracy
        /// Not Perfect but Pretty Pogs NGL
        /// </summary>
        public static Match AnaylseSingleProfile(string pathToFile, FacialData currentData, DebugMode debug)
        {
            //Read the text of the file
            StreamReader sc = new StreamReader(pathToFile);
            string text = sc.ReadToEnd();

            //Deserialize it to an facial object
            FacialData reqData = JsonConvert.DeserializeObject<FacialData>(text);

            //Get Core Data
            CoreFacialData core = reqData.CoreData;

            //Check if the Lenghts of both the measurements is the same.If not, throw exception
            if (currentData.CoreData.measurements.Count != core.measurements.Count) 
                throw new KalosFaceException("Error : Facial data does not match. Length of args is different.");

            //Compare each Data
            float[] args1 = core.measurements.ToArray();
            float[] args2 = currentData.CoreData.measurements.ToArray();

            int rotations = 0;
            float percentage = 0f;
            foreach (float val in args1){

                //Get Related Value
                float val2 = Mathf.Abs(args2[rotations]);
                float val1 = Mathf.Abs(val);

                //Get Value to add
                float valToAdd = 0f;
                string valToDebug = string.Empty;

                //Increment Percentage
                if (val2 > val1){
                    valToAdd = (val1 / val2) * 100f;
                    percentage += valToAdd;
                    valToDebug = string.Format("Final percentage = {0}, Numerator = {1}, Denominator = {2}", valToAdd, val1, val2);
                }
                else if (val1 > val2){
                    valToAdd = (val2 / val1) * 100f;
                    percentage += valToAdd;
                    valToDebug = string.Format("Final percentage = {0}, Numerator = {1}, Denominator = {2}", valToAdd, val2, val1);
                }
                else if (val1 == val2){
                    valToAdd = 100f;
                    percentage += valToAdd;
                    valToDebug = string.Format("Final percentage = {0}, Numerator = {1}, Denominator = {2}", valToAdd, val1, val2);
                }

                //Increment Rotations
                rotations++;

                //Debug
                if (debug == DebugMode.yes)
                    KDebug.Log(valToDebug);
            }

            //Calculate total Percentage
            float total = (float)args1.Length * 100f;
            float finalPercentage = (percentage / total) * 100f;

            //Debug
            if (debug == DebugMode.yes)
                KDebug.Log(finalPercentage);

            //Create Match with this data
            Match Anaylsis = new Match(finalPercentage, reqData, reqData.CoreData, pathToFile);

            //Return that
            return Anaylsis;
        }

        ///<summary>
        ///Filters Best Match From an array
        ///</summary>
        public static Match GetBestMatch(Match[] matches, float tolerance)
        {
            Match leader = new Match(0f);
            foreach (Match curMatch in matches){
                if (curMatch.Percent > leader.Percent && curMatch.Percent > tolerance){
                    //Assign Leader
                    leader = curMatch;
                }
            }

            return leader;
        }

        ///<summary>
        ///Serializes Data
        ///</summary>
        public static async void SerializeData(FacialData data)
        {
            try
            {
                //Main.SpeakDirect("Hello! I am Kalos! This is the first time I have met you! May I know your good name please?");

                //Get Json
                string json = JsonConvert.SerializeObject(data);

                //Get Path
                string date = DateTime.Today.DayOfYear.ToString();
                string name = "Kalos_Face_" + date + "_" + data.ID.ToString();
                string Path = string.Format("{0}/{1}.txt", PATH, name);

                //Check if we already have a file there
                if (File.Exists(Path))
                    throw new KalosFaceException("Error : Ran out of IDS");

                //Create TextWriter
                using (TextWriter writer = new StreamWriter(Path)){
                    await writer.WriteAsync(json);
                }
            }

            catch (ArgumentNullException a){
                KDebug.LogFormat("{0} param, {1} message, {2} InnerException", a.ParamName, a.Message, a.InnerException);
            }
        }

        /// <summary>
        /// Writes to Log. IE, who is using the system
        /// </summary>
        public static async void WriteToLog(User currentUser)
        {
            //Get JSON
            string Json = JsonConvert.SerializeObject(currentUser);

            //Write to file
            using(TextWriter writer = new StreamWriter(LOGPATH)){
                await writer.WriteAsync(Json);
            }
        }
        public static async void WriteToLog(string text)
        {
            //Write to file
            using (TextWriter writer = new StreamWriter(LOGPATH)){
                await writer.WriteAsync(text);
            }
        }

        public object Clone(Kalos_VirtualAssistant main){
            return this;
        }

        public void AssignMain(Kalos_VirtualAssistant ar)
        {
            Main = ar;
        }
    }
}

namespace Kalos.Face.Objects
{
    public class CoreFacialData
    {
        public List<float> measurements = new List<float>();
    }

    public class FacialData
    {
        //Core Data (Measurements)
        public CoreFacialData CoreData;

        //Our User
        public User User;

        //ID
        public int ID;

        //Match
        public Match Match;
    }

    public class AnaylsisResult
    {
        public bool First_time { get; set; }

        public CoreFacialData Data { get; set; }

        public string Path_To_Match { get; set; }

        public Result Result { get; set; }

        public Match Match { get; set; }

        public AnaylsisResult(bool time, CoreFacialData data, string Path, Result res)
        {
            this.First_time = time;
            this.Data = data;
            this.Path_To_Match = Path;
            this.Result = res;
        }

        public AnaylsisResult(bool time, CoreFacialData data, string Path, Result res, Match match)
        {
            this.First_time = time;
            this.Data = data;
            this.Path_To_Match = Path;
            this.Result = res;
            this.Match = match;
        }

        public AnaylsisResult() { }
    }

    public class KalosFaceException : Exception
    {
        public KalosFaceException() { Kalos_EscapeKeyManager.StopPlaying(); }

        public KalosFaceException(string message) : base(message) { Kalos_EscapeKeyManager.StopPlaying(); }

        public KalosFaceException(string message, Exception inner) : base(message, inner) { }

        protected KalosFaceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { Kalos_EscapeKeyManager.StopPlaying(); }
    }

    public class FaceSuccessCallback : Success
    {
        public FaceSuccessCallback(string m)
        {
            SendMessage(m);
        }

        public FaceSuccessCallback(AnaylsisResult result)
        {
            SendAnaylsisResultMessage(result);
        }

        public FaceSuccessCallback(AnaylsisResult result,Match match)
        {
            SendMatchResultMessage(result, match);
        }

        public override void SendMessage(string Message)
        {
            base.SendMessage(Message);
        }

        public override void SendAnaylsisResultMessage(AnaylsisResult result)
        {
            base.SendAnaylsisResultMessage(result);
        }

        public override void SendMatchResultMessage(AnaylsisResult result, Match match)
        {
            base.SendMatchResultMessage(result, match);
        }
    }

    public class Match
    {
        public float Percent { get; set; }

        public FacialData ComparedFaceData { get; set; }

        public CoreFacialData ComparedCoreData { get; set; }

        public string Path { get; set; }

        public Match() { }

        public Match(float dummy) { this.Percent = dummy; }

        public Match(float percent, FacialData ComparedData, CoreFacialData ComparedCoreData, string path)
        {
            this.Percent = percent;
            this.ComparedFaceData = ComparedData;
            this.ComparedCoreData = ComparedCoreData;
            this.Path = path;
        }
    }

    public enum Result
    {
        ExistsSoRead,
        CreateNewFile,
        Error
    }
}

namespace Kalos.Utilities.Interfaces
{
    public class Success
    {
        public Success() { }
        public Success(string message) { }
        public Success(string message, Exception innerException) { }
        [SecuritySafeCritical]
        protected Success(SerializationInfo info, StreamingContext context) { }

        public virtual void SendMessage(string Message){
            KDebug.Log(Message);
        }

        public virtual void SendAnaylsisResultMessage(AnaylsisResult result)
        {
            KDebug.Log("FACE ANAYLSIS RESULT"
                + Environment.NewLine + "Anaylsis Result : " + result.Result.ToString()
                + Environment.NewLine + "Path To Match : " + result.Path_To_Match
                + Environment.NewLine + "First Time : " + result.First_time);
        }

        public virtual void SendMatchResultMessage(AnaylsisResult result, Match match)
        {
            KDebug.Log("FACE ANAYLSIS RESULT"
                + Environment.NewLine + "Anaylsis Result : " + result.Result.ToString()
                + Environment.NewLine + "Path To Match : " + result.Path_To_Match
                + Environment.NewLine + "First Time : " + result.First_time
                + Environment.NewLine + "PERCENTAGE" + match.Percent);
        }

    }

    public interface KAssignable
    {
        public void AssignMain(Kalos_VirtualAssistant ar);
    }

    namespace Empty
    {
        public interface KFace
        {
        }

        public interface KPythonExecutable
        {
        }

        public interface KPythonExecuter
        {
        }

        public interface KMono
        {

        }
    }
}