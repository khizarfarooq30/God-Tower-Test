using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float defaultDuration = 0.3f;
    [SerializeField, Min(0f)] private float defaultStrength = 0.4f;
    [SerializeField, Min(0f)] private float frequency = 28f;

    private Vector3 originalLocalPosition;
    private float duration;
    private float remaining;
    private float strength;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    public void Shake() => Shake(defaultDuration, defaultStrength);

    public void Shake(float shakeDuration, float shakeStrength)
    {
        if (remaining > 0f && strength * (remaining / duration) > shakeStrength)
            return;

        duration = Mathf.Max(0.01f, shakeDuration);
        remaining = duration;
        strength = shakeStrength;
    }

    private void LateUpdate()
    {
        if (remaining <= 0f)
            return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            transform.localPosition = originalLocalPosition;
            return;
        }

        float falloff = remaining / duration;
        float time = Time.time * frequency;
        var offset = new Vector3(
            Mathf.PerlinNoise(time, 0.37f) * 2f - 1f,
            Mathf.PerlinNoise(0.71f, time) * 2f - 1f,
            0f);

        transform.localPosition = originalLocalPosition + offset * (strength * falloff);
    }

    private void OnDisable()
    {
        remaining = 0f;
        transform.localPosition = originalLocalPosition;
    }
}
