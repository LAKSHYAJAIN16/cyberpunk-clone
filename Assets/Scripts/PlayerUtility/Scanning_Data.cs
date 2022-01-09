using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanning_Data : MonoBehaviour
{
    //Data 
    public string Name;
    public string Description;
    public string Specimen;
    public string Affiliation;
    public string CrimeStatus;

    //Behavior
    public Material NormalMaterial;
    public Material SelectedMaterial;

    public void SwitchMat(bool action)
    {
        try
        {
            if (action) this.GetComponent<MeshRenderer>().material = SelectedMaterial;
            else if (!action) this.GetComponent<MeshRenderer>().material = NormalMaterial;
        }

        catch(MissingComponentException)
        {
            if (action) this.GetComponent<SkinnedMeshRenderer>().material = SelectedMaterial;
            else if (!action) this.GetComponent<SkinnedMeshRenderer>().material = NormalMaterial;
        }
    }

    public string GetAffiliation{
        get{
            return Affiliation;
        }
    }

    public string GetName {
        get{
            return Name;
        }
    }

    public string GetSpecimen{
        get{
            return Specimen;
        }
    }

    public string GetCrime{
        get{
            return CrimeStatus;
        }
    }
    
    public string GetDes{
        get{
            return Description;
        }
    }
}
