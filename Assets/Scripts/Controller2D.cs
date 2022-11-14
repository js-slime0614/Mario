using UnityEngine;
using System.Collections;
using Micat1996;

#region How To Use?
/* 해당 클래스를 상속받는 자식 클래스를 만드십시오.
 * 자식 클래스에서 Start에는 ParentStart()를 호출하십시오.
 * 자식 클래스에서 update()에는 ParentUpdate() 를 호출하십시오.
 * 해당 오브젝트의 이동 속도를 변경할 때에는 'moveSpeed' 프로퍼티를 이용하십시오.
 */
#endregion

[RequireComponent(typeof(BoxCollider2D))]
public abstract class Controller2D : MonoBehaviour
{

    #region Show Inspector
    [Tooltip("수평 충돌을 검사하기 위한 레이 개수입니다.")]
    [SerializeField] [Range(4, 20)] private int _HoriRayCount = 4;

    [Tooltip("수직 충돌을 검사하기 위한 레이 개수입니다.")]
    [SerializeField] [Range(4, 20)] private int _VerRayCount = 4;

    [Tooltip("충돌 처리할 레이어를 이 곳에 등록합니다.")]
    [SerializeField] private LayerMask _CollLayer;

    [Tooltip("오를 수 있는 최대 각도입니다.")]
    [SerializeField] private float _MaxClimbAngle = 80;

    [Tooltip("점프하지 않고 내려갈 수 있는 최대 각도입니다.")]
    [SerializeField] private float _MaxDescendAngle = 80;

    [Tooltip("최대 점프 높이입니다.")]
    [SerializeField] private float _MaxJumpHeight = 1;

    [Tooltip("최대 높이까지 걸리는 시간을 설정합니다.\n해당 값은 떨어지는 속도에 영향을 줍니다.")]
    [SerializeField] private float _TimeToJumpApex = 0.5f;

    [Tooltip("점프중일 때 진행중인 방향으로 줄 힘입니다.\n값이 높을수록 점프시 방향을 변경할 때 느리게 변경됩니다.\n해당 값은 음수가 될 수 없습니다.")]
    [Range(0f, 100f)] public float _JumpHoriPower = 0.2f;

    [Tooltip("진행중인 방향으로 지면에서 미끄러지게 될 양입니다.")]
    [SerializeField] private float _SlideMoveValue = 0.1f;

    [Tooltip("해당 오브젝트의 이동 속도입니다.")]
    [SerializeField] private float _MoveSpeed = 6;

    [Tooltip("점프 타입을 설정합니다.")]
    [SerializeField] private JumpType _JumpType = JumpType.HOLD;

    [Tooltip("점프를 몇 번 가능하도록 할 것인지 설정합니다.")]
    [SerializeField] [Range(0, 100)] private int _JumpCount = 1;

    [Tooltip("벽타기를 사용할 것인지 설정합니다.")]
    [SerializeField] private bool _UseWallSlide = false;

    [Tooltip("벽타기를 할 때 내려갈 속도입니다.")]
    [SerializeField] private float _WallSlideSpeed = 0.5f;

    [Tooltip("벽타기 진행중 점프했을 때 이동시킬 X 값입니다.")]
    [SerializeField] private float _WallJumpXValue = 30f;

    #endregion



    #region Hide Inspector
    // 충돌에 대한 정보를 저장할 구조체입니다.
    private CollisionInfo collisions;

    // 오브젝트 테두리 두께를 설정합니다.
    private const float _SkinWidth = 0.015f;

    // 오브젝트의 박스콜라이더를 참조시킬 참조변수입니다.
    private BoxCollider2D _Collider;

    private RaycastOrigins _RayOrigin;

    // 수평 충돌 처리를 위한 레이의 간격입니다.
    private float _HoriRaySpacing;

    // 수직 충돌 처리를 위한 레이의 간격입니다.
    private float _VerRaySpacing;

    // 수평 충돌 처리를 위한 레이의 개수를 저장합니다.
    private int _HoriRayTempCount = 0;

    // 수직 충돌 처리를 위한 레이의 개수를 저장합니다.
    private int _VerRayTempCount = 0;

    // 해당 오브젝트에 적용시킬 중력량입니다. 해당 값은 Physics2D > GravityY 값의 영향을 받습니다.
    private float _Gravity;

    // 중력과 점프 최대 높이를 이용하여 오브젝트에 순간적인 힘을 줄 값을 계산하여 이 변수에 담습니다.
    private float _JumpVelocity;

    // 오브젝트의 가속도입니다.
    private Vector3 _Velocity;

    // 현재 수평 이동 속도입니다. 이 값은 계속 변경됩니다.
    private float _VelocityXSmooth;

    // 해당 오브젝트가 점프중이지 체크할 변수입니다.
    private bool _IsJumping = false;

    // 추가적으로 점프를 몇 번 가능한지 체크할 변수입니다.
    private int _CanJumpCount = 0;

    // 점프키를 눌렀다가 떼었는지 검사할 변수입니다.
    private bool _AddJumpInput = false;

    // 현재 벽타기중인지 체크할 변수입니다.
    private bool _WallSliding = false;
    #endregion



    #region Child Method
    protected void ParentStart()
    {
        _Collider = GetComponent<BoxCollider2D>();
        _Gravity = -(Physics2D.gravity.y * -_MaxJumpHeight) / Mathf.Pow(_TimeToJumpApex, 2);
        _JumpVelocity = Mathf.Abs(_Gravity) * _TimeToJumpApex;
        _CanJumpCount = _JumpCount;
        CalculateRaySpacing();
    }

    // 인자 : 이동시킬 X 값, 점프 키
    protected void ParentUpdate(float moveXValue, KeyCode jumpKey)
    {
        if (_HoriRayTempCount != _HoriRayCount ||
            _VerRayTempCount != _VerRayCount)
        {
            _HoriRayTempCount = _HoriRayCount;
            _VerRayTempCount = _VerRayCount;
            CalculateRaySpacing();
        }

        _Gravity = -(Physics2D.gravity.y * -_MaxJumpHeight) / Mathf.Pow(_TimeToJumpApex, 2);
        _JumpVelocity = Mathf.Abs(_Gravity) * _TimeToJumpApex;

        if (collisions.above || collisions.below) _Velocity.y = 0;

        float targetVelocityX = moveXValue * _MoveSpeed;

        _Velocity.x = Mathf.SmoothDamp(_Velocity.x, targetVelocityX, ref _VelocityXSmooth, (collisions.below) ? _SlideMoveValue : _JumpHoriPower);
        _Velocity.y += _Gravity * Time.deltaTime;

        // 점프 타입에 따라 점프를 처리합니다.
        if (_JumpType == JumpType.HOLD)
        {

            if (_CanJumpCount != 0)
            {
                if (Input.GetKeyDown(jumpKey))
                {
                    _AddJumpInput = true;
                    _IsJumping = true;
                    _Velocity.y = _JumpVelocity;
                    _CanJumpCount--;
                    if (collisions.below)
                    {
                        _Velocity.y = _JumpVelocity;
                    }

                    if (_UseWallSlide && _WallSliding)
                    {
                        if (collisions.left) _Velocity.x = _WallJumpXValue;
                        else if (collisions.right) _Velocity.x = -_WallJumpXValue;
                    }
                }
                else if (Input.GetKey(jumpKey))
                {
                    _Velocity.y -= 0.1f;
                }
            }
            if (Input.GetKeyUp(jumpKey))
            {
                _AddJumpInput = false;
                if (_Velocity.y > 0) _Velocity.y = 0f;
            }
        }
        else if (_JumpType == JumpType.KEYDOWN)
        {
            if (_CanJumpCount != 0)
            {
                if (Input.GetKeyDown(jumpKey))
                {
                    _AddJumpInput = true;
                    _IsJumping = true;
                    _Velocity.y = _JumpVelocity;
                    _CanJumpCount--;
                    if (collisions.below)
                    {
                        _Velocity.y = _JumpVelocity;
                    }

                    if (_UseWallSlide && _WallSliding)
                    {
                        if (collisions.left) _Velocity.x = _WallJumpXValue;
                        else if (collisions.right) _Velocity.x = -_WallJumpXValue;
                    }
                }
                else if (Input.GetKeyUp(jumpKey))
                {
                    _AddJumpInput = false;
                }
            }
        }


        Move(_Velocity);
    }
    #endregion



    #region 2DController Method
    protected void Move(Vector3 velocity)
    {
        velocity *= Time.deltaTime;

        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.velocityOld = velocity;

        if (velocity.y < 0) DescendSlope(ref velocity);
        if (velocity.x != 0) HorizontalCollisions(ref velocity);
        if (velocity.y != 0) VerticalCollisions(ref velocity);
        transform.Translate(velocity);
    }

    private void HorizontalCollisions(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + _SkinWidth;

        for (int i = 0; i < _HoriRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? _RayOrigin.bottomLeft : _RayOrigin.bottomRight;
            rayOrigin += Vector2.up * (_HoriRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, _CollLayer);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= _MaxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - _SkinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > _MaxClimbAngle)
                {
                    velocity.x = (hit.distance - _SkinWidth) * directionX;
                    rayLength = hit.distance;



                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;

                    // 벽타기 구현
                    if (_UseWallSlide && _IsJumping && _Velocity.y <= 0)
                    {
                        velocity.y = -_WallSlideSpeed;
                        _CanJumpCount = _JumpCount;
                        _WallSliding = true;
                    }
                }
            }
        }
        if (_WallSliding)
            _IsJumping = false;

        if (_WallSliding && !collisions.left && !collisions.right && !_IsJumping)
            _WallSliding = false;
    }

    private void VerticalCollisions(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + _SkinWidth;

        for (int i = 0; i < _VerRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? _RayOrigin.bottomLeft : _RayOrigin.topLeft;
            rayOrigin += Vector2.right * (_VerRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, _CollLayer);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                velocity.y = (hit.distance - _SkinWidth) * directionY;
                rayLength = hit.distance;

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;

                // 점프 취소
                if (_IsJumping && collisions.below)
                {
                    _IsJumping = false;
                }

                if (_WallSliding) _WallSliding = false;


                if (_CanJumpCount != _JumpCount && collisions.below) _CanJumpCount = _JumpCount;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }


            }

            if (!_IsJumping && !collisions.below)
            {
                _IsJumping = true;
            }
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + _SkinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? _RayOrigin.bottomLeft : _RayOrigin.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, _CollLayer);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - _SkinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    private void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    private void DescendSlope(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = (directionX == -1) ? _RayOrigin.bottomRight : _RayOrigin.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, _CollLayer);
        if (hit.collider != null)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= _MaxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - _SkinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    private void UpdateRaycastOrigins()
    {
        Bounds bounds = _Collider.bounds;
        bounds.Expand(_SkinWidth * -2);

        _RayOrigin.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        _RayOrigin.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        _RayOrigin.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        _RayOrigin.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    private void CalculateRaySpacing()
    {
        Bounds bounds = _Collider.bounds;
        bounds.Expand(_SkinWidth * -2);
        _HoriRayCount = Mathf.Clamp(_HoriRayCount, 2, int.MaxValue);
        _VerRayCount = Mathf.Clamp(_VerRayCount, 2, int.MaxValue);
        _HoriRaySpacing = bounds.size.y / (_HoriRayCount - 1);
        _VerRaySpacing = bounds.size.x / (_VerRayCount - 1);
    }
    #endregion



    #region Properties
    // getter & setter...
    // 수평 레이 개수에 대한 프로퍼티입니다.
    public int horiRayCount
    {
        get { return _HoriRayCount; }
        set { _HoriRayCount = value; }
    }

    // 수직 레이 개수에 대한 프로퍼티입니다.
    public int verRayCount
    {
        get { return _VerRayCount; }
        set { _VerRayCount = value; }
    }

    // 오를 수 있는 최대 높이입니다.
    public float maxClimbAngle
    {
        get { return _MaxClimbAngle; }
        set { _MaxClimbAngle = value; }
    }

    // 점프하지 않고 내려갈 수 있는 최대 높이입니다.
    public float maxDescendAngle
    {
        get { return _MaxClimbAngle; }
        set { _MaxClimbAngle = value; }
    }

    // 최대 점프 높이에 대한 프로퍼티입니다.
    public float maxJumpHeight
    {
        get { return _MaxJumpHeight; }
        set { _MaxJumpHeight = value; }
    }

    // 최대 점프 높이까지 도달할 때까지 걸리는 시간에 대한 프로퍼티입니다.
    public float timeToJumpApex
    {
        get { return _TimeToJumpApex; }
        set { _TimeToJumpApex = value; }
    }

    // 점프중 진행중인 방향으로 줄 힘에 대한 프로퍼티입니다.
    public float jumpHoriPower
    {
        get { return _JumpHoriPower; }
        set { _JumpHoriPower = value; }
    }

    // 지면에서 미끄러지게 될 양에 대한 프로퍼티입니다.
    public float slideMoveValue
    {
        get { return _SlideMoveValue; }
        set { _SlideMoveValue = value; }
    }

    // 이동 속도에 대한 프로퍼티입니다.
    public float moveSpeed
    {
        get { return _MoveSpeed; }
        set { _MoveSpeed = value; }
    }

    // 점프 타입에 대한 프로퍼티입니다.
    public JumpType jumpType
    {
        get { return _JumpType; }
        set { _JumpType = value; }
    }

    // 현재 몇 번 점프 가능한지에 대한 프로퍼티입니다.
    public int canJumpCount
    {
        get { return _CanJumpCount; }
        set { _CanJumpCount = value; }
    }

    // 점프 최대 카운트에 대한 프로퍼티입니다.
    public int jumpCount
    {
        get { return _JumpCount; }
        set { _JumpCount = value; }
    }

    // 벽타기를 사용할 것인지에 대한 프로퍼티입니다.
    public bool useWallSlide
    {
        get { return _UseWallSlide; }
        set { _UseWallSlide = value; }
    }

    // 벽타기 속도에 대한 프로퍼티입니다.
    public float wallSlideSpeed
    {
        get { return _WallSlideSpeed; }
        set { _WallSlideSpeed = value; }
    }

    // 벽타기중 점프했을 경우 X 값을 얼마나 증가시킬 것인지에 대한 프로퍼티입니다.
    public float wallJumpXValue
    {
        get { return _WallJumpXValue; }
        set { _WallJumpXValue = value; }
    }


    // getter...
    // 해당 오브젝트의 Collider 컴포넌트에 대한 게터입니다.
    public Collider2D getCollider
    {
        get { return _Collider; }
    }

    // 수평 레이 간격에 대한 게터입니다.
    public float getHoriRaySpacing
    {
        get { return _HoriRaySpacing; }
    }

    // 수직 레이 간격에 대한 게터입니다.
    public float getVerRaySpacing
    {
        get { return _VerRaySpacing; }
    }

    // 오브젝트에 적용된 중력값에 대한 게터입니다.
    public float getGravity
    {
        get { return _Gravity; }
    }

    // _JumpVelocity에 대한 게터입니다.
    public float getJumpVelocity
    {
        get { return _JumpVelocity; }
    }

    // 오브젝트의 가속도에 대한 게터입니다.
    public Vector3 getVelocity
    {
        get { return _Velocity; }
    }

    // 점프를 체크할 변수에 대한 게터입니다.
    // 해당 값은 벽타기중이라면 true 입니다.
    public bool isJumping
    {
        get { return _IsJumping; }
    }

    // 벽타기상태에 대한 게터입니다.
    public bool wallSliding
    {
        get { return _WallSliding; }
    }
    #endregion
}

namespace Micat1996
{
    #region Struct

    // 레이를 쏠 끝 좌표들을 저장할 구조체입니다.
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    // 충돌에 관련된 정보를 저장할 구조체입니다.
    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector3 velocityOld;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
    #endregion

    #region Enums

    // HOLD : 누르고 있을 경우에만 점프하며 만약 키가 떼어질 경우 떨어집니다.
    // KEYDOWN : 한 번 입력되었을 경우 점프합니다.
    public enum JumpType { HOLD, KEYDOWN }
    #endregion
}