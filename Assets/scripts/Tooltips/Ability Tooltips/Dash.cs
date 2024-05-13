using UnityEngine;
using UnityEngine.EventSystems;


public class Dash : TooltipTrigger, IPointerEnterHandler, IPointerExitHandler
{

    public override void OnPointerEnter(PointerEventData eventData)
    {

        ArcherAbilities archerAbilities = GameObject.FindGameObjectWithTag("Player").GetComponent<ArcherAbilities>();
        int dashlvl = archerAbilities.dashLvl;
        if (dashlvl == 0)
        {
            header = "Dash";
            content = "Dash a short distance towards your mouse cursor location. \nLevel: " + dashlvl.ToString() + " \nCooldown: 10 / 5 / 3 / 1.5 / 1 seconds";
            TooltipManager.Show(content, header);
        }
        if (dashlvl == 1)
        {
            header = "Dash";
            content = "Dash a short distance towards your mouse cursor location. \nLevel: " + dashlvl.ToString() + " \nCooldown: <b>10</b> / 5 / 3 / 1.5 / 1 seconds";
            TooltipManager.Show(content, header);
        }
        if (dashlvl == 2)
        {
            header = "Dash";
            content = "Dash a short distance towards your mouse cursor location. \nLevel: " + dashlvl.ToString() + " \nCooldown: 10 / <b>5</b> / 3 / 1.5 / 1 seconds";
            TooltipManager.Show(content, header);
        }
        if (dashlvl == 3)
        {
            header = "Dash";
            content = "Dash a short distance towards your mouse cursor location. \nLevel: " + dashlvl.ToString() + " \nCooldown: 3 -> 1.5 seconds";
            TooltipManager.Show(content, header);
        }
        if (dashlvl == 4)
        {
            header = "Dash";
            content = "Dash a short distance towards your mouse cursor location. \nLevel: " + dashlvl.ToString() + " \nCooldown: 1.5 -> 1 seconds";
            TooltipManager.Show(content, header);
        }
        if (dashlvl == 5)
        {
            header = "Dash";
            content = "Dash a short distance towards your mouse cursor location. \nLevel: " + dashlvl.ToString() + " \nCooldown: 1 seconds";
            TooltipManager.Show(content, header);
        }

    }
}
