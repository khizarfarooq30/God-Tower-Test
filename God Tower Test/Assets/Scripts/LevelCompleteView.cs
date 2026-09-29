using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteView : MonoBehaviour
{
    [SerializeField] private LevelFlow flow;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonLabel;
    [SerializeField, Min(0.01f)] private float popDuration = 0.25f;

    private void Awake()
    {
        if (flow == null || panel == null || titleText == null || restartButton == null || nextButton == null || nextButtonLabel == null)
        {
            Debug.LogError($"{nameof(LevelCompleteView)} on '{name}' has unassigned references.", this);
            enabled = false;
            return;
        }

        panel.SetActive(false);
        restartButton.onClick.AddListener(PlayTap);
        nextButton.onClick.AddListener(PlayTap);
        restartButton.onClick.AddListener(flow.RestartLevel);
        nextButton.onClick.AddListener(flow.NextLevel);
    }

    private void OnEnable()
    {
        if (flow == null)
            return;

        flow.LevelStarted += HandleLevelStarted;
        flow.LevelCompleted += HandleLevelCompleted;
    }

    private void OnDisable()
    {
        if (flow == null)
            return;

        flow.LevelStarted -= HandleLevelStarted;
        flow.LevelCompleted -= HandleLevelCompleted;
    }

    private void OnDestroy()
    {
        if (flow == null)
            return;

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(PlayTap);
            restartButton.onClick.RemoveListener(flow.RestartLevel);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(PlayTap);
            nextButton.onClick.RemoveListener(flow.NextLevel);
        }
    }

    private void HandleLevelStarted(int index)
    {
        StopAllCoroutines();
        panel.transform.localScale = Vector3.one;
        panel.SetActive(false);
    }

    private void HandleLevelCompleted(int index, bool isLast)
    {
        titleText.text = isLast ? "ALL LEVELS COMPLETE" : $"LEVEL {index + 1} COMPLETE";
        nextButtonLabel.text = isLast ? "PLAY AGAIN" : "NEXT LEVEL";
        panel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PopIn());
    }

    private System.Collections.IEnumerator PopIn()
    {
        Transform target = panel.transform;
        for (float elapsed = 0f; elapsed < popDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / popDuration;
            float scale = t < 0.6f ? Mathf.Lerp(0.8f, 1.05f, t / 0.6f) : Mathf.Lerp(1.05f, 1f, (t - 0.6f) / 0.4f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private static void PlayTap() => GameAudio.Play(SoundId.ButtonTap);
}
