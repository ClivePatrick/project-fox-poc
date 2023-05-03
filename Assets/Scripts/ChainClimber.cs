using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ChainClimber : MonoBehaviour
{
	public float MaxMountAngle = 20;
	public float MaxClimbSpeed = 4.5f;
	public float ClimbSpeedDamping = 0.1f;

	public Vector2 LinkAnchor => new Vector2(0, Mathf.Repeat(m_Chain.Length, m_PhysicsChain.LinkAnchorDistance) - m_PhysicsChain.LinkAnchorOffset);
	public int LinkIndex => Mathf.FloorToInt(m_Chain.Length / m_PhysicsChain.LinkAnchorDistance);
	public Rigidbody2D Link => m_PhysicsChain.GetLink(LinkIndex);
	public bool Mounted => m_PendulumDistanceJoint != null;
	public bool CanMount => m_Chain.PlayerTension > 0 || Vector2.Angle(Vector2.up, m_Chain.PlayerToPendulum) < MaxMountAngle;

	private Grounded m_Grounded;
	private IdealChain m_Chain;
	private PhysicsChain m_PhysicsChain;
	private float m_ClimbInput;
	private float m_ClimbSpeed;
	private float m_ClimbAcceleration;
	private float m_MountDistance;
	private DistanceJoint2D m_PendulumDistanceJoint;
	private TargetJoint2D m_LinkTargetJoint;
	private Rigidbody2D m_OldLink;

	public void Mount()
	{
		if (Mounted)
		{
			return;
		}

		if (!CanMount)
		{
			return;
		}

		m_PendulumDistanceJoint = gameObject.AddComponent<DistanceJoint2D>();
		m_PendulumDistanceJoint.autoConfigureConnectedAnchor = false;
		m_PendulumDistanceJoint.autoConfigureDistance = false;
		m_PendulumDistanceJoint.anchor = Vector2.zero;
		m_PendulumDistanceJoint.connectedAnchor = Vector2.zero;
		m_PendulumDistanceJoint.connectedBody = m_Chain.PlayerPendulum;
		m_PendulumDistanceJoint.distance = Vector2.Distance(transform.position, m_Chain.PlayerPendulum.position);

		m_MountDistance = m_Chain.Length;

		CreateLinkTargetJoint();
	}

	public void Dismount()
	{
		if (!Mounted)
		{
			return;
		}

		Destroy(m_PendulumDistanceJoint);
		m_PendulumDistanceJoint = null;

		Destroy(m_LinkTargetJoint);
		m_LinkTargetJoint = null;
	}

	public void Climb(float direction)
	{
		m_ClimbInput = direction;
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

	private void Awake()
	{
		m_Grounded = GetComponent<Grounded>();

		m_Chain = FindObjectOfType<IdealChain>();
		m_PhysicsChain = FindObjectOfType<PhysicsChain>();
	}

	private void UpdateDistanceJoint()
	{
		if (!Mounted)
		{
			return;
		}

		m_PendulumDistanceJoint.distance = m_MountDistance - (m_Chain.Length - Vector2.Distance(transform.position, m_Chain.PlayerPendulumPoint));
		m_PendulumDistanceJoint.connectedBody = m_Chain.PlayerPendulum;
	}

	private void CreateLinkTargetJoint()
	{
		m_OldLink = Link;

		m_LinkTargetJoint = Link.AddComponent<TargetJoint2D>();
		m_LinkTargetJoint.autoConfigureTarget = false;
		m_LinkTargetJoint.target = transform.position;
		m_LinkTargetJoint.anchor = LinkAnchor;
		m_LinkTargetJoint.frequency = 15;
		m_LinkTargetJoint.dampingRatio = 1;
	}

	private void FixedUpdate()
	{
		if (!Mounted)
		{
			return;
		}

		if (m_Grounded.OnGround || m_Chain.Anchor.bodyType == RigidbodyType2D.Dynamic)
		{
			Dismount();
			return;
		}

		m_MountDistance -= m_ClimbSpeed * Time.fixedDeltaTime;
		m_MountDistance = Mathf.Clamp(m_MountDistance, 0, m_Chain.MaxLength);

		UpdateDistanceJoint();

		m_LinkTargetJoint.target = transform.position;
		m_LinkTargetJoint.anchor = LinkAnchor;

		if (Link != m_OldLink)
		{
			Destroy(m_LinkTargetJoint);
			CreateLinkTargetJoint();
		}

		m_OldLink = Link;
	}

	private void Update()
	{
		m_ClimbSpeed = Mathf.SmoothDamp(m_ClimbSpeed, m_ClimbInput * MaxClimbSpeed, ref m_ClimbAcceleration, ClimbSpeedDamping);
	}

	private void OnDrawGizmosSelected()
	{
		if (!Mounted)
		{
			return;
		}

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(Link.transform.TransformPoint(LinkAnchor), 0.2f);
	}
}
