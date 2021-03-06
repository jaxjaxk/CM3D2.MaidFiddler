﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using CM3D2.MaidFiddler.Hook;
using param;

namespace CM3D2.MaidFiddler.Plugin.Utils
{
    public static class EnumHelper
    {
        public static readonly Propensity[] Propensities;
        public static readonly Feature[] Features;
        public static readonly MaidChangeType[] MaidChangeTypes;
        public static readonly Personal[] Personalities;
        public static readonly Condition[] Conditions;
        public static readonly ContractType[] ContractTypes;
        public static readonly ContractType MaxContractType;
        public static readonly Condition MaxCondition;
        public static readonly Personal MaxPersonality;
        public static readonly int MaxMaidClass;
        public static readonly Propensity MaxPropensity;
        public static readonly Feature MaxFeature;
        private static readonly Dictionary<int, string> maidClassIDtoNameDic = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> yotogiClassToNameDic = new Dictionary<int, string>();
        public static readonly List<int> EnabledYotogiClasses = new List<int>();

        static EnumHelper()
        {
            Debugger.WriteLine("Initializing Maid and Yotogi class maps...");
            CreateMaidClassAndYotogiClassMaps();

            Debugger.WriteLine("Initializing enum values");
            ContractTypes = GetValues<ContractType>();
            Conditions = GetValues<Condition>();
            Personalities = GetValues<Personal>();
            MaidChangeTypes = GetValues<MaidChangeType>();
            Propensities = GetValues<Propensity>();
            Features = GetValues<Feature>();

            MaxPersonality = Personalities[Personalities.Length - 1];
            MaxCondition = Conditions[Conditions.Length - 1];
            MaxContractType = ContractTypes[ContractTypes.Length - 1];
            MaxPropensity = Propensities[Propensities.Length - 1];
            MaxFeature = Features[Features.Length - 1];

            MaxMaidClass = maidClassIDtoNameDic.Count;
        }

        private static void CreateMaidClassAndYotogiClassMaps()
        {
            MethodInfo createEnabledMap = typeof (MaidParam).GetMethod(
            "CreateMaidClassAndYotogiClassEnabled",
            BindingFlags.Static | BindingFlags.NonPublic);

            createEnabledMap.Invoke(null, null);
            CreateMaidClassNameMap();
            CreateEnabledYotogiClassMap();
        }

        public static string GetYotogiClassName(int yotogiClass)
        {
            string result;
            return yotogiClassToNameDic.TryGetValue(yotogiClass, out result)
                   ? result : yotogiClass.ToString(CultureInfo.InvariantCulture);
        }

        public static bool IsValidYotogiClass(int yotogiClass)
        {
            return yotogiClassToNameDic.ContainsKey(yotogiClass);
        }

        public static string GetMaidClassName(int maidClass)
        {
            return maidClassIDtoNameDic[maidClass];
        }

        private static void CreateMaidClassNameMap()
        {
            if (0 < maidClassIDtoNameDic.Count)
                return;
            int maxVal = 0;
            using (AFileBase aFileBase = GameUty.FileSystem.FileOpen("maid_class_infotext.nei"))
            {
                using (CsvParser csvParser = new CsvParser())
                {
                    bool condition = csvParser.Open(aFileBase);
                    NDebug.Assert(condition, "file open error[maid_class_infotext.nei]");
                    for (int i = 1; i < csvParser.max_cell_y; i++)
                    {
                        if (!csvParser.IsCellToExistData(0, i))
                            continue;
                        string cellAsString = csvParser.GetCellAsString(0, i);
                        maidClassIDtoNameDic.Add(maxVal, cellAsString);
                        maxVal++;
                    }
                }
            }
        }

        private static void CreateEnabledYotogiClassMap()
        {
            Type yotogiClassType = typeof (Maid).Assembly.GetType("param.YotogiClassType");
            MethodInfo getYotogiClassIdFromNameMethod = typeof (MaidParam).GetMethod(
            "GetYotogiClassIdFromName",
            BindingFlags.Public | BindingFlags.Static);
            bool isOldVersion = yotogiClassType != null;

            Action<string> readYotogiClasses = delegate(string fileName)
            {
                fileName += ".nei";
                if (!GameUty.FileSystem.IsExistentFile(fileName))
                    return;
                using (AFileBase aFileBase = GameUty.FileSystem.FileOpen(fileName))
                {
                    using (CsvParser csvParser = new CsvParser())
                    {
                        bool condition = csvParser.Open(aFileBase);
                        NDebug.Assert(condition, fileName + " open failed.");
                        for (int k = 1; k < csvParser.max_cell_y; k++)
                        {
                            if (!csvParser.IsCellToExistData(0, k))
                                continue;
                            string className = csvParser.GetCellAsString(0, k);
                            int key = isOldVersion
                                      ? (int) Enum.Parse(yotogiClassType, className, true)
                                      : (int) getYotogiClassIdFromNameMethod.Invoke(null, new object[] {className});
                            if (!yotogiClassToNameDic.ContainsKey(key))
                                yotogiClassToNameDic.Add(key, className);
                            EnabledYotogiClasses.Add(key);
                        }
                    }
                }
            };

            readYotogiClasses("yotogi_class_enabled_list");
            foreach (string path in GameUty.PathList)
                readYotogiClasses($"yotogi_class_enabled_list_{path}");
            EnabledYotogiClasses.Sort();
        }

        public static string GetName<T>(T value)
        {
            return Enum.GetName(typeof (T), value);
        }

        public static T[] GetValues<T>()
        {
            return (T[]) Enum.GetValues(typeof (T));
        }

        public static bool TryParse<T>(string val, out T result, bool ignoreCase = false)
        {
            try
            {
                result = (T) Enum.Parse(typeof (T), val, ignoreCase);
            }
            catch (Exception)
            {
                result = default(T);
                return false;
            }
            return true;
        }

        public static string EnumsToString<T>(IList<T> keys, char separator)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append(GetName(keys[i]));
                if (i != keys.Count - 1)
                    sb.Append(separator);
            }

            return sb.ToString();
        }

        public static List<T> ParseEnums<T>(string value, char separator)
        {
            List<T> result = new List<T>();

            string[] values = value.Split(new[] {separator}, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
                return new List<T>();
            try
            {
                foreach (T val in
                values.Select(keyCode => (T) Enum.Parse(typeof (T), keyCode, true)).Where(kc => !result.Contains(kc)))
                {
                    result.Add(val);
                }
            }
            catch (Exception)
            {
                return new List<T>();
            }

            return result;
        }
    }
}