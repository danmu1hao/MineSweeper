using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SlowMethod : MonoBehaviour
{
    //1 1 1 0 2
    //0 1 1 1 3
    // Start is called before the first frame update
    List<List<int>> result;
    void Start()
    {
        // 示例输入矩阵
        int[,] matrix = {
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 5 },

        };


        // 求解并输出结果
        // 创建一个 Stopwatch 实例
        Stopwatch stopwatch = new Stopwatch();

        // 开始计时
        stopwatch.Start();

        // 这里放置你要测量时间的代码
        List<List<int>> result = SolveLinearSystem(matrix);

        // 停止计时
        stopwatch.Stop();

        // 输出执行时间（以毫秒为单位）
        Debug.Log($"slow method  执行时间: {stopwatch.ElapsedMilliseconds} 毫秒");

        
        
        
        Debug.Log("slow method 所有满足条件的解为：");
        foreach (var solution in result)
        {
            Debug.Log($"({string.Join(", ", solution)})");
        }
    }

    static List<List<int>> SolveLinearSystem(int[,] matrix)
    {
        float time = 0;
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1) - 1; // 列数减去最后一列常数项
        List<List<int>> solutions = new List<List<int>>();

        // 枚举所有可能的二进制组合
        int numVariables = cols; // 变量数量等于矩阵列数减1
        int numCombinations = (int)Math.Pow(2, numVariables); // 二进制组合总数

        for (int combination = 0; combination < numCombinations; combination++)
        {
            // 将当前组合转换为二进制，作为变量的值
            List<int> variables = new List<int>();
            for (int i = 0; i < numVariables; i++)
            {
                int value = (combination >> i) & 1; // 提取二进制值
                variables.Add(value);
            }

            // 检查该组合是否满足所有方程
            if (IsValidSolution(matrix, variables))
            {
                solutions.Add(variables);
            }
        }

        return solutions;
    }

    static bool IsValidSolution(int[,] matrix, List<int> variables)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1) - 1; // 列数减去最后一列常数项

        // 遍历每一个方程
        for (int i = 0; i < rows; i++)
        {
            int sum = 0;
            for (int j = 0; j < cols; j++)
            {
                sum += matrix[i, j] * variables[j];
            }

            // 检查方程是否成立
            if (sum != matrix[i, cols])
            {
                return false; // 如果不满足，返回false
            }
        }

        return true; // 如果所有方程都满足，返回true
    }
}
