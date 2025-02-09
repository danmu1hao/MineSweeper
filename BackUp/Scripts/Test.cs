using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        // 假设自由变量数量 n
        int n = 3; // 你可以修改n的值来测试不同的情况
        List<int[]> combinations = GenerateBinaryCombinations(n);

        // 输出所有生成的组合
        foreach (var combination in combinations)
        {
            Debug.Log($"组合: {string.Join(", ", combination)}");
        }
    }

    // 生成所有 2^n 的二进制组合
    List<int[]> GenerateBinaryCombinations(int n)
    {
        List<int[]> combinations = new List<int[]>();

        // 总的组合数量是 2^n
        int numCombinations = 1 << n;

        // 遍历所有组合
        for (int i = 0; i < numCombinations; i++)
        {
            int[] combination = new int[n];  // 用来存储当前组合

            // 将i转换为二进制，并将每一位存入combination数组
            for (int j = 0; j < n; j++)
            {
                combination[n - j - 1] = (i >> j) & 1;  // 获取i的第j位
            }

            combinations.Add(combination);  // 将当前组合加入列表
        }

        return combinations;
    }
}
