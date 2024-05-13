using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TooltipManager : MonoBehaviour
{

    public static TooltipManager _instance;
    public Tooltip tooltip;

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


    public static void Show(string content, string header="")
    {
        _instance.tooltip.SetText(content, header);
        _instance.tooltip.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        _instance.tooltip.gameObject.SetActive(false);
    }
}
