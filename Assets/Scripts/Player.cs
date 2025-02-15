using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    public enum State
    {
        Float,
        Roll,
        Flood
    }

    public float floatMaxSpeed = 3.0f;
    public float jumpSpeed = 3.0f;
    public float jumpTime = 0.5f;
    public float floatDec = 1f;
    public float attachAcc = 1f;
    public float rollMaxSpeed = 2.0f;
    public float rollAcc = 2f;
    public float rollDec = 1f;
    public float drillSpeed = 5.0f;
    public float floodAcc = 10f;
    public float floodDec = 5.0f;
    public float floodMaxSpeed = 3.0f;
    public float drillOutCheckDistance = 0.3f;
    public float hitEpsilon = 0.01f;

    public State state = State.Float;

    public Vector2 velocity = Vector2.zero;
    public float inputResistTime = 0.2f;
    public float rotateSpeed = 180.0f;
    public float floatRotateSpeed = 90.0f;
    public bool switchSceneWhenWin = true;

    public float deadReloadSceneTime = 1.5f;
    public AudioClip jumpSoundResource;
    private bool _absorbButtonDown;

    private float? _directRotateSpeed;
    private bool _isDead;

    private bool _jumpButtonDown;
    private bool _jumping;
    private Color _originalColor;
    private Vector2 _originPoint;
    private Coroutine _resetAbsorbButtonDownCoroutine;

    private Coroutine _resetJumpButtonDownCoroutine;
    private float? _targetRotation;

    public Color PlayerColor
    {
        get => GetComponent<BubbleColor>().color;
        set => GetComponent<BubbleColor>().color = value;
    }

    private Vector2 Position
    {
        get => Rigidbody.position;
        set => Rigidbody.position = value;
    }

    private Vector2 Velocity
    {
        get => velocity;
        set => velocity = value;
    }

    private float Rotation
    {
        get => Rigidbody.rotation;
        set => Rigidbody.rotation = value;
    }

    private Rigidbody2D Rigidbody => GetComponent<Rigidbody2D>();
    private AudioSource AudioSource => GetComponent<AudioSource>();

    public State PlayerState
    {
        get => state;
        set
        {
            var preState = state;
            // Exit
            switch (preState)
            {
                case State.Float:
                    _directRotateSpeed = null;
                    break;
                case State.Roll:
                    _targetRotation = null;
                    break;
                case State.Flood:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            state = value;
            // Enter
            switch (value)
            {
                case State.Float:
                    _directRotateSpeed = Random.value > 0.5f
                        ? floatRotateSpeed
                        : -floatRotateSpeed;
                    break;
                case State.Roll:
                    break;
                case State.Flood:
                    _targetRotation = 0.0f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }

    private void Start()
    {
        PlayerState = State.Float;
        Rotation = 0f;
        _originPoint = Position;
        _targetRotation = null;
        _directRotateSpeed = null;
    }

    private void Update()
    {
        if (_isDead) return;
        var targetBubbles = FindObjectsByType<TargetBubble>(FindObjectsSortMode.None).ToList();
        if (targetBubbles.Count(targetBubble => targetBubble.enabled) == 0)
            // Forbid any update
            return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            NextScene();
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LastScene();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
            return;
        }

        if (Input.GetButtonDown("Reset")) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (Input.GetButtonDown("Jump"))
        {
            _jumpButtonDown = true;
            if (_resetJumpButtonDownCoroutine != null) StopCoroutine(_resetJumpButtonDownCoroutine);

            IEnumerator ResetJumpButtonDownCoroutine()
            {
                yield return new WaitForSeconds(inputResistTime);
                _jumpButtonDown = false;
            }

            StartCoroutine(ResetJumpButtonDownCoroutine());
        }

        if (Input.GetButtonDown("Absorb"))
        {
            _absorbButtonDown = true;
            if (_resetAbsorbButtonDownCoroutine != null) StopCoroutine(_resetAbsorbButtonDownCoroutine);

            IEnumerator ResetAbsorbButtonDownCoroutine()
            {
                yield return new WaitForSeconds(inputResistTime);
                _absorbButtonDown = false;
            }

            StartCoroutine(ResetAbsorbButtonDownCoroutine());
        }

        if (_targetRotation.HasValue)
        {
            // [-360, 360]
            var deltaAngle = _targetRotation.Value - Rotation;
            if (deltaAngle > 180.0f) deltaAngle -= 360.0f;
            if (deltaAngle < -180.0f) deltaAngle += 360.0f;
            // [-180, 180]
            Rotation += math.sign(deltaAngle) * math.min(rotateSpeed * Time.deltaTime, math.abs(deltaAngle));
        }
        else if (_directRotateSpeed.HasValue)
        {
            Rotation += _directRotateSpeed.Value * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        InternalUpdate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<Spike>() != null)
        {
            _isDead = true;
            GetComponentInChildren<Animator>().SetBool("Dead", true);

            IEnumerator DeadCoroutine()
            {
                yield return new WaitForSeconds(deadReloadSceneTime);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            StartCoroutine(DeadCoroutine());
        }
    }

    private void InternalUpdate()
    {
        if (_isDead) return;
        var targetBubbles = FindObjectsByType<TargetBubble>(FindObjectsSortMode.None).ToList();
        if (targetBubbles.Count(targetBubble => targetBubble.enabled) == 0)
        {
            if (targetBubbles.Count(targetBubble => !targetBubble.isDead) == 0)
                if (switchSceneWhenWin)
                    NextScene();

            // Forbid any update
            return;
        }

        switch (PlayerState)
        {
            case State.Float:
            {
                var closestPoint = ClosetScenePoint();

                if (closestPoint != null && !_jumping)
                {
                    var direction = closestPoint.Value - Position;
                    Velocity += direction.normalized * (attachAcc * Time.fixedDeltaTime);
                }

                DecVelocity(floatDec);
                Velocity = Vector2.ClampMagnitude(Velocity, floatMaxSpeed);

                var closetHit = ClosetTranslationHit();
                if (closetHit == null)
                {
                    MoveByVelocity();
                }
                else
                {
                    Position += math.max(closetHit.Value.distance - hitEpsilon, 0f) * Velocity.normalized;
                    Velocity = Vector2.zero;
                    PlayerState = State.Roll;
                }

                break;
            }
            case State.Roll:
            {
                if (_jumpButtonDown)
                {
                    AudioSource.PlayOneShot(jumpSoundResource);
                    _jumpButtonDown = false;
                    var closetPoint = ClosetScenePoint();
                    if (closetPoint == null) return;
                    var normal = (Position - closetPoint.Value).normalized;
                    Velocity = normal * jumpSpeed;


                    IEnumerator JumpEndCoroutine()
                    {
                        _jumping = true;
                        yield return new WaitForSeconds(jumpTime);
                        _jumping = false;
                    }

                    StartCoroutine(JumpEndCoroutine());

                    IEnumerator KeepJumpSpeedCoroutine()
                    {
                        yield return null;
                        Velocity = Velocity.normalized * jumpSpeed;
                        if (PlayerState != State.Float) yield break;
                        if (!_jumping) yield break;
                    }

                    StartCoroutine(KeepJumpSpeedCoroutine());

                    PlayerState = State.Float;
                    InternalUpdate();
                    return;
                }

                if (_absorbButtonDown)
                {
                    _absorbButtonDown = false;
                    if (TryAbsorb()) return;
                }


                {
                    var closetPoint = ClosetScenePoint();
                    if (closetPoint == null) return;
                    var closetHit = ClosetSceneHit();
                    if (IsTargetBubbleHit(closetHit.Value))
                    {
                        var targetBubbleColor = GetBubbleColor(closetHit.Value);
                        if (targetBubbleColor != null && SameColor(targetBubbleColor.color, PlayerColor))
                        {
                            GetTargetBubble(closetHit.Value).enabled = false;
                            if (TryAbsorb()) return;
                        }
                    }
                }
                {
                    var closetPoint = ClosetScenePoint();
                    if (closetPoint == null) return;
                    var closetHit = ClosetSceneHit();
                    if (!IsBubbleHit(closetHit.Value))
                    {
                        var horizontalInput = Input.GetAxisRaw("Horizontal");
                        var normal = (Position - closetPoint.Value).normalized;
                        var directionNormal = new Vector2(normal.y, -normal.x);
                        Velocity += directionNormal * (horizontalInput * (rollAcc * Time.fixedDeltaTime));
                        _targetRotation = math.degrees(math.atan2(normal.y, normal.x)) - 90.0f;
                    }
                    else
                    {
                        Velocity = Vector2.zero;
                    }
                }
                DecVelocity(rollDec);
                Velocity = Vector2.ClampMagnitude(Velocity, rollMaxSpeed);

                var expectDistance = Velocity.magnitude * Time.fixedDeltaTime;
                for (var i = 0; i < 100; ++i)
                {
                    var translation = Velocity.normalized * expectDistance;
                    var closetHit = ClosetTranslationHit(translation);
                    if (closetHit == null)
                    {
                        MoveByVelocity();
                        var hit = ClosetSceneHit();
                        var closetPoint = ClosetScenePoint();
                        if (closetPoint == null) return;
                        var direction = closetPoint.Value - Position;
                        Position += direction.normalized * math.max(hit.Value.distance - hitEpsilon, 0f);
                        var directionNormal = new Vector2(direction.y, -direction.x).normalized;
                        Velocity = directionNormal *
                                   (Velocity.magnitude * math.sign(math.dot(directionNormal, Velocity)));
                        break;
                    }

                    var maxDistance = math.max(closetHit.Value.distance - hitEpsilon, 0f);
                    {
                        var closetPoint = ClosetScenePoint();
                        var preNormal = (Position - closetPoint.Value).normalized;
                        Position += maxDistance * Velocity.normalized;
                        var normal = Position - closetHit.Value.point;
                        var unsignedDirection = new Vector2(normal.y, -normal.x).normalized;
                        var sign = math.sign(math.dot(preNormal, unsignedDirection));
                        Velocity = Velocity.magnitude * sign * unsignedDirection;
                    }
                    expectDistance -= maxDistance;
                    if (expectDistance < 0.0) break;
                }

                break;
            }
            case State.Flood:
            {
                var floodHit = ClosetSceneHit();
                var floodingBubbleColor = GetBubbleColor(floodHit.Value);
                if (floodingBubbleColor == null) floodingBubbleColor = GetOverlappingBubbleColor();
                if (floodingBubbleColor == null)
                {
                    PlayerState = State.Roll;
                    InternalUpdate();
                    return;
                }

                var drillingOut = false;
                {
                    // Check drilling out
                    var translation = Velocity.normalized * drillOutCheckDistance;
                    var blockHit =
                        ClosetTranslationHit(translation, hit => !IsBubbleColorHit(hit, floodingBubbleColor));
                    if (blockHit != null)
                        translation = translation.normalized * math.max(blockHit.Value.distance - hitEpsilon, 0);
                    {
                        var prePosition = Position;
                        Position += translation;
                        drillingOut = !IsOverlappingBubble();
                        Position = prePosition;
                    }
                }
                if (drillingOut) Velocity = Velocity.normalized * drillSpeed;
                if (!drillingOut)
                {
                    var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                    Velocity += input * (floodAcc * Time.fixedDeltaTime);
                }

                DecVelocity(floodDec);
                Velocity = Vector2.ClampMagnitude(Velocity, floodMaxSpeed);
                if (drillingOut)
                {
                    var translation = Velocity * Time.fixedDeltaTime;
                    var blockHit =
                        ClosetTranslationHit(translation, hit => !IsBubbleColorHit(hit, floodingBubbleColor));
                    if (blockHit != null)
                        translation = math.max(blockHit.Value.distance - hitEpsilon, 0f) * Velocity.normalized;
                    Vector2? drillStickPosition = null;
                    var prePosition = Position;
                    {
                        Position += translation;
                        var bubbleHit = ClosetTranslationHit(-translation, hit =>
                            IsBubbleColorHit(hit, floodingBubbleColor));
                        if (!IsOverlappingBubble() && bubbleHit != null)
                            drillStickPosition = Position + -translation.normalized *
                                math.max(bubbleHit.Value.distance - hitEpsilon, 0f);
                    }
                    Position = prePosition;
                    if (drillStickPosition != null)
                    {
                        Position = drillStickPosition.Value;
                        PlayerState = State.Roll;
                        return;
                    }
                }

                {
                    var targetHit = ClosetTranslationHit(hit =>
                        IsBubbleColorHit(hit, floodingBubbleColor) && IsTargetBubbleHit(hit));
                    if (targetHit != null) GetTargetBubble(targetHit.Value).enabled = false;
                    var closetHit = ClosetTranslationHit(hit =>
                        !IsBubbleColorHit(hit, floodingBubbleColor));
                    if (closetHit == null)
                    {
                        MoveByVelocity();
                    }
                    else
                    {
                        Position += math.max(closetHit.Value.distance - hitEpsilon, 0f) * Velocity.normalized;
                        Velocity = Vector2.zero;
                    }
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void NextScene()
    {
        if (SceneManager.GetActiveScene().buildIndex != -1)
        {
            var next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next >= SceneManager.sceneCountInBuildSettings) next = 0;
            SceneManager.LoadScene(next);
        }
    }

    private static void LastScene()
    {
        if (SceneManager.GetActiveScene().buildIndex != -1)
        {
            var next = SceneManager.GetActiveScene().buildIndex - 1;
            if (next < 0) next = SceneManager.sceneCountInBuildSettings - 1;
            SceneManager.LoadScene(next);
        }
    }

    private bool TryAbsorb()
    {
        var closetHit = ClosetSceneHit();
        if (closetHit == null) return false;
        var bubbleColor = closetHit.Value.collider.gameObject.GetComponent<BubbleColor>();
        var isTargetBubble = IsTargetBubbleHit(closetHit.Value);
        if (bubbleColor != null && !isTargetBubble)
        {
            PlayerColor = bubbleColor.color;
            var normal = (Position - closetHit.Value.point).normalized;
            Velocity = -normal * drillSpeed;
            PlayerState = State.Flood;
            InternalUpdate();
            return true;
        }

        return false;
    }

    private bool SameColor(Color a, Color b)
    {
        var va = new Vector4(a.r, a.g, a.b, a.a);
        var vb = new Vector4(b.r, b.g, b.b, b.a);
        return math.all(math.abs(va - vb) < 0.01f);
    }

    private bool IsBubbleColorHit(RaycastHit2D hit, BubbleColor floodingBubbleColor)
    {
        var bubbleColor = GetBubbleColor(hit);
        if (bubbleColor == null) return false;
        return SameColor(bubbleColor.color, floodingBubbleColor.color);
    }

    private BubbleColor GetOverlappingBubbleColor()
    {
        var colliders = new List<Collider2D>();
        Rigidbody.Overlap(colliders);
        foreach (var collider in colliders)
        {
            var bubbleColor = collider.GetComponent<BubbleColor>();
            if (bubbleColor != null) return bubbleColor;
        }

        return null;
    }

    private bool IsOverlappingBubble()
    {
        return GetOverlappingBubbleColor() != null;
    }

    private void DecVelocity(float dec)
    {
        var expectDecVelocity = dec * Time.fixedDeltaTime;
        Velocity = Velocity.normalized * math.max(Velocity.magnitude - expectDecVelocity, 0f);
    }

    private void MoveByVelocity()
    {
        Position += Velocity * Time.fixedDeltaTime;
    }

    private RaycastHit2D? ClosetHit(List<RaycastHit2D> hits)
    {
        if (hits.Count == 0) return null;
        var closetHit = hits[0];
        for (var i = 1; i < hits.Count; i++)
            if (hits[i].distance < closetHit.distance)
                closetHit = hits[i];
        return closetHit;
    }

    private RaycastHit2D? ClosetTranslationHit(Vector2 translation)
    {
        var hits = new List<RaycastHit2D>();
        Rigidbody.Cast(translation.normalized, hits, translation.magnitude + hitEpsilon);
        hits.RemoveAll(hit => hit.rigidbody == null);
        return ClosetHit(hits);
    }

    private RaycastHit2D? ClosetTranslationHit(Vector2 translation, Func<RaycastHit2D, bool> filterFunc)
    {
        var hits = new List<RaycastHit2D>();
        Rigidbody.Cast(translation.normalized, hits, translation.magnitude + hitEpsilon);
        hits.RemoveAll(hit => hit.rigidbody == null || !filterFunc(hit));
        return ClosetHit(hits);
    }

    private RaycastHit2D? ClosetTranslationHit(Func<RaycastHit2D, bool> filterFunc)
    {
        var translation = Velocity * Time.fixedDeltaTime;
        return ClosetTranslationHit(translation, filterFunc);
    }

    private RaycastHit2D? ClosetTranslationHit()
    {
        var translation = Velocity * Time.fixedDeltaTime;
        return ClosetTranslationHit(translation);
    }


    private bool IsBubbleHit(RaycastHit2D hit)
    {
        return hit.collider.gameObject.GetComponent<BubbleColor>() != null;
    }

    private TargetBubble GetTargetBubble(RaycastHit2D hit)
    {
        var targetBubble = hit.collider.gameObject.GetComponent<TargetBubble>();
        return targetBubble;
    }

    private bool IsTargetBubbleHit(RaycastHit2D hit)
    {
        var targetBubble = hit.collider.gameObject.GetComponent<TargetBubble>();
        if (targetBubble == null) return false;
        return targetBubble.enabled;
    }

    private BubbleColor GetBubbleColor(RaycastHit2D hit)
    {
        return hit.collider.gameObject.GetComponent<BubbleColor>();
    }

    private bool IsNotBubbleHit(RaycastHit2D hit)
    {
        return hit.collider.gameObject.GetComponent<BubbleColor>() == null;
    }

    private Vector2? ClosetScenePoint()
    {
        Vector2? closestPoint = null;
        foreach (var rigidbody in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
        {
            if (rigidbody == Rigidbody) continue;

            if (closestPoint != null)
            {
                var currentDistance = Vector2.Distance(closestPoint.Value, Position);
                var newDistance = Vector2.Distance(rigidbody.ClosestPoint(Position), Position);
                if (newDistance > currentDistance) continue;
            }

            closestPoint = rigidbody.ClosestPoint(Position);
        }

        return closestPoint;
    }


    private RaycastHit2D? ClosetSceneHit()
    {
        var closetPoint = ClosetScenePoint();
        if (closetPoint == null) return new RaycastHit2D();
        var direction = closetPoint.Value - Position;
        var hits = new List<RaycastHit2D>();
        Rigidbody.Cast(direction.normalized, hits);
        hits.RemoveAll(hit => hit.rigidbody == null);
        return ClosetHit(hits);
    }
}