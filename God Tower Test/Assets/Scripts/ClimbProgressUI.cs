using TMPro;
using UnityEngine;

public class ClimbProgressUI : MonoBehaviour
{
    [SerializeField] private LevelFlow flow;
    [SerializeField] private RectTransform fill;
    [SerializeField] private RectTransform marker;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text maxScoreText;
    [SerializeField, Range(0f, 0.5f)] private float scoreTextEdgePadding = 0.03f;

    private int shownScore = -1;
    private int shownMaxScore = -1;

    private void Awake()
    {
        if (flow == null || fill == null || marker == null || currentScoreText == null || maxScoreText == null)
        {
            Debug.LogError($"{nameof(ClimbProgressUI)} on '{name}' has unassigned references.", this);
            enabled = false;
            return;
        }

        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(1f, 0f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
    }

    private void LateUpdate()
    {
        LevelController level = flow.CurrentLevel;
        if (level == null || !level.enabled)
            return;

        float progress = level.NormalizedProgress;

        Vector2 fillMax = fill.anchorMax;
        fillMax.y = progress;
        fill.anchorMax = fillMax;

        SetAnchorY(marker, progress);
        SetAnchorY(currentScoreText.rectTransform, Mathf.Clamp(progress, scoreTextEdgePadding, 1f - scoreTextEdgePadding));

        int score = level.CurrentScore;
        if (score != shownScore)
        {
            shownScore = score;
            currentScoreText.SetText("{0}", score);
        }

        if (level.MaxScore != shownMaxScore)
        {
            shownMaxScore = level.MaxScore;
            maxScoreText.SetText("{0}", shownMaxScore);
        }
    }

    private static void SetAnchorY(RectTransform rect, float y)
    {
        Vector2 min = rect.anchorMin;
        Vector2 max = rect.anchorMax;
        min.y = y;
        max.y = y;
        rect.anchorMin = min;
        rect.anchorMax = max;
    }
}
