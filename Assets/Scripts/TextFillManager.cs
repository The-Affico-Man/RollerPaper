using UnityEngine;
using TMPro;

public class TextFillManager : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("The TextMeshProUGUI component for the paper length counter.")]
    public TextMeshProUGUI paperLengthText;

    [Header("Fill Effect Settings")]
    [Tooltip("The color of the 'empty' part of the text.")]
    public Color emptyColor = Color.gray;
    [Tooltip("The color of the 'filled' part of the text.")]
    public Color fillColor = Color.white;
    [Tooltip("How 'soft' the edge of the fill is, as a percentage of character height. 0 = hard edge, 0.5 = very soft.")]
    [Range(0.01f, 1.0f)]
    public float gradientSize = 0.25f;


    // --- Private state variables ---
    private SwipeController swipeController;
    private TMP_TextInfo textInfo;

    void Start()
    {
        swipeController = FindFirstObjectByType<SwipeController>();
        if (paperLengthText == null)
        {
            Debug.LogError("TextFillManager: Paper Length Text is not assigned!");
            this.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (swipeController == null || paperLengthText == null)
        {
            return;
        }

        float totalLengthMeters = swipeController.TotalSwipeDistance * 0.0002f;
        float fillProgress = totalLengthMeters % 1.0f;

        ApplyBottomUpFill(fillProgress);
    }

    /// <summary>
    /// This is the new, robust effect. It creates a smooth, gradient-based fill
    /// that correctly resets between meters.
    /// </summary>
    void ApplyBottomUpFill(float progress)
    {
        paperLengthText.ForceMeshUpdate();
        textInfo = paperLengthText.textInfo;

        if (textInfo.characterCount == 0) return;

        // Loop through each character in the text
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            // Get the Y positions of the bottom and top of the character.
            float bottomY = charInfo.bottomLeft.y;
            float topY = charInfo.topLeft.y;
            float charHeight = topY - bottomY;

            // Avoid division by zero for non-rendering characters like spaces.
            if (charHeight <= 0.001f) continue;

            // This is the Y position of the center of our gradient.
            float fillLineY = Mathf.Lerp(bottomY, topY, progress);

            // Calculate the start and end of the soft gradient area.
            float gradientHeight = charHeight * gradientSize;
            float gradientStart = fillLineY - (gradientHeight / 2f);
            float gradientEnd = fillLineY + (gradientHeight / 2f);

            // Loop through the 4 vertices of this character's quad.
            for (int j = 0; j < 4; j++)
            {
                int vertexIndex = charInfo.vertexIndex + j;
                Vector3 vertexPos = textInfo.meshInfo[materialIndex].vertices[vertexIndex];

                // Calculate a "blend factor" from 0 to 1.
                // 0 means the vertex is fully filled.
                // 1 means the vertex is fully empty.
                float blendFactor = Mathf.InverseLerp(gradientStart, gradientEnd, vertexPos.y);

                // Linearly interpolate between the two colors based on the blend factor.
                // This creates the smooth gradient and ensures the colors reset correctly.
                Color32 finalColor = Color.Lerp(fillColor, emptyColor, blendFactor);

                vertexColors[vertexIndex] = finalColor;
            }
        }

        // Apply the updated color data to the mesh.
        paperLengthText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}