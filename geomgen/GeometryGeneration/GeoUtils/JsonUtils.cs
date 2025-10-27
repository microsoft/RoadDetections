// <copyright file="JsonUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Utils;

    public static class JsonUtils
    {
        public static string GetJsonHash(string geoJson)
        {
            return BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(geoJson))).Replace("-", string.Empty);
        }

        public static string Get(string json, string name)
        {
            dynamic g = JsonConvert.DeserializeObject(json);
            return g[name].Value.ToString();
        }

        public static List<string> GetValues(string json, string nameKey, string attributeKey, params KeyValuePair<string, string>[] restrictions)
        {
            var res = new List<string>();

            dynamic g = JsonConvert.DeserializeObject(json);
            foreach (var item in g[nameKey])
            {
                if (restrictions.All(kvp => item[kvp.Key] == kvp.Value))
                {
                    res.Add(item[attributeKey].ToString());
                }
            }

            return res;
        }

        public static string GetProperty(string json, string name)
        {
            dynamic g = JsonConvert.DeserializeObject(json);
            var properties = g.properties ?? g.Properties;
            if (properties[name] == null)
            {
                return null;
            }
            else if (properties[name].HasValues == null || properties[name].HasValues == true)
            {
                return properties[name]["Value"];
            }
            else
            {
                return properties[name];
            }
        }

        public static string SerializeDictionary(Dictionary<string, string> dic)
        {
            return JsonConvert.SerializeObject(dic);
        }

        public static Dictionary<string, string> DeserializeDictionary(string data)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
        }

        public static string AddProperty(string json, string name, string value)
        {
            dynamic g = JsonConvert.DeserializeObject(json);

            if (g.properties[name] != null)
            {
                g.properties.Remove(name);
            }

            g.properties.Add(name, value);

            return JsonConvert.SerializeObject(g);
        }

        public static string RemoveProperty(string json, string name)
        {
            dynamic g = JsonConvert.DeserializeObject(json);

            if (g.properties[name] != null)
            {
                g.properties.Remove(name);
            }

            return JsonConvert.SerializeObject(g);
        }

        public static bool HasProperty(string json, string name)
        {
            dynamic g = JsonConvert.DeserializeObject(json);
            return g.properties[name] != null;
        }

        public static List<string> GetProperties(string json)
        {
            var res = new List<string>();

            dynamic g = JsonConvert.DeserializeObject(json);

            foreach (var key in g.properties)
            {
                res.Add((string)key.Name);
            }

            return res;
        }

        public static double? GetAveragePropertyValue(string json, string propertyKey, string attributeKey, params KeyValuePair<string, string>[] restrictions)
        {
            var values = GetPropertyValues(json, propertyKey, attributeKey, restrictions);
            if (values == null || values.Count == 0)
            {
                return null;
            }

            return values.Average(x => double.Parse(x));
        }

        public static List<string> GetCombinePropertyValue(string json, string propertyKey, string attributeKey, params KeyValuePair<string, string>[] restrictions)
        {
            var values = GetPropertyValues(json, propertyKey, attributeKey, restrictions);
            if (values == null || values.Count == 0)
            {
                return new List<string>();
            }

            return GetPropertyValues(json, propertyKey, attributeKey, restrictions);
        }

        private static List<string> GetPropertyValues(string json, string propertyKey, string attributeKey, params KeyValuePair<string, string>[] restrictions)
        {
            var res = new List<string>();

            dynamic g = JsonConvert.DeserializeObject(json);

            if (g.properties[propertyKey] == null)
            {
                return res;
            }

            foreach (var item in g.properties[propertyKey])
            {
                if (item[attributeKey] == null)
                {
                    continue;
                }

                bool skip = false;
                foreach (var (key, value) in restrictions)
                {
                    if (item[key] != value)
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                if (item[attributeKey].HasValues == null || item[attributeKey].HasValues == true)
                {
                    res.Add(item[attributeKey]["Value"].ToString());
                }
                else
                {
                    res.Add(item[attributeKey].ToString());
                }
            }

            return res;
        }
    }
}
