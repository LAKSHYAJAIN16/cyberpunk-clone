using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DialougueManager : MonoBehaviour
{
    //Instance
    public static DialougueManager Instance { get; set; }

    //Test
    public bool TestDialougue, TestChoice;
    public ChoiceBehavior TestBehavior;

    //Dialougue
    public TextMeshProUGUI nameTXT, dialougueTXT;
    public GameObject DialougueHolder;

    //Choice
    public GameObject ChoiceHolder;
    public TextMeshProUGUI Choice1TXT, Choice2TXT;
    private ChoiceBehavior CIQ;

    //Input
    private float mouseScroll;
    private int SelectedOption = 1;
    private GameObject o1, o2;

    //Bug Fixing
    public const int MaxCharInDial = 2;
    public const int MaxCharInChoi = 23;

    private void Start()
    {
        //Instance
        Instance = this;

        //Get button OBj
        o1 = Choice1TXT.transform.GetChild(0).gameObject;
        o2 = Choice2TXT.transform.GetChild(0).gameObject;

        //Set all the Containers to false
        ChoiceHolder.SetActive(false);
        DialougueHolder.SetActive(false);

        //Test stuff
        if (TestDialougue){
            LoadDialougue("Calvin.","I Like Technoblode");
        }

        if (TestChoice){
            LoadChoice("Me too!", "Nah bub", TestBehavior);
        }
    }

    private void Update()
    {
        //Get prev selected
        int prevS = SelectedOption;

        //Get Alpha Input
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectedOption = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectedOption = 2;

        //Mouse scroll Input
        mouseScroll = Input.GetAxis("Mouse ScrollWheel");

        //Calibrate MouseInput
        if (mouseScroll < 0f){
            if (SelectedOption == 1) SelectedOption = 2;
        }

        else if (mouseScroll > 0f){
            if (SelectedOption == 2) SelectedOption = 1;
        }

        //Then, if our value has changed, call our method to switch the Option
        if (DialougueHolder.activeSelf && prevS != SelectedOption) SwitchOption();

        //If we press the mouse Button OR press Enter, Press that button
        if (Input.GetMouseButtonDown(0) && ChoiceHolder.activeSelf || Input.GetKeyDown(KeyCode.Return) && ChoiceHolder.activeSelf) PressedChoice(SelectedOption);
    }

    public void LoadDialougue(string name, string dialougue)
    {
        try{
            //Enable Dialougue Holder
            DialougueHolder.SetActive(true);

            //Just assign name as it is
            nameTXT.text = name;

            string addon = "";

            //Find out how many characters are in the name
            int noOfCharacters = name.ToCharArray().Length;

            //If it is one, don't do anything
            if (noOfCharacters == MaxCharInDial) addon = "";

            //Else, loop through number of characters and add a space
            else if (noOfCharacters > MaxCharInDial)
            {
                for (int i = 0; i < noOfCharacters; i++) {
                    addon += " ";
                }
            }

            if (noOfCharacters > MaxCharInDial + 3) addon += "   ";

            //FInally, attach addon to string and add the text
            dialougueTXT.text = addon + dialougue;
        }

        catch (ArgumentNullException){
            Debug.LogError("WHAT BRO I MEAN COME ON YOU CALLED IT FROM A DIFFERENT THREAD BUB WTF BRO UNSUB FROM TECHNO");
            return;
        }

        catch (NullReferenceException){
            Debug.LogError("WHAT BRO I MEAN COME ON YOU CALLED IT FROM A DIFFERENT THREAD BUB WTF BRO UNSUB FROM TECHNO");
            return;
        }
    }

    public void HideDialougue()
    {
        dialougueTXT.text = "";
        nameTXT.text = "";
        DialougueHolder.SetActive(false);
    }

    public void LoadChoice(string firstChoice, string SecondChoice, ChoiceBehavior ch)
    {
        //Enable choice holder
        ChoiceHolder.SetActive(true);

        //Set choice txts to texts
        Choice1TXT.text = firstChoice;
        Choice2TXT.text = SecondChoice;

        //Set selected to 1
        SelectedOption = 1;
        SwitchOption();

        //Assign choice Behavior
        this.CIQ = ch;
    }

    public void PressedChoice(int number)
    {
        try{
            //Disable choice GO
            ChoiceHolder.SetActive(false);

            //Send message to ChoiceBehavior
            CIQ.SendMessage("Pressed", number);

        }

        catch(NullReferenceException){
            CIQ.SendMessage("Error", number);
        }
    }

    public void SwitchOption()
    {
        if (SelectedOption == 1){
            o1.SetActive(true);
            o2.SetActive(false);
        }

        else if (SelectedOption == 2){
            o1.SetActive(false);
            o2.SetActive(true);
        }
    }
}
