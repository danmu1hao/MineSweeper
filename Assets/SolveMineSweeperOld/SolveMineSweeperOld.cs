/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SolveMineSweeperOld : MonoBehaviour
{
    //判断debug
    public bool debugMode;
    //自动探索
    public bool autoMode;
    public Dictionary<int, int> oneCountSummary;
    
    /*List<Tuple<List<CellManager>, int>> cellEquation;#1#
    List<string> matrixDataList;

    /// <summary>
    /// 所有的解
    /// </summary>
    public static List<int[]> solutions;
    public static List<CellManager> allSurrondCellManager;
    // Start is called before the first frame update
    void Awake()
    {
        matrixDataList = new List<string>();
        oneCountSummary = new Dictionary<int, int>();

        
        /#1#/ 示例矩阵
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
        PrintMatrix(result);#1#
        
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
        };#1#


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
        /*Debug.Log("print finish");#1#
    }

    

    public void SolveMineSweeperMethod(List<Tuple<List<CellManager>, int>> cellEquation)
    {
        
        #region step1  List<Tuple<List<CellManager>, int>>  -> int[,]

        // 第一步：生成 allCellManager 列表，并为每个 SingleCellManager 分配索引
        allSurrondCellManager = GenerateAllCellManager(cellEquation);

        // 第二步：将 cellEquation 转换为矩阵形式
        int[,] matrix = ConvertToMatrix(cellEquation, allSurrondCellManager);

        string matrixStr=ConvertMatrixToString(matrix);
        if (matrixDataList!=null)
        {
            matrixDataList.Add(matrixStr);
        }
        else
        {
            /*Debug.LogWarning("bug");#1#
        }
        matrixDataList.Add("step 1 over");
        
        PrintMatrix(matrix);
        
        /*Debug.Log("step 1 over");#1#


        #endregion

        #region step2

        matrix = PerfectStaircaseMatrix(matrix);
        matrixStr=ConvertMatrixToString(matrix);
        matrixDataList.Add(matrixStr);
        matrixDataList.Add("step 2 over");
        PrintMatrix(matrix);

        #endregion
        
        
        solutions = SolveMatrix(matrix);
        
        // 将 List<int[]> 转换为 string
        string solutionsAsString = ConvertIntArrayListToString(solutions);

        // 添加到 matrixDataList
        matrixDataList.Add(solutionsAsString);
        matrixDataList.Add("step 3 over");
        // 输出矩阵
        PrintMatrix(matrix);
        
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
            Debug.Log(probaList.Length+"and solution length"+allSurrondCellManager.Count);
        }

        for (int i = 0; i < probaList.Length; i++)
        {
            allSurrondCellManager[i].Probobability = probaList[i];
        }
        

        
        WriteListToTxt(matrixDataList,"testData.txt");
    }
    
    #region Step1

        // 生成不重复的 allCellManager 列表，并为每个 CellManager 分配索引
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
                    cell.index = allCellManager.Count;  // 设置索引
                    allCellManager.Add(cell);  // 加入allCellManager列表
                    cellDict[cell] = cell.index;  // 存入字典，避免重复
                }
                else
                {
                    // 更新cell的索引为已存在的index
                    cell.index = cellDict[cell];
                }
            }
        }

        return allCellManager;
    }
    // 将 cellEquation 转换为矩阵
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
                matrix[i, cell.index] = 1;  // 在对应的列设置为1
            }

            // 设置常数项（方程右边的值）
            matrix[i, cols - 1] = equation.Item2;
        }

        return matrix;
    }

    // 打印矩阵




    #endregion

    #region Step2
    // 高斯消元算法（只允许乘法和减法）
    // 高斯消元算法（只允许乘法和减法）
    /*
    static int[,] GaussianElimination(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < Math.Min(rows, cols); i++)
        {
            // 如果主元为0，则向下交换行
            if (matrix[i, i] == 0)
            {
                for (int k = i + 1; k < rows; k++)
                {
                    if (matrix[k, i] != 0)
                    {
                        SwapRows(matrix, i, k);
                        break;
                    }
                }
            }

            // 消去主元下方的元素，不进行归一化
            for (int k = i + 1; k < rows; k++)
            {
                if (matrix[k, i] != 0) // 确保当前行需要消去
                {
                    int factor = matrix[k, i] / matrix[i, i];
                    for (int j = i; j < cols; j++)
                    {
                        matrix[k, j] = matrix[k, j] - factor * matrix[i, j];
                    }
                }
            }
        }

        return matrix;
    }
    #1#

    // 行交换




// 列交换方法


// 删除行的方法
    void RemoveRow(ref int[,] matrix, int rowToRemove)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        int[,] newMatrix = new int[rows - 1, cols];

        int newRow = 0;
        for (int i = 0; i < rows; i++)
        {
            if (i == rowToRemove) continue; // 跳过需要删除的行

            for (int j = 0; j < cols; j++)
            {
                newMatrix[newRow, j] = matrix[i, j];
            }
            newRow++;
        }

        matrix = newMatrix; // 将原矩阵替换为新矩阵
    }
    // 输出矩阵


    #endregion

    #region checkMatric

        // 完美阶梯矩阵生成函数
    int[,] PerfectStaircaseMatrix(int[,] matrix)
    {
        // 第二步：移除零行（如果有）
        matrix = RemoveZeroRows(matrix);
        
        // 第一步：进行高斯消元
        //待定
        matrix = GaussianEliminationWithColumnSwapNew(matrix);

        // 第二步：移除零行（如果有）
        matrix = RemoveZeroRows(matrix);

        // 第三步：检查阶梯形态，交换列确保前导1的位置正确
        matrix = EnsurePerfectStaircase(matrix);

        
        return matrix;
    }


    
    
    // 移除全零行
    int[,] RemoveZeroRows(int[,] matrix)
    {
        List<int[]> nonZeroRows = new List<int[]>();
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            bool isZeroRow = true;
            for (int j = 0; j < cols - 1; j++)  // 不检查常数列
            {
                if (matrix[i, j] != 0)
                {
                    isZeroRow = false;
                    break;
                }
            }
            if (!isZeroRow)
            {
                int[] row = new int[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = matrix[i, j];
                }
                nonZeroRows.Add(row);
            }
        }

        int[,] newMatrix = new int[nonZeroRows.Count, cols];
        for (int i = 0; i < nonZeroRows.Count; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                newMatrix[i, j] = nonZeroRows[i][j];
            }
        }

        return newMatrix;
    }
// 检查并确保完美阶梯型
    int[,] EnsurePerfectStaircase(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        // 记录最左边的非0数的位置
        int n = CountLeadingZeros(matrix, 0);

        for (int row = 1; row < rows; row++)
        {
            // 检查当前行第一个非零元素的位置
            int currentLeadingZeroIndex = CountLeadingZeros(matrix, row);

            // 如果当前行的非零位置和上一行的 n 差距大于 1，则交换列
            if (currentLeadingZeroIndex - n > 1)
            {
                // 交换列：交换当前行第一个非零元素的列和 n + 1 列
                SwapColumns(matrix, currentLeadingZeroIndex, n + 1);

                // 更新 n 为 n+1
                n++;
            }
            else
            {
                // 如果不需要交换列，更新 n 为当前行的 leading zero 位置
                n = currentLeadingZeroIndex;
            }
        }

        return matrix;
    }

    // 计算当前行前导零的个数
    int CountLeadingZeros(int[,] matrix, int row)
    {
        int cols = matrix.GetLength(1);
        for (int col = 0; col < cols - 1; col++)
        {
            if (matrix[row, col] != 0)
            {
                return col;
            }
        }
        return cols - 1;
    }

    // 交换矩阵中的两列
    void SwapColumns(int[,] matrix, int col1, int col2)
    {
        int rows = matrix.GetLength(0);
        for (int i = 0; i < rows; i++)
        {
            int temp = matrix[i, col1];
            matrix[i, col1] = matrix[i, col2];
            matrix[i, col2] = temp;
        }
        // 交换 CellManager 列表中的索引
        if (col1 < allSurrondCellManager.Count && col2 < allSurrondCellManager.Count)
        {
            // 交换 allCellManager 中对应的 col1 和 col2 索引
            CellManager tempManager = allSurrondCellManager[col1];
            allSurrondCellManager[col1] = allSurrondCellManager[col2];
            allSurrondCellManager[col2] = tempManager;
        }
    }

    // 交换矩阵中的两行
    /*void SwapRows(int[,] matrix, int row1, int row2)
    {
        int cols = matrix.GetLength(1);
        for (int i = 0; i < cols; i++)
        {
            int temp = matrix[row1, i];
            matrix[row1, i] = matrix[row2, i];
            matrix[row2, i] = temp;
        }
    }#1#

     #endregion

     #region MyRegion


     

         #endregion
     
    #region step3

        // 解决矩阵方程，找到所有可能的解
    List<int[]> SolveMatrix(int[,] matrix)
    {
        int rowLength = matrix.GetLength(0);
        int colLength = matrix.GetLength(1) - 1;  // 列数减去最后一列常数项

        // 初始化解的列表
        List<int[]> solutions = new List<int[]>();

        // 获取所有自由变量的组合
        List<int[]> freeVarCombinations = GetFreeVarCombinationsNew(matrix, rowLength, colLength);
        // 使用 Debug.Log 来调试输出每个自由变量组合
        if (debugMode)
        {
            foreach (var variable in freeVarCombinations)
            {
                Debug.Log($"组合: {string.Join(", ", variable)}");
            }
        }
        
        //问题出现在这里，他没用到freeVars，正确做法应该是转换
        
        
        // 对每个自由变量的组合，求出完整的解
        foreach (var freeVars in freeVarCombinations)
        {
            bool validSolution = true;
            // 从下往上推导
            for (int row = rowLength - 1; row >= 0; row--)
            {
                int leadingVar = GetLeadingVar(matrix, row);
                if (leadingVar == -1)
                {
                    continue;
                }
                int sum = matrix[row, colLength];  // 方程右侧的常数
                if (debugMode)
                {
                    Debug.Log("row is "+row+"leading var"+leadingVar+" "+"sum is " +sum);
                }
                // 计算已知变量的和
                for (int col = leadingVar + 1; col < colLength; col++)
                {

                    if (freeVars[col] != -1)
                    {
                        
                        sum -= freeVars[col] * matrix[row, col];
                    }
                }
                
                sum/= matrix[row, leadingVar];
                if (debugMode)
                {
                    Debug.Log("now sum is " +sum);
                }
                // 唯一确定当前行的变量
                if (sum == 0 || sum == 1)
                {
                    freeVars[leadingVar] = sum;
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.Log("不符合要求");
                    }
                    // 如果结果不是0或1，跳过该自由变量组合
                    validSolution = false;
                    break;
                }
            }
            
            // 如果所有变量都已确定，保存解
            if (validSolution)
            {
                solutions.Add(freeVars);
            }
        }
        PrintMatrix(matrix);
        return solutions;
        
        //确认完毕
    }

    // 获取底部的自由变量组合
    List<int[]> GetFreeVarCombinations(int[,] matrix, int rows, int cols)
    {
        List<int[]> combinations = new List<int[]>();
        int freeVarStart = 0;

        /#1#/ 找到最后一行的第一个非零元素，作为自由变量的起点
        for (int col = 0; col < cols; col++)
        {
            if (matrix[rows - 1, col] != 0)  // 注意这里的rows - 1，表示最后一行
            {
                freeVarStart = col;
                break;
            }
        }#1#
        
        // 从最后一行开始逐行向上寻找第一个不为零的行
        bool check=false;
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                // 找到该行的第一个非零元素
                if (matrix[row, col] != 0)
                {
                    freeVarStart = col;
                    check = true;
                    break;
                }
            }
            if(check){break;}
        }

        if (debugMode)
        {
            Debug.Log("初始点为"+freeVarStart);
        }
        // 0 0 1 1 1
        // 自由变量的数量 cols=5 freeVarStart=2
        int numFreeVars = cols - freeVarStart-1;
        int numCombinations = 1 << numFreeVars;

        // 生成自由变量的所有二进制组合
        for (int i = 0; i < numCombinations; i++)
        {
            int[] freeVars = new int[cols];
            for (int j = 0; j < numFreeVars; j++)
            {
                freeVars[freeVarStart +1+ j] = (i >> j) & 1;  // 二进制组合 (0 或 1)
            }
            combinations.Add(freeVars);
        }
        /*Debug.Log("一共有多少种可能"+combinations);#1#
        return combinations;
    }

    List<int[]> GetFreeVarCombinationsNew(int[,] matrix, int rows, int cols)
    {
        List<int[]> combinations = new List<int[]>();

        // 计算有效方程的数量（非零行）
        int numEquations = 0;
        for (int row = 0; row < rows; row++)
        {
            bool isNonZeroRow = false;
            for (int col = 0; col < cols; col++)  // 不检查最后一列（常数列）
            {
                if (matrix[row, col] != 0)
                {
                    isNonZeroRow = true;
                    break;
                }
            }
            if (isNonZeroRow)
            {
                numEquations++;
            }
        }

        // 计算自由变量的数量：变量数（cols - 1） - 方程数
        int numFreeVars = (cols ) - numEquations;
        int numCombinations = 1 << numFreeVars;  // 2^numFreeVars 种组合

        if (debugMode)
        {
            Debug.Log("有效方程数: " + numEquations);
            Debug.Log("自由变量数: " + numFreeVars);
        }

        //5列 自由变量3个 0 1 不管 2 3 4 设置 初始 2 =5-3
        int freeVarStart= cols - numFreeVars;

        // 生成自由变量的所有二进制组合
        for (int i = 0; i < numCombinations; i++)
        {
            int[] freeVars = new int[cols];  // 生成自由变量的组合

            // 根据二进制组合设置自由变量的值（0 或 1）
            for (int j = 0; j < numFreeVars; j++)
            {
                freeVars[freeVarStart + j] = (i >> j) & 1;  // 二进制组合 (0 或 1)
            }

            combinations.Add(freeVars);
        }

        return combinations;
    }

    
    // 获取每行的主导变量（第一个非零元素的位置）
    int GetLeadingVar(int[,] matrix, int row)
    {
        for (int col = 0; col < matrix.GetLength(1) - 1; col++)
        {

            if (matrix[row, col] !=0)
            {
                return col;
            }
        }
        return -1;  // 应该不会出现这种情况
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
            string path = Application.dataPath + "/" + fileName;

            // 使用 StreamWriter 将 List<string> 写入文件
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                foreach (string line in stringList)
                {
                    writer.WriteLine(line);
                }
            }
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

    public void autoPlay()
    {
        List<CellManager> openCellList = new List<CellManager>();
        foreach (var cellManager in allSurrondCellManager) 
        {
            if (!cellManager.isOpen)
            {
                openCellList.Add(cellManager);
            }
        }
    }
   
}
*/
