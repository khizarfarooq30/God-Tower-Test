using System;
using UnityEngine;

public class LevelFlow : MonoBehaviour
{
    [SerializeField] private LevelController[] levels;
    [SerializeField, Min(0)] private int firstLevelIndex;

    private LevelController current;

    public event Action<int> LevelStarted;
    public event Action<int, bool> LevelCompleted;

    public int CurrentIndex { get; private set; } = -1;
    public LevelController CurrentLevel => current;
    public int LevelCount => levels.Length;
    public bool IsLastLevel => CurrentIndex == levels.Length - 1;

    private void Awake()
    {
        if (levels == null || levels.Length == 0 || Array.IndexOf(levels, null) >= 0)
        {
            Debug.LogError($"{nameof(LevelFlow)} on '{name}' needs a complete list of levels.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled)
            return;

        foreach (LevelController level in levels)
            level.gameObject.SetActive(false);

        LoadLevel(Mathf.Clamp(firstLevelIndex, 0, levels.Length - 1));
    }

    public void LoadLevel(int index)
    {
        if (!enabled || index < 0 || index >= levels.Length)
            return;

        if (current != null)
        {
            current.LevelCompleted -= HandleLevelCompleted;
            current.StopLevel();
            current.gameObject.SetActive(false);
        }

        CurrentIndex = index;
        current = levels[index];
        current.gameObject.SetActive(true);
        current.LevelCompleted += HandleLevelCompleted;
        current.StartLevel();
        LevelStarted?.Invoke(index);
    }

    public void RestartLevel()
    {
        if (current == null)
            return;

        current.StartLevel();
        LevelStarted?.Invoke(CurrentIndex);
    }

    public void NextLevel()
    {
        LoadLevel(IsLastLevel ? 0 : CurrentIndex + 1);
    }

    private void HandleLevelCompleted()
    {
        LevelCompleted?.Invoke(CurrentIndex, IsLastLevel);
    }
}
