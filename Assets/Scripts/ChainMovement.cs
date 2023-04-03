using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChainMovement : MonoBehaviour
{
	public float ClimbSpeed = 4.5f;
	public float MountRadius = 1;
	public float MountedLinkDistanceLimit = 1;
	public float DismountImpulse = 60;
	public float DismountMaxAngle = 60;

	public bool Mounted => m_Attachment != null;
	public Vector2 MountedLinkAnchor => new Vector2(0, Mathf.Repeat(m_MountDistance, m_Chain.LinkAnchorDistance) - m_Chain.LinkAnchorOffset);
	public int MountedLinkIndex => Mathf.FloorToInt(m_MountDistance / m_Chain.LinkAnchorDistance);
	public Rigidbody2D MountedLinkBody => m_Chain.Links[MountedLinkIndex];
	public Vector2 MountedLinkAnchorWorldPosition => MountedLinkBody.transform.TransformPoint(MountedLinkAnchor);
	public Vector2 ClimbDirection => -MountedLinkBody.transform.up * m_ClimbDirection;

	private Chain m_Chain;
	private float m_MountDistance;
	private TargetJoint2D m_Attachment;
	private Rigidbody2D m_RigidBody;
	private float m_DismountDirection;
	private List<Collider2D> m_MountableLinks = new List<Collider2D>();
	private Grounded m_Grounded;
	private float m_ClimbDirection;

	public void Mount()
	{
		if (m_Grounded.OnGround)
		{
			return;
		}

		if (Mounted)
		{
			return;
		}

		var furthestLink = GetFurthestMountableLink();

		if (furthestLink == null)
		{
			return;
		}

		m_Chain = furthestLink.Chain;
		m_MountDistance = m_Chain.LinkAnchorDistance * (furthestLink.LinkIndex + 0.5f);

		m_Attachment = gameObject.AddComponent<TargetJoint2D>();
		m_Attachment.target = MountedLinkAnchorWorldPosition;
	}

	public void Dismount()
	{
		if (!Mounted)
		{
			return;
		}

		Destroy(m_Attachment);
		m_Attachment = null;

		if (!m_Grounded.OnGround)
		{
			var dismountDirection = Quaternion.Euler(0, 0, DismountMaxAngle * -m_DismountDirection) * Vector2.up;
			m_RigidBody.AddForce(dismountDirection * DismountImpulse, ForceMode2D.Impulse);
		}
	}

	public void Climb(float direction)
	{
		m_ClimbDirection = direction;
	}

	private void OnMove(InputValue value)
	{
		m_DismountDirection = value.Get<float>();
	}

	private void OnJump()
	{
		Dismount();
	}

	private void OnMount()
	{
		Mount();
	}

	private void OnClimb(InputValue value)
	{
		var direction = value.Get<float>();
		Climb(direction);
	}

	private void FixedUpdate()
	{
		if (!Mounted)
		{
			return;
		}

		if (m_Grounded.OnGround)
		{
			Dismount();
			return;
		}

		var playerToMountedLink = MountedLinkAnchorWorldPosition - (Vector2)transform.position;
		var climbingTowardsPlayer = Vector2.Dot(playerToMountedLink.normalized, ClimbDirection) < 0;
		
		if (playerToMountedLink.magnitude < MountedLinkDistanceLimit || climbingTowardsPlayer)
		{
			m_MountDistance -= m_ClimbDirection * ClimbSpeed * Time.fixedDeltaTime;
			m_MountDistance = Mathf.Clamp(m_MountDistance, 0, m_Chain.Length - 0.001f);
		}

		m_Attachment.target = MountedLinkAnchorWorldPosition;
	}

	private IEnumerable<MountableLink> GetFurthestMountableLinks()
	{
		var linkFilter = new ContactFilter2D();
		linkFilter.layerMask = LayerMask.GetMask("Chain");

		var linksCount = Physics2D.OverlapCircle(transform.position, MountRadius, linkFilter, m_MountableLinks);

		// Group links by their chain and return the chains with their furthest link index
		return m_MountableLinks.Take(linksCount)
			.Select(x => new MountableLink { Chain = x.GetComponentInParent<Chain>(), LinkIndex = x.transform.GetSiblingIndex() })
			.Where(x => x.Chain != null)
			.GroupBy(x => x.Chain)
			.Select(x => new MountableLink { Chain = x.Key, LinkIndex = x.Min(y => y.LinkIndex) });
	}

	private MountableLink GetFurthestMountableLink()
	{
		var chains = GetFurthestMountableLinks();

		var furthestChain = chains
			.OrderByDescending(x => x.Chain.MountPriority)
			.FirstOrDefault();

		return furthestChain;
	}

	private void Awake()
	{
		m_RigidBody = GetComponent<Rigidbody2D>();
		m_Grounded = GetComponent<Grounded>();
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		if (Mounted)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(MountedLinkAnchorWorldPosition, 0.15f);

			var climbDirection = new Ray(transform.position, ClimbDirection);
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(climbDirection);

			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, MountedLinkDistanceLimit);
		}
		else
		{
			Gizmos.color = Color.white;
			Gizmos.DrawWireSphere(transform.position, MountRadius);

			var furthestLink = GetFurthestMountableLink();

			if (furthestLink != null)
			{
				var (chain, linkIndex) = furthestLink;
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(chain.Links[linkIndex].transform.position, 0.15f);
			}
		}
	}
}
