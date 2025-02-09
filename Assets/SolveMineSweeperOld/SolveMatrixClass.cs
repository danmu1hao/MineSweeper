using System.Collections.Generic;
using UnityEngine;

class SolveMatrixClass 
{
    // 解决矩阵方程，找到所有可能的解
   public static List<int[]> SolveMatrix(int[,] matrix)
    {
        int rowLength = matrix.GetLength(0);
        int colLength = matrix.GetLength(1) - 1;  // 列数减去最后一列常数项

        // 初始化解的列表
        List<int[]> solutions = new List<int[]>();

        // 获取所有自由变量的组合
        List<int[]> freeVarCombinations = GetFreeVarCombinations(matrix, rowLength, colLength);
        // 使用 Debug.Log 来调试输出每个自由变量组合
        foreach (var variable in freeVarCombinations)
        {
            Debug.Log($"组合: {string.Join(", ", variable)}");
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
                int sum = matrix[row, colLength];  // 方程右侧的常数
                Debug.Log("row is "+row+"leading var"+leadingVar+" "+"sum is " +sum);
                // 计算已知变量的和
                for (int col = leadingVar + 1; col < colLength; col++)
                {
                    Debug.Log(col + "  " + freeVars[col]);
                    if (freeVars[col] != -1)
                    {
                        
                        sum -= freeVars[col];
                    }
                }
                Debug.Log("now sum is " +sum);
                // 唯一确定当前行的变量
                if (sum == 0 || sum == 1)
                {
                    freeVars[leadingVar] = sum;
                }
                else
                {
                    Debug.Log("不符合要求");
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

        return solutions;
        
        //确认完毕
    }

    // 获取底部的自由变量组合
   public static List<int[]> GetFreeVarCombinations(int[,] matrix, int rows, int cols)
    {
        List<int[]> combinations = new List<int[]>();
        int freeVarStart = 0;

        // 找到最后一行的第一个非零元素，作为自由变量的起点
        for (int col = 0; col < cols; col++)
        {
            if (matrix[rows - 1, col] != 0)  // 注意这里的rows - 1，表示最后一行
            {
                freeVarStart = col;
                break;
            }
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

        return combinations;
    }

    // 获取每行的主导变量（第一个非零元素的位置）
   static int GetLeadingVar(int[,] matrix, int row)
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
}
