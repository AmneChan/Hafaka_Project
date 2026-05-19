using UnityEngine;

public class GrapplePoint : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.8f, 0.9f);
    [SerializeField] private float gizmoRadius = 0.3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        
        var h = gizmoRadius * 1.5f;
        Gizmos.DrawLine(transform.position - Vector3.right * h, transform.position + Vector3.right * h);
        Gizmos.DrawLine(transform.position - Vector3.up * h, transform.position + Vector3.up * h);
    }
}