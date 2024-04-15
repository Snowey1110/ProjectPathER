using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterClass
{
    public string characterClassName;


    public string CharacterClassName
    {
        get { return characterClassName; }
        set { characterClassName = value; }
        
    }

    public string getName()
    {
        return characterClassName;
    }
}

   
