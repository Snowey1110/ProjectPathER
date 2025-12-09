using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class keepOneCopy : MonoBehaviour
{
    private static GameObject instance;
    public GameObject hideAtStart;

    private void Awake()
    {
        if (instance == null)
        {
            // If this is the first instance, set it as the instance
            instance = gameObject;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If there is already an instance, destroy this one
            Destroy(gameObject);
        }
    }


}
