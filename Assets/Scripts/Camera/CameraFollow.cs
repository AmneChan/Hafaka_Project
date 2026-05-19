using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Camera")]
    [SerializeField] private Transform target;
    [Tooltip("Z offset from the player. Negative = behind the player on the z axis.")]
    [SerializeField] private float zOffset = -10f;
    [SerializeField] private Vector2 xyOffset = new Vector2(0f, 1.5f);
    
    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.18f;
    
    [Header("Look-Ahead")]
    [Tooltip("Camera leads the player horizontally bt his many units.")]
    [SerializeField] private float lookAheadX = 2f;
    
    private Vector3 _velocity =  Vector3.zero;
    private float _lastFacingX = 1f;

    private void LateUpdate()
    {
        if (!target)
            return;

        var facingX = Mathf.Sign(target.localScale.x);
        if (facingX != 0f)
            _lastFacingX = facingX;

        var desired = new Vector3(
            target.position.x + xyOffset.x * _lastFacingX * lookAheadX,
            target.position.y + xyOffset.y,
            target.position.z + zOffset
        );
        
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }
}