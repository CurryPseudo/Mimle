using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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
    private bool jumping;

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

    private Rigidbody2D Rigidbody => GetComponent<Rigidbody2D>();

    private void Start()
    {
        state = State.Float;
    }

    private void FixedUpdate()
    {
        InternalUpdate();
    }

    private void InternalUpdate()
    {
        switch (state)
        {
            case State.Float:
            {
                var closestPoint = ClosetScenePoint();

                if (closestPoint != null && !jumping)
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
                    state = State.Roll;
                }

                break;
            }
            case State.Roll:
            {
                if (Input.GetButton("Jump"))
                {
                    var closetPoint = ClosetScenePoint();
                    if (closetPoint == null) return;
                    var normal = (Position - closetPoint.Value).normalized;
                    Velocity = normal * jumpSpeed;


                    IEnumerator JumpEndCoroutine()
                    {
                        jumping = true;
                        yield return new WaitForSeconds(jumpTime);
                        jumping = false;
                    }

                    StartCoroutine(JumpEndCoroutine());

                    IEnumerator KeepJumpSpeedCoroutine()
                    {
                        yield return null;
                        Velocity = Velocity.normalized * jumpSpeed;
                        if (state != State.Float) yield break;
                        if (!jumping) yield break;
                    }

                    StartCoroutine(KeepJumpSpeedCoroutine());

                    state = State.Float;
                    InternalUpdate();
                    return;
                }

                if (Input.GetButton("Absorb"))
                {
                    var closetHit = ClosetSceneHit();
                    if (closetHit == null) return;
                    var bubbleColor = closetHit.Value.collider.gameObject.GetComponent<BubbleColor>();
                    var isTargetBubble = IsTargetBubbleHit(closetHit.Value);
                    if (bubbleColor != null && !isTargetBubble)
                    {
                        PlayerColor = bubbleColor.color;
                        var normal = (Position - closetHit.Value.point).normalized;
                        Velocity = -normal * drillSpeed;
                        state = State.Flood;
                        InternalUpdate();
                        return;
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
                    }
                    else
                    {
                        Velocity = Vector2.zero;
                    }
                }
                DecVelocity(rollDec);

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
                var drillingOut = false;
                {
                    // Check drilling out
                    var translation = Velocity.normalized * drillOutCheckDistance;
                    var blockHit = ClosetTranslationHit(translation, hit => !IsBubbleColorHit(hit, floodingBubbleColor));
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
                        state = State.Roll;
                        return;
                    }
                }

                {
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

    private bool IsBubbleColorHit(RaycastHit2D hit, BubbleColor floodingBubbleColor)
    {
        if (IsTargetBubbleHit(hit)) return false;
        var bubbleColor = GetBubbleColor(hit);
        if (bubbleColor == null) return true;
        return bubbleColor.color == floodingBubbleColor.color;
    }

    private bool IsOverlappingBubble()
    {
        var colliders = new List<Collider2D>();
        Rigidbody.Overlap(colliders);
        var targetPositionOverlapBubble = false;
        foreach (var collider in colliders)
            if (collider.GetComponent<BubbleColor>() != null)
            {
                targetPositionOverlapBubble = true;
                break;
            }

        return targetPositionOverlapBubble;
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
        return ClosetHit(hits);
    }

    private RaycastHit2D? ClosetTranslationHit(Vector2 translation, Func<RaycastHit2D, bool> filterFunc)
    {
        var hits = new List<RaycastHit2D>();
        Rigidbody.Cast(translation.normalized, hits, translation.magnitude + hitEpsilon);
        hits.RemoveAll(hit => !filterFunc(hit));
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
        return ClosetHit(hits);
    }
}