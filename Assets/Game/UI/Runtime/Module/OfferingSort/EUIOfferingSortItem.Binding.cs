/*=============================================================
 * author       : Bingo
 * prefab name  : EUIOfferingSortItem
 * page name    : 
 * update time  : 2026/10/1 19:34:51
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIOfferingSortItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Altar/Board
        /// </summary>
        private UnityEngine.RectTransform Board;

        /// <summary>
        /// Altar/Board/OfferingTemplate
        /// </summary>
        private UnityEngine.RectTransform OfferingTemplate;

        /// <summary>
        /// DragLayer
        /// </summary>
        private UnityEngine.RectTransform DragLayer;

        /// <summary>
        /// Altar/Timer
        /// </summary>
        private TMP_Text Timer;

        /// <summary>
        /// Altar/Title
        /// </summary>
        private TMP_Text Title;

        /// <summary>
        /// Altar/Instruction
        /// </summary>
        private TMP_Text Instruction;

        /// <summary>
        /// HintPanel/Hint
        /// </summary>
        private TMP_Text Hint;



    public override void OnBind()
    {
        base.OnBind();
            Board = ControlMap["Board"] as UnityEngine.RectTransform;
            OfferingTemplate = ControlMap["OfferingTemplate"] as UnityEngine.RectTransform;
            DragLayer = ControlMap["DragLayer"] as UnityEngine.RectTransform;
            Timer = ControlMap["Timer"] as TMP_Text;
            Title = ControlMap["Title"] as TMP_Text;
            Instruction = ControlMap["Instruction"] as TMP_Text;
            Hint = ControlMap["Hint"] as TMP_Text;

    }
}
}
