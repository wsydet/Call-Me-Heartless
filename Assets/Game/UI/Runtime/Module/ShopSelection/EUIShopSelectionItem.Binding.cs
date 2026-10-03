/*=============================================================
 * author       : Bingo
 * prefab name  : EUIShopSelectionItem
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
    public partial class EUIShopSelectionItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// ShopPanel/Product0
        /// </summary>
        private Button Product0;

        /// <summary>
        /// ShopPanel/Name0
        /// </summary>
        private TMP_Text Name0;

        /// <summary>
        /// ShopPanel/Price0
        /// </summary>
        private TMP_Text Price0;

        /// <summary>
        /// ShopPanel/Product1
        /// </summary>
        private Button Product1;

        /// <summary>
        /// ShopPanel/Name1
        /// </summary>
        private TMP_Text Name1;

        /// <summary>
        /// ShopPanel/Price1
        /// </summary>
        private TMP_Text Price1;

        /// <summary>
        /// ShopPanel/Product2
        /// </summary>
        private Button Product2;

        /// <summary>
        /// ShopPanel/Name2
        /// </summary>
        private TMP_Text Name2;

        /// <summary>
        /// ShopPanel/Price2
        /// </summary>
        private TMP_Text Price2;

        /// <summary>
        /// ShopPanel/CancelButton
        /// </summary>
        private Button CancelButton;

        /// <summary>
        /// ShopPanel/TitleText
        /// </summary>
        private TMP_Text TitleText;

        /// <summary>
        /// ShopPanel/BalanceText
        /// </summary>
        private TMP_Text BalanceText;

        /// <summary>
        /// HintText
        /// </summary>
        private TMP_Text HintText;



    public override void OnBind()
    {
        base.OnBind();
            Product0 = ControlMap["Product0"] as Button;
            Name0 = ControlMap["Name0"] as TMP_Text;
            Price0 = ControlMap["Price0"] as TMP_Text;
            Product1 = ControlMap["Product1"] as Button;
            Name1 = ControlMap["Name1"] as TMP_Text;
            Price1 = ControlMap["Price1"] as TMP_Text;
            Product2 = ControlMap["Product2"] as Button;
            Name2 = ControlMap["Name2"] as TMP_Text;
            Price2 = ControlMap["Price2"] as TMP_Text;
            CancelButton = ControlMap["CancelButton"] as Button;
            TitleText = ControlMap["TitleText"] as TMP_Text;
            BalanceText = ControlMap["BalanceText"] as TMP_Text;
            HintText = ControlMap["HintText"] as TMP_Text;

    }
}
}
