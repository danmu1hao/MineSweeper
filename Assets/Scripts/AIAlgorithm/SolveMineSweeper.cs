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

    }
    public void PrintMatrix(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            string row = "";  // 一行のデータを連結するための文字列
            for (int j = 0; j < cols; j++)
            {
                row += $"{matrix[i, j]} ";  // 各要素を行に追加
            }

            if (debugMode)
            {
                Debug.Log(row);  // 完成した行を出力
            }
        }
        /*Debug.Log("出力完了");*/
    }

public Tuple<List<CellManager>, List<int[]>> SolveMineSweeperMethodNew(List<Tuple<List<CellManager>, int>> cellEquation)
{
    TimerUtility.StartTimer();
    #region step1: List<Tuple<List<CellManager>, int>> -> int[,]

    List<int[]> solutions = new List<int[]>();
    List<CellManager> allSurrondCellManagers = new List<CellManager>();

    // ステップ1: allCellManagerリストを生成し、各SingleCellManagerにインデックスを割り当て
    allSurrondCellManagers = GenerateAllCellManager(cellEquation);

    // ステップ2: cellEquationを行列形式に変換
    int[,] matrix = ConvertToMatrix(cellEquation, allSurrondCellManagers);

    string matrixStr = ConvertMatrixToString(matrix);
    if (matrixDataList != null)
    {
        matrixDataList.Add(matrixStr);
    }
    else
    {
        /*Debug.LogWarning("バグ発生");*/
    }
    matrixDataList.Add("step 1 完了");
    PrintMatrix(matrix);
    #endregion
    TimerUtility.StopAndLog("step1");
    
    TimerUtility.StartTimer();
    List<int[]> solveList = MatrixSolver.Solve(matrix, allSurrondCellManagers);
    if (solveList==null)
    {
        GameManager.instance.GameReStart();
        return null;
    }
    /*List<int[]> solveList = SolveMatrixClass.SolveMatrix(matrix);*/
    TimerUtility.StopAndLog("step2");
    TimerUtility.StartTimer();
    // 行列を出力
    if (debugMode)
    {
        PrintMatrix(matrix);
    }

    // List<int[]>に変換
    solutions = solveList.Select(list => list.ToArray()).ToList();

    // 解を出力
    if (solutions.Count == 0)
    {
        if (debugMode)
        {
            Debug.Log("解が見つかりませんでした。");
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

    // 各行に含まれる1の数を集計（数量・出現回数）
    oneCountSummary = CountOnes(solutions);
    // 集計結果を出力
    foreach (var pair in oneCountSummary)
    {
        if (debugMode)
        {
            Debug.Log(pair.Key + " 個の1が " + pair.Value + " 回出現しました");
        }
        matrixDataList.Add(pair.Key + " 個の1が " + pair.Value + " 回出現しました");
    }

    if (solutions.Count != 0)
    {
        if (debugMode)
        {
            Debug.LogWarning("バグ!!!");
        }
    }

    float[] probaList = CalculateClassProbabilities(solutions);

    if (debugMode)
    {
        Debug.Log(probaList.Length + " と解の長さ: " + allSurrondCellManagers.Count);
    }

    for (int i = 0; i < probaList.Length; i++)
    {
        allSurrondCellManagers[i].Probobability = probaList[i];
    }
    TimerUtility.StopAndLog("step3");
    return new Tuple<List<CellManager>, List<int[]>>(allSurrondCellManagers, solutions);
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

        //阶梯矩阵
        matrix = GaussianElimination(matrix);
        
        return matrix;
    }






    #endregion







    #region 新しいマトリックス解決法
// 假设 GenerateCabSet 里有个 GenerateSets 方法能生成
// 长度 = length 的 0/1 数组组合，且里面 1 的个数 = targetOnes
public static class GenerateCabSet
{
    public static List<int[]> GenerateSets(int length, int targetOnes)
    {
        // 你自己已有实现，这里只是举个例子
        // 返回所有长度为 length 的0/1组合，且 1 的数量 = targetOnes
        List<int[]> results = new List<int[]>();
        GenerateSetsRecursive(new int[length], 0, targetOnes, results);
        return results;
    }

    private static void GenerateSetsRecursive(int[] current, int index, int targetOnes, List<int[]> results)
    {
        if (index == current.Length)
        {
            // 检查是否满足1的数量
            int sum = 0;
            foreach (var val in current) sum += val;
            if (sum == targetOnes)
            {
                // 注意要克隆，否则后面递归会改动
                results.Add((int[])current.Clone());
            }
            return;
        }

        // 两种分支：给当前位置赋0 or 1
        // 赋0
        current[index] = 0;
        GenerateSetsRecursive(current, index + 1, targetOnes, results);

        // 赋1
        current[index] = 1;
        GenerateSetsRecursive(current, index + 1, targetOnes, results);
    }
}

public class MatrixSolver
{

    public static List<int[]> Solve(int[,] matrix, List<CellManager> allSurrondCellManagers)
    {
        // -1 を使用して「未知」を表します
        int colCount = matrix.GetLength(1) - 1;  // 最後の列は目標値のため、solver には含まれません
        int[] solver = new int[colCount];
        for (int i = 0; i < colCount; i++)
        {
            solver[i] = -1;  // まだ 0 か 1 かが確定していないことを示す
        }

        // 確率が 0.999 を超える場合は 1 とし、0.001 を下回る場合は 0 とする。それ以外は -1 のまま保持
        for (int i = 0; i < colCount; i++)
        {
            double prob = allSurrondCellManagers[i].Probobability;
            if (prob > 0.999)
            {
                solver[i] = 1;  
            }
            else if (prob < 0.001)
            {
                solver[i] = 0;  
            }
            // それ以外は -1 のまま保持
        }

        // 解のリストを準備、初期状態では 1 つの初期解のみを含む
        List<int[]> solveList = new List<int[]>
        {
            solver
        };

        // 各行を処理し、それぞれの行の制約条件を現在の解リストに適用し、新しい解リストを生成する
        for (int row = 0; row < matrix.GetLength(0); row++)
        {
            solveList = ProcessSolveList(matrix, row, solveList);
            if(solveList.Count > 100000)
            {
                return null;
            }
        }

        // 最終的な solveList にはすべての行を満たす解が格納される
        return solveList;
    }
    /// <summary>
    /// 针对特定的行，把已有的解列表进行拓展、过滤，生成新的解列表
    /// </summary>
    private static List<int[]> ProcessSolveList(int[,] matrix, int row, List<int[]> solveList)
    {
        List<int[]> temp = new List<int[]>();

        foreach (var partialSolve in solveList)
        {
            // 针对这一行处理 partialSolve，可能产生若干新的可行解
            List<int[]> newSolutions = ProcessRow(matrix, row, partialSolve);
            temp.AddRange(newSolutions);
        }

        return temp;
    }

    /// <summary>
    /// 针对某一行，给定一个「部分解」，尝试生成符合这一行约束的新解
    /// </summary>
    private static List<int[]> ProcessRow(int[,] matrix, int row, int[] solve)
    {
        int cols = matrix.GetLength(1) - 1; // 最后一列是目标值
        int targetSum = matrix[row, cols];  // 这一行的目标值
        int knownSum = 0;                   // 已知变量累加出的和
        List<int> freeVars = new List<int>(); // 还未知的列(且该行该列系数=1)

        // 先把确定为1的列加进 knownSum，确定为0的略过，-1 且 matrix[row,i]=1 的记为自由变量
        for (int i = 0; i < cols; i++)
        {
            if (solve[i] == 1)
            {
                // 这一列确定为1，那么它贡献 matrix[row, i] * 1
                knownSum += matrix[row, i];
            }
            else if (solve[i] == 0)
            {
                // 确定为0，对 knownSum 没影响
            }
            else if (solve[i] == -1 && matrix[row, i] == 1)
            {
                // -1 => 未知 且 系数=1 => 这列对这一行方程有贡献，但尚未确定
                freeVars.Add(i);
            }
            // 如果 solve[i] == -1 但 matrix[row, i] == 0，则对本行没贡献，也不做 freeVars
        }

        // 计算剩余需要多少列为1
        int remainingSum = targetSum - knownSum;

        // 如果 remainingSum 超过可用的 freeVars 数量，或者小于0，说明不可能满足
        if (remainingSum < 0 || remainingSum > freeVars.Count)
        {
            return new List<int[]>(); // 直接返回空
        }

        // 针对 freeVars.Count 个未知列，要选出 remainingSum 个为1，其余为0
        // 所以去生成所有满足“1 的个数 = remainingSum”的 0/1 组合
        List<int[]> combinations = GenerateCabSet.GenerateSets(freeVars.Count, remainingSum);

        // 逐个组合去构造新的解
        List<int[]> result = new List<int[]>();
        foreach (var comb in combinations)
        {
            // 先 copy 一份原先的 solve
            int[] newSolve = (int[])solve.Clone();

            // 再把 comb 里的 0/1 填到 freeVars 对应的位置上
            for (int idx = 0; idx < freeVars.Count; idx++)
            {
                int colIndex = freeVars[idx];
                newSolve[colIndex] = comb[idx];
            }

            result.Add(newSolve);
        }

        return result;
    }
}



    #endregion
#region How many Mine?

// 各行に出現する1の数を集計
Dictionary<int, int> CountOnes(List<int[]> matrix)
{
    Dictionary<int, int> oneCountSummary = new Dictionary<int, int>();

    // 各行をループ
    foreach (var row in matrix)
    {
        int oneCount = 0;

        // 現在の行で1の数をカウント
        foreach (var element in row)
        {
            if (element == 1)
            {
                oneCount++;
            }
        }

        // カウント結果を辞書に追加
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

// 行列を文字列に変換
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
        result = result.Trim() + "\n"; // 行末の余分な空白を削除し、改行を追加
    }

    return result.Trim(); // 最後の改行を削除
}

// 高斯消元算法
static int[,] GaussianElimination(int[,] matrix)
{
    int rows = matrix.GetLength(0);
    int cols = matrix.GetLength(1);

    for (int i = 0; i < Mathf.Min(rows, cols - 1); i++) // cols-1 因为最后一列是常数列
    {
        // 如果主元为0，进行行交换
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

        // 消去主元下方的元素
        for (int k = i + 1; k < rows; k++)
        {
            if (matrix[k, i] != 0)
            {
                int factor = matrix[k, i] / matrix[i, i];
                for (int j = i; j < cols; j++)
                {
                    matrix[k, j] -= factor * matrix[i, j];
                }
            }
        }
    }

    return matrix;
}
static void SwapRows(int[,] matrix, int row1, int row2)
{
    int cols = matrix.GetLength(1);
    for (int i = 0; i < cols; i++)
    {
        int temp = matrix[row1, i];
        matrix[row1, i] = matrix[row2, i];
        matrix[row2, i] = temp;
    }
}

// List<string> を txt ファイルに書き込む
void WriteListToTxt(List<string> stringList, string fileName)
{
    if (writeText)
    {
        /*string path = Application.dataPath + "/" + Random.Range(0, 1111111) + " " + fileName;

        // StreamWriterを使用して List<string> をファイルに書き込む
        using (StreamWriter writer = new StreamWriter(path, false))
        {
            foreach (string line in stringList)
            {
                writer.WriteLine(line);
            }
        }*/
    }
}

// List<int[]> を文字列に変換
string ConvertIntArrayListToString(List<int[]> solutions)
{
    string result = "";

    foreach (int[] row in solutions)
    {
        result += string.Join(" ", row) + "\n"; // int[] を空白区切りの文字列に変換し、改行を追加
    }

    return result.Trim(); // 最後の改行を削除
}

#endregion

// 各列の確率を計算
float[] CalculateClassProbabilities(List<int[]> solutions)
{
    int rows = solutions.Count; // 解の数
    int cols = 0;
    if (solutions.Count >= 1)
    {
        cols = solutions[0].Length; // 列の数
    }
    float[] probabilities = new float[cols];

    // 各列で1の出現回数をカウント
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
        probabilities[col] = (float)count / rows; // 1の出現確率を計算
    }

    return probabilities;
}


     
}
