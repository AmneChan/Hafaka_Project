using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform grappleAnchorRoot;
    [SerializeField] private LineRenderer grappleLine;
    [SerializeField] private Camera mainCamera;
    
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference primaryAction;
    [SerializeField] private InputActionReference secondaryAction;
    [SerializeField] private InputActionReference dashAction;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float gravity = 9.81f;
    
    [Header("Jump")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpHeight = 3.5f;
    
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;
    
    [Header("Grapple")]
    [SerializeField] private float grappleSpeed = 18f;
    [SerializeField] private float grappleRadius = 12f;
    [SerializeField] private float grappleStopDistance = 0.6f;

    private Vector3 _velocity;
    private int _jumpsRemaining;
    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDirection;
    private bool _isGrappling;
    private Transform _grappleTarget;

    private void Awake()
    {
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        primaryAction.action.Enable();
        secondaryAction.action.Enable();
        dashAction.action.Enable();
        
        jumpAction.action.performed += OnJump;
        primaryAction.action.performed += OnAttack;
        secondaryAction.action.performed += OnGrappleStart;
        secondaryAction.action.canceled += OnGrappleCancel;
        dashAction.action.performed += OnDash;
    }

    private void OnDisable()
    {
        jumpAction.action.performed -= OnJump;
        primaryAction.action.performed -= OnAttack;
        secondaryAction.action.performed -= OnGrappleStart;
        secondaryAction.action.canceled -= OnGrappleCancel;
        dashAction.action.performed -= OnDash;
        
        moveAction.action.Disable();
        jumpAction.action.Disable();
        primaryAction.action.Disable();
        secondaryAction.action.Disable();
        dashAction.action.Disable();
    }

    private void Update()
    {
        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer -= Time.deltaTime;
        }

        if (_isDashing)
        {
            HandleDash();
            return;
        }

        if (_isGrappling)
        {
            HandleGrapple();
            return;
        }

        HandleMovement();
        UpdateGrappleLine();
    }

    private void HandleMovement()
    {
        var grounded = controller.isGrounded;

        if (grounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
            _jumpsRemaining = maxJumps;
        }
        
        var horizontal = moveAction.action.ReadValue<Vector2>().x;
        _velocity.x = horizontal * moveSpeed;
        
        _velocity.y += gravity * Time.deltaTime;

        _velocity.z = 0f;
        
        controller.Move(_velocity * Time.deltaTime);

        if (horizontal != 0f)
            transform.localScale = new Vector3(Mathf.Sign(horizontal), 1f, 1f);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (_isDashing || _isGrappling)
            return;

        if (_jumpsRemaining > 0)
        {
            _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            _jumpsRemaining--;
        }
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (_isDashing || _dashCooldownTimer > 0f || _isGrappling)
            return;
        
        var horizontal = moveAction.action.ReadValue<Vector2>().x;
        _dashDirection = horizontal != 0f ? Mathf.Sign(horizontal) : Mathf.Sign(transform.localScale.x);

        _isDashing = true;
        _dashTimer = dashDuration;
        _velocity.y = 0f;
    }
    
    private void HandleDash()
    {
        _dashTimer -= Time.deltaTime;

        var dashVelocity = new Vector3(_dashDirection * dashSpeed, 0f, 0f);
        controller.Move(dashVelocity * Time.deltaTime);

        if (_dashTimer <= 0f)
        {
            _isDashing = false;
            _dashCooldownTimer = dashCooldown;
            _velocity.x = _dashDirection * moveSpeed;
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        //TODO: add attack logic
    }

    private void OnGrappleStart(InputAction.CallbackContext context)
    {
        if (_isGrappling || _isDashing)
            return;

        var best = FindClosestGrapplePoint();
        if (best == null)
            return;

        _grappleTarget = best;
        _isGrappling = true;
        _velocity = Vector3.zero;

        if (grappleLine)
        {
            grappleLine.enabled = true;
            grappleLine.positionCount = 2;
        }
    }

    private void OnGrappleCancel(InputAction.CallbackContext context)
    {
        StopGrapple();
    }

    private void HandleGrapple()
    {
        if (!_grappleTarget)
        {
            StopGrapple();
            return;
        }
        
        var direction = _grappleTarget.position - transform.position;
        direction.z = 0f;

        if (direction.magnitude <= grappleStopDistance)
        {
            StopGrapple();
            return;
        }
        
        controller.Move(direction.normalized * (grappleSpeed * Time.deltaTime));
        UpdateGrappleLine();
    }

    private void StopGrapple()
    {
        _isGrappling = false;
        _grappleTarget = null;
        _jumpsRemaining = maxJumps;
        
        if (grappleLine)
            grappleLine.enabled = false;
    }

    private Transform FindClosestGrapplePoint()
    {
        if (!grappleAnchorRoot)
            return null;
        
        var mouseScreen = Mouse.current.position.ReadValue();
        var ray = mainCamera.ScreenPointToRay(mouseScreen);

        var playerZ = transform.position.z;
        var t = (playerZ - ray.origin.z) / ray.direction.z;
        var mouseWorld = ray.origin + ray.direction * t;

        var toMouse = (mouseWorld - transform.position).normalized;

        Transform best = null;
        var bestScore = float.MinValue;

        foreach (Transform point in grappleAnchorRoot)
        {
            var toPoint = point.position - transform.position;
            toPoint.z = 0f;
            
            var distance = toPoint.magnitude;
            if (distance > grappleRadius)
                continue;
            
            var dot = Vector3.Dot(toMouse, toPoint.normalized);
            var score = dot - (distance / grappleRadius) * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                best = point;
            }
        }

        return best;
    }

    private void UpdateGrappleLine()
    {
        if (!grappleLine || !grappleLine.enabled)
            return;
        
        grappleLine.SetPosition(0, transform.position);
        grappleLine.SetPosition(1, _grappleTarget ? _grappleTarget.position : transform.position);
    }
}