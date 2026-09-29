using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BumpEventController : MonoBehaviour
{
    private static readonly Vector2[] AttackOrigins =
    {
        new Vector2(-1f, 0f),
        new Vector2(1f, 0f),
        new Vector2(-1f, 1f),
        new Vector2(1f, 1f),
        new Vector2(-1f, -1f),
        new Vector2(1f, -1f)
    };

    [Header("References")]
    [SerializeField] private TowerClimber climber;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform gloveArea;
    [SerializeField] private Image[] gloves;
    [SerializeField] private Image impactFlash;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private AudioSource audioSource;

    [Header("Assets")]
    [SerializeField] private Sprite fallbackGloveSprite;
    [SerializeField] private AudioClip punchSfx;
    [SerializeField] private AudioClip[] punchVariations;
    [SerializeField, Range(0f, 1f)] private float lightPunchVolume = 0.6f;

    [Header("Gloves")]
    [SerializeField, Min(10f)] private float gloveSize = 420f;
    [SerializeField, Min(0f)] private float gloveStagger = 0.05f;
    [SerializeField, Min(0.01f)] private float gloveTravelDuration = 0.2f;
    [SerializeField, Min(0f)] private float gloveHoldDuration = 0.1f;
    [SerializeField, Min(0.01f)] private float gloveExitDuration = 0.15f;
    [SerializeField, Min(0f)] private float punchThroughDistance = 90f;
    [SerializeField] private bool rotateGlovesToTravel;
    [SerializeField, Min(0f)] private float gloveRotationJitter = 8f;
    [SerializeField] private Vector2 playerTargetScreenOffset = new Vector2(0f, 80f);

    [Header("Impact")]
    [SerializeField, Min(0)] private int impactOnGloveIndex = 1;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float flashMaxAlpha = 0.9f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.3f;
    [SerializeField, Min(0f)] private float shakeStrength = 0.6f;
    [SerializeField, Min(0f)] private float finalShakeStrength = 0.8f;

    private Vector2[] starts;
    private Vector2[] ends;
    private float[] baseScales;
    private float flashElapsed = -1f;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (gloveArea == null)
            gloveArea = transform as RectTransform;

        if (worldCamera == null)
            worldCamera = Camera.main;

        gloves ??= new Image[0];
        starts = new Vector2[gloves.Length];
        ends = new Vector2[gloves.Length];
        baseScales = new float[gloves.Length];

        foreach (Image glove in gloves)
        {
            if (glove == null)
                continue;

            if (glove.sprite == null && fallbackGloveSprite != null)
                glove.sprite = fallbackGloveSprite;

            glove.preserveAspect = true;
            glove.raycastTarget = false;
            RectTransform rect = glove.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(gloveSize, gloveSize);
        }

        if (impactFlash != null)
            impactFlash.raycastTarget = false;

        ResetVisuals();
    }

    public void Trigger()
    {
        if (IsPlaying || !isActiveAndEnabled)
            return;

        StartCoroutine(PlayBarrage());
    }

    private void Update()
    {
        if (flashElapsed < 0f || impactFlash == null)
            return;

        flashElapsed += Time.deltaTime;
        float t = flashElapsed / flashDuration;

        if (t >= 1f)
        {
            flashElapsed = -1f;
            SetFlash(0f, 1f);
            return;
        }

        float alpha = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
        float scale = t < 0.3f ? Mathf.Lerp(0.8f, 1.1f, t / 0.3f) : Mathf.Lerp(1.1f, 1f, (t - 0.3f) / 0.7f);
        SetFlash(alpha * flashMaxAlpha, scale);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ResetVisuals();
    }

    private IEnumerator PlayBarrage()
    {
        IsPlaying = true;
        PrepareGloves(TargetPosition());

        bool impactDone = false;
        var arrived = new bool[gloves.Length];
        int impactIndex = Mathf.Clamp(impactOnGloveIndex, 0, Mathf.Max(0, gloves.Length - 1));
        float gloveLifetime = gloveTravelDuration + gloveHoldDuration + gloveExitDuration;
        float impactTime = impactIndex * gloveStagger + gloveTravelDuration;
        float total = Mathf.Max(0, gloves.Length - 1) * gloveStagger + gloveLifetime;

        for (float elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
        {
            if (!impactDone && elapsed >= impactTime)
            {
                impactDone = true;
                PlayImpact();
            }

            for (int i = 0; i < gloves.Length; i++)
            {
                if (gloves[i] == null)
                    continue;

                float local = elapsed - i * gloveStagger;
                bool visible = local >= 0f && local < gloveLifetime;
                gloves[i].enabled = visible;

                if (visible)
                    AnimateGlove(i, local);

                if (!arrived[i] && local >= gloveTravelDuration)
                {
                    arrived[i] = true;
                    OnGloveArrived(i, impactIndex);
                }
            }

            yield return null;
        }

        if (!impactDone)
            PlayImpact();

        HideGloves();
        IsPlaying = false;
    }

    private void AnimateGlove(int index, float local)
    {
        RectTransform rect = gloves[index].rectTransform;
        float scale = baseScales[index];

        if (local < gloveTravelDuration)
        {
            float t = local / gloveTravelDuration;
            rect.anchoredPosition = Vector2.LerpUnclamped(starts[index], ends[index], t * t);
            scale *= Mathf.Lerp(0.85f, 1.15f, t);
        }
        else
        {
            rect.anchoredPosition = ends[index];
            float exit = (local - gloveTravelDuration - gloveHoldDuration) / gloveExitDuration;
            scale *= 1.15f * (1f - Mathf.Clamp01(exit));
        }

        rect.localScale = new Vector3(scale, scale, 1f);
    }

    private void PrepareGloves(Vector2 target)
    {
        Rect area = gloveArea.rect;
        Vector2 halfSize = area.size * 0.5f + Vector2.one * gloveSize * 0.5f;

        for (int i = 0; i < gloves.Length; i++)
        {
            if (gloves[i] == null)
                continue;

            Vector2 origin = AttackOrigins[i % AttackOrigins.Length];
            Vector2 travel = -origin.normalized;

            starts[i] = Vector2.Scale(origin, halfSize);
            ends[i] = target + travel * punchThroughDistance;
            baseScales[i] = 0.95f + (i % 3) * 0.08f;

            float jitter = ((i * 37) % 11 - 5) / 5f * gloveRotationJitter;
            float angle = rotateGlovesToTravel ? Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg + jitter : jitter;

            RectTransform rect = gloves[i].rectTransform;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            rect.localScale = new Vector3(baseScales[i], baseScales[i], 1f);
            rect.anchoredPosition = starts[i];
            gloves[i].enabled = false;
        }
    }

    private Vector2 TargetPosition()
    {
        if (climber == null || worldCamera == null)
            return Vector2.zero;

        Vector3 screen = worldCamera.WorldToScreenPoint(climber.transform.position);
        if (screen.z <= 0f)
            return Vector2.zero;

        Vector2 screenPoint = (Vector2)screen + playerTargetScreenOffset;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(gloveArea, screenPoint, null, out Vector2 local)
            ? local
            : Vector2.zero;
    }

    private void OnGloveArrived(int index, int impactIndex)
    {
        if (index == impactIndex)
            return;

        if (audioSource != null && punchVariations != null && punchVariations.Length > 0)
        {
            AudioClip clip = punchVariations[index % punchVariations.Length];
            if (clip != null)
                audioSource.PlayOneShot(clip, lightPunchVolume);
        }

        if (index == gloves.Length - 1 && cameraShake != null)
            cameraShake.Shake(shakeDuration, finalShakeStrength);
    }

    private void PlayImpact()
    {
        if (climber != null)
            climber.ReceiveHit();

        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeStrength);

        if (impactFlash != null)
        {
            flashElapsed = 0f;
            SetFlash(0f, 0.8f);
        }

        if (audioSource != null && punchSfx != null)
            audioSource.PlayOneShot(punchSfx);
    }

    private void ResetVisuals()
    {
        HideGloves();
        flashElapsed = -1f;
        SetFlash(0f, 1f);
        IsPlaying = false;
    }

    private void HideGloves()
    {
        foreach (Image glove in gloves)
        {
            if (glove != null)
                glove.enabled = false;
        }
    }

    private void SetFlash(float alpha, float scale)
    {
        if (impactFlash == null)
            return;

        Color color = impactFlash.color;
        color.a = alpha;
        impactFlash.color = color;
        impactFlash.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }
}
