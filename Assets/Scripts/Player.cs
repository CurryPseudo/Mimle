using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum State
    {
        Float,
        Roll
    }

    public float floatMaxSpeed = 3.0f;
    public float attachAcc = 1f;
    public float rollMaxSpeed = 2.0f;
    public float rollAcc = 2f;
    public float rollDec = 1f;
    public float hitEpsilon = 0.01f;

    public State state = State.Float;

    public Vector2 velocity = Vector2.zero;

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

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.Float:
            {
                var closestPoint = ClosetScenePoint();

                if (closestPoint != null)
                {
                    var direction = closestPoint.Value - Position;
                    Velocity += direction.normalized * (attachAcc * Time.fixedDeltaTime);
                }

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
                {
                    var closetPoint = ClosetScenePoint();
                    if (closetPoint == null) return;
                    var horizontalInput = Input.GetAxisRaw("Horizontal");
                    var verticalInput = Input.GetAxisRaw("Vertical");
                    var input = new Vector2(horizontalInput, verticalInput);
                    var expectAccVelocity = input.normalized * (rollAcc * Time.fixedDeltaTime);
                    {
                        var direction = (closetPoint.Value - Position).normalized;
                        var directionNormal = new Vector2(direction.y, -direction.x);
                        expectAccVelocity = math.dot(expectAccVelocity, directionNormal) * directionNormal;
                    }
                    Velocity += expectAccVelocity;
                }
                var expectDecVelocity = rollDec * Time.fixedDeltaTime;
                expectDecVelocity = math.min(Velocity.magnitude, expectDecVelocity);
                Velocity -= Velocity.normalized * expectDecVelocity;
                Velocity = Vector2.ClampMagnitude(Velocity, rollMaxSpeed);

                var closetHit = ClosetTranslationHit();
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
                }
                else
                {
                    var maxDistance = math.max(closetHit.Value.distance - hitEpsilon, 0f);
                    Position += maxDistance * Velocity.normalized;
                    Velocity = Vector2.zero;
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
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

    private RaycastHit2D? ClosetTranslationHit()
    {
        var hits = new List<RaycastHit2D>();
        var translation = Velocity * Time.fixedDeltaTime;
        Rigidbody.Cast(translation.normalized, hits, translation.magnitude + hitEpsilon);
        return ClosetHit(hits);
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