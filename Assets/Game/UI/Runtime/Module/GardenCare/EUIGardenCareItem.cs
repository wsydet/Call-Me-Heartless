using System;
using Game.GardenCare;
using Game.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public partial class EUIGardenCareItem
    {
        private GardenCareGame _game;
        private bool _paused;
        private Button[] _cells = Array.Empty<Button>();
        private readonly RectTransform[] _insects = new RectTransform[4];
        private static string T(string key, string fallback) => NovelLocalization.Runtime("ui.gardenCare." + key, fallback);

        public void Configure(GardenCareGame game, Action start, Action<int> pull, Action<int, bool> deliver, Action next)
        {
            _game = game;
            Canvas.ForceUpdateCanvases();
            StartButton.onClick.AddListener(() => start());
            ContinueButton.onClick.AddListener(() => next());
            _cells = new Button[game.PlantCount];
            for (int i = 0; i < BoardRoot.childCount; i++)
                BoardRoot.GetChild(i).gameObject.SetActive(i < game.PlantCount);
            for (int i = 0; i < _cells.Length; ++i)
            {
                int index = i;
                _cells[i] = BoardRoot.Find("Cell" + i).GetComponent<Button>();
                _cells[i].onClick.AddListener(() => pull(index));
                // Only some crops have a companion weed; both plants have independent hit targets.
                bool weed = game.IsWeed(i);
                int slot = game.PlantingCell(i);
                var rect = (RectTransform)_cells[i].transform;
                var center = new Vector2((slot % game.Config.columns + .5f) / game.Config.columns + game.WeedSide(i) * .284f / game.Config.columns,
                    1 - (slot / game.Config.columns + .5f) / game.Config.rows - (weed ? .105f / game.Config.rows : 0));
                var halfSize = weed ? new Vector2(.104f / game.Config.columns, .276f / game.Config.rows) : new Vector2(.164f / game.Config.columns, .36f / game.Config.rows);
                rect.anchorMin = center - halfSize;
                rect.anchorMax = center + halfSize;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                _cells[i].GetComponent<Image>().color = Color.clear;
            }
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < _insects.Length; ++i)
            {
                int index = i;
                var insect = _insects[i] = DragLayer.Find("Insect" + i).GetComponent<RectTransform>();
                insect.gameObject.SetActive(i < game.Config.insectCount);
                if (i >= game.Config.insectCount) continue;
                insect.position = _cells[game.InsectCell(i)].transform.position + BoardRoot.TransformVector(new Vector3(20, 17, 0));
                var pointer = insect.GetComponent<GardenCarePointerHandler>();
                if (!pointer) throw new InvalidOperationException("菜地拖拽组件缺失");
                pointer.Configure(DragLayer, RefugeRoot,
                    () => _game != null && !_paused && _game.Phase == GardenCarePhase.Playing,
                    hit => deliver(index, hit));
            }
            Refresh(game, false);
        }

        public void Refresh(GardenCareGame game, bool paused)
        {
            _game = game; _paused = paused;
            bool playing = game.Phase == GardenCarePhase.Playing && !paused;
            TaskText.text = T("Title", "照料菜地");
            TimerText.text = string.Format(T("Timer", "剩余 {0} 秒"), Mathf.CeilToInt((float)game.Remaining));
            TimerText.gameObject.SetActive(!game.Config.tutorial);
            ProgressText.text = string.Format(T("Progress", "拔草 {0}/{1}    移虫 {2}/{3}"),
                game.PulledCount, game.Config.weedCount, game.DeliveredCount, game.Config.insectCount);
            string hint = game.HintKey;
            string fallback = hint.EndsWith("Intro") ? "点掉杂草，把菜上的虫子拖到右侧草丛。准备好后开始。60 秒后自动开始。"
                : hint.EndsWith("KeepCrop") ? "这是菜苗，请保留它。"
                : hint.EndsWith("WeedRemoved") ? "杂草已拔除。"
                : hint.EndsWith("DropInRefuge") ? "把虫子放到右侧草丛里。"
                : hint.EndsWith("InsectMoved") ? "虫子已移到草丛。"
                : hint.EndsWith("Completed") ? "菜地照料完成！点击继续，或等待 15 秒返回。"
                : hint.EndsWith("TimeUp") ? "今天先照料到这里。点击继续，或等待 15 秒返回。"
                : "点击杂草拔除；拖动虫子到右侧草丛。";
            string feedback = game.Phase == GardenCarePhase.Result ? (game.Outcome == "completed" ?
                (game.LostCropCount == 0 ? "菜地收拾干净了，菜苗也都好好的。" : $"杂草和虫子处理好了，不过误拔了 {game.LostCropCount} 株菜苗，下次仔细些。") : "今天先照料到这里。")
                : hint.EndsWith("CropRemoved") ? "哎呀，这是菜苗，已经拔掉了……看准旁边细瘦的杂草再动手。"
                : game.Config.tutorial && game.Elapsed >= game.Config.tutorialHintDelay ?
                    (game.PulledCount == 0 ? "菜苗旁边那些细瘦的杂草，可以点一下拔掉。"
                    : game.PulledCount < game.Config.weedCount ? "有些菜苗旁才长了杂草。看准再拔，别把菜苗一起拔掉。"
                    : "菜叶上还有虫子，把它们拖到右边的草丛里吧。")
                : hint.EndsWith("KeepCrop") || hint.EndsWith("DropInRefuge") ? NovelLocalization.Runtime(hint, fallback) : "";
            FeedbackText.text = paused ? T("Paused", "已暂停") : feedback;
            StartButton.gameObject.SetActive(false);
            ContinueButton.gameObject.SetActive(false);
            StartButton.interactable = ContinueButton.interactable = !paused;
            StartButton.GetComponentInChildren<TMP_Text>(true).text = T("Start", "开始照料");
            ContinueButton.GetComponentInChildren<TMP_Text>(true).text = T("Continue", "继续剧情");
            RefugeRoot.Find("RefugeLabel").GetComponent<TMP_Text>().text = T("Refuge", "草丛\n将虫子放在这里");
            for (int i = 0; i < _cells.Length; ++i)
            {
                var cell = _cells[i]; if (!cell) continue;
                bool cleared = game.IsPulled(i), weed = game.IsWeed(i);
                cell.interactable = playing && !cleared;
                cell.transform.Find("Label").GetComponent<TMP_Text>().text = cleared ? "" : weed ? T("Weed", "杂草") : T("Crop", "菜苗");
                foreach (string part in new[] { "Stem", "Leaf0", "Leaf1" })
                {
                    var image = cell.transform.Find(part).GetComponent<Image>();
                    image.gameObject.SetActive(!cleared);
                    image.color = weed ? new Color(.64f, .64f, .20f) : new Color(.22f, .74f, .42f);
                }
            }
            for (int i = 0; i < _insects.Length; ++i)
            {
                var insect = _insects[i]; if (!insect) continue;
                bool visible = i < game.Config.insectCount && !game.IsDelivered(i);
                insect.gameObject.SetActive(visible);
                insect.GetComponent<Image>().raycastTarget = playing;
                if (!playing) insect.GetComponent<GardenCarePointerHandler>().CancelDrag();
                insect.Find("Label").GetComponent<TMP_Text>().text = T("Insect", "虫");
            }
        }

        public override void OnClose()
        {
            _game = null;
            StartButton?.onClick.RemoveAllListeners();
            ContinueButton?.onClick.RemoveAllListeners();
            foreach (var cell in _cells) if (cell) cell.onClick.RemoveAllListeners();
            foreach (var insect in _insects) if (insect) insect.GetComponent<GardenCarePointerHandler>()?.ResetHandler();
            base.OnClose();
        }
        public override void OnDispose() { OnClose(); base.OnDispose(); }
    }
}

