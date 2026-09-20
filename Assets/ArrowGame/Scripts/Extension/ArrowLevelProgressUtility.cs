using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ArrowMaze
{
    /// <summary>
    /// 关卡进度：PlayerDataModel.LevelId 为线性进度（按 ArrowLevelProgress 子关卡累加），
    /// 由此解析主关卡、子关卡进度，并映射到实际加载的 Level 文件编号。
    /// </summary>
    public static class ArrowLevelProgressUtility
    {
        /// <summary>关卡配置总数</summary>
        public const int MaxLevelConfigCount = 110;
        /// <summary>超出总数后循环区间的起始关卡编号</summary>
        public const int LoopLevelStart = 50;
        /// <summary>循环区间关卡数量（50-110）</summary>
        public const int LoopLevelCount = MaxLevelConfigCount - LoopLevelStart + 1;

        public struct LevelProgressState
        {
            /// <summary>线性进度（与 PlayerDataModel.LevelId 一致）</summary>
            public int LinearLevelId;
            /// <summary>当前主关卡</summary>
            public int MainLevel;
            /// <summary>当前主关卡内子关卡序号（1-based）</summary>
            public int CurrentSubLevel;
            /// <summary>当前主关卡子关卡总数</summary>
            public int TotalSubLevel;
            /// <summary>实际加载的 Level 文件编号</summary>
            public int ActualLevelFileIndex;
        }

        public static bool TryGetProgressTable(out ArrowLevelProgress[] rows)
        {
            rows = null;
            if (GF.DataTable == null)
                return false;
            var table = GF.DataTable.GetDataTable<ArrowLevelProgress>();
            if (table == null)
                return false;
            rows = table.GetAllDataRows()
                .OrderBy(r => r.MainLevel)
                .ToArray();
            return rows != null && rows.Length > 0;
        }

        /// <summary>
        /// 由线性 LevelId 解析主关卡与子关卡进度。
        /// </summary>
        public static LevelProgressState GetStateFromLinearLevelId(int linearLevelId)
        {
            int linear = Mathf.Max(1, linearLevelId);
            if (!TryGetProgressTable(out var rows))
            {
                return new LevelProgressState
                {
                    LinearLevelId = linear,
                    MainLevel = linear,
                    CurrentSubLevel = 1,
                    TotalSubLevel = 1,
                    ActualLevelFileIndex = ResolveLevelFileIndex(linear)
                };
            }

            int totalLinear = 0;
            for (int i = 0; i < rows.Length; i++)
                totalLinear += rows[i].SubLevel;

            // 主关卡进度封顶：超过配置总子关卡后停留在最后一主关卡，不再回到 1
            int mappedLinear = totalLinear > 0 ? Mathf.Min(linear, totalLinear) : linear;

            int remaining = mappedLinear;
            ArrowLevelProgress matched = rows[rows.Length - 1];
            foreach (var row in rows)
            {
                if (remaining <= row.SubLevel)
                {
                    matched = row;
                    break;
                }
                remaining -= row.SubLevel;
            }

            int currentSub = remaining > 0 ? remaining : matched.SubLevel;
            return new LevelProgressState
            {
                LinearLevelId = linear,
                MainLevel = matched.MainLevel,
                CurrentSubLevel = currentSub,
                TotalSubLevel = matched.SubLevel,
                ActualLevelFileIndex = ResolveLevelFileIndex(linear)
            };
        }

        /// <summary>
        /// ArrowLevelProgress 配置的总子关卡数（线性进度满额）。
        /// </summary>
        public static int GetTotalConfiguredLinearLevels()
        {
            if (!TryGetProgressTable(out var rows))
                return 0;
            int totalLinear = 0;
            for (int i = 0; i < rows.Length; i++)
                totalLinear += rows[i].SubLevel;
            return totalLinear;
        }

        /// <summary>
        /// 是否已完成 ArrowLevelProgress 配置的全部关卡（线性进度超过总子关卡数）。
        /// </summary>
        public static bool HasCompletedAllConfiguredLevels(int linearLevelId)
        {
            int totalLinear = GetTotalConfiguredLinearLevels();
            return totalLinear > 0 && linearLevelId > totalLinear;
        }

        /// <summary>
        /// ArrowLevelProgress 表最后一行的主关卡，即目标关卡。
        /// </summary>
        public static int GetTargetMainLevel()
        {
            if (!TryGetProgressTable(out var rows))
                return 1;
            return rows[rows.Length - 1].MainLevel;
        }

        /// <summary>
        /// levelCurTxt：主关卡 &gt; 1 显示当前主关卡，否则显示下一主关卡。
        /// </summary>
        public static int GetDisplayCurrentMainLevel(int linearLevelId)
        {
            var state = GetStateFromLinearLevelId(linearLevelId);
            return state.MainLevel > 1 ? state.MainLevel : state.MainLevel + 1;
        }

        /// <summary>
        /// 已完成的主关卡数量（打通该主关卡后计数，而非进入该主关卡）。
        /// </summary>
        public static int GetCompletedMainLevelCount(int linearLevelId)
        {
            if (HasCompletedAllConfiguredLevels(linearLevelId))
                return GetTargetMainLevel();

            var state = GetStateFromLinearLevelId(linearLevelId);
            return Mathf.Max(0, state.MainLevel - 1);
        }

        /// <summary>
        /// 线性进度映射到 Level{index}.json 文件编号（1-110 直连，&gt;110 时循环 50-110）。
        /// </summary>
        public static int ResolveLevelFileIndex(int linearLevelId)
        {
            int index = Mathf.Max(1, linearLevelId);
            if (index <= MaxLevelConfigCount)
                return index;
            return (index - MaxLevelConfigCount - 1) % LoopLevelCount + LoopLevelStart;
        }
    }
}
