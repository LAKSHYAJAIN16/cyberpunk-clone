using System;
using UnityEngine;
using TMPro;
using Kalos.Main;
using Kalos.Speech.Localization;
using Kalos.Media.Images;
using Kalos.Face.Retrievation;
using Kalos.Utilities.Interfaces;
using Kalos.Utilities.Interfaces.Empty;
using Kalos.Utilities.Personalization.CPU;
using Kalos.Utilities.Personalization.General;

namespace Kalos.Utilities.EscapeKeys
{
    public class Kalos_EscapeKeyManager : MonoBehaviour , KAssignable , KCloneable , KMono
    {
        public static bool TextInput = false;
        public static bool DebugMode = false;
        public static bool IsFace = true;

        public TMP_InputField InputField;
        public TextMeshProUGUI PlayerTXT, DebugTXT;
        public Kalos_VirtualAssistant Main;

        void Update()
        {
            if (IsHoldingControl && Input.GetKeyDown(KeyCode.T))
            {
                TextInput = !TextInput;
                InputField.gameObject.SetActive(TextInput);
                PlayerTXT.text = "";
            }

            if (IsHoldingControl && Input.GetKeyDown(KeyCode.D))
            {
                DebugMode = !DebugMode;
                DebugTXT.gameObject.SetActive(DebugMode);
            }

            if (TextInput && Input.GetKeyDown(KeyCode.Return))
            {
                Main.Speak(InputField.text);
                InputField.text = "";
            }

            if (DebugMode)
            {
                string txt = Kalos_Personalization.Operating_System().ToString()
                    + Environment.NewLine + Kalos_CPU.Usage
                    + Environment.NewLine + Kalos_CPU.Ram
                    + Environment.NewLine + Kalos_Personalization.FPS().ToString()
                    + Environment.NewLine + Kalos_Personalization.Username()
                    + Environment.NewLine + Environment.ProcessorCount.ToString();

                DebugTXT.text = txt;
            }

            if (IsHoldingShift && Input.GetKey(KeyCode.G) && IsHoldingControl) {
                StopPlaying();
            }

            if (IsHoldingControl && Input.GetKeyDown(KeyCode.Q)) {
                Kalos_Images.DeleteAllImages();
            }

            if (IsHoldingControl && Input.GetKeyDown(KeyCode.D)) {
                Kalos_Images.DeleteAllWebCamImages();
            }

            if (IsHoldingControl && Input.GetKeyDown(KeyCode.H)){
                Kalos_Face faceSr = this.transform.GetComponent<Kalos_Face>();
                IsFace = !IsFace;
                FaceMode des = FaceMode.no;
                if (IsFace) des = FaceMode.call;

                faceSr.SwitchMode(des);
                faceSr.enabled = IsFace;
            }

            if (IsHoldingShift && Input.GetKeyDown(KeyCode.Alpha1)){
                Kalos_LocalizationModule.ChangeCountry("us");
                Main.SpeakDirect("Changed Narrator to American");
            }

            if (IsHoldingShift && Input.GetKeyDown(KeyCode.Alpha2)){
                Kalos_LocalizationModule.ChangeCountry("india");
                Main.SpeakDirect("Changed Narrator to Indian");
            }

            if (IsHoldingShift && Input.GetKeyDown(KeyCode.Alpha3)){
                Kalos_LocalizationModule.ChangeCountry("germany");
                Main.SpeakDirect("Changed Narrator to German");
            }

            if (IsHoldingShift && Input.GetKeyDown(KeyCode.Alpha4)){
                Kalos_LocalizationModule.ChangeCountry("france");
                Main.SpeakDirect("Changed Narrator to French");
            }
        }

        public static void StopPlaying()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();

#endif
        }

        public static bool IsHoldingControl
        {
            get
            {
                if (Input.GetKey(KeyCode.LeftControl)) return true;
                if (Input.GetKey(KeyCode.RightControl)) return true;
                if (Input.GetKey(KeyCode.LeftCommand)) return true;
                if (Input.GetKey(KeyCode.RightCommand)) return true;

                return false;
            }
        }

        public static bool IsHoldingShift
        {
            get
            {
                if (Input.GetKey(KeyCode.LeftShift)) return true;
                if (Input.GetKeyDown(KeyCode.RightShift)) return true;

                return false;
            }
        }

        public void AssignMain(Kalos_VirtualAssistant ar)
        {
            this.Main = ar;
        }

        public object Clone(Kalos_VirtualAssistant main)
        {
            return this;
        }
    }
}
