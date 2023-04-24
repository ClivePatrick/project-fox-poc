using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class IdealChain : MonoBehaviour
{
	public UnityEvent<Vector2> CornerAdded;
	public UnityEvent<Vector2> CornerRemoved;
	public LayerMask Collision;
	public float Width = 0.15f;
	public float MaxLength = 15;
	public float MaxTensionForce = 1000;
	public Rigidbody2D Anchor;
	public Rigidbody2D Player;

	public bool HasPendulumPoints => m_Points.Count > 0;
	public Vector2 AnchorPendulumPoint => HasPendulumPoints ? m_Points.First().Position : Player.position;
	public Vector2 PlayerPendulumPoint => HasPendulumPoints ? m_Points.Last().Position : Anchor.position;
	public Rigidbody2D AnchorPendulum => HasPendulumPoints ? m_AnchorDistanceJoint.attachedRigidbody : Player;
	public Rigidbody2D PlayerPendulum => HasPendulumPoints ? m_PlayerDistanceJoint.attachedRigidbody : Anchor;
	public Vector2 AnchorToPendulum => (AnchorPendulumPoint - Anchor.position).normalized;
	public Vector2 PlayerToPendulum => (PlayerPendulumPoint - Player.position).normalized;
	public float AnchorTension => m_AnchorDistanceJoint.reactionForce.magnitude;
	public float PlayerTension => m_PlayerDistanceJoint.reactionForce.magnitude;
	public float Length => GetLength();

	private const int k_SweepSteps = 16;
	private const float k_MinCornerDistance = 0.001f;

	private LineRenderer m_LineRenderer;
	private List<ChainPoint> m_Points = new List<ChainPoint>();
	private Vector2 m_PreviousAnchorPosition;
	private Vector2 m_PreviousPlayerPosition;
	private DistanceJoint2D m_AnchorDistanceJoint;
	private DistanceJoint2D m_PlayerDistanceJoint;
	private DistanceJoint2D m_MaxDistanceJoint;

	//public void GetPendulumPoint(float distance, out Vector2 point, out Vector2 pendulum)
	//{
	//	if (!HasPendulumPoints)
	//	{
	//		distance = Mathf.Clamp(distance, 0, Vector2.Distance(Anchor.position, Player.position));
	//		point = (Player.position - Anchor.position).normalized * distance;
	//		pendulum = Anchor.position;

	//		return;
	//	}

	//	if (distance < Vector2.Distance(Anchor.position, AnchorPendulumPoint))
	//	{
	//		point = (AnchorPendulumPoint - Anchor.position).normalized * distance;
	//		pendulum = Anchor.position;

	//		return;
	//	}

	//	for (var i = 0; i < m_Corners.Count - 1; i++)
	//	{
	//		var corner = m_Corners[i];
	//		var nextCorner = m_Corners[i + 1];

	//		var sideLength = Vector2.Distance(corner.Position, nextCorner.Position);

	//		if (distance < sideLength)
	//		{
	//			point = Vector2.Lerp(corner.Position, nextCorner.Position, distance / sideLength);
	//			pendulum = corner.Position;

	//			return;
	//		}

	//		distance -= sideLength;
	//	}

	//	point = Player.position;
	//	pendulum = PlayerPendulumPoint;
	//}

	private void Awake()
	{
		m_LineRenderer = GetComponent<LineRenderer>();
	}

	private void Start()
	{
		m_LineRenderer.startWidth = Width;
		m_LineRenderer.endWidth = Width;

		m_PreviousAnchorPosition = Anchor.position;
		m_PreviousPlayerPosition = Player.position;

		m_AnchorDistanceJoint = CreatePendulum(Anchor);
		m_PlayerDistanceJoint = CreatePendulum(Player);

		m_MaxDistanceJoint = Player.gameObject.AddComponent<DistanceJoint2D>();
		m_MaxDistanceJoint.autoConfigureConnectedAnchor = false;
		m_MaxDistanceJoint.autoConfigureDistance = false;
		m_MaxDistanceJoint.maxDistanceOnly = true;
		m_MaxDistanceJoint.anchor = Vector2.zero;
		m_MaxDistanceJoint.connectedAnchor = Vector2.zero;
		m_MaxDistanceJoint.connectedBody = Anchor;
		m_MaxDistanceJoint.distance = MaxLength;
	}

	private void FixedUpdate()
	{
		UpdatePoints();
		UpdateDistanceJoints();
		ApplyTensionForces();
		UpdateLineRenderer();

		m_PreviousAnchorPosition = Anchor.position;
		m_PreviousPlayerPosition = Player.position;
	}

	private bool SweepCorner(Vector2 origin, Vector2 from, Vector2 to, out ChainPoint corner)
	{
		corner = new ChainPoint();

		if (Physics2D.OverlapPoint(from, Collision) != null)
		{
			return false;
		}

		var hit = LineCastSweep(origin, from, to);

		if (!hit)
		{
			return false;
		}

		var colliderCorner = ColliderCorners.GetCorner(hit.collider, hit.point);

		corner.Offset = (Vector2)hit.transform.InverseTransformPoint(colliderCorner.Position + colliderCorner.Normal * Width / 2);
		corner.Normal = colliderCorner.Normal;
		corner.Collider = hit.collider;
		corner.Rigidbody = hit.rigidbody;

		if (Vector2.Distance(corner.Position, origin) < k_MinCornerDistance)
		{
			return false;
		}

		return true;
	}

	private bool CanRemoveCorner(ChainPoint corner, Vector2 previousPosition, Vector2 nextPosition)
	{
		var previousEdgeDirection = (corner.Position - previousPosition).normalized;
		var previousEdgeNormal = Vector2.Perpendicular(previousEdgeDirection);

		var clockwiseCorner = Vector2.Dot(previousEdgeNormal, corner.Normal) < 0;

		if (clockwiseCorner)
		{
			previousEdgeNormal *= -1;
		}

		var directionToNext = (nextPosition - corner.Position).normalized;

		return Vector2.Dot(previousEdgeNormal, directionToNext) > 0;
	}

	private void UpdatePoints()
	{
		if (SweepCorner(HasPendulumPoints ? PlayerPendulumPoint : Anchor.position, Player.position, m_PreviousPlayerPosition, out var playerCorner))
		{
			CornerAdded?.Invoke(playerCorner.Position);
			m_Points.Add(playerCorner);
		}

		if (SweepCorner(HasPendulumPoints ? AnchorPendulumPoint : Player.position, Anchor.position, m_PreviousAnchorPosition, out var anchorCorner))
		{
			CornerAdded?.Invoke(anchorCorner.Position);
			m_Points.Insert(0, anchorCorner);
		}

		if (HasPendulumPoints && CanRemoveCorner(m_Points.Last(), m_Points.Count > 1 ? m_Points[m_Points.Count - 2].Position : Anchor.position, Player.position))
		{
			CornerRemoved?.Invoke(m_Points.Last().Position);
			m_Points.RemoveAt(m_Points.Count - 1);
		}

		if (HasPendulumPoints && CanRemoveCorner(m_Points.First(), m_Points.Count > 1 ? m_Points[1].Position : Player.position, Anchor.position))
		{
			CornerRemoved?.Invoke(m_Points.First().Position);
			m_Points.RemoveAt(0);
		}

		//foreach (var point in m_Corners)
		//{
		//	point.OldPosition = point.Position;
		//}
	}

	private void UpdateDistanceJoints()
	{
		m_MaxDistanceJoint.distance = MaxLength;

		m_PlayerDistanceJoint.enabled = HasPendulumPoints;
		m_AnchorDistanceJoint.enabled = HasPendulumPoints;

		if (!HasPendulumPoints)
		{
			return;
		}

		m_PlayerDistanceJoint.transform.position = PlayerPendulumPoint;
		m_PlayerDistanceJoint.distance = Mathf.Max(0, MaxLength - (Length - Vector2.Distance(Player.position, PlayerPendulumPoint)));

		m_AnchorDistanceJoint.transform.position = AnchorPendulumPoint;
		m_AnchorDistanceJoint.distance = Mathf.Max(0, MaxLength - (Length - Vector2.Distance(Anchor.position, AnchorPendulumPoint)));
	}

	private DistanceJoint2D CreatePendulum(Rigidbody2D connectedBody)
	{
		var pendulum = new GameObject($"{connectedBody.name}Pendulum", typeof(Rigidbody2D), typeof(DistanceJoint2D));
		pendulum.transform.parent = transform;
		pendulum.transform.position = Vector2.zero;

		var rigidBody = pendulum.GetComponent<Rigidbody2D>();
		rigidBody.bodyType = RigidbodyType2D.Static;

		var distanceJoint = pendulum.GetComponent<DistanceJoint2D>();
		distanceJoint.autoConfigureConnectedAnchor = false;
		distanceJoint.autoConfigureDistance = false;
		distanceJoint.maxDistanceOnly = true;
		distanceJoint.anchor = Vector2.zero;
		distanceJoint.connectedAnchor = Vector2.zero;
		distanceJoint.connectedBody = connectedBody;
		distanceJoint.distance = MaxLength;
		distanceJoint.enabled = false;

		return distanceJoint;
	}

	private void ApplyTensionForces()
	{
		if (!HasPendulumPoints)
		{
			return;
		}

		if (Anchor.bodyType != RigidbodyType2D.Dynamic)
		{
			return;
		}

		var forceOnAnchor = Mathf.Min(MaxTensionForce, m_PlayerDistanceJoint.reactionForce.magnitude);
		Anchor.AddForce((AnchorPendulumPoint - Anchor.position).normalized * forceOnAnchor);

		var forceOnPlayer = Mathf.Min(MaxTensionForce, m_AnchorDistanceJoint.reactionForce.magnitude);
		Player.AddForce((PlayerPendulumPoint - Player.position).normalized * forceOnPlayer);
	}

	private void UpdateLineRenderer()
	{
		if (m_LineRenderer == null)
		{
			return;
		}

		m_LineRenderer.positionCount = m_Points.Count + 2;
		m_LineRenderer.SetPosition(0, Anchor.position);

		for (var i = 0; i < m_Points.Count; i++)
		{
			m_LineRenderer.SetPosition(i + 1, m_Points[i].Position);
		}

		m_LineRenderer.SetPosition(m_Points.Count + 1, Player.position);
	}

	private RaycastHit2D LineCastSweep(Vector2 origin, Vector2 from, Vector2 to)
	{
		for (var i = 0; i < k_SweepSteps; i++)
		{
			var end = Vector2.Lerp(to, from, (i + 1) / (float)k_SweepSteps);
			var hit = Physics2D.Linecast(origin, end, Collision);

			if (hit)
			{
				return hit;
			}
		}

		return new RaycastHit2D();
	}

	private float GetLength()
	{
		if (!HasPendulumPoints)
		{
			return Vector2.Distance(Anchor.position, Player.position);
		}

		return GetSegmentLength() + Vector2.Distance(Anchor.position, AnchorPendulumPoint) +
			Vector2.Distance(Player.position, PlayerPendulumPoint);
	}

	private float GetSegmentLength()
	{
		if (!HasPendulumPoints)
		{
			return 0;
		}

		var length = 0.0f;

		for (var i = 0; i < m_Points.Count - 1; i++)
		{
			var corner = m_Points[i];
			var nextCorner = m_Points[i + 1];

			length += Vector2.Distance(corner.Position, nextCorner.Position);
		}

		return length;
	}

	private void OnDrawGizmosSelected()
	{
		//var distance = Mathf.Repeat(Time.time, Length);

		//GetPendulumPoint(distance, out var point, out var pendulum);

		//Gizmos.color = Color.red;
		//Gizmos.DrawWireSphere(point, Width / 2);

		//Gizmos.color = Color.green;
		//Gizmos.DrawWireSphere(pendulum, Width / 2);

		for (int i = 0; i < m_Points.Count; i++)
		{
			var corner = m_Points[i];

			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(corner.Position, Width / 2);

			Gizmos.color = Color.red;
			Gizmos.DrawRay(corner.Position, corner.Normal);

			if (i < m_Points.Count - 1)
			{
				var nextCorner = m_Points[i + 1];

				Gizmos.color = Color.cyan;
				Gizmos.DrawLine(corner.Position, nextCorner.Position);
			}
		}
	}
}
