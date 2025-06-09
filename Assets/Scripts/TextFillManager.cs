using UnityEngine;
using TMPro;

public class TextFillManager : MonoBehaviour
{
    [Header("Core Components")]
    public TextMeshProUGUI paperLengthText;

    [Header("Script References")]
    public PaperRoller paperRoller;
    public ContinuousPaperManager continuousPaperManager;

    [Header("Fill Effect Settings")]
    public Color emptyColor = Color.gray;
    public Color fillColor = Color.white;
    [Range(0.01f, 1.0f)]
    public float gradientSize = 0.25f;

    void Start()
    {
        if (paperRoller == null) paperRoller = FindFirstObjectByType<PaperRoller>();
        if (continuousPaperManager == null) continuousPaperManager = FindFirstObjectByType<ContinuousPaperManager>();
        if (paperLengthText == null || paperRoller == null || continuousPaperManager == null)
        {
            Debug.LogError("TextFillManager is missing a critical reference!");
            this.enabled = false;
        }
    }

    void LateUpdate()
    {
        float worldDistance = paperRoller.WorldSpaceDistancePulled;
        float conversionFactor = continuousPaperManager.realWorldMetersPerTile / continuousPaperManager.paperTileLength;
        float totalLengthMeters = worldDistance * conversionFactor;

        float fillProgress = totalLengthMeters % 1.0f;
        ApplyBottomUpFill(fillProgress);
    }

    void ApplyBottomUpFill(float progress)
    {
        paperLengthText.ForceMeshUpdate();
        TMP_TextInfo textInfo = paperLengthText.textInfo;
        if (textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            float bottomY = charInfo.bottomLeft.y;
            float topY = charInfo.topLeft.y;
            float charHeight = topY - bottomY;

            if (charHeight <= 0.001f) continue;

            float fillLineY = Mathf.Lerp(bottomY, topY, progress);
            float gradientHeight = charHeight * gradientSize;
            float gradientStart = fillLineY - (gradientHeight / 2f);
            float gradientEnd = fillLineY + (gradientHeight / 2f);

            for (int j = 0; j < 4; j++)
            {
                int vertexIndex = charInfo.vertexIndex + j;
                Vector3 vertexPos = textInfo.meshInfo[materialIndex].vertices[vertexIndex];
                float blendFactor = Mathf.InverseLerp(gradientStart, gradientEnd, vertexPos.y);
                Color32 finalColor = Color.Lerp(fillColor, emptyColor, blendFactor);
                vertexColors[vertexIndex] = finalColor;
            }
        }
        paperLengthText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}