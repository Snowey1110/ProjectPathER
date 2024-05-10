using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Start is called before the first frame update
    public string message;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager._instance.setAndShowToolTip(message);
    }

    
    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager._instance.hideToolTip();
    }
    
}

