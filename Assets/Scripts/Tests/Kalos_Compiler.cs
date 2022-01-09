using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Kalos_Compiler : MonoBehaviour
{
    public string[] Code;

    //Variables
    public Dictionary<string, float> FloatVariables = new Dictionary<string, float>();
    public Dictionary<string, int> IntVariables = new Dictionary<string, int>();
    public int index;

    public static readonly string LegalCharacters = "abcdefghijklmnopqrstuvwxyz1234567890<>?/|][{}()*&^%$#@!=.";

    //Math Operators
    public static readonly string[] MathOperators =
    {
        "+=",
        "-=",
        "*=",
        "/=",
        "//=",
        "**="
    };

    //Other functions
    public static readonly string[] OtherFunctions =
    {
        "yeet",
        "delVar",
        "wholeDel"
    };

    //Data types
    public static readonly string[] DataTypes =
    {
        "int",
        "float",
        "bob"
    };

    private void Start(){
        CompileCode();
    }

    private void CompileCode()
    {
        //Get variables
        Dictionary<string, float>.ValueCollection values = FloatVariables.Values;

        //LegalCharacters
        char[] characters = LegalCharacters.ToCharArray();

        index = 0;
        foreach (string character in Code)
        {
            //Check for illegal character
            char[] stepCh = character.ToCharArray();

            bool illegalString = false;
            for (int i = 0; i < stepCh.Length + 1; i++)
            {
                if (i == stepCh.Length) {
                    illegalString = true;
                    break;
                }

                if (illegalString) break;

                char characterLiteral = stepCh[i];
                for (int x = 0; x < characters.Length + 1; x++)
                {
                    if (x == characters.Length) {
                        illegalString = true;
                        Debug.LogError("We're sorry.Illegal string Literal : " + characterLiteral);
                        break;
                    }

                    if (characterLiteral == characters[x])
                        break;
                }
            }

            //Get prev Character
            string prevCharacter = Code[index];
            illegalString = false;

            //Add index
            index++;

            //Check for math Operators
            if (character == "=") {
                EqualsToOperator();
            }

            if (character == "+=") {
                AddToEqual();
            }

            if (character == "-=") {
                SubToEqual();
            }

            if (character == "*="){
                MulToEqual();
            }

            if (character == "**="){
                ExpToEqual();
            }

            //Check for Debug
            if (character == "yeet"){
                WriteInConsole();
            }

            //Check for Delete
            if (character == "delVar"){
                DeleteKey();
            }

            if (character == "wholeDel"){
                WholeDelete();
            }
        }
    }

    private void EqualsToOperator()
    {
        string dataType;

        //Check to see if we've given a data type
        try{
            string variableType = Code[index - 3];
            dataType = variableType;
            if (dataType != "int" && dataType != "float") dataType = "bob";

            if (FloatVariables.ContainsKey(dataType))
                dataType = "float";
            else if (IntVariables.ContainsKey(dataType))
                dataType = "int";
        }

        catch(IndexOutOfRangeException){
            dataType = "bob";
        }

        //Get variable name
        string varName = Code[index - 2];

        //Get string left hand
        string leftHand = Code[index];

        //Assign local value
        float value = 0f;

        //Check for bob
        if (dataType == "bob"){
            dataType = GetDataType(leftHand);
        }

        //Check if the leftHand is a variable
        if (FloatVariables.ContainsKey(leftHand) || IntVariables.ContainsKey(leftHand)) {
            value = FloatVariables[leftHand];
        }


        //Else, just parse the value, accoring to data type
        if (!FloatVariables.ContainsKey(leftHand) && dataType == "float"){
            value = float.Parse(Code[index]);
        }

        if (!IntVariables.ContainsKey(leftHand) && dataType == "int"){
            value = int.Parse(Code[index]);
        }

        //Assign it to dictonary(if the key is not already made)
        if (!FloatVariables.ContainsKey(varName) && dataType == "float") {
            FloatVariables.Add(varName, value);
            return;
        }

        if (!IntVariables.ContainsKey(varName) && dataType == "int"){
            IntVariables.Add(varName, (int)value);
        }

        //If the variable is already made, modify value
        if (FloatVariables.ContainsKey(varName)&&dataType == "float"){
            FloatVariables[varName] = value;
        }

        if (IntVariables.ContainsKey(varName) && dataType == "int"){
            IntVariables[varName] = (int)value;
        }
    }

    private void AddToEqual()
    {
        //Get variable name
        string varName = Code[index - 2];

        //Get left hand, the value we need to operate
        string leftHand = Code[index];

        //Assign local valueToAdd
        float valueToAdd = 1f;

        //Check if the leftHand is a variable
        if (FloatVariables.ContainsKey(leftHand)){
            valueToAdd = FloatVariables[leftHand];
        }

        //Else, just parse the value
        else if (!FloatVariables.ContainsKey(leftHand)){
            valueToAdd = float.Parse(Code[index]);
        }

        //Get already existing value
        float existingValue = FloatVariables[varName];

        //Modify value
        float newVal = existingValue + valueToAdd;

        //Assign value to dictonary
        FloatVariables[varName] = newVal;
    }

    private void SubToEqual()
    {
        //Get variable name
        string varName = Code[index - 2];

        //Get left hand, the value we need to operate
        string leftHand = Code[index];

        //Assign local valueToAdd
        float valueToAdd = 1f;

        //Check if the leftHand is a variable
        if (FloatVariables.ContainsKey(leftHand))
        {
            valueToAdd = FloatVariables[leftHand];
        }

        //Else, just parse the value
        else if (!FloatVariables.ContainsKey(leftHand))
        {
            valueToAdd = float.Parse(Code[index]);
        }

        //Get already existing value
        float existingValue = FloatVariables[varName];

        //Modify value
        float newVal = existingValue - valueToAdd;

        //Assign value to dictonary
        FloatVariables[varName] = newVal;
    }

    private void MulToEqual()
    {
        //Get variable name
        string varName = Code[index - 2];

        //Get left hand, the value we need to operate
        string leftHand = Code[index];

        //Assign local valueToAdd
        float valueToAdd = 1f;

        //Check if the leftHand is a variable
        if (FloatVariables.ContainsKey(leftHand))
        {
            valueToAdd = FloatVariables[leftHand];
        }

        //Else, just parse the value
        else if (!FloatVariables.ContainsKey(leftHand))
        {
            valueToAdd = float.Parse(Code[index]);
        }

        //Get already existing value
        float existingValue = FloatVariables[varName];

        //Modify value
        float newVal = existingValue * valueToAdd;

        //Assign value to dictonary
        FloatVariables[varName] = newVal;
    }

    private void ExpToEqual()
    {
        //Get variable name
        string varName = Code[index - 2];

        //Get left hand, the value we need to operate
        string leftHand = Code[index];

        //Assign local valueToAdd
        float valueToAdd = 1f;

        //Check if the leftHand is a variable
        if (FloatVariables.ContainsKey(leftHand))
        {
            valueToAdd = FloatVariables[leftHand];
        }

        //Else, just parse the value
        else if (!FloatVariables.ContainsKey(leftHand))
        {
            valueToAdd = float.Parse(Code[index]);
        }

        //Get already existing value
        float existingValue = FloatVariables[varName];

        //Modify value
        float newVal = (float)Math.Pow((double)existingValue, (double)valueToAdd);

        //Assign value to dictonary
        FloatVariables[varName] = newVal;
    }

    private void WriteInConsole()
    {
        //String to print
        string text = "NAN";

        //First we just try to parse it if its just a float
        try{
            float x = float.Parse(Code[index]);
            text = x.ToString();
        }

        catch (FormatException)
        {
            try
            {
                if (FloatVariables.ContainsKey(Code[index])){
                    //Is variable, set text equal to value
                    text = FloatVariables[Code[index]].ToString();
                }

                //Else, just get the face value
                else{
                    text = Code[index];
                }
            }

            catch (IndexOutOfRangeException)
            {
                //print face
                text = Code[index];
            }
        }

        //print string
        print(text);
    }

    private void DeleteKey()
    {
        //Get variable name
        string varName = Code[index];

        //If the variable with this name exists, delete key
        if (FloatVariables.ContainsKey(varName))
            FloatVariables.Remove(varName);

        else{
            Debug.Log("Error : No key with that name found");
        }
    }

    private void WholeDelete()
    {
        FloatVariables.Clear();
        IntVariables.Clear();
    }

    private string GetDataType(string value)
    {
        //Get value in float
        float val = float.Parse(value);

        //Check for float
        if (val % 1 != 0)
            return "float";

        else
            return "int";
    }
}
