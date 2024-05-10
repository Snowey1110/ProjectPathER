using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Code inspired by BMo, video: https://youtu.be/y2N_J391ptg?si=O1Td_A-A7zCR_3iC
public class TooltipManager : MonoBehaviour
{

    public static TooltipManager _instance;
    public TextMeshProUGUI textComponent;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = true;
        gameObject.SetActive(false);
        textComponent.canvas.sortingOrder = 999;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Input.mousePosition;
    }

    public void setAndShowToolTip(string message)
    {
        gameObject.SetActive(true);
        textComponent.text = message;
    }

    public void hideToolTip()
    {
        gameObject.SetActive(false);
        textComponent.text = string.Empty;
    }
}
