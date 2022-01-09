using System;
using System.Linq;
using System.Collections.Generic;
using Kalos.Utilities.General;
using UnityEngine;
using UnityEngine.UI;
using Kalos.Utilities.Debug;

namespace Kalos.Brain.Emotions
{
    public class Kalos_EmotionController : MonoBehaviour
    {
        //Total number of hormones. The More, the more complex the brain will be
        internal const int Hormones = 500;

        //Sigma for the hormone to Progress. Again, The More, the more complex the brain will be
        internal const double sigma = 0.5;

        //Emotion Co-efficient. How much emotion affects the body
        internal double strength { get; set; }

        //Override Value. After how much the body duplicates the emotion
        internal const double override_value = 0.2;

        //Reference to our Hormones
        internal Hormone[] hormones { get; set; }

        //Debug Mode
        public DebugMode debug;
        public GameObject Prefab;
        public Canvas canvas;

        //Constructor
        /*public Kalos_EmotionController(double? strength = null)
        {
            //Get Strength
            double local_strength = strength ?? 1;
            this.strength = local_strength;

            //Initialize Hormones
            List<Hormone> hormones_L = new List<Hormone>();
            for (int i = 0; i < Hormones; i++){
                hormones_L.Add(new Hormone(HormoneType.Phyprotencin));
                CreateNeuron();
            }
        }*/

        internal void React(double value, HormoneType type)
        {
            //Calculate Value using Formula
            double value_s = Math.Abs(sigma * value);
            double emotion = Math.Sin(Math.Tanh(value_s * strength));

            //Check how many  it would affect(So like 1 is all of them, 0.5 is like half)
            int affectors = (int)(hormones.Length * emotion);

            //Spawn a couple new hormone with that type
            for (int i = 0; i < (int)emotion * 20; i++){
                Hormone spawn = new Hormone(type);
                List<Hormone> hormones_L = hormones.ToList();
                hormones_L.Add(spawn);
                hormones = hormones_L.ToArray();
                CreateHormoneVisual(spawn);
            }

            //Loop through
            for (int i = 0; i < affectors; i++)
            {
                //Get the Hormone
                Hormone hormone = hormones[i];

                //Get its type
                HormoneType hormoneType = hormone.type;

                if (hormoneType == type) 
                {
                    //Increase Strength using Formula
                    hormone.strength = Math.Pow(Math.Sin(hormone.strength * 2) * emotion, 1.2);
                    KDebug.Log("Ok It works");

                    //If its strength is greater than the override value, duplicate emotion
                    if (hormone.strength >= override_value)
                    {
                        //Get the Next Hormone
                        try{
                            Hormone next_hormone = hormones[i + 1];

                            //Change its type but lower the strength
                            next_hormone.type = type;
                            next_hormone.strength += Math.Pow(Math.Sin(hormone.strength / 2), 2);
                        }

                        catch{

                        }
                    }
                }

                else if (type != hormoneType)
                {
                    //Change Type but lower strength
                    hormone.type = type;
                    hormone.strength /= (i / 69);
                }

                //Update
                UpdateVisuals();
            }
        }

        internal void CreateHormoneVisual(Hormone h)
        {
            GameObject n = Instantiate(Prefab, 
                canvas.transform.position+(Vector3)(UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(-450f, 450f))
                ,Quaternion.Euler(Vector3.zero));
            n.transform.SetParent(canvas.transform);
            Image image = n.GetComponent<Image>();
            image.color = HormoneToColor(h);
            h.Representation = image;
        }

        private void Start()
        {
            //Get Strength
            strength = 2;

            //Initialize Hormones
            List<Hormone> hormones_L = new List<Hormone>();
            for (int i = 0; i < Hormones; i++)
            {
                Hormone myBoi = new Hormone(HormoneType.Phyprotencin);
                hormones_L.Add(myBoi);
                CreateHormoneVisual(myBoi);
            }
            hormones = hormones_L.ToArray();

            React(10, HormoneType.Phyprotencin);
            React(50, HormoneType.Dopamine);
        }

        internal Color HormoneToColor(Hormone hormone)
        {
            if (hormone.type == HormoneType.Dopamine){
                return new Color(Color.blue.r, (float)(Color.blue.g * hormone.strength), (float)(Color.blue.b * hormone.strength));
            }
            if (hormone.type == HormoneType.Phyprotencin){
                return new Color(Color.gray.r, Color.gray.g, (float)(Color.gray.b * hormone.strength));
            }
            if (hormone.type == HormoneType.Serotinin){
                return new Color((float)(Color.red.r * hormone.strength), Color.red.g, Color.red.b);
            }
            return Color.black;
        }

        internal void UpdateVisuals()
        {
            foreach (Hormone item in hormones){
                item.Representation.color = HormoneToColor(item);
            }
        }
    }

    internal enum HormoneType{
        Dopamine,
        Serotinin,
        Phyprotencin
    }

    internal class Hormone
    {
        internal HormoneType type { get; set; }

        internal double strength { get; set; }

        internal Image Representation { get; set; }

        public Hormone(HormoneType type){
            this.strength = 1 * UnityEngine.Random.value;
            this.type = type;
        }
    }
}
