using UnityEngine;

public class TowerSectionStack : MonoBehaviour
{
    [SerializeField] private GameObject sectionPrefab;
    [SerializeField, Min(1)] private int sectionCount = 27;
    [SerializeField, Min(0.01f)] private float sectionHeight = 5f;
    [SerializeField] private Material[] sectionMaterials;

    [ContextMenu("Rebuild Sections")]
    private void RebuildSections()
    {
        if (sectionPrefab == null)
        {
            Debug.LogError($"{nameof(TowerSectionStack)} on '{name}' has no section prefab.", this);
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        for (int i = 0; i < sectionCount; i++)
        {
            GameObject section = Instantiate(sectionPrefab, transform);
            section.name = $"Section_{i + 1:00}";
            section.transform.localPosition = new Vector3(0f, sectionHeight * i, 0f);
            section.transform.localRotation = Quaternion.identity;

            if (sectionMaterials != null && sectionMaterials.Length > 0)
            {
                Renderer body = section.GetComponentInChildren<Renderer>();
                if (body != null)
                    body.sharedMaterial = sectionMaterials[i % sectionMaterials.Length];
            }
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}
