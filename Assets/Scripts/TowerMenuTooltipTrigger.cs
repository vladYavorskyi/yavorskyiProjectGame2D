using UnityEngine;
using UnityEngine.EventSystems;

public class TowerMenuTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PreparationBuildMenuUI menuUI;
    private int optionIndex;

    public void Setup(PreparationBuildMenuUI menu, int index)
    {
        menuUI = menu;
        optionIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menuUI != null)
        {
            menuUI.ShowTooltipForIndex(optionIndex);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (menuUI != null)
        {
            menuUI.HideTooltip();
        }
    }
}
