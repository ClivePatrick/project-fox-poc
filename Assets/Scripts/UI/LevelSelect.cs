using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;


public class LevelSelect : MonoBehaviour
{
    private string m_Level0 = "Level 0";
    private string m_Level1 = "Level 1";
    private string m_Level2 = "Level 2";
    private string m_Level3 = "Level 3";
    private VisualElement m_LevelSelectRoot;
    private VisualElement m_PauseRoot;

    private void OnEnable()
    {
        m_LevelSelectRoot = GetComponent<UIDocument>().rootVisualElement;
        GetComponent<UIDocument>().enabled = true;

        GameObject m_PauseObject = GameObject.Find("PauseMenu");
        if (m_PauseObject != null)
        {
        m_LevelSelectRoot.style.display = DisplayStyle.None;
		    m_PauseRoot = m_PauseObject.GetComponent<UIDocument>().rootVisualElement;
        }

        //Get buttons
        Button m_Level0Button = m_LevelSelectRoot.Q<Button>("Level0");
        Button m_Level1Button = m_LevelSelectRoot.Q<Button>("Level1");
        Button m_Level2Button = m_LevelSelectRoot.Q<Button>("Level2");
        Button m_Level3Button = m_LevelSelectRoot.Q<Button>("Level3");
        Button m_BackButton = m_LevelSelectRoot.Q<Button>("BackButton");

        //Assign event for the buttons
        m_Level0Button.clicked += () => LoadScene(m_Level0);
        m_Level1Button.clicked += () => LoadScene(m_Level1);
        m_Level2Button.clicked += () => LoadScene(m_Level2);
        m_Level3Button.clicked += () => LoadScene(m_Level3);


        if (m_PauseObject != null)
        {
            m_BackButton.clicked += () => ReturnToPause();
        }
    }

    private void ReturnToPause()
    {
        m_LevelSelectRoot.style.display = DisplayStyle.None;
        m_PauseRoot.style.display = DisplayStyle.Flex;

        var Button = m_PauseRoot.Q<Button>("Resume");
        Button.Focus();
    }
    private void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
        AudioListener.pause = false;
    }
}
