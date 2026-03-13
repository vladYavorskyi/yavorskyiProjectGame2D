using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerDragBuildItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TowerBuildController buildController;
    [SerializeField] private int optionIndex;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Image sourceIcon;
    [SerializeField] private Image dragGhostPrefab;
    [SerializeField] private float ghostAlpha = 0.85f;
    [SerializeField] private PreparationBuildMenuUI buildMenuUI;

    private Image activeGhost;
    private RectTransform activeGhostRect;

    private void Awake()
    {
        if (buildController == null)
        {
            buildController = FindFirstObjectByType<TowerBuildController>();
        }

        if (uiCanvas == null)
        {
            uiCanvas = GetComponentInParent<Canvas>();
        }

        if (sourceIcon == null)
        {
            sourceIcon = GetComponent<Image>();
        }

        if (buildMenuUI == null)
        {
            buildMenuUI = FindFirstObjectByType<PreparationBuildMenuUI>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
        {
            return;
        }

        if (buildMenuUI != null)
        {
            buildMenuUI.OnDragBuildStarted();
        }

        CreateGhost();
        MoveGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeGhostRect == null)
        {
            return;
        }

        MoveGhost(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activeGhost != null)
        {
            Destroy(activeGhost.gameObject);
            activeGhost = null;
            activeGhostRect = null;
        }

        if (!CanStartDrag())
        {
            if (buildMenuUI != null)
            {
                buildMenuUI.OnDragBuildFinished();
            }

            return;
        }

        buildController.TryBuildAtScreenPosition(eventData.position, optionIndex);
        if (buildMenuUI != null)
        {
            buildMenuUI.OnDragBuildFinished();
        }
    }

    private bool CanStartDrag()
    {
        return buildController != null
            && buildController.CanBuildInCurrentPhase()
            && buildController.CanAffordOption(optionIndex);
    }

    private void CreateGhost()
    {
        if (uiCanvas == null)
        {
            return;
        }

        Image ghost = null;
        if (dragGhostPrefab != null)
        {
            ghost = Instantiate(dragGhostPrefab, uiCanvas.transform);
        }
        else
        {
            GameObject go = new("TowerDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(uiCanvas.transform, false);
            ghost = go.GetComponent<Image>();
        }

        ghost.raycastTarget = false;
        if (sourceIcon != null)
        {
            ghost.sprite = sourceIcon.sprite;
            ghost.type = sourceIcon.type;
            ghost.preserveAspect = true;
            ghost.rectTransform.sizeDelta = sourceIcon.rectTransform.rect.size;
        }

        Color c = ghost.color;
        c.a = Mathf.Clamp01(ghostAlpha);
        ghost.color = c;

        activeGhost = ghost;
        activeGhostRect = ghost.rectTransform;
        activeGhostRect.SetAsLastSibling();
    }

    private void MoveGhost(Vector2 screenPosition)
    {
        if (uiCanvas == null || activeGhostRect == null)
        {
            return;
        }

        RectTransform canvasRect = uiCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Camera cam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPos))
        {
            activeGhostRect.anchoredPosition = localPos;
        }
    }
}
