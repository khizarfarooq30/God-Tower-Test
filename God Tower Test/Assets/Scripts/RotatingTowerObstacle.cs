using UnityEngine;

public class RotatingTowerObstacle : MonoBehaviour, ILevelResettable
{
    [SerializeField] private Transform pivot;
    [SerializeField] private float rotationSpeed = 60f;

    private Quaternion initialRotation;

    private void Awake()
    {
        if (pivot == null)
        {
            Debug.LogError($"{nameof(RotatingTowerObstacle)} on '{name}' has no pivot assigned.", this);
            enabled = false;
            return;
        }

        initialRotation = pivot.localRotation;
    }

    public void Configure(float speed)
    {
        rotationSpeed = speed;
    }

    public void ResetForLevel()
    {
        if (pivot != null)
            pivot.localRotation = initialRotation;
    }

    private void Update()
    {
        pivot.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }
}
