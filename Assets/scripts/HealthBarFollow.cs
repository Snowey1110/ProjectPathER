using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Put this script on Health Bar Canvas
public class HealthBarFollow : MonoBehaviour
{
    public Transform objectToFollow;
    RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    private void Update()
    {
        rectTransform.anchoredPosition = objectToFollow.localPosition;
    }
}
