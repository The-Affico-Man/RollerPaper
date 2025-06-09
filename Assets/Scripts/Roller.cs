using UnityEngine;

public class Roller : MonoBehaviour
{
    [Header("Components")]
    public Transform rollerModel;

    [Header("Spin Settings")]
    public float spinMultiplier = 1.0f;

    [Header("Shake Settings")]
    public float maxShakeAmount = 0.02f;
    public float minShakeSpeed = 10f;
    public float maxShakeSpeed = 80f;
    public float returnSmoothness = 10f;

    private Vector3 originalPosition;

    private void Start()
    {
        if (rollerModel != null)
        {
            originalPosition = rollerModel.localPosition;
        }
    }

    public void SpinRoller(float amount)
    {
        if (rollerModel == null) return;
        rollerModel.Rotate(Vector3.right, amount * spinMultiplier * Time.deltaTime, Space.Self);
    }

    public void SetShake(float shakeFactor)
    {
        if (rollerModel == null) return;
        if (shakeFactor > 0.01f)
        {
            float currentShakeAmount = maxShakeAmount * shakeFactor;
            float currentShakeSpeed = Mathf.Lerp(minShakeSpeed, maxShakeSpeed, shakeFactor);
            Vector3 randomOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * currentShakeSpeed, 0f) * 2f - 1f,
                Mathf.PerlinNoise(0f, Time.time * currentShakeSpeed) * 2f - 1f,
                0f) * currentShakeAmount;
            rollerModel.localPosition = originalPosition + randomOffset;
        }
        else
        {
            rollerModel.localPosition = Vector3.Lerp(rollerModel.localPosition, originalPosition, Time.deltaTime * returnSmoothness);
        }
    }
}