using TMPro;
using UnityEngine;

public class LevelLabelView : MonoBehaviour
{
    [SerializeField] private LevelFlow flow;
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        if (flow == null || label == null)
        {
            Debug.LogError($"{nameof(LevelLabelView)} on '{name}' has unassigned references.", this);
            return;
        }

        flow.LevelStarted += HandleLevelStarted;
    }

    private void OnDisable()
    {
        if (flow != null)
            flow.LevelStarted -= HandleLevelStarted;
    }

    private void HandleLevelStarted(int index) => label.text = $"LEVEL {index + 1}";
}
