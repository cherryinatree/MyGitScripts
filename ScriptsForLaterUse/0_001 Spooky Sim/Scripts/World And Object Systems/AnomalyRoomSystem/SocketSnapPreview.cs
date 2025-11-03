// SocketSnapPreview.cs
using UnityEditor;
using UnityEngine;

public class SocketSnapPreview : MonoBehaviour
{
    public Transform targetExit;
    public GameObject prefabToTest;

    private void OnDrawGizmos()
    {
        if (!targetExit || !prefabToTest) return;

        // temp spawn in editor preview (not saved)
        var temp = PrefabUtility.InstantiatePrefab(prefabToTest) as GameObject;
        if (!temp) return;

        try
        {
            var tag = temp.GetComponent<RoomTag>() ?? temp.AddComponent<RoomTag>();
            var entrance = tag.EntranceSocket;
            temp.transform.position = targetExit.position;
            temp.transform.rotation = targetExit.rotation;
            SocketSnap.SnapEntranceToExit(temp.transform, entrance, targetExit);

            // Draw its bounds lightly
            Gizmos.color = new Color(1, 1, 1, 0.25f);
            var renders = temp.GetComponentsInChildren<Renderer>();
            foreach (var r in renders) Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
        finally
        {
            if (Application.isEditor) DestroyImmediate(temp);
            else Destroy(temp);
        }
    }
}
