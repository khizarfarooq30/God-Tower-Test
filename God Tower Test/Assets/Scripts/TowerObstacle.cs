using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerObstacle : MonoBehaviour
{
    private const string ObstacleLayerName = "TowerObstacle";

    private void Reset()
    {
        int layer = LayerMask.NameToLayer(ObstacleLayerName);
        if (layer >= 0)
            gameObject.layer = layer;
    }
}
