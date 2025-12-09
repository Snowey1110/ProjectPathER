using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundMenu : MenuToggle
{
    public GameObject smb; //Sound Menu background

    void Start()
    {
        if (smb != null)
        {
            smb.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        }
    }

}
