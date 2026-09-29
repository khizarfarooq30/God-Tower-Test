using System;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public enum LevelState { NotStarted, Playing, Completed }

    private const float FinishTolerance = 0.01f;

    [SerializeField] private TowerClimber climber;
    [SerializeField] private TowerCameraController cameraController;
    [SerializeField] private Transform tower;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform finishPoint;
    [SerializeField, Min(1)] private int maxScore = 5000;

    private ILevelResettable[] resettables;

    public event Action LevelStarted;
    public event Action LevelCompleted;

    public LevelState State { get; private set; }
    public TowerClimber Climber => climber;
    public Transform StartPoint => startPoint;
    public Transform FinishPoint => finishPoint;
    public int MaxScore => maxScore;
    public float StartHeight => startPoint.position.y;
    public float FinishHeight => finishPoint.position.y;
    public float NormalizedProgress => State == LevelState.Completed
        ? 1f
        : Mathf.InverseLerp(StartHeight, FinishHeight, climber.transform.position.y);
    public int CurrentScore => Mathf.RoundToInt(NormalizedProgress * maxScore);

    private void Awake()
    {
        if (climber == null || tower == null || startPoint == null || finishPoint == null)
        {
            Debug.LogError($"{nameof(LevelController)} on '{name}' is missing climber, tower, start point or finish point.", this);
            enabled = false;
            return;
        }

        resettables = GetComponentsInChildren<ILevelResettable>(true);
    }

    public void StartLevel()
    {
        if (!enabled)
            return;

        foreach (ILevelResettable resettable in resettables)
            resettable.ResetForLevel();

        climber.SetTower(tower);
        climber.PlaceAt(startPoint.position);
        climber.SetControlEnabled(true);

        if (cameraController != null)
        {
            cameraController.SetTower(tower);
            cameraController.ResetCamera();
        }

        State = LevelState.Playing;
        LevelStarted?.Invoke();
    }

    public void Configure(Transform newTower, Transform newStartPoint, Transform newFinishPoint, int newMaxScore)
    {
        tower = newTower;
        startPoint = newStartPoint;
        finishPoint = newFinishPoint;
        maxScore = Mathf.Max(1, newMaxScore);
    }

    public void StopLevel()
    {
        State = LevelState.NotStarted;
    }

    private void Update()
    {
        if (State != LevelState.Playing)
            return;

        if (climber.State == TowerClimber.ClimbState.Attached &&
            climber.transform.position.y >= finishPoint.position.y - FinishTolerance)
            Complete();
    }

    private void Complete()
    {
        State = LevelState.Completed;
        climber.SetControlEnabled(false);
        LevelCompleted?.Invoke();
    }
}
