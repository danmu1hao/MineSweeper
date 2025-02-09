using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SingleCellDisplay : MonoBehaviour
{
    #region 変数

    public GameObject prob;
    public GameObject cellCover;
    public GameObject cellBomb;
    public GameObject cellFlag;
    public TextMeshProUGUI cellNumber;

    public TextMeshProUGUI otherText;
    
    
    public CellManager cellManager;

    public int positionX;
    public int positionY;

    #endregion



    public void Init(int positionX,int positionY)
    {
        prob.SetActive(false);
         this.positionX = positionX;
         this.positionY = positionY;
        cellCover.gameObject.GetComponent<Image>().color = new Color32(132,132,132,255);
        cellCover.SetActive(true);
        cellBomb.SetActive(false);
        cellFlag.SetActive(false);
        
    }
    public void Init()
    {
        prob.SetActive(false);
        cellCover.gameObject.GetComponent<Image>().color = new Color32(132,132,132,255);
        cellCover.SetActive(true);
        cellBomb.SetActive(false);
        cellFlag.SetActive(false);
        
    }
    #region UI
    public void SetNumber(int i)
    {
        cellManager.boomNumber = i;
        cellNumber.text = cellManager.boomNumber.ToString();
        cellBomb.SetActive(false);
    }
    public void ShowCell()
    {

        cellCover.SetActive(!cellManager.isOpen);
    }
    public void SetBomb()
    {
        cellManager.boomNumber = -1;
        cellBomb.SetActive(true);
    }
    public void SetFlag()
    {
        cellFlag.SetActive(true);
    }
    public void CloseFlag()
    {
        cellFlag.SetActive(false);
    }
    

    #endregion
  
}

