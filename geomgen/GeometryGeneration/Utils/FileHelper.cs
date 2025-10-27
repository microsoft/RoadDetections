// <copyright file="FileHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class FileHelper
    {
        public static IEnumerable<string> Transpose(List<string> data)
        {
            var rows = new List<string[]>();
            foreach (var item in data)
            {
                rows.Add(item.Split('\t'));
            }

            var res = new List<string>();
            int n = rows.Max(row => row.Length);
            for (int i = 0; i < n; i++)
            {
                var cur = new string[rows.Count];
                for (int j = 0; j < rows.Count; j++)
                {
                    if (i < rows[j].Length)
                    {
                        cur[j] = rows[j][i];
                    }
                }

                res.Add(string.Join("\t", cur));
            }

            return res;
        }

        public static Dictionary<string, int> GetHeader(string[] columns)
        {
            var header = new Dictionary<string, int>();
            for (int i = 0; i < columns.Length; i++)
            {
                header.Add(columns[i], i);
            }

            return header;
        }

        public static void LoadDataFromString(string input, char separator, out Dictionary<string, int> header, out string[] data)
        {
            header = new Dictionary<string, int>();
            var res = new List<string>();
            foreach (var item in input.Split(separator))
            {
                var (key, value) = item.Split('=');
                header.Add(key, header.Count);
                res.Add(value);
            }

            data = res.ToArray();
        }

        public static void LoadFileWithHeader(string filePath, out Dictionary<string, int> header, out List<string[]> data)
        {
            header = null;
            data = new List<string[]>();
            foreach (var line in File.ReadLines(filePath))
            {
                var tags = line.Split('\t');
                if (header == null)
                {
                    header = GetHeader(tags);
                    continue;
                }

                data.Add(tags);
            }
        }

        public static void LoadFile(string filePath, string[] columnNames, out Dictionary<string, int> header, out List<string[]> data)
        {
            header = GetHeader(columnNames);

            data = new List<string[]>();
            foreach (var line in File.ReadLines(filePath))
            {
                var tags = line.Split('\t');
                data.Add(tags);
            }
        }

        public static Dictionary<string, string> LoadDictionary(string filePath, string keyColumn, string valueColumn)
        {
            LoadFileWithHeader(filePath, out var header, out var data);
            var res = new Dictionary<string, string>();
            foreach (var item in data)
            {
                var key = item[header[keyColumn]];
                var value = item[header[valueColumn]];

                if (res.ContainsKey(key) && res[key] == value)
                {
                    continue;
                }

                res.Add(key, value);
            }

            return res;
        }

        public static Dictionary<T, string[]> LoadDictionary<T>(string filePath, string[] columnNames, Func<string[], T> keySelector, out Dictionary<string, int> header, bool skipDuplicates = false)
        {
            header = GetHeader(columnNames);

            var res = new Dictionary<T, string[]>();
            foreach (var line in File.ReadLines(filePath))
            {
                var tags = line.Split('\t');
                var key = keySelector(tags);
                if (skipDuplicates && res.ContainsKey(key))
                {
                    continue;
                }

                res.Add(key, tags);
            }

            return res;
        }

        public static Dictionary<T1, T2> LoadDictionary<T1, T2>(string filePath, Func<string[], T1> keySelector, Func<string[], T2> valueSelector, bool skipDuplicates = false)
        {
            var res = new Dictionary<T1, T2>();
            foreach (var line in File.ReadLines(filePath))
            {
                var tags = line.Split('\t');
                var key = keySelector(tags);
                if (skipDuplicates && res.ContainsKey(key))
                {
                    continue;
                }

                res.Add(key, valueSelector(tags));
            }

            return res;
        }

        public static void Output(Dictionary<string, int> header, IEnumerable<string[]> data, string path)
        {
            Output(header.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key), data, path);
        }

        public static void Output(IEnumerable<string> header, IEnumerable<string[]> data, string path)
        {
            File.WriteAllLines(path, new[] { string.Join("\t", header) });
            File.AppendAllLines(path, data.Select(items => string.Join("\t", items)));
        }
    }
}
