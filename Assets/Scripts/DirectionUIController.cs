using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionUIController : MonoBehaviour
{
    public void OnClickUp()
    {
        SendDirection(UnitDirection.Up);
    }
    public void OnClickRight()
    {
        SendDirection(UnitDirection.Right);
    }
    public void OnClickDown()
    {
        SendDirection(UnitDirection.Down);
    }
    public void OnClickLeft()
    {
        SendDirection(UnitDirection.Left);
    }

    public void OnClickCancel()
    {
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
