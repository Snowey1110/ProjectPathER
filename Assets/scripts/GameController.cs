using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    public MapGenerator mapgen;

    void Start()
    {
        mapgen.DrawMapInEditor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

