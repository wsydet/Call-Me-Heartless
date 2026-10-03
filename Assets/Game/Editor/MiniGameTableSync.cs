using System;
using System.Linq;
using Ember.Table.Editor;
using Game.Table;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public sealed class MiniGameTableSync : AssetPostprocessor
    {
        private static readonly string[] Keys = { "study_settings", "feather_difficulty", "garden_care", "shop_settings", "shop_products" };

        [MenuItem("Call Me Heartless/小游戏/烘焙数值配表")]
        public static void Bake()
        {
            var result = EmberTablePipeline.BakeAndGenerateAll();
            if(!result.Succeeded) throw new InvalidOperationException(string.Join("\n",result.Diagnostics.Select(d=>d.ToString())));
            Sync();
        }

        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] old)
        {
            if(imported.Any(p=>Keys.Any(k=>p=="Assets/GameResource/Resources/Config/Tables/"+k+".bytes")))
                EditorApplication.delayCall += Sync;
        }

        [InitializeOnLoadMethod]
        private static void AfterReload() => EditorApplication.delayCall += Sync;

        public static void Sync()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || Keys.Any(k=>!Resources.Load<TextAsset>("Config/Tables/"+k)))return;
            Game.OfferingSort.Editor.OfferingSortFingerprintSync.Sync();
            Game.PathCleaning.Editor.PathCleaningFingerprintSync.Sync();
            SyncAsset("TalismanPractice/TalismanPracticeStep", "study_settings", "feather_difficulty");
            SyncAsset("GardenCare/GardenCareBridge", "garden_care");
            SyncAsset("ShopSelection/HDrinkSelection", "shop_settings", "shop_products");
        }

        private static void SyncAsset(string suffix, params string[] keys)
        {
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/GameResource/Authoring/Narrative/Scripts/"+suffix+".asset");
            if(!asset)return;
            var so=new SerializedObject(asset);var field=so.FindProperty("_configFingerprint");
            if(field==null)throw new InvalidOperationException("小游戏步骤缺少配置指纹："+suffix);
            string value=MiniGameTableData.Fingerprint(keys);
            if(field.stringValue==value)return;
            field.stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
