using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchingDirections : MonoBehaviour
{
    public ContactFilter2D castFilter;
    public float groundDistance = 0.05f;
    public float wallDistance = 0.2f;
    public float ceilingDistance = 0.05f;
    BoxCollider2D touchingCol;
    Animator animator;

    RaycastHit2D[] groundHits = new RaycastHit2D[5];
    RaycastHit2D[] wallHits = new RaycastHit2D[5];
    RaycastHit2D[] ceilingHits = new RaycastHit2D[5];

    [SerializeField]
    private bool _isGrounded;

    public bool IsGrounded
    {
        get
        {
            return _isGrounded;
        }
        private set
        {
            _isGrounded = value;
            animator.SetBool(AnimationStrings.isGrounded, value);
        }
    }

        [SerializeField]
    private bool _isOnWall;

    public bool IsOnWall
    {
        get
        {
            return _isOnWall;
        }
        private set
        {
            _isOnWall = value;
            animator.SetBool(AnimationStrings.isOnWall, value);
        }
    }

        [SerializeField]
    private bool _isOnCeiling;

    private Vector2 wallCheckDirection => gameObject.transform.localScale.x > 0 ? Vector2.right : Vector2.left;

    public bool IsOnCeiling
    {
        get
        {
            return _isOnCeiling;
        }
        private set
        {
            _isOnCeiling = value;
            animator.SetBool(AnimationStrings.isOnCeiling, value);
        }
    }

    public Vector2 Normal
    {
        get
        {
            foreach (var hit in wallHits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Wall"))
                {
                    return hit.normal;
                }
            }
            return Vector2.zero;
        }
    }

    private void Awake()
    {
        touchingCol = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {Vector2 raycastStartPoint1 = (Vector2)transform.position + new Vector2(touchingCol.size.x * 0.5f, -touchingCol.size.y * 0.5f);
    Vector2 raycastStartPoint2 = (Vector2)transform.position + new Vector2(-touchingCol.size.x * 0.5f, -touchingCol.size.y * 0.5f);

    // 레이를 쏘고 바닥에 닿았는지 확인
    bool isGrounded1 = Physics2D.Raycast(raycastStartPoint1, Vector2.down, groundDistance, castFilter.layerMask);
    bool isGrounded2 = Physics2D.Raycast(raycastStartPoint2, Vector2.down, groundDistance, castFilter.layerMask);

    // 두 레이 중 하나라도 바닥에 닿았다면 isGrounded를 true로 설정
    IsGrounded = isGrounded1 || isGrounded2;
        Vector2 raycastStartPoint = (Vector2)transform.position - new Vector2(touchingCol.size.x * 0.5f, touchingCol.size.y * 0.5f);

        //IsGrounded = Physics2D.Raycast(raycastStartPoint, Vector2.down, groundDistance, castFilter.layerMask);
        IsOnWall = Physics2D.Raycast(raycastStartPoint, Vector2.right, wallDistance, castFilter.layerMask) ||
                    Physics2D.Raycast(raycastStartPoint, Vector2.left, wallDistance, castFilter.layerMask);
        IsOnCeiling = Physics2D.Raycast(raycastStartPoint, Vector2.up, ceilingDistance, castFilter.layerMask);
    }
}
