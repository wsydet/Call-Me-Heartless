using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Table;
using Game.Table.Generated;
using UnityEngine;

namespace Game.Table
{
    public static class MiniGameTableData
    {
        public static T SelectLevel<T>(T[] rows, int visit, Func<T, int> level)
        {
            if (visit < 1) throw new ArgumentOutOfRangeException(nameof(visit));
            var ordered = rows.OrderBy(level).ToArray();
            if (ordered.Length == 0) throw new InvalidOperationException("小游戏难度表为空。");
            for (int i = 0; i < ordered.Length; i++)
                if (level(ordered[i]) != i + 1) throw new InvalidOperationException("小游戏难度须从 1 开始连续编号且不重复。");
            return ordered[Math.Min(visit - 1, ordered.Length - 1)];
        }

        public static T[] Load<T>(string key) where T : class
        {
            using var engine = new EmberTableEngine();
            var catalog = GameTables.CreateCatalog();
            var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                if (asset) bytes[entry.TableId] = asset.bytes;
            }
            if (!engine.Load(catalog, bytes).Succeeded || !engine.Database.TryGetTable(key, out EmberTable<T> table))
                throw new InvalidOperationException("小游戏配表加载失败：" + key);
            return table.ToArray();
        }

        public static string Fingerprint(params string[] keys)
        {
            var bytes = new List<byte>();
            foreach (var key in keys)
            {
                var asset = Resources.Load<TextAsset>("Config/Tables/" + key);
                if (!asset) throw new InvalidOperationException("小游戏配表产物缺失：" + key);
                bytes.AddRange(BitConverter.GetBytes(asset.bytes.Length)); bytes.AddRange(asset.bytes);
            }
            using var hash = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }
}
