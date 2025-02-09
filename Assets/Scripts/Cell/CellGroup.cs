using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellGroup
{
    public List<CellManager> cellManagers;
    public List<int[]> solutions;

    public CellGroup()
    {
        cellManagers = new List<CellManager>();
        solutions = new List<int[]>();
    }

    public Dictionary<int, List<int>> dictForPSEQ_SE;
}
