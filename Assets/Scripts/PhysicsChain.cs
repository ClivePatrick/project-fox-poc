using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhysicsChain : MonoBehaviour
{
	public float Length;
	public Vector2 LinkSize = new Vector2(0.3f, 0.9f);
	public GameObject Link;
	public Rigidbody2D Anchor;
	public Rigidbody2D Player;
	//public float TargetJointFrequency = 15;
	//public float TargetJointDampingRatio = 1;

	private const float k_MinContactImpulse = 2;
	private const float k_MinContactDuration = 0.1f;
	//private const float k_MaxContactVelocity = 0.5f;
	//private const float k_MinReactionForce = 300;

	public float LinkAnchorDistance => LinkSize.y - LinkSize.x;
	public float LinkAnchorOffset => LinkAnchorDistance / 2;

	public int Links => m_Links.Length;
	public bool IsPendulumPoint(int index) => m_ContactDuration[Mathf.Clamp(index, 0, m_Links.Length - 1)] > k_MinContactDuration;

	private float[] m_ContactDuration;
	private Rigidbody2D[] m_Links;
	//private TargetJoint2D m_AnchorTargetJoint;
	//private TargetJoint2D m_PlayerTargetJoint;


	public float AveragedChainSpeed
	{
		get
		{
			var velocity = 0f;
			foreach (Rigidbody2D rb in m_Links)
			{
				velocity += rb.velocity.magnitude;
			}
			return velocity / m_Links.Length;
		}
	}

	public Rigidbody2D GetLink(int index)
	{
		return m_Links[Mathf.Clamp(index, 0, m_Links.Length - 1)];
	}

	private void Start()
	{
		CreateCompactChain();
		Recall.activate += ResetChain;

		//m_AnchorTargetJoint = m_Links.First().gameObject.AddComponent<TargetJoint2D>();
		//m_AnchorTargetJoint.anchor = Vector2.zero;
		//m_AnchorTargetJoint.autoConfigureTarget = false;
		//m_AnchorTargetJoint.target = Anchor.position;
		//m_AnchorTargetJoint.frequency = TargetJointFrequency;
		//m_AnchorTargetJoint.dampingRatio = TargetJointDampingRatio;

		//m_PlayerTargetJoint = m_Links.Last().gameObject.AddComponent<TargetJoint2D>();
		//m_PlayerTargetJoint.anchor = Vector2.zero;
		//m_PlayerTargetJoint.autoConfigureTarget = false;
		//m_PlayerTargetJoint.target = Player.position;
		//m_PlayerTargetJoint.frequency = TargetJointFrequency;
		//m_PlayerTargetJoint.dampingRatio = TargetJointDampingRatio;
	}

	private void FixedUpdate()
	{
		//m_AnchorTargetJoint.target = Anchor.position;
		//m_PlayerTargetJoint.target = Player.position;

		m_Links.First().MovePosition(Anchor.position);
		m_Links.Last().MovePosition(Player.position);

		UpdatePendulumPoints();
	}

	private void UpdatePendulumPoints()
	{
		var contacts = new ContactPoint2D[16];

		for (int i = 0; i < m_Links.Length; i++)
		{
			var link = m_Links[i];

			//var hingeJoint = link.GetComponent<HingeJoint2D>();

			//if (hingeJoint == null)
			//{
			//	continue;
			//}

			//var reactionForce = hingeJoint.reactionForce.magnitude;

			var spriteRenderer = link.GetComponent<SpriteRenderer>();

			var contactFilter = new ContactFilter2D();
			contactFilter.SetLayerMask(LayerMask.GetMask("Terrain"));

			var contactsLength = link.GetContacts(contactFilter, contacts);

			var isPendulumPoint = false;

			for (int j = 0; j < contactsLength; j++)
			{
				var contact = contacts[j];

				var impulse = Mathf.Sqrt(contact.normalImpulse * contact.normalImpulse + contact.tangentImpulse * contact.tangentImpulse);

				//var velocity = contact.relativeVelocity.magnitude;

				//if (impulse > k_MinContactImpulse && reactionForce > k_MinReactionForce)
				//if (impulse > k_MinContactImpulse && velocity < k_MaxContactVelocity)
				if (impulse > k_MinContactImpulse)
				{
					isPendulumPoint = true;

					//Debug.Log(hingeJoint.reactionForce.magnitude);

					//Debug.DrawLine(contact.point, contact.point + contact.normal * contact.normalImpulse, Color.red);
					//Debug.DrawLine(contact.point, contact.point + contact.normal.Perpendicular1() * contact.tangentImpulse, Color.blue);

					break;
				}
			}

			if (isPendulumPoint)
			{
				m_ContactDuration[i] += Time.fixedDeltaTime;
			}
			else
			{
				m_ContactDuration[i] = 0;
			}

			//spriteRenderer.color = isPendulumPoint ? Color.red : Color.white;
			spriteRenderer.color = m_ContactDuration[i] > k_MinContactDuration ? Color.red : Color.white;
		}
	}

	private void CreateCompactChain()
	{
		var chainDirection = (Player.position - Anchor.position).normalized;
		var links = Mathf.CeilToInt(Length / LinkAnchorDistance);
		var linksBetween = Mathf.CeilToInt(Vector2.Distance(Anchor.position, Player.position) / LinkAnchorDistance);

		m_Links = new Rigidbody2D[links];
		m_ContactDuration = new float[links];

		var position = Anchor.position;

		for (var i = 0; i < links; i++)
		{
			var direction = i / linksBetween % 2 == 0 ? chainDirection : -chainDirection;

			var link = CreateLink();

			link.name = $"Link{i}";
			link.transform.parent = transform;
			link.transform.rotation = Quaternion.FromToRotation(Vector2.up, direction);
			link.transform.position = position;

			position += direction * LinkAnchorDistance;

			m_Links[i] = link;

			link.GetComponent<Link>().index = i;
		}

		for (var i = 1; i < m_Links.Length; i++)
		{
			ConnectLink(m_Links[i], m_Links[i - 1]);
		}
	}

	private void CreateChain()
	{
		var direction = (Player.position - Anchor.position).normalized;
		var rotation = Quaternion.FromToRotation(Vector2.up, direction);
		var links = Mathf.CeilToInt(Length / LinkAnchorDistance);

		m_Links = new Rigidbody2D[links];

		for (var i = 0; i < m_Links.Length; i++)
		{
			var link = CreateLink();

			link.name = $"Link{i}";
			link.transform.parent = transform;
			link.transform.rotation = rotation;
			link.transform.position = Anchor.position + direction * i * LinkAnchorDistance;

			m_Links[i] = link;
		}

		for (var i = 1; i < m_Links.Length; i++)
		{
			ConnectLink(m_Links[i], m_Links[i - 1]);
		}
	}

	private Rigidbody2D CreateLink()
	{
		return Instantiate(Link).GetComponent<Rigidbody2D>();
	}

	private void ConnectLink(Rigidbody2D link, Rigidbody2D previousLink)
	{
		var hingeJoint = link.gameObject.AddComponent<HingeJoint2D>();
		hingeJoint.connectedBody = previousLink;
		hingeJoint.autoConfigureConnectedAnchor = false;
		hingeJoint.connectedAnchor = new Vector2(0, LinkAnchorOffset);
		hingeJoint.anchor = new Vector2(0, -LinkAnchorOffset);
	}

	public void ResetChain()
	{
		foreach (Rigidbody2D rb in m_Links)
		{
			rb.transform.position = Player.position;
		}
	}

	public void OnDisable()
	{
		Recall.activate -= ResetChain;
	}
}
