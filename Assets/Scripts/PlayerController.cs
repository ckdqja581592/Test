using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damageable))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float airWalkSpeed = 3f;
    public float jumpImpulse = 10f;
    public int remainingJumps = 1;
    private bool isOnWallJumpCooldown = false;
    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Damageable damageable;
    Rigidbody2D rb;
    Animator animator;
    
    private GameObject currentOneWayPlatform;
    [SerializeField] private CapsuleCollider2D playerCollider;
    [SerializeField] private Collider2D wallCollider;

    private bool isJumpingOffWall = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentOneWayPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentOneWayPlatform = null;
        }
    }

    private IEnumerator DisableCollision()
    {
        BoxCollider2D platformCollider = currentOneWayPlatform.GetComponent<BoxCollider2D>();
        Physics2D.IgnoreCollision(playerCollider, platformCollider);
        yield return new WaitForSeconds(1f);
        Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
    }

    public float CurrentMoveSpeed
    {
        get
        {
            if (CanMove)
            {
                if (IsMoving && !touchingDirections.IsOnWall)
                {
                    if (touchingDirections.IsGrounded)
                    {
                        if (IsRunning)
                        {
                            return runSpeed;
                        }
                        else
                        {
                            return walkSpeed;
                        }
                    }
                    else
                    {
                        return airWalkSpeed;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }

    [SerializeField]
    private bool _isMoving = false;

    public bool IsMoving
    {
        get
        {
            return _isMoving;
        }
        private set
        {
            _isMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);
        }
    }

    [SerializeField]
    private bool _isRunning = false;

    public bool IsRunning
    {
        get
        {
            return _isRunning;
        }
        set
        {
            _isRunning = value;
            animator.SetBool(AnimationStrings.isRunning, value);
        }
    }

    public bool _isFacingRight = true;

    public bool IsFacingRight
    {
        get
        {
            return _isFacingRight;
        }
        private set
        {
            if (_isFacingRight != value)
            {
                transform.localScale *= new Vector2(-1, 1);
            }
            _isFacingRight = value;
        }
    }

    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }
    }

    public bool IsAlive
    {
        get
        {
            return animator.GetBool(AnimationStrings.isAlive);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damageable = GetComponent<Damageable>();
        wallCollider = GetComponentInChildren<Collider2D>();
    }

    public float blinkDistance = 5f; // 블링크 거리
    public float blinkDuration = 0.2f; // 블링크 지속 시간
    private bool isBlinking = false;
    private float blinkCooldown = 3f; // 블링크 쿨타임 (3초)

    public void OnBlink(InputAction.CallbackContext context)
    {
        if (context.started && !isBlinking && CanMove)
        {
            StartCoroutine(PerformBlink());
        }
    }

    private IEnumerator PerformBlink()
    {
        // 블링크 시작
        isBlinking = true;

        // 현재 위치와 바라보는 방향을 얻어옴
        Vector2 startPosition = transform.position;
        Vector2 blinkDirection = IsFacingRight ? Vector2.right : Vector2.left;
        
        // 블링크 끝 위치 계산
        Vector2 endPosition = startPosition + blinkDirection * blinkDistance;

        // 무적 활성화
        damageable.isInvincible = true;

        // 플레이어 이동
        rb.position = endPosition;

        // 무적 비활성화
        yield return new WaitForSeconds(blinkDuration);
        damageable.isInvincible = false;

        // 블링크 종료
        isBlinking = false;
    }

    private void FixedUpdate()
    {
        if (!damageable.LockVelocity)
            rb.velocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.velocity.y);

        // 벽에 붙어 있을 때 이동을 막기 위해 추가 제어 로직이 필요합니다.
        if (touchingDirections.IsOnWall && !isJumpingOffWall)
        {
            // 벽에 붙어 있다면 수평 속도를 0으로 설정하여 움직이지 않도록 합니다.
            rb.velocity = new Vector2(0f, rb.velocity.y);

            // 벽에 붙어 있을 때만 점프할 수 있도록 검사합니다.
            if (CanMove && !touchingDirections.IsGrounded && !isOnWallJumpCooldown)
            {
                WallJump();
            }
        }
    }

    private void WallJump()
    {
        // 벽에서의 점프 로직
        Vector2 wallJumpDirection = IsFacingRight ? Vector2.left : Vector2.right;
        Jump(jumpImpulse);
        rb.velocity = new Vector2(rb.velocity.x, jumpImpulse);
        rb.AddForce(wallJumpDirection * jumpImpulse * 0.5f, ForceMode2D.Impulse);
        isOnWallJumpCooldown = true;
        StartCoroutine(DisableWallCollision());
        isJumpingOffWall = true;
    }

    private IEnumerator DisableWallCollision()
    {
        // 벽과의 충돌 무시 설정
        Physics2D.IgnoreCollision(playerCollider, wallCollider, true);

        // 일정 시간 후에 충돌 무시 해제
        yield return new WaitForSeconds(0.5f);

        Physics2D.IgnoreCollision(playerCollider, wallCollider, false);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (IsAlive)
        {
            IsMoving = moveInput != Vector2.zero;
            SetFacingDirection(moveInput);
        }
        else
        {
            IsMoving = false;
        }
    }

    private void SetFacingDirection(Vector2 moveInput)
    {
        if (moveInput.x > 0 && !IsFacingRight)
        {
            IsFacingRight = true;
        }
        else if (moveInput.x < 0 && IsFacingRight)
        {
            IsFacingRight = false;
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            IsRunning = true;
        }
        else if (context.canceled)
        {
            IsRunning = false;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && CanMove)
        {
            if (touchingDirections.IsGrounded)
            {
                remainingJumps = 1;
                Jump(jumpImpulse);
            }
            else if (remainingJumps > 0)
            {
                remainingJumps--;
                Jump(jumpImpulse);
            }
            else if (touchingDirections.IsOnWall && !isOnWallJumpCooldown)
            {
                WallJump();
            }
        }
    }
    // 1 jump
    /*public void OnJump(InputAction.CallbackContext context)
    {
        // TODO Check if alive as well
        if(context.started && touchingDirections.IsGrounded && CanMove)
        {
            animator.SetTrigger(AnimationStrings.jump);
            rb.velocity = new Vector2(rb.velocity.x, jumpImpulse);
        }
    }*/

    // double jump
    /*public void OnJump(InputAction.CallbackContext context)
    {
        // TODO Check if alive as well
        if (context.started && CanMove)
        {
            if (touchingDirections.IsGrounded)
            {
                remainingJumps = 1; // 초기 점프 횟수 설정
                animator.SetTrigger(AnimationStrings.jump);
                rb.velocity = new Vector2(rb.velocity.x, jumpImpulse);
            }
            else if (remainingJumps > 0) // 더블 점프 가능한 경우
            {
                remainingJumps--; // 더블 점프 횟수 감소
                animator.SetTrigger(AnimationStrings.jump);
                rb.velocity = new Vector2(rb.velocity.x, jumpImpulse);
            }
        }
    }*/
    private void Jump(float jumpForce)
    {
        animator.SetTrigger(AnimationStrings.jump);
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            animator.SetTrigger(AnimationStrings.attack);
        }
    }

    public void OnRangedAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            animator.SetTrigger(AnimationStrings.rangedAttackTrigger);
        }
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        rb.velocity = new Vector2(knockback.x, rb.velocity.y + knockback.y);
    }

    public void DownJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (currentOneWayPlatform != null)
            {
                StartCoroutine(DisableCollision());
                animator.SetTrigger(AnimationStrings.jump);
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fallen"))
        {
            // "fallen" 영역에 닿았을 때, 최대 체력의 10%를 감소시킵니다.
            int maxHealth = damageable.MaxHealth;
            int healthToReduce = maxHealth / 10; // 최대 체력의 10%를 계산
            bool damageApplied = damageable.ApplyDamage(healthToReduce, Vector2.zero); // 체력 감소

            if (damageApplied)
            {
                // "respawn" 영역으로 이동합니다.
                MoveToRespawnZone();
            }
        }
    }
    private void MoveToRespawnZone()
    {
        // 이동할 위치를 설정하십시오. 예를 들어, respawn 위치를 Vector3로 지정합니다.
        Vector3 respawnPosition = new Vector3(-17.41f, 5.24f, 0f); // 원하는 위치로 변경하십시오.

        // 플레이어를 respawn 위치로 이동시킵니다.
        transform.position = respawnPosition;
    }
    
}