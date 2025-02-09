using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SolveMineSweeper : MonoBehaviour
{
    #region 変数

    public static SolveMineSweeper instance;
    public bool useNewSolveMethod;
    
    //判断debug
    public bool debugMode;
    //自动探索
    public bool autoMode;
    public Dictionary<int, int> oneCountSummary;
    
    /*List<Tuple<List<CellManager>, int>> cellEquation;*/
    List<string> matrixDataList;



    #endregion
    
    void Awake()
    {
        matrixDataList = new List<string>();
        oneCountSummary = new Dictionary<int, int>();
        instance = this;

        /*// 示例矩阵
        int[,] matrix = {
            { 1, 1, 1, 1, 1, 3 },
            { 0, 1, 1, 0, 0, 1 },
            { 0, 0, 0, 1, 1, 1 },
            { 1, 0, 0, 0, 0, 1 },
            { 1, 0, 1, 1, 0, 1 },
            { 0, 0, 1, 0, 0, 0 },
            { 0, 0, 1, 1, 0, 0 }
        };

        // 执行高斯消元与列交换并去除零行
        int[,] result = PerfectStaircaseMatrix(matrix);

        // 打印处理后的矩阵
        PrintMatrix(result);*/

        /*gameObject.GetComponent<Calculate>();
        CellManager cell1=new CellManager();
        CellManager cell2= new CellManager();
        CellManager cell3= new CellManager();
        CellManager cell4= new CellManager();
        CellManager cell5= new CellManager();
        CellManager cell6= new CellManager();
        // 示例的 cellEquation 数据
        cellEquation = new List<Tuple<List<CellManager>, int>>
        {
            Tuple.Create(new List<CellManager> {
                cell1,cell2,cell3
            }, 2),
            Tuple.Create(new List<CellManager> {
                cell2,cell3,cell4
            }, 2),
            Tuple.Create(new List<CellManager> {
                cell3,cell4,cell5,cell6
            }, 3),

            Tuple.Create(new List<CellManager> {
                cell4,cell5,cell6
            }, 2)
            ,

            Tuple.Create(new List<CellManager> {
                cell1,cell6
            }, 2)
        };*/


    }
    public void PrintMatrix(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            string row = "";  // 用于拼接一行的数据
            for (int j = 0; j < cols; j++)
            {
                row += $"{matrix[i, j]} ";  // 将每个元素添加到行
            }

            if (debugMode)
            {
                Debug.Log(row);  // 输出完整的一行
            }
        }
        /*Debug.Log("print finish");*/
    }
    public Tuple<List<CellManager>,List<int[]>> SolveMineSweeperMethodNew(List<Tuple<List<CellManager>, int>> cellEquation)
    {
        #region step1  List<Tuple<List<CellManager>, int>>  -> int[,]

        List<int[]> solutions=new List<int[]>();
        List<CellManager> allSurrondCellManagerGroups=new List<CellManager>();
        
        // 第一步：生成 allCellManager 列表，并为每个 SingleCellManager 分配索引
        allSurrondCellManagerGroups = GenerateAllCellManager(cellEquation);

        // 第二步：将 cellEquation 转换为矩阵形式
        int[,] matrix = ConvertToMatrix(cellEquation, allSurrondCellManagerGroups);

        string matrixStr=ConvertMatrixToString(matrix);
        if (matrixDataList!=null)
        {
            matrixDataList.Add(matrixStr);
        }
        else
        {
            /*Debug.LogWarning("bug");*/
        }
        matrixDataList.Add("step 1 over");
        PrintMatrix(matrix);
        #endregion


        List<List<int>> solveList = new List<List<int>> {
            new List<int>() // 初始为空解
        };
        for (int i = 0; i < allSurrondCellManagerGroups.Count; i++)
        {
            List<int> tempList = new List<int>();
            if (allSurrondCellManagerGroups[i].Probobability>0.999f)
            {
                tempList[i]
            }
        }
        // 逐行处理矩阵
        for (int row = 0; row < matrix.GetLength(0); row++)
        {
            solveList = MatrixSolver.ProcessSolveList(matrix, row, solveList);
        }
        // 输出矩阵
        PrintMatrix(matrix);
        // 转换为 List<int[]>
        solutions = solveList.Select(list => list.ToArray()).ToList();

        // 输出所有解
        if (solutions.Count == 0)
        {
            if (debugMode)
            {
                Debug.Log("没有找到解。");
            }
        }
        else
        {
            foreach (var solution in solutions)
            {
                if (debugMode)
                {
                    Debug.Log($"解: {string.Join(", ", solution)}");
                }
            }
        }

        
        // 统计每行1的数量    数量 次数
        oneCountSummary= CountOnes(solutions);
        // 输出统计结果
        foreach (var pair in oneCountSummary)
        {
            if (debugMode)
            {
                Debug.Log(pair.Key + " 个 1 出现了 " + pair.Value + " 次");
            }
            matrixDataList.Add(pair.Key + " 個 1　 " + pair.Value + " 回出現しました");
        }
        if (solutions.Count!=0)
        {
            if (debugMode)
            {
                Debug.LogWarning("bug!!!");
            }
        }
        float[] probaList=  CalculateClassProbabilities(solutions);


        if (debugMode)
        {
            Debug.Log(probaList.Length+"and solution length"+allSurrondCellManagerGroups.Count);
        }

        for (int i = 0; i < probaList.Length; i++)
        {
            allSurrondCellManagerGroups[i].Probobability = probaList[i];
        }

        return new Tuple<List<CellManager>, List<int[]>>(allSurrondCellManagerGroups,solutions);
    }


    
    #region CellManagerとマトリックスを生成


    /// <summary>
    /// 例えば、
    /// cell1,cell2|2
    /// cell2,cell3|1
    /// =>list{cell1,cell2,cell3}
    /// 
    /// </summary>
    /// <param name="cellEquation"></param>
    /// <returns></returns>
    public static List<CellManager> GenerateAllCellManager(List<Tuple<List<CellManager>, int>> cellEquation)
    {
        List<CellManager> allCellManager = new List<CellManager>();
        Dictionary<CellManager, int> cellDict = new Dictionary<CellManager, int>();

        foreach (var equation in cellEquation)
        {
            foreach (var cell in equation.Item1)
            {
                // 如果字典中没有此cell，就添加到allCellManager并给它分配index
                if (!cellDict.ContainsKey(cell))
                {
                    cell.indexForSolver = allCellManager.Count;  // 设置索引
                    allCellManager.Add(cell);  // 加入allCellManager列表
                    cellDict[cell] = cell.indexForSolver;  // 存入字典，避免重复
                }
                else
                {
                    // 更新cell的索引为已存在的index
                    cell.indexForSolver = cellDict[cell];
                }
            }
        }

        return allCellManager;
    }
    /// <summary>
    /// Listから、マトリックスへ
    /// </summary>
    /// <param name="cellEquation"></param>
    /// <param name="allCellManager"></param>
    /// <returns></returns>
    public static int[,] ConvertToMatrix(List<Tuple<List<CellManager>, int>> cellEquation, List<CellManager> allCellManager)
    {
        int rows = cellEquation.Count;
        int cols = allCellManager.Count + 1;  // 列数为cell数量加1（用于存储常数项）

        int[,] matrix = new int[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            var equation = cellEquation[i];

            // 遍历左边的 cell 列表
            foreach (var cell in equation.Item1)
            {
                matrix[i, cell.indexForSolver] = 1;  // 在对应的列设置为1
            }

            // 设置常数项（方程右边的值）
            matrix[i, cols - 1] = equation.Item2;
        }

        return matrix;
    }






    #endregion







    #region 新しいマトリックス解決法
    class GenerateCabSet
    {
        
        public static List<int[]> GenerateSets(int a, int b)
        {
            List<int[]> results = new List<int[]>();
            int[] current = new int[a];

            // 调用递归生成所有集合
            GenerateRecursive(current, 0, b, results);
            return results;
        }

        private static void GenerateRecursive(int[] current, int index, int remainingOnes, List<int[]> results)
        {
            // 如果已经填满数组
            if (index == current.Length)
            {
                // 检查剩余的1是否为0
                if (remainingOnes == 0)
                {
                    results.Add((int[])current.Clone());
                }
                return;
            }

            // 填充当前位为0
            current[index] = 0;
            GenerateRecursive(current, index + 1, remainingOnes, results);

            // 如果还有剩余的1，填充当前位为1
            if (remainingOnes > 0)
            {
                current[index] = 1;
                GenerateRecursive(current, index + 1, remainingOnes - 1, results);
            }
        }
    }
    class MatrixSolver
    {
        public static List<List<int>> ProcessSolveList(int[,] matrix, int row, List<List<int>> solveList)
        {
            // 临时存储新的解
            List<List<int>> temp = new List<List<int>>();

            // 遍历 solveList 中的每个解
            foreach (var solve in solveList)
            {
                // 调用之前的 ProcessRow 方法，处理当前解并得到所有可能的新解
                List<List<int>> intList = ProcessRow(matrix, row, solve);

                // 将新解添加到临时存储 temp 中
                temp.AddRange(intList);
            }

            // 返回更新后的 solveList
            return temp;
        }
        
        //input 矩阵 第几行 目前的答案
        //返回 对于目前的情况来说的所有可能
        /*
            int[,] matrix = {
                {0, 0, 1, 1, 1, 1, 0, 2}
            };

            List<int> solve = new List<int> { 1, 1, 0, 1 }; // 已知解
         */
        public static List<List<int>> ProcessRow(int[,] matrix, int row, List<int> solve)
        {
            int cols = matrix.GetLength(1) - 1; // 获取矩阵的列数，排除最后一列
            int targetSum = matrix[row, cols]; // 当前行的目标和
            int knownSum = 0; // 已知变量的和
            List<int> freeVars = new List<int>(); // 存储自由变量的索引

            // 计算已知变量的和并找到自由变量
            for (int i = 0; i < cols; i++)
            {
                if (i < solve.Count) // 如果 solve 中已有值
                {
                    knownSum += solve[i] * matrix[row, i];
                }
                else if (matrix[row, i] == 1) // 未知变量
                {
                    freeVars.Add(i);
                }
            }

            // 剩余和
            int remainingSum = targetSum - knownSum;

            // 检查剩余和是否有效
            if (remainingSum < 0 || remainingSum > freeVars.Count)
            {
                return new List<List<int>>(); // 无法满足条件，返回空列表
            }

            // 生成自由变量的所有组合
            List<int[]> combinations = GenerateCabSet.GenerateSets(freeVars.Count, remainingSum);

            // 构造返回的解列表
            List<List<int>> intList = new List<List<int>>();
            foreach (var combination in combinations)
            {
                List<int> newSolve = new List<int>(solve); // 创建 solve 的副本
                foreach (var index in freeVars)
                {
                    newSolve.Add(combination[freeVars.IndexOf(index)]);
                }
                intList.Add(newSolve);
            }

            return intList;
        }
    }

    #endregion

    #region  How many Mine?

    // 统计每行出现1的数量并进行汇总
    Dictionary<int, int> CountOnes(List<int[]> matrix)
    {
        Dictionary<int, int> oneCountSummary = new Dictionary<int, int>();

        // 遍历每一行
        foreach (var row in matrix)
        {
            int oneCount = 0;

            // 统计当前行中1的数量
            foreach (var element in row)
            {
                if (element == 1)
                {
                    oneCount++;
                }
            }

            // 将每行1的数量添加到字典中
            if (oneCountSummary.ContainsKey(oneCount))
            {
                oneCountSummary[oneCount]++;
            }
            else
            {
                oneCountSummary[oneCount] = 1;
            }
        }

        return oneCountSummary;
    }

    #endregion

    #region TestData

    public bool writeText;
    // 将矩阵转换为一个字符串
    string ConvertMatrixToString(int[,] matrix)
    {
        string result = "";
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result += matrix[i, j].ToString() + " ";
            }
            result = result.Trim() + "\n"; // 去除行末的空格并添加换行符
        }

        return result.Trim(); // 去除最后多余的换行符
    }

    // 将 List<string> 写入到 txt 文件
    void WriteListToTxt(List<string> stringList, string fileName)
    {
        if (writeText)
        {
            /*string path = Application.dataPath + "/" +Random.Range(0,1111111)+" "+ fileName;

            // 使用 StreamWriter 将 List<string> 写入文件
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                foreach (string line in stringList)
                {
                    writer.WriteLine(line);
                }
            }*/
        }
    }
    
    // 将 List<int[]> 转换为 string
    string ConvertIntArrayListToString(List<int[]> solutions)
    {
        string result = "";

        foreach (int[] row in solutions)
        {
            result += string.Join(" ", row) + "\n";  // 将 int[] 转换为用空格分隔的字符串，并换行
        }

        return result.Trim();  // 去除最后多余的换行符
    }


    #endregion
// 计算每个类的概率
    float[] CalculateClassProbabilities(List<int[]> solutions)
    { 

        int rows = solutions.Count;  // 解的数量
        int cols = 0;
        if (solutions.Count>=1)
        {
            cols = solutions[0].Length;  // 类的数量
        }
        float[] probabilities = new float[cols];

        // 统计每个类中出现1的次数
        for (int col = 0; col < cols; col++)
        {
            int count = 0;
            for (int row = 0; row < rows; row++)
            {
                if (solutions[row][col] == 1)
                {
                    count++;
                }
            }
            probabilities[col] = (float)count / rows;  // 计算该列出现1的概率
        }

        return probabilities;
    }


   
}
