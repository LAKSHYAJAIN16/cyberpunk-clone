using UnityEngine;
using TMPro;
using Player;

namespace Player
{
    public class Scanner : MonoBehaviour
    {
        //Instance
        public static Scanner Instance { get; set; }

        //Functinality
        public Camera Cam;
        public float zoomFOv, LerpingSpeed;
        public GameObject postPro;
        public float MaxDistance = 1000f;

        //UI
        public GameObject UiHolder;
        public TextMeshProUGUI nameTXT, descriptionTXT, affiliationTXT, crimeTXT, specimenTXT;

        //Normalized vals;
        private float normFOV, FOV, normMaxDist;

        //Some Bools :L
        private bool isZomming, SLT = false;

        //Array of ScanningData
        private Scanning_Data[] AllScanables;

        private void Awake(){
            Instance = this;
        }

        private void Start()
        {
            normFOV = Cam.fieldOfView;
            normMaxDist = MaxDistance;
            AllScanables = FindObjectsOfType<Scanning_Data>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) UpdateBool();

            if (isZomming)
            {
                Zoom();
            }

            else if (!isZomming)
            {
                FOV = Mathf.Lerp(FOV, normFOV, Time.deltaTime * LerpingSpeed);
                postPro.SetActive(false);
                HideUI();
            }

            Cam.fieldOfView = FOV;
        }

        private void Zoom()
        {
            //Lerp FOV
            if (MaxDistance == normMaxDist)
                FOV = Mathf.Lerp(FOV, zoomFOv, Time.deltaTime * LerpingSpeed);

            //PostPro
            postPro.SetActive(true);

            //Raycast vars
            Vector3 pos = Cam.transform.position;
            Vector3 dir = Cam.transform.forward;
            RaycastHit hit;

            //If we zoomed last time, well, zoom again
            if (SLT) MaxDistance = normMaxDist * 2f;

            //Zoom with C, Zoom out with x
            float zoomIncrement = 1f;
            if (Input.GetKey(KeyCode.C))
            {
                FOV -= zoomIncrement;
                MaxDistance += zoomIncrement;
                PlayerMovement.Instance.ChangeSens(-zoomIncrement);
            }

            if (Input.GetKey(KeyCode.X))
            {
                FOV += zoomIncrement;
                MaxDistance -= zoomIncrement;
                PlayerMovement.Instance.ChangeSens(zoomIncrement);
            }

            //Shoot RayCast
            if (Physics.Raycast(pos, dir, out hit, MaxDistance))
            {
                if (hit.transform.TryGetComponent(out Scanning_Data sc))
                {
                    UpdateUI(sc);
                    SLT = true;
                    return;
                }

                else
                {
                    HideUI();
                    SLT = false;
                }
            }
        }

        private void UpdateBool()
        {
            //Update bool
            isZomming = !isZomming;

            //Get array and switch material
            AllScanables = FindObjectsOfType<Scanning_Data>();
            foreach (Scanning_Data curreentObj in AllScanables)
            {
                curreentObj.SwitchMat(isZomming);
            }

            //Reset some vals
            MaxDistance = normMaxDist;
            PlayerMovement.Instance.ResetSens();
            SLT = false;
        }

        private void UpdateUI(Scanning_Data data)
        {
            //Get values
            string name = data.GetName;
            string affiliation = data.GetAffiliation;
            string crime = data.GetCrime;
            string specimen = data.GetSpecimen;
            string description = data.GetDes;

            //Update UI accondringly
            UiHolder.SetActive(true);
            nameTXT.text = name;
            affiliationTXT.text = affiliation;
            crimeTXT.text = crime;
            specimenTXT.text = specimen;
            descriptionTXT.text = description;

            //Change mat
            AllScanables = FindObjectsOfType<Scanning_Data>();
            foreach (Scanning_Data item in AllScanables)
            {
                item.SwitchMat(true);
            }
            data.SwitchMat(false);
        }

        private void HideUI()
        {
            UiHolder.SetActive(false);

            if (isZomming)
            {
                //Change material
                foreach (Scanning_Data curreentObj in AllScanables)
                {
                    curreentObj.SwitchMat(true);
                }
            }
        }
    }
}
