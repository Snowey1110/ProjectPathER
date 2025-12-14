using UnityEngine;
using UnityEngine.EventSystems;

public class Dash : TooltipTrigger, IPointerEnterHandler, IPointerExitHandler
{
    public override void OnPointerEnter(PointerEventData eventData)
    {
        // Find the Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Get the specific DashAbility component
        // (This assumes you attached DashAbility to the player prefab)
        DashAbility dashAbility = player.GetComponent<DashAbility>();

        // Default to 0 if the player doesn't have the ability yet
        int dashlvl = (dashAbility != null) ? dashAbility.currentLevel : 0;

        // Set the text
        header = "Dash";

        // Define the cooldown string based on level
        string cdText = "";

        switch (dashlvl)
        {
            case 0:
                cdText = "10 / 5 / 3 / 1.5 / 1";
                break;
            case 1:
                cdText = "<b>10</b> / 5 / 3 / 1.5 / 1";
                break;
            case 2:
                cdText = "10 / <b>5</b> / 3 / 1.5 / 1";
                break;
            case 3:
                cdText = "10 / 5 / <b>3</b> / 1.5 / 1";
                break;
            case 4:
                cdText = "10 / 5 / 3 / <b>1.5</b> / 1";
                break;
            case 5:
                cdText = "10 / 5 / 3 / 1.5 / <b>1</b>";
                break;
            default: // Level > 5
                cdText = "<b>1</b>";
                break;
        }

        content = $"Dash a short distance towards your mouse cursor location.\nLevel: {dashlvl}\nCooldown: {cdText} seconds";

        TooltipManager.Show(content, header);
    }
}