using Game.Narrative;
using UnityEngine;

namespace Game.CMH.Rewards
{
    /// <summary>奖励业务的薄入口；本步骤参数只使用Command.Value里的奖励ID。</summary>
    [CreateAssetMenu(menuName = "Call Me Heartless/配表奖励步骤", fileName = "TableRewardStep")]
    public sealed class TableRewardStepSO : NovelCustomStepSO
    {
        #region 编辑器面板参数
        [SerializeField, HideInInspector] private string _tableFingerprint;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override string ScriptId => "Game.CMH.Rewards.TableRewardStep";
        public override string DisplayName => "配表奖励 · novel_rewards";
        public override string Summary() => "按奖励ID读取对象、数量、倍率与上下限";
        public override string Validate(NovelCustomStepValidation validation)
        {
#if UNITY_EDITOR
            if (!NovelRewardService.IsReady) NovelRewardService.LoadBakedForEditor();
#endif
            if (!NovelRewardService.IsReady) return "奖励配表未加载：novel_rewards";
            if (string.IsNullOrEmpty(_tableFingerprint) || _tableFingerprint != NovelRewardService.Fingerprint)
                return "奖励表语义已变化，请同步奖励步骤指纹后重试";
            return null;
        }
        public override void OnBegin(NovelCustomStepContext context)
        {
            if (_tableFingerprint != NovelRewardService.Fingerprint)
            { context.Fail("奖励表与剧情指纹不一致，拒绝按变化后的数值续跑"); return; }
            if (context.Command.Value.Type != NovelValueType.String)
            { context.Fail("奖励步骤参数必须是奖励ID字符串"); return; }
            NovelRewardService.Apply(context, context.Command.Value.String);
        }
        #endregion
    }
}
