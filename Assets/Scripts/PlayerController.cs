using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _jumpHeight = 3f;
    [SerializeField] private float _gravity = 10f;
    [SerializeField] private float _digDownHeight = 2f;
    [SerializeField] private float _digUpHeight = 1f;
    [SerializeField] private float _groundedDistance = 0.05f;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _drillBounceLayerMask;
    [SerializeField] private LayerMask _lavaLayerMask;
    [SerializeField] private float _groundingForce = 0.2f;
    [SerializeField] private float _maxFallSpeed = 6f;
    [SerializeField] private float _bounceSpeedFactor = 0.5f;
    [SerializeField] private float _drillSpeed = 4f;
    [SerializeField] private float _airControl = 0.2f;
    [SerializeField] private float _circleCastRadiusFrac = 0.9f;
    [SerializeField] private float _drillExitHeight = 2f;
    [SerializeField] private float _drillHorizontalDamping = 3f;
    [SerializeField] private ParticleSystem _deathParticles;
    [SerializeField] private ParticleSystem _drillParticles;
    
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _spriteTransform;
    
    [SerializeField] private float _drillRotationHeight = 1.5f;
    [SerializeField] private float _drillRotationDuration = 0.25f;

    [SerializeField] private AudioSource _drillSound;
    [SerializeField] private AudioSource _bounceSound;
    [SerializeField] private AudioSource _deathSound;
    [SerializeField] private AudioSource _jumpSound;
    [SerializeField] private AudioSource _stepSound;
    
    [SerializeField] private string _idleAnimationID = "Base Layer.Idle";
    [SerializeField] private string _walkRightAnimationID = "Base Layer.WalkRight";
    [SerializeField] private string _walkLeftAnimationID = "Base Layer.WalkLeft";
    [SerializeField] private string _jumpAnimationID = "Base Layer.Jump";
    [SerializeField] private string _drillAnimationID = "Base Layer.Drill";
    
    private Rigidbody2D _rb;
    private CircleCollider2D _col;
    private bool _drilling = false;
    private FrameInput _frameInput;
    private bool _grounded = true;
    private bool _jumpQueued;

    private Vector2 _frameVelocity;

    private float _fallTime = 0f;
    private float _maxAirHeight;
    private float _minAirHeight;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CircleCollider2D>();

        transform.position = Checkpoint.CurrentCheckpoint?.transform.position ?? transform.position;
        
        _minAirHeight = transform.position.y;
        _maxAirHeight = transform.position.y;
    }

    private void Update()
    {
        FetchInput();

        HandleAnimation();
        HandleRotation();
    }

    private void HandleAnimation()
    {
        if (_drilling)
        {
            _animator.Play(_drillAnimationID);
        }
        else
        {
            if (!_grounded)
            {
                _animator.Play(_jumpAnimationID);
            }
            else
            {
                if (_frameVelocity.x > 0.1f)
                {
                    _animator.Play(_walkRightAnimationID);
                }
                else if (_frameVelocity.x < -0.1f)
                {
                    _animator.Play(_walkLeftAnimationID);
                }
                else
                {
                    _animator.Play(_idleAnimationID);
                }
            }
        }

        if ((_drilling && _grounded) && !_drillSound.isPlaying)
        {
            _drillParticles.Play();
            _drillSound.Play();
        }
        else if (!(_drilling && _grounded) && _drillSound.isPlaying)
        {
            _drillParticles.Stop();
            _drillSound.Stop();
        }
    }

    private void HandleRotation()
    {
        if (_drilling)
        {
            _spriteTransform.up = _frameVelocity.normalized;
        }
        if (!_drilling && !_grounded && _maxAirHeight - transform.position.y >= _drillRotationHeight)
        {
            if (!_grounded) _fallTime += Time.deltaTime / _drillRotationDuration;

            _spriteTransform.up = LerpAngle(Vector2.up, _frameVelocity.normalized, Mathf.Clamp01(_fallTime));
        }
        else if (!_drilling)
        {
            _fallTime = 0f;
            _spriteTransform.up = Vector2.up;
        }
    }
    
    public static Vector2 LerpAngle(Vector2 current, Vector2 target, float t)
    {
        float angleCurrent = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
        float angleTarget = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;

        float angle = Mathf.LerpAngle(angleCurrent, angleTarget, t);

        Vector2 result;
        result.x = Mathf.Cos(angle * Mathf.Deg2Rad);
        result.y = Mathf.Sin(angle * Mathf.Deg2Rad);

        return result.normalized; // Ensure it's a unit vector
    }

    private void FetchInput()
    {
        _frameInput = new FrameInput
        {
            JumpDown = Input.GetButtonDown("Jump"),
            JumpHeld = Input.GetButton("Jump"),
            Movement = new Vector2(Input.GetAxisRaw("Horizontal"), 0f)
        };

        if (_frameInput.JumpDown)
        {
            _jumpQueued = true;
        }

        if (Input.GetButtonDown("Reset")) SceneLoader.i.ReloadScene();
    }

    private void FixedUpdate()
    {
        if (!_drilling)
        {
            CheckCollisionsNew();
            if (!_drilling)
            {
                HandleMovement();
                HandleJump();
                HandleGravity();
            };
        }
        else
        {
            CheckDrillCollisionsNew();
            
            HandleDrillMovement();
            HandleDrillGravity();
            
            if (!_grounded) HandleMovement();
        }

        if (_grounded)
        {
            _minAirHeight = _rb.position.y;
            _maxAirHeight = _rb.position.y;
        }
        else
        {
            _minAirHeight = Mathf.Min(_minAirHeight, _rb.position.y);
            _maxAirHeight = Mathf.Max(_maxAirHeight, _rb.position.y);
        }

        var walking = !_drilling && _grounded && Mathf.Abs(_frameVelocity.x) > 0.1f;
        if (walking && !_stepSound.isPlaying)
        {
            _stepSound.Play();
        }
        if (!walking && _stepSound.isPlaying)
        {
            _stepSound.Stop();
        }
        
        ApplyMovement();
            
        _jumpQueued = false;
        
        LavaCheck();
    }

    private void LavaCheck()
    {
        var coll = Physics2D.OverlapCircle(_col.bounds.center, _col.radius / _circleCastRadiusFrac, _lavaLayerMask);
        if (coll)
        {
            StartCoroutine(DeathAnimation());
        }
    }

    IEnumerator DeathAnimation()
    {
        _spriteTransform.gameObject.SetActive(false);
        _deathParticles.Play();
        _deathSound.Play();
        
        yield return null;
        
        SceneLoader.i.ReloadScene();
    }

    private void CheckCollisionsNew()
    {
        var groundHit = Physics2D.CircleCast(_col.bounds.center, _col.radius * _circleCastRadiusFrac, Vector2.down, _groundedDistance, _groundLayerMask);

        if (!_grounded && groundHit) // Landing
        {
            var height = _maxAirHeight - transform.position.y;
            if (height >= _digDownHeight)
            {
                if (Vector2.Dot(groundHit.normal, Vector2.up) > 0.9 && _frameVelocity.y < 0) // Hit upwards face
                {
                    var bounceHit = Physics2D.CircleCast(_col.bounds.center, _col.radius * _circleCastRadiusFrac, Vector2.down, _groundedDistance, _drillBounceLayerMask);
                    if (bounceHit)
                    {
                        var bounceVel = new Vector2(0f, HeightToVelocity(height - 1f));
                        _frameVelocity = Vector2.up * bounceVel;
                        _minAirHeight = transform.position.y;
                        _maxAirHeight = transform.position.y;
                        _bounceSound.Play();
                    }
                    else StartDrillDown();
                }
            }
        }
        
        var roofHit = Physics2D.CircleCast(_col.bounds.center, _col.radius * _circleCastRadiusFrac, Vector2.up, _groundedDistance, _groundLayerMask);

        if (!_grounded && roofHit)
        {
            var height = transform.position.y - _minAirHeight;
            if (height <= _digUpHeight)
            {
                if (Vector2.Dot(roofHit.normal, Vector2.down) > 0.9 && _frameVelocity.y > 0) // Hit downwards face
                {
                    var bounceHit = Physics2D.CircleCast(_col.bounds.center, _col.radius * _circleCastRadiusFrac, Vector2.up, _groundedDistance, _drillBounceLayerMask);
                    if (bounceHit)
                    {
                        _frameVelocity.y = -Mathf.Abs(_frameVelocity.y);
                        _bounceSound.Play();
                    }
                    else StartDrillUp();
                }
            }
            else
            {
                _frameVelocity.y = 0f;
            }
        }

        _grounded = groundHit;
    }

    void StartDrillDown()
    {
        _drilling = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _frameVelocity = Vector2.down * _drillSpeed;
    }

    void StartDrillUp()
    {
        _drilling = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _frameVelocity = Vector2.up * _drillSpeed;
    }

    private void CheckDrillCollisionsNew()
    {
        var moveDir = _frameVelocity.normalized;
        var moveAmount = _frameVelocity.magnitude * Time.fixedDeltaTime;
        var groundHit = Physics2D.CircleCast(_col.bounds.center, _col.radius * _circleCastRadiusFrac, moveDir, moveAmount, _groundLayerMask);
        var bounceHit = Physics2D.CircleCast(_col.bounds.center, _col.radius * _circleCastRadiusFrac, moveDir, moveAmount, _drillBounceLayerMask);

        if (!_grounded && groundHit) // Landing
        {
            if (Vector2.Dot(groundHit.normal, Vector2.up) > 0.9 && _frameVelocity.y < 0) // Hit upwards face
            {
                var height = _maxAirHeight - transform.position.y;
                if (height >= _digDownHeight)
                {
                    if (bounceHit)
                    {
                        var bounceVel = new Vector2(0f, HeightToVelocity(height - 1f));
                        _frameVelocity = Vector2.up * bounceVel;
                        _minAirHeight = transform.position.y;
                        _maxAirHeight = transform.position.y;
                        _bounceSound.Play();
                    }
                }
                else
                {
                    _drilling = false;
                    _rb.bodyType = RigidbodyType2D.Dynamic;
                    return;
                }
            }
            else if (Vector2.Dot(groundHit.normal, Vector2.down) > 0.9 && _frameVelocity.y > 0) // Hit downwards face
            {
                var height = transform.position.y - _minAirHeight;
                if (height < _digUpHeight)
                {
                    if (bounceHit)
                    {
                        _frameVelocity.y = -Mathf.Abs(_frameVelocity.y);
                        _minAirHeight = transform.position.y;
                        _maxAirHeight = transform.position.y;
                        _bounceSound.Play();
                    }
                }
                else
                {
                    _drilling = false;
                    _rb.bodyType = RigidbodyType2D.Dynamic;
                    return;
                }
            }
        }
        if (!groundHit && _grounded) // Exiting
        {
            var exitVel = HeightToVelocity(_drillExitHeight);
            _frameVelocity = _frameVelocity.normalized * exitVel;
            _minAirHeight = transform.position.y;
            _maxAirHeight = transform.position.y;
        }

        if (_grounded && groundHit && bounceHit) // Bounce underground
        {
            if (Vector2.Dot(-bounceHit.normal, moveDir) > 0) // Correct Normal
            {
                _frameVelocity = Vector2.Reflect(_frameVelocity, bounceHit.normal);
                _minAirHeight = transform.position.y;
                _maxAirHeight = transform.position.y;
                _bounceSound.Play();
            }
        }

        _grounded = groundHit;
    }

    private bool CanStartDrilling(Vector2 normal)
    {
        var mag = Vector2.Dot(-normal, _frameVelocity);
        if (mag > 0)
        {
            if (Vector2.Dot(normal, Vector2.up) > 0 && _maxAirHeight - transform.position.y > _digDownHeight ||
                Vector2.Dot(normal, Vector2.down) > 0 && transform.position.y - _minAirHeight < _digUpHeight)
            {
                return true;
            }
        }

        return false;
    }
    
    private void HandleMovement()
    {
        var target = _frameInput.Movement.x * _moveSpeed;
        if (_grounded)
        {
            _frameVelocity.x = target;
        }
        else
        {
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, target, _moveSpeed * _airControl * Time.fixedDeltaTime);
        }
    }

    private void HandleDrillMovement()
    {
        if (_grounded)
        {
            _frameVelocity = new Vector2(0f, _frameVelocity.y).normalized * _drillSpeed;
        }
    }

    private void HandleDrillGravity()
    {
        if (!_grounded)
        {
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_maxFallSpeed, _gravity * Time.fixedDeltaTime);
        }
    }

    private float HeightToVelocity(float height) => Mathf.Sqrt(2 * height * _gravity);
    
    private void HandleJump()
    {
        if (_grounded && _jumpQueued)
        {
            _frameVelocity.y = HeightToVelocity(_jumpHeight);
            _jumpSound.Play();
        }
    }
    
    private void HandleGravity()
    {
        if (_grounded && _frameVelocity.y <= 0) _frameVelocity.y = -_groundingForce;
        else
        {
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_maxFallSpeed, _gravity * Time.fixedDeltaTime);
        }
    }
    
    private void ApplyMovement()
    {
        _rb.linearVelocity = _frameVelocity;
    }

    private struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Movement;
    }
}
