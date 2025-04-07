using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PolygonCollider2D))]
public class PolygonColliderGridSnapEditor : Editor
{
    public float snapIncrement = 1.0f; // Adjust this to your grid size

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw the default inspector

        snapIncrement = EditorGUILayout.FloatField("Snap Increment", snapIncrement);

        if (GUILayout.Button("Snap to Grid"))
        {
            SnapColliderToGrid();
        }
    }

    private void SnapColliderToGrid()
    {
        PolygonCollider2D polyCollider = (PolygonCollider2D)target;
        Vector2[] path = polyCollider.points;

        for (int i = 0; i < path.Length; i++)
        {
            path[i].x = Mathf.Round(path[i].x / snapIncrement) * snapIncrement;
            path[i].y = Mathf.Round(path[i].y / snapIncrement) * snapIncrement;
        }

        polyCollider.points = path;

        // Optional: Mark the object as dirty so the changes are saved.
        EditorUtility.SetDirty(polyCollider);
        Undo.RecordObject(polyCollider, "Snap PolygonCollider2D to Grid");
    }
}