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
    public float remainingJumps = 0;
    private bool isOnWallJumpCooldown = false;
    public float blinkDistance = 5f; // 블링크 거리
    public float blinkDuration = 0.2f; // 블링크 지속 시간
    private bool isBlinking = false;
    public float blinkCooldown = 3f; // 블링크 쿨타임 (3초)
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

    public void OnBlink(InputAction.CallbackContext context)
    {
        if (context.started && !isBlinking && CanMove)
        {
            StartCoroutine(PerformBlink());
        }
    }

    private bool canBlink = true; // 블링크 사용 가능 여부를 나타내는 변수

    private IEnumerator PerformBlink()
{
    // 현재 위치와 바라보는 방향을 얻어옴
    Vector2 startPosition = transform.position;
    Vector2 blinkDirection = IsFacingRight ? Vector2.right : Vector2.left;

    // 레이캐스트를 쏘아 Ground 레이어를 가진 오브젝트를 체크
    RaycastHit2D hit = Physics2D.Raycast(startPosition, blinkDirection, blinkDistance, LayerMask.GetMask("Ground"));

    // Ground 레이어를 가진 오브젝트와 충돌하지 않았을 때 블링크 실행
    if (hit.collider == null)
    {
        // 블링크 시작
        isBlinking = true;

        // 무적 활성화
        damageable.isInvincible = true;

        // 블링크 끝 위치 계산
        Vector2 endPosition = startPosition + blinkDirection * blinkDistance;

        // 플레이어 이동
        rb.position = endPosition;

        // 무적 비활성화
        yield return new WaitForSeconds(blinkCooldown);
        isBlinking = false; // 블링크 쿨타임 후에 다시 블링크 가능하게 설정

    }
}

   private void FixedUpdate()
{
    // 물리 연산 - 이동 로직
    if (!damageable.LockVelocity)
        rb.velocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.velocity.y);

    if (touchingDirections.IsOnWall && !isJumpingOffWall)
    {
        // 벽에 붙어 있다면 수평 속도를 0으로 설정해서 움직이지 않게
        rb.velocity = new Vector2(0f, rb.velocity.y);

        // Shift 키를 누를 때만 벽 반대편으로 튕기도록
        if (CanMove && !touchingDirections.IsGrounded && Input.GetKey(KeyCode.LeftShift) && !isOnWallJumpCooldown && touchingDirections.Normal != Vector2.zero)
        {
            Vector2 jumpDirection = touchingDirections.Normal.normalized;
            WallJump(jumpDirection);
        }
    }
}

private void Update()
{
    if (Input.GetKeyDown(KeyCode.LeftShift) && touchingDirections.IsOnWall && !isOnWallJumpCooldown)
    {
        // Shift 키를 눌렀을 때 벽 반대편으로 튕기는 기능을 호출
        TryWallJump();
    }
}
private void TryWallJump()
{
    // 캐릭터가 오른쪽을 보고 있으면 왼쪽으로, 왼쪽을 보고 있으면 오른쪽으로 점프하도록 설정
    Vector2 jumpDirection = IsFacingRight ? Vector2.left : Vector2.right;

    // WallJump 함수에 jumpDirection을 전달
    WallJump(jumpDirection);
}
private void WallJump(Vector2 jumpDirection)
{
    Jump(jumpImpulse);
    rb.velocity = new Vector2(jumpDirection.x * jumpImpulse, jumpImpulse);
    isOnWallJumpCooldown = true;
    StartCoroutine(DisableWallJumpCooldown());
    isJumpingOffWall = true;
}
private IEnumerator DisableWallJumpCooldown()
{
    yield return new WaitForSeconds(1.0f); // 벽 점프 쿨타임 (1초로 설정)

    isOnWallJumpCooldown = false;
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
public bool hasDBJumpBuff = false;
    public void OnJump(InputAction.CallbackContext context)
{
    if (context.started && CanMove)
    {
        Vector2 jumpDirection = IsFacingRight ? Vector2.right : Vector2.left; // 플레이어가 바라보는 방향으로 설정

        if (touchingDirections.IsGrounded)
        {
            remainingJumps = 0;

            // DBJump 버프 아이템을 획득한 경우
            if (hasDBJumpBuff) 
            {
                remainingJumps = 1; // DBJump 버프로 인해 2번 점프 가능
            }

            Jump(jumpImpulse);
        }
        else if (remainingJumps > 0)
        {
            remainingJumps--;
            Jump(jumpImpulse);
        }
        else if (touchingDirections.IsOnWall && !isOnWallJumpCooldown)
        {
            WallJump(jumpDirection); // jumpDirection을 전달하여 WallJump 호출
        }
    }
}
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

    /*public void DownJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (currentOneWayPlatform != null)
            {
                StartCoroutine(DisableCollision());
                animator.SetTrigger(AnimationStrings.jump);
            }
        }
    }*/
    public void DownJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (currentOneWayPlatform != null)
            {
                // 만약 현재 밟고 있는 플랫폼이 DontDJ 태그를 가진 타일이라면 다운점프를 허용x
                if (!currentOneWayPlatform.CompareTag("UPOnly"))
                {
                    StartCoroutine(DisableCollision());
                    animator.SetTrigger(AnimationStrings.jump);
                }
                else
                {
                    // DontDJ 태그를 가진 타일 위에 있을 때 다운점프를 허용하지 않는 메시지 출력
                    Debug.Log("다운점프가 금지된 타일 위에 있습니다.");
                }
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fallen"))
        {
            // "fallen" 영역에 닿았을 때, 최대 체력의 10%를 감소.
            int maxHealth = damageable.MaxHealth;
            int healthToReduce = maxHealth / 10; // 최대 체력의 10%를 계산
            bool damageApplied = damageable.ApplyDamage(healthToReduce, Vector2.zero); // 체력 감소

            if (damageApplied)
            {
                // "respawn" 영역으로 이동
                MoveToRespawnZone();
            }
        }
    }
    private void MoveToRespawnZone()
    {
        // Respawn 영역을 찾기
        GameObject[] respawnAreas = GameObject.FindGameObjectsWithTag("RespawnArea");

        if (respawnAreas.Length > 0)
        {
            Vector3 respawnPosition = respawnAreas[0].transform.position;

            // 플레이어를 respawn 위치로 이동
            transform.position = respawnPosition;
        }
        else
        {
            // Respawn 영역을 찾지 못한 경우에 대한 예외 처리
            Debug.LogWarning("Respawn 영역을 찾을 수 없습니다.");
        }
    }
    //리스폰 에리어를 체크 하는데 ex 1~ 10 까지 있다. 2와 3구간에 있다 > 무조건 전 숫자로
    //or 리스폰 에리어를 플레이가 화면에는 안보이지만 체크포인트 개념으로 해서 체크포인트를 넘어가지 않았으면 가장 최근 체크포인트로 리스폰
    
    //중간고사 전까지 가호확실하게 구현할것 몇가지
    // 점프 횟수 늘려주는 가호 + 블링크 거리 증가 + 블링크 쿨타임 감소
}