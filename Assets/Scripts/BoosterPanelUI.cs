// BoosterPanelUI.cs (REVISED to be a Listener)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BoosterPanelUI : MonoBehaviour
{
    [Header("Core Components")]
    public Animator panelAnimator;
    public GameObject closeBlocker;
    public Button pullTabButton;

    [Header("Turbo Paw UI")]
    public GameObject turboPawItemContainer; // The parent object for the whole row
    public TextMeshProUGUI turboPawCountText;
    public Button useTurboPawButton;
    public Button getMoreTurboPawsButton;
    public Image turboPawSelectionOverlay;

    [Header("Lucky Cat UI")]
    public GameObject luckyCatItemContainer; // The parent object for the whole row
    public TextMeshProUGUI luckyCatCountText;
    public Button useLuckyCatButton;
    public Button getMoreLuckyCatsButton;
    public Image luckyCatSelectionOverlay;

    [Header("Active Booster Timers")]
    public GameObject activeTurboPawUIPanel;
    public Image activeTurboPawTimerFill;
    public TextMeshProUGUI activeTurboPawTimerText;
    public GameObject activeLuckyCatUIPanel;
    public Image activeLuckyCatTimerFill;
    public TextMeshProUGUI activeLuckyCatTimerText;

    private bool isPanelOpen = false;
    private bool turboPawSelected = false;
    private bool luckyCatSelected = false;
    private Coroutine turboPawTimerCoroutine;
    private Coroutine luckyCatTimerCoroutine;

    // --- Event Subscription ---
    private void OnEnable()
    {
        BoosterManager.OnBoosterDataChanged += RefreshAllUI;
    }

    private void OnDisable()
    {
        BoosterManager.OnBoosterDataChanged -= RefreshAllUI;
    }

    private void Start()
    {
        pullTabButton.onClick.AddListener(TogglePanel);
        closeBlocker.GetComponent<Button>().onClick.AddListener(ClosePanel);
        useTurboPawButton.onClick.AddListener(OnSelectTurboPaw);
        useLuckyCatButton.onClick.AddListener(OnSelectLuckyCat);
        getMoreLuckyCatsButton.onClick.AddListener(() => FindObjectOfType<ShopManager>()?.ToggleShopPanel());
        // TODO: Wire up getMoreTurboPawsButton to an ad manager call

        // Initial state
        closeBlocker.SetActive(false);
        turboPawSelectionOverlay.gameObject.SetActive(false);
        luckyCatSelectionOverlay.gameObject.SetActive(false);

        // We don't call RefreshAllUI() here. We wait for BoosterManager to load and fire its event.
    }

    public void TogglePanel()
    {
        if (isPanelOpen) ClosePanel();
        else OpenPanel();
    }

    private void OpenPanel()
    {
        if (isPanelOpen) return;
        isPanelOpen = true;
        panelAnimator.SetTrigger("Show");
        closeBlocker.SetActive(true);
        RefreshAllUI(); // Refresh when opening to ensure it's current
    }

    public void ClosePanel()
    {
        if (!isPanelOpen) return;
        isPanelOpen = false;
        ActivateSelectedBoosters();
        panelAnimator.SetTrigger("Hide");
        closeBlocker.SetActive(false);
    }

    private void OnSelectTurboPaw()
    {
        turboPawSelected = !turboPawSelected;
        turboPawSelectionOverlay.gameObject.SetActive(turboPawSelected);
    }

    private void OnSelectLuckyCat()
    {
        luckyCatSelected = !luckyCatSelected;
        luckyCatSelectionOverlay.gameObject.SetActive(luckyCatSelected);
    }

    private void ActivateSelectedBoosters()
    {
        if (turboPawSelected) BoosterManager.Instance.UseTurboPaws();
        if (luckyCatSelected) BoosterManager.Instance.UseLuckyCat();

        turboPawSelected = false;
        luckyCatSelected = false;
        turboPawSelectionOverlay.gameObject.SetActive(false);
        luckyCatSelectionOverlay.gameObject.SetActive(false);
    }

    // --- The Master UI Update Method ---
    private void RefreshAllUI()
    {
        if (BoosterManager.Instance == null) return;
        BoosterManager manager = BoosterManager.Instance;

        // --- INVENTORY PANEL ---
        // Turbo Paws
        int usesLeft = manager.turboPawsFreeDailyUses - manager.TurboPawsUsedToday;
        bool canUseTurbo = manager.CanUseTurboPaws();
        useTurboPawButton.gameObject.SetActive(canUseTurbo);
        getMoreTurboPawsButton.gameObject.SetActive(!canUseTurbo);
        turboPawCountText.text = $"Uses: {usesLeft}/{manager.turboPawsFreeDailyUses}";
        useTurboPawButton.interactable = !FindObjectOfType<PaperRoller>().isBoostActive;
        turboPawItemContainer.SetActive(true); // Ensure it's visible

        // Lucky Cat
        int luckyCatAmount = manager.LuckyCatInventory;
        bool hasLuckyCats = luckyCatAmount > 0;
        useLuckyCatButton.gameObject.SetActive(hasLuckyCats);
        getMoreLuckyCatsButton.gameObject.SetActive(!hasLuckyCats);
        luckyCatCountText.text = $"Owned: {luckyCatAmount}";
        useLuckyCatButton.interactable = !manager.IsLuckyCatActive;
        luckyCatItemContainer.SetActive(true); // Ensure it's visible

        // --- ACTIVE TIMER UI ---
        // Turbo Paws Timer
        if (FindObjectOfType<PaperRoller>().isBoostActive)
        {
            if (turboPawTimerCoroutine == null)
            {
                float duration = FindObjectOfType<PaperRoller>().boostDuration;
                turboPawTimerCoroutine = StartCoroutine(TimerCoroutine(duration, activeTurboPawUIPanel, activeTurboPawTimerFill, activeTurboPawTimerText));
            }
        }
        else
        {
            if (turboPawTimerCoroutine != null)
            {
                StopCoroutine(turboPawTimerCoroutine);
                turboPawTimerCoroutine = null;
                activeTurboPawUIPanel.SetActive(false);
            }
        }

        // Lucky Cat Timer
        if (manager.IsLuckyCatActive)
        {
            if (luckyCatTimerCoroutine == null)
            {
                luckyCatTimerCoroutine = StartCoroutine(TimerCoroutine(manager.luckyCatDuration, activeLuckyCatUIPanel, activeLuckyCatTimerFill, activeLuckyCatTimerText));
            }
        }
        else
        {
            if (luckyCatTimerCoroutine != null)
            {
                StopCoroutine(luckyCatTimerCoroutine);
                luckyCatTimerCoroutine = null;
                activeLuckyCatUIPanel.SetActive(false);
            }
        }
    }

    private IEnumerator TimerCoroutine(float duration, GameObject panel, Image timerFill, TextMeshProUGUI timerText)
    {
        panel.SetActive(true);
        float timeLeft = duration;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            timerFill.fillAmount = timeLeft / duration;
            timerText.text = $"{Mathf.CeilToInt(timeLeft)}s";
            yield return null;
        }
        panel.SetActive(false);
    }
}