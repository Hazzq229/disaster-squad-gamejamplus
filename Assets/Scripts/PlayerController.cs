using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private Rigidbody _rb;
    [SerializeField] private Animator _animator;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _rotationSpeed = 15f;

    private Vector2 _inputVector;
    private bool _isMoving;

    private void Awake()
    {
        _playerInput = new InputSystem_Actions();
        _rb = GetComponent<Rigidbody>();
        
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        
        // Setup Input Events
        _playerInput.Player.Move.started += OnMovementInput;
        _playerInput.Player.Move.performed += OnMovementInput;
        _playerInput.Player.Move.canceled += OnMovementInput;
    }

    private void OnEnable() => _playerInput.Player.Enable();
    private void OnDisable() => _playerInput.Player.Disable();

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        _inputVector = context.ReadValue<Vector2>();
        _isMoving = _inputVector.x != 0 || _inputVector.y != 0;
    }

    private void Update()
    {
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    private void MovePlayer()
    {
        // Hitung target velocity
        // mengubah input (X, Y) jadi arah dunia 3D (X, Z)
        Vector3 targetDirection = new Vector3(_inputVector.x, 0f, _inputVector.y).normalized;
        
        // Terapkan kecepatan langsung
        // mengambil Velocity Y yang lama (Gravitasi) agar karakter tidak melayang
        Vector3 targetVelocity = targetDirection * _moveSpeed;
        targetVelocity.y = _rb.velocity.y; 

        // Set velocity Rigidbody
        _rb.velocity = targetVelocity;
    }

    private void RotatePlayer()
    {
        if (_isMoving)
        {
            // Arah tujuan hadap
            Vector3 direction = new Vector3(_inputVector.x, 0f, _inputVector.y).normalized;
            
            // Hitung rotasi target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Gunakan MoveRotation untuk memutar Rigidbody secara fisik
            Quaternion nextRotation = Quaternion.Slerp(_rb.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(nextRotation);
        }
    }

    private void HandleAnimation()
    {
        if (_animator == null) return;

        bool isWalking = _animator.GetBool("isWalking");
        
        if (_isMoving && !isWalking)
            _animator.SetBool("isWalking", true);
        else if (!_isMoving && isWalking)
            _animator.SetBool("isWalking", false);
    }
}