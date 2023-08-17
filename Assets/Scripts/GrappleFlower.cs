using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleFlower : MonoBehaviour
{
    private Collider2D m_Collider;
    private Anchor m_Anchor;

    private void Start()
    {
        m_Collider = GetComponent<Collider2D>();
    }

    public void EnableCollider()
    {
        m_Collider.enabled = true;
    }

    public void DisableCollider()
    {
        m_Collider.enabled = false;
        if (m_Anchor != null)
        {
            m_Anchor.FreeForDuration(0.1f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        m_Anchor = collision.gameObject.GetComponent<Anchor>();
    }
}
