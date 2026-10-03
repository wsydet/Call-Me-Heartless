/*=============================================================
 * author       : Bingo
 * prefab name  : EUITalismanPracticeItem
 * page name    : 
 * update time  : 2026/10/3 1:09:19
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUITalismanPracticeItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// PracticePanel/StageText
        /// </summary>
        private TMP_Text StageText;

        /// <summary>
        /// PracticePanel/TimerText
        /// </summary>
        private TMP_Text TimerText;

        /// <summary>
        /// PracticePanel/ProgressText
        /// </summary>
        private TMP_Text ProgressText;

        /// <summary>
        /// HintText
        /// </summary>
        private TMP_Text HintText;

        /// <summary>
        /// PracticePanel/StartButton
        /// </summary>
        private Button StartButton;

        /// <summary>
        /// PracticePanel/NextButton
        /// </summary>
        private Button NextButton;

        /// <summary>
        /// PracticePanel/DifferenceRoot
        /// </summary>
        private UnityEngine.RectTransform DifferenceRoot;

        /// <summary>
        /// PracticePanel/AnswerRoot
        /// </summary>
        private UnityEngine.RectTransform AnswerRoot;

        /// <summary>
        /// PracticePanel/ShopRoot
        /// </summary>
        private UnityEngine.RectTransform ShopRoot;

        /// <summary>
        /// PracticePanel/FeatherRoot
        /// </summary>
        private UnityEngine.RectTransform FeatherRoot;

        /// <summary>
        /// PracticePanel
        /// </summary>
        private UnityEngine.RectTransform PracticePanel;

        /// <summary>
        /// PracticePanel/ShopRoot/DrawRoot
        /// </summary>
        private UnityEngine.RectTransform DrawRoot;

        /// <summary>
        /// PracticePanel/ShopRoot/ClearButton
        /// </summary>
        private Button ClearButton;

        /// <summary>
        /// PracticePanel/ShopRoot/SubmitButton
        /// </summary>
        private Button SubmitButton;

        /// <summary>
        /// PracticePanel/ShopRoot/OrderText
        /// </summary>
        private TMP_Text OrderText;

        /// <summary>
        /// PracticePanel/ShopRoot/QueueText
        /// </summary>
        private TMP_Text QueueText;

        /// <summary>
        /// PracticePanel/ShopRoot/CustomerPortrait
        /// </summary>
        private Image CustomerPortrait;

        /// <summary>
        /// PracticePanel/ShopRoot/PatienceFill
        /// </summary>
        private Image PatienceFill;

        /// <summary>
        /// PracticePanel/FeatherRoot/BlowButton
        /// </summary>
        private Button BlowButton;

        /// <summary>
        /// PracticePanel/FeatherRoot/HoldFill
        /// </summary>
        private Image HoldFill;

        /// <summary>
        /// PracticePanel/FeatherRoot/FlightArea/TargetFrame
        /// </summary>
        private UnityEngine.RectTransform TargetFrame;

        /// <summary>
        /// PracticePanel/FeatherRoot/FlightArea/Feather
        /// </summary>
        private UnityEngine.RectTransform Feather;



    public override void OnBind()
    {
        base.OnBind();
            StageText = ControlMap["StageText"] as TMP_Text;
            TimerText = ControlMap["TimerText"] as TMP_Text;
            ProgressText = ControlMap["ProgressText"] as TMP_Text;
            HintText = ControlMap["HintText"] as TMP_Text;
            StartButton = ControlMap["StartButton"] as Button;
            NextButton = ControlMap["NextButton"] as Button;
            DifferenceRoot = ControlMap["DifferenceRoot"] as UnityEngine.RectTransform;
            AnswerRoot = ControlMap["AnswerRoot"] as UnityEngine.RectTransform;
            ShopRoot = ControlMap["ShopRoot"] as UnityEngine.RectTransform;
            FeatherRoot = ControlMap["FeatherRoot"] as UnityEngine.RectTransform;
            PracticePanel = ControlMap["PracticePanel"] as UnityEngine.RectTransform;
            DrawRoot = ControlMap["DrawRoot"] as UnityEngine.RectTransform;
            ClearButton = ControlMap["ClearButton"] as Button;
            SubmitButton = ControlMap["SubmitButton"] as Button;
            OrderText = ControlMap["OrderText"] as TMP_Text;
            QueueText = ControlMap["QueueText"] as TMP_Text;
            CustomerPortrait = ControlMap["CustomerPortrait"] as Image;
            PatienceFill = ControlMap["PatienceFill"] as Image;
            BlowButton = ControlMap["BlowButton"] as Button;
            HoldFill = ControlMap["HoldFill"] as Image;
            TargetFrame = ControlMap["TargetFrame"] as UnityEngine.RectTransform;
            Feather = ControlMap["Feather"] as UnityEngine.RectTransform;

    }
}
}
