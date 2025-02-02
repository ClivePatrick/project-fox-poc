using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationController : MonoBehaviour
{
    private Animator m_Animator;
    private Grounded m_Grounded;
    private ChainClimber m_ChainClimber;
    private AnchorThrower m_Thrower;
    private AnchorHolder m_Holder;
    private Rigidbody2D m_RigidBody;
    private bool m_Moving;

    private InputAction m_Move;

    private Camera m_cam;

    // Start is called before the first frame update
    void Start()
    {
        m_Animator = GetComponentInChildren<Animator>();
        m_Grounded = GetComponent<Grounded>();
        m_RigidBody = GetComponent<Rigidbody2D>();
        m_ChainClimber = GetComponent<ChainClimber>();
        m_cam = Camera.main;
        m_Thrower = GetComponent<AnchorThrower>();
        m_Holder = GetComponent<AnchorHolder>();
        m_Thrower.Throw.AddListener(TriggerThrow);
        m_Thrower.WindUp.AddListener(TriggerWindUp);

        m_Move = GetComponent<PlayerInput>().actions["Move"];
    }

    private void Update()
    {
        var input = m_Move.ReadValue<float>();

        m_Animator.SetFloat("InputVelocity", input);
        m_Animator.SetBool("IsMoving", input != 0);

        if (input != 0)
        {
            m_Animator.SetFloat("Facing", input);
        }

        m_Animator.SetBool("Grounded", m_Grounded.OnGround);
        m_Animator.SetBool("Climbing", m_ChainClimber.Mounted);
        m_Animator.SetFloat("VerticalVelocity", m_RigidBody.velocity.y);
        m_Animator.SetFloat("HorizontalVelocity", m_RigidBody.velocity.x);
        m_Animator.SetFloat("AimDirection", m_Thrower.AimDirection);
        m_Animator.SetBool("Surfing", m_Holder.Surfing);
    }

    private void TriggerThrow()
    {
        m_Animator.SetTrigger("AnchorThrown");
    }

    private void TriggerWindUp()
    {
        m_Animator.SetTrigger("WindingUp");
    }

    void OnClimb(InputValue value)
    {
        m_Animator.SetBool("ClimbingMoving", value.Get<float>() != 0);
    }
}
