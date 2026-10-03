/*=============================================================
 * author       : Bingo
 * prefab name  : EUIPathCleaningItem
 * page name    : 
 * update time  : 2026/10/2 5:45:35
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIPathCleaningItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Landscape/Board
        /// </summary>
        private UnityEngine.RectTransform Board;

        /// <summary>
        /// Landscape/Board/SpotTemplate
        /// </summary>
        private UnityEngine.RectTransform SpotTemplate;

        /// <summary>
        /// Landscape/Toolbar
        /// </summary>
        private UnityEngine.RectTransform Toolbar;

        /// <summary>
        /// Landscape/Toolbar/ToolTemplate
        /// </summary>
        private UnityEngine.RectTransform ToolTemplate;

        /// <summary>
        /// ToolCursor
        /// </summary>
        private UnityEngine.RectTransform ToolCursor;

        /// <summary>
        /// Landscape/Timer
        /// </summary>
        private TMP_Text Timer;

        /// <summary>
        /// Landscape/Title
        /// </summary>
        private TMP_Text Title;

        /// <summary>
        /// Landscape/Instruction
        /// </summary>
        private TMP_Text Instruction;

        /// <summary>
        /// HintPanel/Hint
        /// </summary>
        private TMP_Text Hint;

        /// <summary>
        /// Landscape/Board/TrashBin
        /// </summary>
        private UnityEngine.RectTransform TrashBin;

        /// <summary>
        /// Landscape/Board/LeafPile
        /// </summary>
        private UnityEngine.RectTransform LeafPile;

        /// <summary>
        /// Landscape/Board/CleanEffectTemplate
        /// </summary>
        private UnityEngine.RectTransform CleanEffectTemplate;

        /// <summary>
        /// ToolCursor/BroomVisual
        /// </summary>
        private UnityEngine.RectTransform BroomVisual;



    public override void OnBind()
    {
        base.OnBind();
            Board = ControlMap["Board"] as UnityEngine.RectTransform;
            SpotTemplate = ControlMap["SpotTemplate"] as UnityEngine.RectTransform;
            Toolbar = ControlMap["Toolbar"] as UnityEngine.RectTransform;
            ToolTemplate = ControlMap["ToolTemplate"] as UnityEngine.RectTransform;
            ToolCursor = ControlMap["ToolCursor"] as UnityEngine.RectTransform;
            Timer = ControlMap["Timer"] as TMP_Text;
            Title = ControlMap["Title"] as TMP_Text;
            Instruction = ControlMap["Instruction"] as TMP_Text;
            Hint = ControlMap["Hint"] as TMP_Text;
            TrashBin = ControlMap["TrashBin"] as UnityEngine.RectTransform;
            LeafPile = ControlMap["LeafPile"] as UnityEngine.RectTransform;
            CleanEffectTemplate = ControlMap["CleanEffectTemplate"] as UnityEngine.RectTransform;
            BroomVisual = ControlMap["BroomVisual"] as UnityEngine.RectTransform;

    }
}
}
