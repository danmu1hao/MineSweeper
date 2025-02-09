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
    //p(b, n)χ(C − {b} − (∪S(b, n)))
    public static List<CellManager> FILTER_S(List<CellManager> cells)
    {
        //放弃分区的思路？
        List<CellGroup> cellGroups=new List<CellGroup>();
        foreach (var cellManager in AIManager.instance.surroundCell)
        {
            if (!cellGroups.Contains(cellManager.cellGroup) && cellManager.cellGroup!=null)
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
                    cell.nearbyUnknownCell+=1;
                }
            }
            //我们只关心自己为0的可能
        }
        float otherProb = AIManager.instance.otherProb;
        CellGroup cellGroup=new CellGroup();
        cellGroup=MergeCellGroup(cellGroups);

        //周围有有情报的吗？
        List<CellManager> near=new List<CellManager>();
        List<CellManager> others=new List<CellManager>();

        
        bool success=false;
        foreach (var cell in cells)
        {
            success = false;
            foreach (var nearbyCell in AIManager.instance.ExploreSurroundings(new List<CellManager>(){cell}))
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
        /*Debug.Log("near"+near.Count+"others"+others.Count());*/
        //near, other
        //other简单

        #region near
        //两个字典 一个用来统计概率·，一个用来看每个情况有多少个安全格子
        Dictionary<int, int> oneAppearTimesDict = new Dictionary<int, int>();
        Dictionary<int, List<CellManager>> oneAppearTimesAndSafeCellListDict = new Dictionary<int, List<CellManager>>();

        
        //真正重要的是near的周围一圈
        foreach (var nearCellManager in near)
        {
            List<CellManager> haveDataCellManager=new List<CellManager>();
            List<CellManager> surrondCellManagers = AIManager.instance.ExploreSurroundings(near);
            foreach (var cellManager in surrondCellManagers)
            {
                if (AIManager.instance.surroundCell.Contains(cellManager))
                {
                    haveDataCellManager.Add(cellManager);
                }
            }
            
            Tuple<Dictionary<int, int>,Dictionary<int, List<CellManager>>>  tuple=PSEQ_S_Caculate(cellGroup,nearCellManager);
            oneAppearTimesDict = tuple.Item1;
            oneAppearTimesAndSafeCellListDict = tuple.Item2;
            // 计算总和
            int totalValue = 0;
            foreach (var pair in oneAppearTimesDict)
            {
                totalValue += pair.Value;
            }
            /*Debug.Log("一共有多少种可能"+totalValue);*/
            int temp = 0;
            foreach (var pair in oneAppearTimesAndSafeCellListDict)
            {
                temp += pair.Value.Count;
            }
            /*Debug.Log("剩下多少"+temp);*/
            float PSEQ_S = 0;

            for (int i = 0; i <= 8; i++)
            {
                if (oneAppearTimesDict.ContainsKey(i))
                {
                    if (oneAppearTimesAndSafeCellListDict.ContainsKey(i) && oneAppearTimesAndSafeCellListDict[i].Count!=0)
                    {
                        float prob=(float)oneAppearTimesDict[i]/totalValue;

                        for (int j = 0; j < nearCellManager.nearbyUnknownCell; j++)
                        {
                            prob*=1-otherProb;
                        }
                        
                        int restSafeCell=oneAppearTimesAndSafeCellListDict[i].Count+nearCellManager.nearbyUnknownCell;

                        PSEQ_S+=prob*1;

                        if (prob*oneAppearTimesAndSafeCellListDict[i].Count>0)
                        {
                            /*
                            Debug.Log(i+"这个是i"+prob+"概率和安全格子为 "+oneAppearTimesAndSafeCellListDict[i].Count+"PSEQ当前为"+PSEQ_S);
                        */
                        }
                        break;
                    }
                }
            }
            nearCellManager.PSEQ_S = PSEQ_S;
            if (PSEQ_S>0)
            {
                /*Debug.Log("我们算出"+nearCellManager.PSEQ_S);*/
            }
        }


        #endregion
                
        
        #region other

        if (others.Count!=0)
        {
            foreach (var cell in others)
            {
                cell.nearbyUnknownCell = AIManager.instance.ExploreSurroundingsCount(cell);

                //我们只关心自己为0的可能
            }

            /*Debug.Log(otherProb);*/
            //概率为全部为安全
            /*Debug.Log("最小邻接数"+minValue);*/
            foreach (var cellManager in others)
            {
                float prob_other=1;
                for (int i = 0; i < cellManager.nearbyUnknownCell; i++)
                {
                    prob_other*=1-otherProb;
                }
                cellManager.PSEQ_S = prob_other* 1;
            }
            /*Debug.Log("其他的PSEQ为"+prob_other+" "+minValue);*/
        }
        //这一圈在外面的一圈


        #endregion

        // 找到 PSEQ_S 最大值
        float maxPSEQ_S = cells.Max(cell => cell.PSEQ_S);

        // 找到所有 PSEQ_S 等于最大值的元素
        var maxCells = cells.Where(cell => Mathf.Approximately(cell.PSEQ_S, maxPSEQ_S)).ToList();

        if (AIManager.instance.debugMode)
        {
            Debug.Log("我们最终有这么多格子: " + maxCells.Count);
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
                        //如果部位0，那么排除掉
                        if (cellManagerTemp.indexInCellGroup>solution.Length-1 || cellManagerTemp.indexInCellGroup<0)
                        {
                            Debug.LogWarning("坐标不对"+cellManagerTemp.indexInCellGroup+" "+solution.Length);
                            AIManager.instance.error = true;
                            /*Debug.Break(); */
                        }
                        else if (solution[cellManagerTemp.indexInCellGroup] != 0)
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

    /*#region PSEQ_SE_CoreLogic

        static Tuple<Dictionary<int, int>, Dictionary<int, List<CellManager>>> PSEQ_SE_Caculate(CellManager cell)
    {
        //step1: 整理
        //周囲の全てのセル確認
        List<CellManager> surrondCellList = AIManager.instance.ExploreSurroundings(cell);
        //周囲のセルグループ確認
        List<CellGroup> surrondCellGroups=new List<CellGroup>();
        //周囲のセルグループのターゲットインデックス
        Dictionary<CellGroup,List<int>> cellGroupTargetIndexsDict=new Dictionary<CellGroup, List<int>>();    
        //type1:情報持つセル type2:情報持たないセル
        List<CellManager> inGroupCells=new List<CellManager>();
        List<CellManager> notInGroupCells= new List<CellManager>();
        foreach (var cellManager in surrondCellList)
        {
            if (cellManager.cellGroup!=null)
            {
                inGroupCells.Add(cellManager);
            }
            else
            {
                notInGroupCells.Add(cellManager);
            }
        }
        //indexの整理
        foreach (var surrondCell in surrondCellList)
        {
            if (!surrondCellGroups.Contains(surrondCell.cellGroup))
            {
                surrondCellGroups.Add(surrondCell.cellGroup);
            }

            if (cellGroupTargetIndexsDict.ContainsKey(surrondCell.cellGroup))
            {
                cellGroupTargetIndexsDict[surrondCell.cellGroup].Add(surrondCell.indexInCellGroup);
            }
            else
            {
                cellGroupTargetIndexsDict.Add(surrondCell.cellGroup, new List<int>(){surrondCell.indexInCellGroup});
            }
        }
    
        //step2: 情報を整理
        //初期化
        foreach (var cellGroup in surrondCellGroups)
        {
            cellGroup.dictForPSEQ_SE = new Dictionary<int, List<int>>();
        }
        //周囲のセルグループの情報を整理
        foreach (var cellGroupAndIndexPair in cellGroupTargetIndexsDict)
        {
            CellGroup cellGroup= cellGroupAndIndexPair.Key;
            foreach (var solution in cellGroup.solutions)
            {
                //このグループ内の、この解に対して、隣接するセルのマイン数
                int groupTargetCellMineCount = 0;
                foreach (var index in cellGroupAndIndexPair.Value)
                {
                    groupTargetCellMineCount+= solution[index];
                }

                if (cellGroup.dictForPSEQ_SE.ContainsKey(groupTargetCellMineCount))
                {
                    HashSet<int> newList = new HashSet<int>(cellGroup.dictForPSEQ_SE[groupTargetCellMineCount]);
                    newList.UnionWith(solution.ToList());
                    cellGroup.dictForPSEQ_SE[groupTargetCellMineCount] = newList.ToList();
                }
                else
                {
                    cellGroup.dictForPSEQ_SE.Add(groupTargetCellMineCount,cellGroupAndIndexPair.Value);
                }
            }
        }
        //step3
        //用意したdictを使って計算
        //N=groupA+groupB+...+UnknownCell
        //之后用gpt给我的代码
        int allsurrond
        int allGroupTargetCellMineCount = 0;
        
        foreach (var VARIABLE in COLLECTION)
        {
            
        }
    }

    public Dictionary<int, int> GenerateDict(List<CellGroup> cellGroups)
    {
        // 计算所有组合
        Dictionary<int, List<int[]>> groupCombinations = GetKeyCombinations(cellGroups);

        // 重叠处理
        Dictionary<int, List<HashSet<int>[]>> overlappedgroupCombinations = GenerateOverlappedResult(groupCombinations);

        Dictionary<int, int> cellNumAndSafeCell=new Dictionary<int, int>();
        //正是计算
        foreach (var pair in overlappedgroupCombinations)
        {
            int count = 0;
            foreach (var hashSet in pair.Value)
            {

                List<int> indexList= new List<int>();
                foreach (int index in hashSet)
                {
                    
                }
            }
        }
    }
    
    
    /// <summary>
    ///  intput 0,1 0,1
    ///  output
    /// 0:[0, 0]
    /// 1:[0, 1] [1, 0]
    /// 2:[1,1]
    /// </summary>
    /// <param name="cellGroups"></param>
    /// <returns></returns>

    static Dictionary<int, List<int[]>> GetKeyCombinations(List<CellGroup> cellGroups)
    {
        Dictionary<int, List<int[]>> result = new Dictionary<int, List<int[]>>();

        // 获取每个 CellGroup 的 key 列表
        List<List<int>> allKeys = new List<List<int>>();
        foreach (var group in cellGroups)
        {
            allKeys.Add(new List<int>(group.dictForPSEQ_SE.Keys));
        }

        // 递归生成所有组合
        GenerateCombinations(allKeys, 0, new int[cellGroups.Count], result);

        return result;
    }

    static void GenerateCombinations(
        List<List<int>> allKeys,
        int index,
        int[] currentCombination,
        Dictionary<int, List<int[]>> result)
    {
        if (index >= allKeys.Count)
        {
            int sum = currentCombination.Sum();

            if (!result.ContainsKey(sum))
            {
                result[sum] = new List<int[]>();
            }
            result[sum].Add((int[])currentCombination.Clone());
            return;
        }

        foreach (int key in allKeys[index])
        {
            currentCombination[index] = key;
            GenerateCombinations(allKeys, index + 1, currentCombination, result);
        }
    }

    static Dictionary<int, List<HashSet<int>[]>> GenerateOverlappedResult(Dictionary<int, List<int[]>> result)
    {
        var overlappedResult = new Dictionary<int, List<HashSet<int>[]>>();

        foreach (var pair in result)
        {
            int key = pair.Key;
            List<int[]> combinations = pair.Value;

            // 初始化集合列表
            HashSet<int>[] overlappedCombination = new HashSet<int>[combinations[0].Length];
            for (int i = 0; i < overlappedCombination.Length; i++)
            {
                overlappedCombination[i] = new HashSet<int>();
            }

            // 填充集合
            foreach (var combination in combinations)
            {
                for (int i = 0; i < combination.Length; i++)
                {
                    overlappedCombination[i].Add(combination[i]);
                }
            }

            // 确保唯一性：只记录一次
            if (!overlappedResult.ContainsKey(key))
            {
                overlappedResult[key] = new List<HashSet<int>[]>
                {
                    overlappedCombination.Select(set => new HashSet<int>(set)).ToArray()
                };
            }
        }

        return overlappedResult;
    }

    #endregion*/

    
    //p(b, n)χ(C − {b} − (∪S(b, n)))
    /// <summary>
    /// Eとsの考えかたは基本的同じ
    /// </summary>
    /// <param name="cells"></param>
    /// <returns></returns>
    public static List<CellManager> FILTER_E(List<CellManager> cells)
    {
        //放弃分区的思路？
        List<CellGroup> cellGroups=new List<CellGroup>();
        foreach (var cellManager in AIManager.instance.surroundCell)
        {
            if (!cellGroups.Contains(cellManager.cellGroup) && cellManager.cellGroup!=null)
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
                    cell.nearbyUnknownCell+=1;
                }
            }
            //我们只关心自己为0的可能
        }
        float otherProb = AIManager.instance.otherProb;
        CellGroup cellGroup=new CellGroup();
        cellGroup=MergeCellGroup(cellGroups);

        //周围有有情报的吗？
        List<CellManager> near=new List<CellManager>();
        List<CellManager> others=new List<CellManager>();

        
        bool success=false;
        foreach (var cell in cells)
        {
            success = false;
            foreach (var nearbyCell in AIManager.instance.ExploreSurroundings(new List<CellManager>(){cell}))
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
        /*Debug.Log("near"+near.Count+"others"+others.Count());*/
        //near, other
        //other简单

        #region near
        //两个字典 一个用来统计概率·，一个用来看每个情况有多少个安全格子
        Dictionary<int, int> oneAppearTimesDict = new Dictionary<int, int>();
        Dictionary<int, List<CellManager>> oneAppearTimesAndSafeCellListDict = new Dictionary<int, List<CellManager>>();

        
        //真正重要的是near的周围一圈
        foreach (var nearCellManager in near)
        {
            List<CellManager> haveDataCellManager=new List<CellManager>();
            List<CellManager> surrondCellManagers = AIManager.instance.ExploreSurroundings(near);
            foreach (var cellManager in surrondCellManagers)
            {
                if (AIManager.instance.surroundCell.Contains(cellManager))
                {
                    haveDataCellManager.Add(cellManager);
                }
            }
            
            Tuple<Dictionary<int, int>,Dictionary<int, List<CellManager>>>  tuple=PSEQ_S_Caculate(cellGroup,nearCellManager);
            oneAppearTimesDict = tuple.Item1;
            oneAppearTimesAndSafeCellListDict = tuple.Item2;
            // 计算总和
            int totalValue = 0;
            foreach (var pair in oneAppearTimesDict)
            {
                totalValue += pair.Value;
            }
            /*Debug.Log("一共有多少种可能"+totalValue);*/
            int temp = 0;
            foreach (var pair in oneAppearTimesAndSafeCellListDict)
            {
                temp += pair.Value.Count;
            }
            /*Debug.Log("剩下多少"+temp);*/
            float PSEQ_E = 0;

            for (int i = 0; i <= 8; i++)
            {
                if (oneAppearTimesDict.ContainsKey(i))
                {
                    if (oneAppearTimesAndSafeCellListDict.ContainsKey(i) && oneAppearTimesAndSafeCellListDict[i].Count!=0)
                    {
                        float prob=(float)oneAppearTimesDict[i]/totalValue;
                        for (int j = 0; j < nearCellManager.nearbyUnknownCell; j++)
                        {
                            prob*=1-otherProb;
                        }
                        
                        int restSafeCell=oneAppearTimesAndSafeCellListDict[i].Count+nearCellManager.nearbyUnknownCell;
                        PSEQ_E+=prob*restSafeCell;
                        if (prob*oneAppearTimesAndSafeCellListDict[i].Count>0)
                        {
                            /*
                            Debug.Log(prob+"概率和安全格子为 "+restSafeCell+"PSEQ当前为"+PSEQ_E);
                            Debug.Log(nearCellManager.nearbyUnknownCell+"安全格子为 "+oneAppearTimesAndSafeCellListDict[i].Count);
                        */
                        }
                        break;
                    }
                }
            }
            nearCellManager.PSEQ_E = PSEQ_E;
            if (PSEQ_E>0)
            {
                /*Debug.Log("我们算出"+nearCellManager.PSEQ_S);*/
            }
        }


        #endregion
                
        
        #region other

        if (others.Count!=0)
        {

            

            foreach (var cellManager in others)
            {
                float prob_other=1;
                for (int i = 0; i < cellManager.nearbyUnknownCell; i++)
                {
                    prob_other*=1-otherProb;
                }
                cellManager.PSEQ_E= prob_other* cellManager.nearbyUnknownCell;
            }
            /*Debug.Log("其他的PSEQ为"+prob_other+" "+minValue);*/
        }
        //这一圈在外面的一圈


        #endregion

        // 找到 PSEQ_S 最大值
        float maxPSEQ_E= cells.Max(cell => cell.PSEQ_E);

        // 找到所有 PSEQ_S 等于最大值的元素
        var maxCells = cells.Where(cell => Mathf.Approximately(cell.PSEQ_E, maxPSEQ_E)).ToList();

        if (AIManager.instance.debugMode)
        {
            Debug.Log("我们最终有这么多格子: " + maxCells.Count);
        }

        return maxCells;
    }
    public static  List<CellManager>  FILTER_Q(List<CellManager> cellManagers)
    {
        foreach (var cellManager in cellManagers)
        {
            float result=0;
            foreach (float prob in FILTER_Q_Prob(cellManager))
            {

                if (prob>0)
                {
                    double temp = prob * Math.Log(prob, 2)*-1;
                    result += (float)temp;
                }
            }
        
            result=(float)Math.Round(result, 3);
            cellManager.PSEQ_Q = result;
        }
        // 找到 PSEQ_Q 最大值
        float maxPSEQ_Q = cellManagers.Max(cell => cell.PSEQ_Q);

        // 找到所有 PSEQ_Q 等于最大值的元素
        var maxCells = cellManagers.Where(cell => Mathf.Approximately(cell.PSEQ_Q, maxPSEQ_Q)).ToList();

        if (AIManager.instance.debugMode)
        {
            Debug.Log("我们最终有这么多格子: " + maxCells.Count);
        }

        return maxCells;
    }
    
    //方块的邻居中恰好𝑖i 个地雷的概率
    //计算情报熵log2
    /// <summary>
    /// 我需要准备好所有的相邻格子放进去就会自动生成概率
    /// </summary>
    /// <param name="cells"></param>
   public static List<float>  FILTER_Q_Prob(CellManager targetCell)
   {

       
        List<CellManager> cells = new List<CellManager>();
       //探索四周
       // 遍历周围的八个格子
       
       int x = targetCell.position.Item2;
       int y = targetCell.position.Item1;
       for (int i = -1; i <= 1; i++)
       {
           for (int j = -1; j <= 1; j++)
           {
               // 跳过中心点（cellManager 自身）
               if (i == 0 && j == 0)
               {
                   continue;
               }

               int targetX = x + i;
               int targetY = y + j;

               // 检查是否在有效的网格范围内
               if (targetX >= 0 && targetX < GameManager.instance.cellList.GetLength(1) &&
                   targetY >= 0 && targetY < GameManager.instance.cellList.GetLength(0))
               {
                   // 获取目标格子
                   CellManager target = GameManager.instance.cellList[targetY, targetX];

                   // 检查目标格子是否符合条件
                   if (!target.isOpen)
                   {
                       //不大开的放进去
                       cells.Add(target);
                   }
               }
           }
       }
        
       if (targetCell.position.Item2==2&&targetCell.position.Item1==1)
       {
           foreach (var target in cells)
           {
               /*
               Debug.Log("坐标是"+target.position.Item2+" "+ target.position.Item1+"概率是"+target.Probobability);
                */
           }
       }
        
        //首先，周围有几个？
        int surrondCell=cells.Count;

        Dictionary<int, List<int[]>> dict = GenerateLists(surrondCell);
        List<float> probList=new List<float>();
        for (int i = 0; i < surrondCell; i++)
        {
            //这个是大的概率，有i个1的概率
            float allProb=0;
            //现在是一个list，里面是所有包含i个1的list


            for (int j = 0; j < dict[i].Count; j++)
            {
                //这个是每个情况具体的概率
                float tempProb=1;
                for (int k = 0; k < surrondCell; k++)
                {
                    if ( dict[i][j][k]==1)
                    {
                        tempProb *= cells[k].Probobability;
                    }
                    else
                    {
                        tempProb *= (1- cells[k].Probobability);
                    }
                
                }
                tempProb=(float)Math.Round(tempProb, 3);
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

        PSEQ.FILTER_S(guessList);
            
        foreach (var cell in GameManager.instance.cellList)
        {
            cell.cellModel.GetComponent<SingleCellDisplay>().prob.SetActive(false);
            if (!cell.isOpen&&cell.PSEQ_S!=0)
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
            if (!cell.isOpen&&cell.PSEQ_E!=0)
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
        List<CellManager> tempList=new List<CellManager>();
        foreach (var cell in GameManager.instance.cellList)
        {
            tempList.Add(cell);
        }
        FILTER_Q(tempList);

        foreach (var cell in GameManager.instance.cellList)
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
            Debug.Log("isBoom?"+cellGroup.solutions[solutionIndex][i]);
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
