using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    private bool m_PlayerInRange;
    private bool m_Togglable;
    [SerializeField]
    private GameObject m_ConnectedObject;
    [SerializeField]
    private Sprite m_UntoggledSprite;
    [SerializeField]
    private Sprite m_ToggledSprite;
    private SpriteRenderer m_Renderer;

    [SerializeField]
    private AudioClip m_ToggleOn;
    [SerializeField]
    private AudioClip m_ToggleOff;

    private void Start()
    {
        WorldInteract.Activated += Toggle;
        m_Renderer = GetComponent<SpriteRenderer>();
        m_Renderer.sprite = m_UntoggledSprite;
        m_Togglable = true;
        m_ConnectedObject.GetComponent<IToggle>().DisableSelfToggle();
    }

    private void Toggle()
    {
        if (!m_PlayerInRange) return;
        if (!m_Togglable) return;

        var toggle = m_ConnectedObject.GetComponent<IToggle>();
        m_Togglable = false;

        AudioController.PlaySound(m_ToggleOn, 1, 1, MixerGroup.SFX);

        StartCoroutine(ResetState(toggle.GetResetTime()));
        toggle.Toggle();
        m_Renderer.sprite = m_ToggledSprite;
    }

    public IEnumerator ResetState(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_Renderer.sprite = m_UntoggledSprite;
        m_Togglable = true;

        AudioController.PlaySound(m_ToggleOff, 1, 1, MixerGroup.SFX);
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            m_PlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            m_PlayerInRange = false;
        }
    }

    private void OnDisable()
    {
        WorldInteract.Activated -= Toggle;

    }
}
