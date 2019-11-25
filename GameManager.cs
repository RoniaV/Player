using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager GM;

    public delegate void VoidDelegate();
    public event VoidDelegate GameOver;    
    public event VoidDelegate Pause, Resume;

    private bool gameOver, paused;

    void Awake()
    {
        GM = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Pause += Paused;
        Resume += Reanuded;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P) && !paused)
        {
            Pause();
        }
        else if(Input.GetKeyDown(KeyCode.P) && paused)
        {
            Resume();
        }
    }

    public void Over()
    {
        gameOver = true;
        GameOver();
    }

    private void Paused()
    {
        paused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Reanuded()
    {
        paused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
