using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ember.Table;
using Game.Narrative;
using Game.Table;

namespace Game.CMH.Rewards
{
    /// <summary>配表奖励业务。剧情步骤只传奖励ID，所有奖励数值来自本次加载的表。</summary>
    public static class NovelRewardService
    {
        #region 内部参数
        private static Dictionary<string, NovelRewardRow> _rows;
        public static string Fingerprint { get; private set; }
        public static bool IsReady => _rows != null;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Install(EmberTableDatabase database)
        {
            Uninstall();
            if (database == null || !database.TryGetTable("novel_rewards", out EmberTable<NovelRewardRow> table)) return;
            var rows = table.ToArray();
            foreach (var row in rows)
            {
                string error = ValidateRow(row);
                if (error != null) throw new InvalidOperationException(error);
            }
            _rows = rows.ToDictionary(r => r.Id, StringComparer.Ordinal);
            Fingerprint = ConfigurationFingerprint(rows);
        }
        public static void Uninstall() { _rows = null; Fingerprint = null; }
        public static bool TryGet(string id, out NovelRewardRow row)
        {
            row = null;
            return _rows != null && !string.IsNullOrWhiteSpace(id) && _rows.TryGetValue(id, out row);
        }
        public static string ValidateRow(NovelRewardRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id)) return "奖励行缺少ID";
            if (string.IsNullOrWhiteSpace(row.TargetVariableId)) return "奖励对象未配置：" + row.Id;
            if (row.Minimum > row.Maximum) return "奖励上下限无效：" + row.Id;
            return null;
        }
        public static int Calculate(NovelRewardRow row, int current, int multiplier)
        {
            string error = ValidateRow(row);
            if (error != null) throw new InvalidOperationException(error);
            if (!row.Enabled) return current;
            long result = checked((long)current + (long)row.Amount * multiplier);
            return (int)Math.Max(row.Minimum, Math.Min(row.Maximum, result));
        }
        public static string ConfigurationFingerprint(IEnumerable<NovelRewardRow> rows)
        {
            var content = new StringBuilder();
            foreach (var row in rows.OrderBy(r => r.Id, StringComparer.Ordinal))
            {
                // Length-prefixed fields avoid ambiguous concatenations; row ordering is irrelevant.
                foreach (string part in new[] { row.Id, row.TargetVariableId,
                    row.Amount.ToString(CultureInfo.InvariantCulture), row.MultiplierVariableId ?? "",
                    row.Minimum.ToString(CultureInfo.InvariantCulture), row.Maximum.ToString(CultureInfo.InvariantCulture),
                    row.Enabled ? "1" : "0" }) content.Append(part.Length).Append(':').Append(part);
            }
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()))).Replace("-", "").ToLowerInvariant();
        }
        public static string ValidateReward(string id, NovelCustomStepValidation validation)
        {
            if (!TryGet(id, out var row)) return "奖励配表未加载或ID不存在：" + id;
            string error = ValidateRow(row);
            if (error != null || !row.Enabled || validation == null) return error;
            if (!validation.TryGetVariable(NovelVariableScope.Global, row.TargetVariableId, out var target) || target.Type != NovelValueType.Int)
                return "奖励对象必须是已声明的全局整数变量：" + row.TargetVariableId;
            if (!string.IsNullOrEmpty(row.MultiplierVariableId) &&
                (!validation.TryGetVariable(NovelVariableScope.Global, row.MultiplierVariableId, out var multiplier) || multiplier.Type != NovelValueType.Int))
                return "奖励倍率必须是已声明的全局整数变量：" + row.MultiplierVariableId;
            return null;
        }
        public static void Apply(NovelCustomStepContext context, string id)
        {
            if (!TryGet(id, out var row)) { context.Fail("奖励配表未加载或ID不存在：" + id); return; }
            if (!row.Enabled) { context.Complete(); return; }
            if (!context.TryGetVariable(NovelVariableScope.Global, row.TargetVariableId, out var target) || target.Type != NovelValueType.Int)
            { context.Fail("奖励对象无效：" + row.TargetVariableId); return; }
            int multiplier = 1;
            if (!string.IsNullOrEmpty(row.MultiplierVariableId))
            {
                if (!context.TryGetVariable(NovelVariableScope.Global, row.MultiplierVariableId, out var count) || count.Type != NovelValueType.Int)
                { context.Fail("奖励倍率无效：" + row.MultiplierVariableId); return; }
                multiplier = count.Int;
            }
            int result;
            try { result = Calculate(row, target.Int, multiplier); }
            catch (Exception e) { context.Fail("奖励计算失败：" + id + " / " + e.Message); return; }
            if (!context.SetVariable(NovelVariableScope.Global, row.TargetVariableId, new NovelValue(result), out var error))
            { context.Fail("奖励写入失败：" + error); return; }
            context.Complete();
        }
#if UNITY_EDITOR
        // Editor preview uses the same generated schema and baked bytes as runtime; never the authoring CSV.
        public static void LoadBakedForEditor()
        {
            var catalog = Game.Table.Generated.GameTables.CreateCatalog();
            var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                string path = "Assets/GameResource/Resources/" + entry.ResourcePath + ".bytes";
                if (System.IO.File.Exists(path)) bytes.Add(entry.TableId, System.IO.File.ReadAllBytes(path));
            }
            var engine = new EmberTableEngine();
            if (!engine.Load(catalog, bytes).Succeeded) throw new InvalidOperationException("编辑预览配表加载失败");
            Install(engine.Database);
        }
#endif
        #endregion
    }
}
