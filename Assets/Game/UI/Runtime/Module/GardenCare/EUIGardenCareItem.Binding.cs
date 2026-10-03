/*=============================================================
 * author       : Bingo
 * prefab name  : EUIGardenCareItem
 * page name    : 
 * update time  : 2026/10/3 1:09:18
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIGardenCareItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// GardenPanel/BoardRoot
        /// </summary>
        private UnityEngine.RectTransform BoardRoot;

        /// <summary>
        /// GardenPanel/RefugeRoot
        /// </summary>
        private UnityEngine.RectTransform RefugeRoot;

        /// <summary>
        /// GardenPanel/DragLayer
        /// </summary>
        private UnityEngine.RectTransform DragLayer;

        /// <summary>
        /// GardenPanel/TaskText
        /// </summary>
        private TMP_Text TaskText;

        /// <summary>
        /// GardenPanel/TimerText
        /// </summary>
        private TMP_Text TimerText;

        /// <summary>
        /// GardenPanel/ProgressText
        /// </summary>
        private TMP_Text ProgressText;

        /// <summary>
        /// FeedbackText
        /// </summary>
        private TMP_Text FeedbackText;

        /// <summary>
        /// GardenPanel/StartButton
        /// </summary>
        private Button StartButton;

        /// <summary>
        /// GardenPanel/ContinueButton
        /// </summary>
        private Button ContinueButton;



    public override void OnBind()
    {
        base.OnBind();
            BoardRoot = ControlMap["BoardRoot"] as UnityEngine.RectTransform;
            RefugeRoot = ControlMap["RefugeRoot"] as UnityEngine.RectTransform;
            DragLayer = ControlMap["DragLayer"] as UnityEngine.RectTransform;
            TaskText = ControlMap["TaskText"] as TMP_Text;
            TimerText = ControlMap["TimerText"] as TMP_Text;
            ProgressText = ControlMap["ProgressText"] as TMP_Text;
            FeedbackText = ControlMap["FeedbackText"] as TMP_Text;
            StartButton = ControlMap["StartButton"] as Button;
            ContinueButton = ControlMap["ContinueButton"] as Button;

    }
}
}
