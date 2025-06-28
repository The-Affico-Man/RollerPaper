using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI descriptionText;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public Button claimButton;
    public GameObject claimButtonObject;
    public GameObject completedOverlay;
}