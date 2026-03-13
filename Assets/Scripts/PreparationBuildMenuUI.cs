using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TowerMenuEntryUI
{
    public Button button;
    public TMP_Text label;
}

public class PreparationBuildMenuUI : MonoBehaviour
{
    [SerializeField] private TowerBuildController buildController;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameEconomy economy;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private List<TowerMenuEntryUI> entries = new();
    [SerializeField] private CanvasGroup panelCanvasGroup;

    private bool buttonsBound;
    private bool isDraggingBuildItem;

    private void Awake()
    {
        if (buildController == null)
        {
            buildController = FindFirstObjectByType<TowerBuildController>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (economy == null)
        {
            economy = FindFirstObjectByType<GameEconomy>();
        }

        if (panelCanvasGroup == null && panelRoot != null)
        {
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.PhaseChanged += OnPhaseChanged;
        }

        if (economy != null)
        {
            economy.GoldChanged += OnGoldChanged;
        }

        BindButtons();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.PhaseChanged -= OnPhaseChanged;
        }

        if (economy != null)
        {
            economy.GoldChanged -= OnGoldChanged;
        }

        HideTooltip();
    }

    private void Update()
    {
        RefreshButtons();
    }

    private void OnPhaseChanged(RoundPhase _)
    {
        RefreshAll();
    }

    private void OnGoldChanged(int _)
    {
        RefreshButtons();
    }

    private void BindButtons()
    {
        if (buttonsBound || buildController == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            int index = i;
            TowerMenuEntryUI entry = entries[i];
            if (entry == null || entry.button == null)
            {
                continue;
            }

            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => OnEntryClicked(index));

            TowerMenuTooltipTrigger trigger = entry.button.GetComponent<TowerMenuTooltipTrigger>();
            if (trigger == null)
            {
                trigger = entry.button.gameObject.AddComponent<TowerMenuTooltipTrigger>();
            }

            trigger.Setup(this, index);
        }

        buttonsBound = true;
    }

    private void OnEntryClicked(int index)
    {
        if (buildController == null || !buildController.CanBuildInCurrentPhase())
        {
            return;
        }

        buildController.SelectBuildOption(index);
        RefreshButtons();
    }

    private void RefreshAll()
    {
        RefreshPanelVisibility();
        RefreshButtons();
    }

    private void RefreshPanelVisibility()
    {
        if (panelRoot == null)
        {
            return;
        }

        bool visible = buildController != null && buildController.CanBuildInCurrentPhase();
        panelRoot.SetActive(visible);
        if (!visible)
        {
            HideTooltip();
        }
        else
        {
            ApplyDragVisualState();
        }
    }

    private void RefreshButtons()
    {
        if (buildController == null)
        {
            return;
        }

        int selected = buildController.GetSelectedOptionIndex();
        int optionCount = buildController.GetBuildOptionCount();
        bool canBuild = buildController.CanBuildInCurrentPhase();
        int currentGold = economy != null ? economy.CurrentGold : 0;

        if (titleText != null)
        {
            titleText.text = canBuild ? "Preparation: choose tower" : "Battle: building disabled";
        }

        for (int i = 0; i < entries.Count; i++)
        {
            TowerMenuEntryUI entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            TowerBuildOption option = i < optionCount ? buildController.GetBuildOption(i) : null;
            bool valid = option != null && option.towerData != null;

            if (entry.label != null)
            {
                entry.label.text = BuildLabel(option, i, valid, selected == i);
            }

            if (entry.button != null)
            {
                bool enoughGold = valid && currentGold >= option.towerData.cost;
                entry.button.interactable = valid && canBuild && enoughGold;
            }
        }
    }

    private string BuildLabel(TowerBuildOption option, int index, bool valid, bool selected)
    {
        if (!valid)
        {
            return $"[{index + 1}] Empty";
        }

        TowerData data = option.towerData;
        string marker = selected ? "> " : string.Empty;
        return $"{marker}{data.towerType} ({data.cost}g)";
    }

    public void ShowTooltipForIndex(int index)
    {
        if (tooltipRoot == null || tooltipText == null || buildController == null)
        {
            return;
        }

        TowerBuildOption option = buildController.GetBuildOption(index);
        if (option == null || option.towerData == null)
        {
            HideTooltip();
            return;
        }

        tooltipText.text = BuildTooltipText(option.towerData);
        tooltipRoot.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    public void OnDragBuildStarted()
    {
        isDraggingBuildItem = true;
        HideTooltip();
        ApplyDragVisualState();
    }

    public void OnDragBuildFinished()
    {
        isDraggingBuildItem = false;
        ApplyDragVisualState();
    }

    private void ApplyDragVisualState()
    {
        if (panelCanvasGroup == null || panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        panelCanvasGroup.alpha = isDraggingBuildItem ? 0f : 1f;
        panelCanvasGroup.blocksRaycasts = !isDraggingBuildItem;
        panelCanvasGroup.interactable = !isDraggingBuildItem;
    }

    private string BuildTooltipText(TowerData data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        string role = data.towerType switch
        {
            TowerType.Archer => "Single target, balanced DPS.",
            TowerType.Mage => "AoE damage in impact radius.",
            TowerType.Freezer => "Applies slow effect to enemy.",
            TowerType.Cannon => "High single-target burst damage.",
            _ => "Tower"
        };

        string extra = data.towerType switch
        {
            TowerType.Mage => $"\nAoE Radius: {data.splashRadius:0.0}",
            TowerType.Freezer => $"\nSlow: {data.slowMultiplier:0.00} for {data.slowDuration:0.0}s",
            _ => string.Empty
        };

        return $"{data.towerType}\nCost: {data.cost}g\nDamage: {data.damage}\nRate: {data.attacksPerSecond:0.00}/s\nRange: {data.range:0.0}\nProjectile Speed: {data.projectileSpeed:0.0}\n{role}{extra}";
    }
}
