using System.Collections;
using System.Threading;
using UnityEngine;

public class testClick : MonoBehaviour
{
    private bool isPlayerClicked = false;
    private bool isObjectClicked = false;
    private GameObject clickedObject = null;

    private void Start()
    {

        StartCoroutine(Print());
        StartCoroutine(Click());
    }

    private void Update()
    {

    }

    IEnumerator Click()
    {

            // 检测玩家点击
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("click");
                yield return new WaitForSeconds(2);

            }

    }
    IEnumerator Print()
    {
        Debug.Log(1);
        yield return null;
        Debug.Log(2);
    }
    IEnumerator Print1()
    {
        Debug.Log(11);
        yield return new WaitForSeconds(2.0f);
        Debug.Log(22);
    }
}
