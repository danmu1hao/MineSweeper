using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PSEQ : MonoBehaviour
{
    #region 変数

    public static List<int[]> solutions;
    public static List<CellManager> allSurrondCellManager;

    #endregion


    #region PSEQ_Logic

    
    public static List<CellManager> FILTER_S(List<CellManager> cells)
    {
        List<CellGroup> cellGroups = new List<CellGroup>();
        foreach (var cellManager in AIManager.instance.surroundCell)
        {
            if (!cellGroups.Contains(cellManager.cellGroup) && cellManager.cellGroup != null)
            {
                cellGroups.Add(cellManager.cellGroup);
            }
        }
        foreach (var cell in cells)
        {
            List<CellManager> cellManagers = AIManager.instance.ExploreSurroundings(cell);
            cell.nearbyUnknownCell = 0;
            foreach (var cellManager in cellManagers)
            {
                if (!AIManager.instance.surroundCell.Contains(cellManager))
                {
                    cell.nearbyUnknownCell += 1;
                }
            }
            // 自分が0である可能性だけを気にする
        }
        float otherProb = AIManager.instance.otherProb;
        CellGroup cellGroup = new CellGroup();
        cellGroup = MergeCellGroup(cellGroups);

        // 周囲に情報のあるセルはあるか？
        List<CellManager> near = new List<CellManager>();
        List<CellManager> others = new List<CellManager>();

        bool success = false;
        foreach (var cell in cells)
        {
            success = false;
            foreach (var nearbyCell in AIManager.instance.ExploreSurroundings(new List<CellManager>() { cell }))
            {
                if (AIManager.instance.surroundCell.Contains(nearbyCell))
                {
                    near.Add(cell);
                    success = true;
                }
            }
            if (!success)
            {
                others.Add(cell);
            }
        }
        /*Debug.Log("near" + near.Count + "others" + others.Count());*/
        // near, other
        // otherは単純に処理

        #region near
        // 2つの辞書、1つは確率を記録するため、もう1つは各状況での安全セル数を記録するため
        Dictionary<int, int> oneAppearTimesDict = new Dictionary<int, int>();
        Dictionary<int, List<CellManager>> oneAppearTimesAndSafeCellListDict = new Dictionary<int, List<CellManager>>();

        // near周辺の一帯が重要
        foreach (var nearCellManager in near)
        {
            List<CellManager> haveDataCellManager = new List<CellManager>();
            List<CellManager> surrondCellManagers = AIManager.instance.ExploreSurroundings(near);
            foreach (var cellManager in surrondCellManagers)
            {
                if (AIManager.instance.surroundCell.Contains(cellManager))
                {
                    haveDataCellManager.Add(cellManager);
                }
            }

            Tuple<Dictionary<int, int>, Dictionary<int, List<CellManager>>> tuple = PSEQ_S_Caculate(cellGroup, nearCellManager);
            oneAppearTimesDict = tuple.Item1;
            oneAppearTimesAndSafeCellListDict = tuple.Item2;
            // 合計値を計算
            int totalValue = 0;
            foreach (var pair in oneAppearTimesDict)
            {
                totalValue += pair.Value;
            }
            /*Debug.Log("全可能性の合計数" + totalValue);*/
            int temp = 0;
            foreach (var pair in oneAppearTimesAndSafeCellListDict)
            {
                temp += pair.Value.Count;
            }
            /*Debug.Log("残りの数" + temp);*/
            float PSEQ_S = 0;

            for (int i = 0; i <= 8; i++)
            {
                if (oneAppearTimesDict.ContainsKey(i))
                {
                    if (oneAppearTimesAndSafeCellListDict.ContainsKey(i) && oneAppearTimesAndSafeCellListDict[i].Count != 0)
                    {
                        float prob = (float)oneAppearTimesDict[i] / totalValue;

                        for (int j = 0; j < nearCellManager.nearbyUnknownCell; j++)
                        {
                            prob *= 1 - otherProb;
                        }

                        int restSafeCell = oneAppearTimesAndSafeCellListDict[i].Count + nearCellManager.nearbyUnknownCell;

                        PSEQ_S += prob * 1;

                        if (prob * oneAppearTimesAndSafeCellListDict[i].Count > 0)
                        {
                            /*
                            Debug.Log(i + "はi" + prob + "の確率と安全セル数 " + oneAppearTimesAndSafeCellListDict[i].Count + "現在のPSEQは" + PSEQ_S);
                            */
                        }
                        break;
                    }
                }
            }
            nearCellManager.PSEQ_S = PSEQ_S;
            if (PSEQ_S > 0)
            {
                /*Debug.Log("計算されたPSEQ_S：" + nearCellManager.PSEQ_S);*/
            }
        }
        #endregion

        #region other

        if (others.Count != 0)
        {
            foreach (var cell in others)
            {
                cell.nearbyUnknownCell = AIManager.instance.ExploreSurroundingsCount(cell);

                // 自分が0である可能性だけを気にする
            }

            /*Debug.Log(otherProb);*/
            // 全部安全である確率
            /*Debug.Log("最小近接数" + minValue);*/
            foreach (var cellManager in others)
            {
                float prob_other = 1;
                for (int i = 0; i < cellManager.nearbyUnknownCell; i++)
                {
                    prob_other *= 1 - otherProb;
                }
                cellManager.PSEQ_S = prob_other * 1;
            }
            /*Debug.Log("その他のPSEQは" + prob_other + " " + minValue);*/
        }
        // 外側の一帯

        #endregion

        // PSEQ_Sの最大値を探す
        float maxPSEQ_S = cells.Max(cell => cell.PSEQ_S);

        // 最大値に等しいすべての要素を探す
        var maxCells = cells.Where(cell => Mathf.Approximately(cell.PSEQ_S, maxPSEQ_S)).ToList();

        if (AIManager.instance.debugMode)
        {
            Debug.Log("最終的に得られたセル数: " + maxCells.Count);
        }

        return maxCells;
    }
    
    static Tuple<Dictionary<int, int>,Dictionary<int, List<CellManager>>> PSEQ_S_Caculate(CellGroup cellGroup,CellManager cell)
    {
                  
            //两个字典 一个用来统计概率·，一个用来看每个情况有多少个安全格子
            Dictionary<int, int> oneAppearTimesDict = new Dictionary<int, int>();
            Dictionary<int, List<CellManager>> oneAppearTimesAndSafeCellListDict = new Dictionary<int, List<CellManager>>();
            
            //接下来就是找solution的排列
            //首先根据数值来筛选对应的solution
            List<int> nearCellIndex=new List<int>();
            List<CellManager> nearbyCells = AIManager.instance.ExploreSurroundings(new List<CellManager>(){cell});
            foreach (var cellManager in nearbyCells)
            {
                if (AIManager.instance.surroundCell.Contains(cellManager))
                {
                    nearCellIndex.Add(cellManager.indexInCellGroup);
                }
            }
            /*foreach (var cellManager in nearbyCells)
            {
                cellManager.newIndex = cellGroup.cellManagers.IndexOf(cellManager);
                /*Debug.Log("这个的index是"+cellManager.index+"长度为"+cellGroup.cellManagers.Count);#1#
                nearCellIndex.Add(cellManager.newIndex);
            }*/

            if (cellGroup.solutions==null)
            {
                /*Debug.LogWarning("似乎solution为空");*/
            }
            //有两个要关注的 1：概率 2：安全格子
            /*Debug.Log("一共有多少种solution"+cellGroup.solutions.Count);*/
            foreach (var solution in cellGroup.solutions)
            {
                int oneAppearTimes= 0;
                //1 概率
                foreach (var index in nearCellIndex)
                {
                    oneAppearTimes += solution[index];
                }
                if (oneAppearTimesDict.ContainsKey(oneAppearTimes))
                {
                    oneAppearTimesDict[oneAppearTimes] += 1;
                }
                else
                {
                    oneAppearTimesDict.Add(oneAppearTimes,1);
                }
                //2:安全格子
                if (oneAppearTimesAndSafeCellListDict.ContainsKey(oneAppearTimes))
                {

                    var originalList = oneAppearTimesAndSafeCellListDict[oneAppearTimes];
                    
                    /*Debug.Log(oneAppearTimes+" rest "+originalList.Count);*/
                    foreach (var cellManagerTemp in originalList.ToList()) // 使用副本遍历
                    {
                        /*if (cellManagerTemp.indexInCellGroup>solution.Length-1 || cellManagerTemp.indexInCellGroup<0)
                        {
                            Debug.LogWarning("坐标不对"+cellManagerTemp.indexInCellGroup+" "+solution.Length);
                            AIManager.instance.error = true;
                            /*Debug.Break(); #1#
                        }*/
                        if (solution[cellManagerTemp.indexInCellGroup] != 0)
                        {
                            originalList.Remove(cellManagerTemp); // 修改原集合
                        }
                    }
                    /*Debug.Log(oneAppearTimes+" rest "+originalList.Count);*/
                }
                else
                {
                    List<CellManager> cellManagers = new List<CellManager>();
                    for (int i = 0; i < solution.Length; i++)
                    {
                        if (solution[i]==0)
                        {
                            cellManagers.Add(cellGroup.cellManagers[i]);
                        }
                    }
                    oneAppearTimesAndSafeCellListDict.Add(oneAppearTimes,cellManagers);
                }
            }

            /*
            foreach (var VARIABLE in oneAppearTimesAndSafeCellListDict)
            {
                if (VARIABLE.Value.Count>0)
                {
                    Debug.Log("对于数字"+VARIABLE.Key+"还剩下几个确定安全的格子？"+VARIABLE.Value.Count);
                }
            }
            */
            int temp = 0;
            foreach (var pair in oneAppearTimesAndSafeCellListDict)
            {
                temp += pair.Value.Count;
            }
            /*Debug.Log("剩下多少"+temp);*/
            return new Tuple<Dictionary<int, int>, Dictionary<int, List<CellManager>>>(oneAppearTimesDict,
                oneAppearTimesAndSafeCellListDict);
    }


    
    //p(b, n)χ(C − {b} − (∪S(b, n)))
/// <summary>
/// EとSの考え方は基本的に同じ
/// </summary>
/// <param name="cells"></param>
/// <returns></returns>
public static List<CellManager> FILTER_E(List<CellManager> cells)
{
    // 分区の考えを放棄する？
    List<CellGroup> cellGroups = new List<CellGroup>();
    foreach (var cellManager in AIManager.instance.surroundCell)
    {
        if (!cellGroups.Contains(cellManager.cellGroup) && cellManager.cellGroup != null)
        {
            cellGroups.Add(cellManager.cellGroup);
        }
    }
    foreach (var cell in cells)
    {
        List<CellManager> cellManagers = AIManager.instance.ExploreSurroundings(cell);
        cell.nearbyUnknownCell = 0;
        foreach (var cellManager in cellManagers)
        {
            if (!AIManager.instance.surroundCell.Contains(cellManager))
            {
                cell.nearbyUnknownCell += 1;
            }
        }
        // 自分が0である可能性だけを気にする
    }
    float otherProb = AIManager.instance.otherProb;
    CellGroup cellGroup = new CellGroup();
    cellGroup = MergeCellGroup(cellGroups);

    // 周囲に情報のあるセルはあるか？
    List<CellManager> near = new List<CellManager>();
    List<CellManager> others = new List<CellManager>();

    bool success = false;
    foreach (var cell in cells)
    {
        success = false;
        foreach (var nearbyCell in AIManager.instance.ExploreSurroundings(new List<CellManager>() { cell }))
        {
            if (AIManager.instance.surroundCell.Contains(nearbyCell))
            {
                near.Add(cell);
                success = true;
            }
        }
        if (!success)
        {
            others.Add(cell);
        }
    }
    /*Debug.Log("near" + near.Count + "others" + others.Count());*/
    // near, other
    // otherは単純に処理

    #region near
    // 2つの辞書、1つは確率を記録するため、もう1つは各状況での安全セル数を記録するため
    Dictionary<int, int> oneAppearTimesDict = new Dictionary<int, int>();
    Dictionary<int, List<CellManager>> oneAppearTimesAndSafeCellListDict = new Dictionary<int, List<CellManager>>();

    // near周辺の一帯が重要
    foreach (var nearCellManager in near)
    {
        List<CellManager> haveDataCellManager = new List<CellManager>();
        List<CellManager> surrondCellManagers = AIManager.instance.ExploreSurroundings(near);
        foreach (var cellManager in surrondCellManagers)
        {
            if (AIManager.instance.surroundCell.Contains(cellManager))
            {
                haveDataCellManager.Add(cellManager);
            }
        }

        Tuple<Dictionary<int, int>, Dictionary<int, List<CellManager>>> tuple = PSEQ_S_Caculate(cellGroup, nearCellManager);
        oneAppearTimesDict = tuple.Item1;
        oneAppearTimesAndSafeCellListDict = tuple.Item2;
        // 合計値を計算
        int totalValue = 0;
        foreach (var pair in oneAppearTimesDict)
        {
            totalValue += pair.Value;
        }
        /*Debug.Log("全可能性の合計数" + totalValue);*/
        int temp = 0;
        foreach (var pair in oneAppearTimesAndSafeCellListDict)
        {
            temp += pair.Value.Count;
        }
        /*Debug.Log("残りの数" + temp);*/
        float PSEQ_E = 0;

        for (int i = 0; i <= 8; i++)
        {
            if (oneAppearTimesDict.ContainsKey(i))
            {
                if (oneAppearTimesAndSafeCellListDict.ContainsKey(i) && oneAppearTimesAndSafeCellListDict[i].Count != 0)
                {
                    float prob = (float)oneAppearTimesDict[i] / totalValue;
                    for (int j = 0; j < nearCellManager.nearbyUnknownCell; j++)
                    {
                        prob *= 1 - otherProb;
                    }

                    int restSafeCell = oneAppearTimesAndSafeCellListDict[i].Count + nearCellManager.nearbyUnknownCell;
                    PSEQ_E += prob * restSafeCell;
                    if (prob * oneAppearTimesAndSafeCellListDict[i].Count > 0)
                    {
                        /*
                        Debug.Log(prob + "確率と安全セル数 " + restSafeCell + "現在のPSEQは" + PSEQ_E);
                        Debug.Log(nearCellManager.nearbyUnknownCell + "安全セル数 " + oneAppearTimesAndSafeCellListDict[i].Count);
                        */
                    }
                    break;
                }
            }
        }
        nearCellManager.PSEQ_E = PSEQ_E;
        if (PSEQ_E > 0)
        {
            /*Debug.Log("計算されたPSEQ_E：" + nearCellManager.PSEQ_S);*/
        }
    }
    #endregion

    #region other

    if (others.Count != 0)
    {
        foreach (var cellManager in others)
        {
            float prob_other = 1;
            for (int i = 0; i < cellManager.nearbyUnknownCell; i++)
            {
                prob_other *= 1 - otherProb;
            }
            cellManager.PSEQ_E = prob_other * cellManager.nearbyUnknownCell;
        }
        /*Debug.Log("その他のPSEQは" + prob_other + " " + minValue);*/
    }
    // 外側の一帯

    #endregion

    // PSEQ_Eの最大値を探す
    float maxPSEQ_E = cells.Max(cell => cell.PSEQ_E);

    // 最大値に等しいすべての要素を探す
    var maxCells = cells.Where(cell => Mathf.Approximately(cell.PSEQ_E, maxPSEQ_E)).ToList();

    if (AIManager.instance.debugMode)
    {
        Debug.Log("最終的に得られたセル数: " + maxCells.Count);
    }

    return maxCells;
}

public static List<CellManager> FILTER_Q(List<CellManager> cellManagers)
{
    foreach (var cellManager in cellManagers)
    {
        float result = 0;
        foreach (float prob in FILTER_Q_Prob(cellManager))
        {
            if (prob > 0)
            {
                double temp = prob * Math.Log(prob, 2) * -1;
                result += (float)temp;
            }
        }

        result = (float)Math.Round(result, 3);
        cellManager.PSEQ_Q = result;
    }
    // PSEQ_Qの最大値を探す
    float maxPSEQ_Q = cellManagers.Max(cell => cell.PSEQ_Q);

    // 最大値に等しいすべての要素を探す
    var maxCells = cellManagers.Where(cell => Mathf.Approximately(cell.PSEQ_Q, maxPSEQ_Q)).ToList();

    if (AIManager.instance.debugMode)
    {
        Debug.Log("最終的に得られたセル数: " + maxCells.Count);
    }

    return maxCells;
}

/// <summary>
/// 方塊の隣にちょうど𝑖個の地雷が存在する確率を計算
/// 情報エントロピーlog2の計算
/// 必要な隣接セルを準備しておけば自動で確率を生成
/// </summary>
/// <param name="targetCell"></param>
public static List<float> FILTER_Q_Prob(CellManager targetCell)
{
    List<CellManager> cells = new List<CellManager>();
    // 周囲を探索
    // 周囲の8セルをループ
    int x = targetCell.position.Item2;
    int y = targetCell.position.Item1;
    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            // 中心セル（自身）をスキップ
            if (i == 0 && j == 0)
            {
                continue;
            }

            int targetX = x + i;
            int targetY = y + j;

            // 有効なグリッド範囲内かを確認
            if (targetX >= 0 && targetX < GameManager.instance.cellList.GetLength(1) &&
                targetY >= 0 && targetY < GameManager.instance.cellList.GetLength(0))
            {
                // 対象セルを取得
                CellManager target = GameManager.instance.cellList[targetY, targetX];

                // 対象セルが条件を満たすか確認
                if (!target.isOpen)
                {
                    // 未開のセルを追加
                    cells.Add(target);
                }
            }
        }
    }

    if (targetCell.position.Item2 == 2 && targetCell.position.Item1 == 1)
    {
        foreach (var target in cells)
        {
            /*
            Debug.Log("座標: " + target.position.Item2 + " " + target.position.Item1 + " 確率: " + target.Probobability);
            */
        }
    }

    // 周囲のセル数をカウント
    int surroundCell = cells.Count;

    Dictionary<int, List<int[]>> dict = GenerateLists(surroundCell);
    List<float> probList = new List<float>();
    for (int i = 0; i < surroundCell; i++)
    {
        // これは全体の確率（𝑖個の地雷が存在する確率）
        float allProb = 0;
        // 現在のリストは𝑖個の地雷を含む全リスト

        for (int j = 0; j < dict[i].Count; j++)
        {
            // 各場合の具体的な確率
            float tempProb = 1;
            for (int k = 0; k < surroundCell; k++)
            {
                if (dict[i][j][k] == 1)
                {
                    tempProb *= cells[k].Probobability;
                }
                else
                {
                    tempProb *= (1 - cells[k].Probobability);
                }
            }
            tempProb = (float)Math.Round(tempProb, 3);
            allProb += tempProb;
        }

        probList.Add(allProb);
    }

    return probList;
}


    #endregion
    #region ToolMethod

    // セクションを分けるのをやめて、まず簡単にマージする
    public static CellGroup MergeCellGroup(List<CellGroup> cellGroups)
    {
        if (cellGroups == null || cellGroups.Count == 0)
        {
            /*Debug.LogWarning("バグ");*/
            return null; // マージする CellGroup がない場合は直接 null を返す
        }

        // 新しいマージ結果の CellGroup を作成
        CellGroup mergedGroup = new CellGroup();

        // cellManagers をマージ
        foreach (var cellGroup in cellGroups)
        {
            if (cellGroup != null)
            {
                mergedGroup.cellManagers.AddRange(cellGroup.cellManagers);
            }
            else
            {
                /*Debug.LogWarning("バグ!!!!");*/
            }
        }

        // solutions をマージ
        var mergedSolutions = MergeSolutions(cellGroups);
        
        mergedGroup.solutions = mergedSolutions;

        foreach (var cellManager in mergedGroup.cellManagers)
        {
            cellManager.indexInCellGroup= mergedGroup.cellManagers.IndexOf(cellManager);
        }
        // マージ結果を出力（デバッグ用）
        /*Debug.Log($"Merged cellManagers count: {mergedGroup.cellManagers.Count}");
        Debug.Log($"Merged solutions count: {mergedGroup.solutions.Count}");*/
        return mergedGroup;
    }

    static List<int[]> MergeSolutions(List<CellGroup> cellGroups)
    {
        if (cellGroups == null || cellGroups.Count == 0)
        {
            return new List<int[]>(); // CellGroup がない場合、空のリストを返す
        }

        return MergeRecursive(cellGroups, 0);
    }

    private static List<int[]> MergeRecursive(List<CellGroup> cellGroups, int index)
    {
        if (index == cellGroups.Count - 1)
        {
            // 再帰の最後の CellGroup に到達した場合、その solutions を直接返す
            return cellGroups[index].solutions;
        }

        // 現在の CellGroup の solutions を取得
        var currentSolutions = cellGroups[index].solutions;

        // 残りの CellGroup の solutions を取得（再帰的に呼び出す）
        var mergedNextSolutions = MergeRecursive(cellGroups, index + 1);

        // 現在の solutions と再帰結果をマージ
        List<int[]> mergedResults = new List<int[]>();

        foreach (var solution1 in currentSolutions)
        {
            foreach (var solution2 in mergedNextSolutions)
            {
                // マージ後の solution を作成
                int[] mergedSolution = new int[solution1.Length + solution2.Length];
                solution1.CopyTo(mergedSolution, 0);
                solution2.CopyTo(mergedSolution, solution1.Length);

                // 結果リストに追加
                mergedResults.Add(mergedSolution);
            }
        }

        return mergedResults;
    }

    #endregion

    #region PSEQ_UI

    public void InitForPSEQ()
    {
        foreach (var cellManager in GameManager.instance.cellList)
        {
            cellManager.PSEQ_S = 0;
            cellManager.PSEQ_E = 0;
            cellManager.PSEQ_Q = 0;
            
        }
    }
    public void ShowS()
    {
        InitForPSEQ();
        List<CellManager> guessList = AIManager.instance.FindAllWithMinNumber(GameManager.instance.restCellManagers);
        Debug.Log(guessList.Count);
        PSEQ.FILTER_S(guessList);
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
        }


        foreach (var cell in guessList)
        {
            
            if (!cell.isOpen)
            {
                /*FILTER_S(GameManager.instance.restCellManagers);*/
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(true);
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.text= cell.PSEQ_S.ToString("F3");
            }
        }
    }
    public void CloseS()
    {
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            if (!cell.isOpen)
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(false);
            }
            else
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            }
        }

    }
    
    
    public void ShowE()
    {
        InitForPSEQ();
        List<CellManager> guessList = AIManager.instance.FindAllWithMinNumber(GameManager.instance.restCellManagers);
        PSEQ.FILTER_E(guessList);
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
        }
        foreach (var cell in guessList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            if (!cell.isOpen)
            {
                /*FILTER_S(GameManager.instance.restCellManagers);*/
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(true);
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.text= cell.PSEQ_E.ToString("F3");
            }
        }
    }
    public void CloseE()
    {
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            if (!cell.isOpen)
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(false);
            }
            else
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            }
        }

    }



    public void ShowQ()
    {
        InitForPSEQ();
        List<CellManager> guessList = AIManager.instance.FindAllWithMinNumber(GameManager.instance.restCellManagers);

        FILTER_Q(guessList);
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
        }
        foreach (var cell in guessList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            if (!cell.isOpen)
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(true);
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.text= cell.PSEQ_Q.ToString("F3");
            }
        }

    }
    public void ShowTargetQ()
    {
        List<CellManager> guessList = AIManager.instance.FindAllWithMinNumber(GameManager.instance.restCellManagers);
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
        }
        FILTER_Q(guessList);
        foreach (var cell in guessList)
        {
            if (!cell.isOpen)
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(true);
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.text= cell.PSEQ_Q.ToString();
            }
        }
        Func<CellManager, float> sortKey= (cell) => -cell.PSEQ_Q;
        guessList = guessList.OrderBy(sortKey).ToList();
        guessList[0].cellModel.GetComponent<SingleCellDisplay>().cellCover.GetComponent<Image>().color=Color.green;
        tempTarget=guessList[0].cellModel;
    }

    GameObject tempTarget;
    public void CloseQ()
    {
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            if (!cell.isOpen)
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().otherText.gameObject.SetActive(false);
            }
            else
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            }
        }

        if (tempTarget!=null)
        {
            tempTarget.GetComponent<SingleCellDisplay>().cellCover.GetComponent<Image>().color=Color.grey;
        }
    }

    #endregion




    /// <summary>
    /// n,包含n个1的list int[]
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    static Dictionary<int, List<int[]>> GenerateLists(int n)
    {
        Dictionary<int, List<int[]>> dict = new Dictionary<int, List<int[]>>();
        int totalCombinations = (int)Math.Pow(2, n);

        for (int i = 0; i < totalCombinations; i++)
        {
            int[] binaryArray = ConvertToBinaryArray(i, n);
            int onesCount = binaryArray.Count(x => x == 1);

            if (!dict.ContainsKey(onesCount))
            {
                dict[onesCount] = new List<int[]>();
            }
            dict[onesCount].Add(binaryArray);
        }

        return dict;
    }

    static int[] ConvertToBinaryArray(int num, int length)
    {
        int[] binaryArray = new int[length];
        for (int i = 0; i < length; i++)
        {
            binaryArray[length - i - 1] = (num >> i) & 1;
        }
        return binaryArray;
    }
    
    #region UI
    public static int[] ConvertIndex(int indexAll, List<CellGroup> cellGroups)
    {
        int groupCount = cellGroups.Count;
        int[] indices = new int[groupCount];

        // 首先，获取每个 CellGroup 的基数（solutions 长度）
        int[] radixes = new int[groupCount];
        for (int i = 0; i < groupCount; i++)
        {
            radixes[i] = cellGroups[i].solutions.Count;
        }

        // 计算每个位置的基数乘积，用于除法和取模
        Debug.Log(groupCount);
        int[] radixProducts = new int[groupCount];
        Debug.Log(radixProducts.Count());
        radixProducts[groupCount - 1] = 1;
        for (int i = groupCount - 2; i >= 0; i--)
        {
            radixProducts[i] = radixes[i + 1] * radixProducts[i + 1];
        }

        // 开始计算每个索引
        for (int i = 0; i < groupCount; i++)
        {
            indices[i] = indexAll / radixProducts[i];
            indexAll = indexAll % radixProducts[i];
        }

        return indices;
    }
    
    int index = 0;
    public void ShowMineUI(int solutionIndex,CellGroup cellGroup)
    {
        for (int i = 0; i < cellGroup.cellManagers.Count(); i++)
        {
            /*Debug.Log("isBoom?"+cellGroup.solutions[solutionIndex][i]);*/
            CellManager cellManager=cellGroup.cellManagers[i];
            if (cellGroup.solutions[solutionIndex][i]==1)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellBomb.SetActive(true);
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.SetActive(false);
            }
            else
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellBomb.SetActive(false);
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.SetActive(true);
            }

        }

    }

    public void StartShowMine()
    {
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            if (cell.isSurrondCell)
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            }
        }
        
        index = 0;
        int[] indexList= ConvertIndex(index, AIManager.instance.allSurrondCellGroups);
        for (int i = 0; i < AIManager.instance.allSurrondCellGroups.Count(); i++)
        {
            
            ShowMineUI(indexList[i],AIManager.instance.allSurrondCellGroups[i]);
        }

    }
    public void ShowNextMineUI()
    {
        index++;
        int indexMax = 1;
        foreach (var cellGroup in AIManager.instance.allSurrondCellGroups)
        {
            indexMax *= cellGroup.solutions.Count;
        }
        if (index>=indexMax)
        {
            index = 0;
        }

        int[] indexList= ConvertIndex(index, AIManager.instance.allSurrondCellGroups);
        for (int i = 0; i < AIManager.instance.allSurrondCellGroups.Count(); i++)
        {

            ShowMineUI(indexList[i],AIManager.instance.allSurrondCellGroups[i]);
        }
    }

    public void CloseMine()
    {
        index = 0;
        foreach (var cellManagerGroup in AIManager.instance.allSurrondCellGroups)
        {
            foreach (var cellManager in cellManagerGroup.cellManagers)
            {
                cellManager.cellModel.GetComponent<SingleCellDisplay>().cellCover.SetActive(true);
                if (cellManager.isBoom)
                {

                    cellManager.cellModel.GetComponent<SingleCellDisplay>().cellBomb.SetActive(true);
                }
                else
                {
                    cellManager.cellModel.GetComponent<SingleCellDisplay>().cellBomb.SetActive(false);
                }
            }
        }
        
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(true);
            if (!cell.isOpen)
            {

            }
            else
            {
                cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            }
        }
        
    }

    #endregion
    /*public void CheckSolutions()
    {

        foreach (var solution in solutions)
        {
            //准备数值
            allSurrondCellManager = AIManager.instance.allSurrondCellManagerGroups;
            //赋值
            for (int i = 0; i < AIManager.instance.allSurrondCellManagerGroups.Count; i++)
            {
                allSurrondCellManager[i].tempNum=solution[i];
            }
            
        }
    }*/
    

   

    public bool CheckNearBy(CellManager cell1, CellManager cell2) 
    {
        // 2つのセルの x 座標と y 座標を取得
        int x1 = cell1.position.Item1;
        int y1 = cell1.position.Item2;
        int x2 = cell2.position.Item1;
        int y2 = cell2.position.Item2;

        // 九宮格（3x3 の範囲）内にあるかどうかを判定
        if (Math.Abs(x1 - x2) <= 1 && Math.Abs(y1 - y2) <= 1)
        {
            return true; // 2つのセルは隣接している
        }
        else
        {
            return false; // 2つのセルは隣接していない
        }
    }


}
