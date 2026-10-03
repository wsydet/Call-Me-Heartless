using System;
using Ember.Basic;
using Game.CMH.Rewards;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>烘焙奖励表变化时把语义哈希同步到步骤资产，令旧档兼容性检查可见。</summary>
    public sealed class NovelRewardFingerprintSync : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            if (Array.Exists(imported, p => p == "Assets/GameResource/Resources/Config/Tables/novel_rewards.bytes"))
                EditorApplication.delayCall += Sync;
        }
        public static void Sync()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                NovelRewardService.LoadBakedForEditor();
                foreach (string guid in AssetDatabase.FindAssets("t:TableRewardStepSO"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TableRewardStepSO>(AssetDatabase.GUIDToAssetPath(guid));
                    if (!asset) continue;
                    var data = new SerializedObject(asset);
                    var hash = data.FindProperty("_tableFingerprint");
                    if (hash.stringValue == NovelRewardService.Fingerprint) continue;
                    hash.stringValue = NovelRewardService.Fingerprint;
                    data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
                }
            }
            catch (Exception e) { EmberDebug.LogError("Game.CMH.Rewards", "奖励配表指纹同步失败：" + e.Message); }
        }
    }
}
