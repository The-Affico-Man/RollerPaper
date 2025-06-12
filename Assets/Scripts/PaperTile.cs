using UnityEngine;

/// <summary>
/// A self-contained component that lives on the paper tile prefab.
/// Its only job is to manage its own appearance by applying a material to its child meshes.
/// </summary>
public class PaperTile : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Drag all the child mesh objects that should be colored here.")]
    public SkinnedMeshRenderer[] skinnedMeshRenderersToColor; // Changed from MeshRenderer

    /// <summary>
    /// A public method that other scripts can call to update this tile's skin.
    /// </summary>
    /// <param name="skinMaterial">The new material to apply.</param>
    public void SetSkin(Material skinMaterial)
    {
        if (skinMaterial == null)
        {
            Debug.LogWarning("SetSkin was called with a null material.", this.gameObject);
            return;
        }

        if (skinnedMeshRenderersToColor == null || skinnedMeshRenderersToColor.Length == 0)
        {
            Debug.LogWarning("PaperTile has no MeshRenderers assigned in the Inspector.", this.gameObject);
            return;
        }

        // Loop through every renderer we have assigned and apply the material.
        foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderersToColor)
        {
            if (renderer != null)
            {
                renderer.material = skinMaterial;
            }
        }
    }
}