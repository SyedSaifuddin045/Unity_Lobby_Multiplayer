using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _acceleration = 80;
    [SerializeField] private float _maxVelocity = 10;
    [SerializeField] private float _rotationSpeed = 450;
    [SerializeField] private float _shootForce = 500;

    private Vector3 _input;
    private Rigidbody _rb;
    private Camera _cam;
    private Plane _groundPlane = new(Vector3.up, Vector3.zero);
    private PlayerFootballHandler _footballHandler;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _footballHandler = GetComponent<PlayerFootballHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        if (!_rb)
        {
            _rb = GetComponent<Rigidbody>();
        }
        _cam = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner) return;

        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        HandleRotationClientSide();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootFootballServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (_rb != null)
        {
            RequestMoveServerRpc(_input);
        }
        else
        {
            Debug.LogError("Rigidbody is null on client!");
        }
    }

    [ServerRpc]
    private void RequestMoveServerRpc(Vector3 input)
    {
        if (_rb == null)
        {
            Debug.LogError("Rigidbody is null on server!");
            return;
        }

        Vector3 movement = input.normalized * (_acceleration * Time.fixedDeltaTime);
        _rb.velocity += movement;
        _rb.velocity = Vector3.ClampMagnitude(_rb.velocity, _maxVelocity);
    }

    private void HandleRotationClientSide()
    {
        if (_cam == null) return;

        var ray = _cam.ScreenPointToRay(Input.mousePosition);

        if (_groundPlane.Raycast(ray, out var enter))
        {
            var hitPoint = ray.GetPoint(enter);
            hitPoint.y = transform.position.y;
            var dir = hitPoint - transform.position;

            var targetRotation = Quaternion.LookRotation(dir);
            RequestRotateServerRpc(targetRotation);
        }
    }

    [ServerRpc]
    private void RequestRotateServerRpc(Quaternion targetRotation)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void ShootFootballServerRpc()
    {
        if (_footballHandler == null || !_footballHandler.HasFootballAttached) return;

        var football = _footballHandler.DetachFootball();
        var footballRb = football.GetComponent<Rigidbody>();
        if (footballRb != null)
        {
            footballRb.AddForce(transform.forward * _shootForce);
        }
    }
}
