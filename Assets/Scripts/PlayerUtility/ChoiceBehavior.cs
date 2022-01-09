using UnityEngine;

public class ChoiceBehavior : MonoBehaviour
{
    public ChoiceTypes ChoiceType;

    public void SendMessage(string action, int button)
    {
        if (action == "Pressed") Press(button);

        if (action == "Error"){
            Debug.LogError("Trash Kill Noobs");
            return;
        }
    }

    private void Press(int Index)
    {
        if (ChoiceType == ChoiceTypes.NAN) return;

        if (ChoiceType == ChoiceTypes.Test)
        {
            if (Index == 1){
                DialougueManager.Instance.HideDialougue();
                DialougueManager.Instance.LoadDialougue("Calvin.", "Do you want to form a Techno Fan community?");

                DialougueManager.Instance.LoadChoice("Yes", "No", this);
            }

            else if (Index == 2){
                DialougueManager.Instance.HideDialougue();
                DialougueManager.Instance.LoadDialougue("Calvin.", "Why?");

                DialougueManager.Instance.LoadChoice("Because he's bad", "Idduno", this);
            }
        }
    }
}

public enum ChoiceTypes
{
    Test,
    NAN
}
