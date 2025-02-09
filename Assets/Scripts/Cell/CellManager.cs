using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellManager
{
    public GameObject cellModel;
    // isBoom => 1
    public bool isBoom;

    public float weight;

    /// <summary>
    /// 周囲のセルをマークするためのフラグ
    /// 未知のセルを分類する際に使用
    /// </summary>
    public bool isSurrondCell;

    private float probobability;
    public float Probobability
    {
        get
        {
            return (float)Math.Round(probobability, 3); // 小数点第3位で丸める
        }
        set
        {
            probobability = value;
        }
    }

    public bool isOpen = false;

    public int boomNumber;
    /// <summary>
    /// (y, x) 座標
    /// </summary>
    public Tuple<int, int> position;
    public bool isFlaged;

    

    
    
    /// <summary>
    /// 周囲の未知のセルの数を示す。注意：未知セルであり、隣接セルではない。
    /// </summary>
    public int nearbyUnknownCell = 0;

    public int indexForSolver;
    
    public float PSEQ_S;
    public float PSEQ_E = 0;
    public float PSEQ_Q = 0;

    public int indexInCellGroup;
    
    public CellGroup cellGroup;
}