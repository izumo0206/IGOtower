using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionUIController : MonoBehaviour
{
    [Header("効果音")]
    public AudioClip buttonSound;
    public AudioClip cancelSound;
    public void OnClickUp()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(buttonSound);
        }
        SendDirection(UnitDirection.Up);
    }
    public void OnClickRight()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(buttonSound);
        }
        SendDirection(UnitDirection.Right);
    }
    public void OnClickDown()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(buttonSound);
        }
        SendDirection(UnitDirection.Down);
    }
    public void OnClickLeft()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(buttonSound);
        }
        SendDirection(UnitDirection.Left);
    }

    public void OnClickCancel()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(cancelSound);
        }
        GameManager gm = FindObjectOfType<GameManager>();
        if(gm != null)
        {
            gm.CancelPlacement();
        }
    }

    private void SendDirection(UnitDirection dir)
    {
        GameManager gm = FindObjectOfType<GameManager>();

        if(gm != null)
        {
            gm.OnDirectionSelected(dir);
        }
    }
}
