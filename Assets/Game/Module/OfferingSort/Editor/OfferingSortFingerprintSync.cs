using System;
using System.IO;
using System.Security.Cryptography;
using Ember.Basic;
using UnityEditor;
namespace Game.OfferingSort.Editor
{
    public sealed class OfferingSortFingerprintSync : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] old)
        {if(Array.Exists(imported,p=>p=="Assets/GameResource/Resources/Config/Tables/offering_sort.bytes"))EditorApplication.delayCall+=Sync;}
        public static void Sync()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                string path="Assets/GameResource/Resources/Config/Tables/offering_sort.bytes";if(!File.Exists(path))return;
                string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
                foreach(var guid in AssetDatabase.FindAssets("t:OfferingSortStepSO"))
                {var asset=AssetDatabase.LoadAssetAtPath<OfferingSortStepSO>(AssetDatabase.GUIDToAssetPath(guid));var so=new SerializedObject(asset);var field=so.FindProperty("_configFingerprint");if(field.stringValue==hash)continue;field.stringValue=hash;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
            }catch(Exception e){EmberDebug.LogError("Game.OfferingSort","小游戏指纹同步失败："+e.Message);}
        }
    }
}
