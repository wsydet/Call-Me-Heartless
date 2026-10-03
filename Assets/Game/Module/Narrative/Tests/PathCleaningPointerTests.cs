using System;
using System.Collections.Generic;
using System.Linq;
using Ember.UI;
using Ember.UIExtension;
using Game.PathCleaning;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed class PathCleaningPointerTests
    {
        [TestCase("deposit")]
        [TestCase("switch")]
        [TestCase("timeout")]
        public void TrashIsPickedUpWithOneClickAndOnlyClearsWhenDeposited(string finish)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("Trash click test",typeof(RectTransform),typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root,scene);EUIItem item=null;
            try
            {
                var ui=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("UI/Module/PathCleaning/Prefabs/EUIPathCleaningItem"),root.transform,false);
                Assert.IsTrue(EUIItemFactory.TryCreate(ui,out item,out var error),error);
                item.Show();Canvas.ForceUpdateCanvases();
                var game=new PathCleaningGame(PathCleaningModule.LoadConfig()[1],PathCleaningModule.LoadTypes(),PathCleaningModule.LoadTools(),3);
                var logic=item.Logic;
                void Refresh()=>logic.GetType().GetMethod("Refresh").Invoke(logic,new object[]{game});
                logic.GetType().GetMethod("Configure").Invoke(logic,new object[]{
                    game,(Action<string>)(id=>{game.Select(id);Refresh();}),(Action<int>)(i=>{game.Clean(i);Refresh();}),
                    (Action<int,string>)((i,target)=>{game.Clean(i,target);Refresh();}),
                    (Action<int,Vector2,Vector2>)((_,__,___)=>{}),(Action<Vector2,Vector2,bool>)((_,__,___)=>{})});
                void Click(GameObject go)=>ExecuteEvents.Execute(go,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                var trash=game.Spots.Select((spot,index)=>(spot,index)).First(p=>p.spot.Type.Id=="trash");
                var original=ui.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Spot"+trash.index).gameObject;
                var bin=((RectTransform)logic.ControlMap["TrashBin"]).gameObject;
                var hint=(TMPro.TMP_Text)logic.ControlMap["Hint"];
                Assert.IsFalse(hint.transform.parent.GetComponent<Image>().enabled,"No second opaque dialogue frame");
                Click(original);Assert.IsTrue(original.activeSelf);Assert.AreEqual(0,game.ClearedCount);
                Click(ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Tool_tongs").gameObject);
                Click(original);
                var held=ui.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="HeldTrash").GetComponent<Image>();
                Assert.IsTrue(held.gameObject.activeSelf);Assert.IsFalse(held.raycastTarget);
                Assert.AreEqual(original.GetComponent<Image>().sprite,held.sprite);
                Assert.IsFalse(original.activeSelf);Assert.AreEqual(0,game.ClearedCount);
                Assert.IsNull(original.GetComponent("PathCleaningDragHandle"));
                if(finish=="deposit")
                {
                    Click(bin);Click(bin);
                    Assert.IsTrue(trash.spot.Cleared);Assert.AreEqual(1,game.ClearedCount);Assert.IsFalse(original.activeSelf);
                }
                else
                {
                    if(finish=="switch")Click(ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Tool_brush").gameObject);
                    else
                    {
                        game.Tick(game.Config.TimeLimitSeconds + 1);
                        Assert.IsTrue(game.Settled);
                        Assert.AreEqual("timeout",game.Result.Outcome);
                        Refresh();
                    }
                    Click(bin);Assert.AreEqual(0,game.ClearedCount);Assert.IsTrue(original.activeSelf);
                }
                Assert.IsFalse(held.gameObject.activeSelf);
            }
            finally{item?.Dispose();UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [TestCase("PathCleaning")]
        [TestCase("OfferingSort")]
        public void ActualPrefabShowsTimeUpAfterDeadline(string kind)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Timeout notice test", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root, scene);
            EUIItem item = null;
            try
            {
                var prefab = Resources.Load<GameObject>("UI/Module/" + kind + "/Prefabs/EUI" + kind + "Item");
                var ui = UnityEngine.Object.Instantiate(prefab, root.transform, false);
                Assert.IsTrue(EUIItemFactory.TryCreate(ui, out item, out var error), error);
                item.Show();Canvas.ForceUpdateCanvases();
                object game;
                double timeout;
                Func<bool> settled;
                object[] arguments;
                if(kind == "PathCleaning")
                {
                    var cleaning = new PathCleaningGame(PathCleaningModule.LoadConfig()[1], PathCleaningModule.LoadTypes(), PathCleaningModule.LoadTools(), 3);
                    game = cleaning;
                    timeout = cleaning.Config.TimeLimitSeconds;
                    settled = () => cleaning.Settled && cleaning.Result.Outcome == "timeout";
                    arguments = new object[] { game, (Action<string>)(_ => {}), (Action<int>)(_ => {}), (Action<int,string>)((_,__) => {}),
                        (Action<int,Vector2,Vector2>)((_,__,___) => {}), (Action<Vector2,Vector2,bool>)((_,__,___) => {}) };
                }
                else
                {
                    var sorting = new Game.OfferingSort.OfferingSortGame(Game.OfferingSort.OfferingSortModule.LoadConfig()[1], 3);
                    game = sorting;
                    timeout = sorting.Config.TimeLimitSeconds;
                    settled = () => sorting.Settled && sorting.Result.Outcome == "timeout";
                    arguments = new object[] { game, (Action<int,int>)((_,__) => {}) };
                }
                var logic = item.Logic;
                logic.GetType().GetMethod("Configure").Invoke(logic, arguments);
                Assert.Greater(timeout, 0);
                game.GetType().GetMethod("Tick").Invoke(game, new object[] { timeout + 1 });
                Assert.IsTrue(settled(), "The configured deadline must be reached before checking the notice");
                logic.GetType().GetMethod("Refresh").Invoke(logic, new[] { game });
                var hint = (TMPro.TMP_Text)logic.ControlMap["Hint"];
                Assert.AreEqual("时间到了！", hint.text);
                Assert.IsTrue(hint.gameObject.activeInHierarchy);
                Assert.AreEqual("剩余 0秒", ((TMPro.TMP_Text)logic.ControlMap["Timer"]).text);
            }
            finally
            {
                item?.Dispose();UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        [TestCase("brush")]
        [TestCase("broom")]
        public void ActualPrefabRoutesSmallPointerDragsToMudAndRefreshesItsTexture(string tool)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("A2 pointer test");
            SceneManager.MoveGameObjectToScene(root, scene);
            EUIItem item = null;
            RenderTexture target = null;
            try
            {
                var cameraObject = new GameObject("Camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 5;
                target = new RenderTexture(1600, 900, 24);
                camera.targetTexture = target;
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var prefab = Resources.Load<GameObject>("UI/Module/PathCleaning/Prefabs/EUIPathCleaningItem");
                var ui = UnityEngine.Object.Instantiate(prefab, canvasObject.transform, false);
                Assert.IsTrue(EUIItemFactory.TryCreate(ui, out item, out var error), error);
                item.Show();
                Canvas.ForceUpdateCanvases();
                var game = new PathCleaningGame(PathCleaningModule.LoadConfig()[0], PathCleaningModule.LoadTypes(), PathCleaningModule.LoadTools(), 3);
                var logic = item.Logic;
                var refresh = logic.GetType().GetMethod("Refresh");
                void Refresh() => refresh.Invoke(logic, new object[] { game });
                logic.GetType().GetMethod("Configure").Invoke(logic, new object[]
                {
                    game,
                    (Action<string>)(id => { game.Select(id); Refresh(); }),
                    (Action<int>)(i => { game.Clean(i); Refresh(); }),
                    (Action<int, string>)((i, destination) => { game.Clean(i, destination); Refresh(); }),
                    (Action<int, Vector2, Vector2>)((i, from, to) => { game.Wipe(i, from, to); Refresh(); }),
                    (Action<Vector2, Vector2, bool>)((from, to, begin) => { game.Sweep(from, to, begin); Refresh(); })
                });
                var button = ui.GetComponentsInChildren<Button>().Single(b => b.name == "Tool_" + tool);
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                Assert.AreEqual(tool, game.SelectedTool);
                Canvas.ForceUpdateCanvases();
                camera.Render(); // Populate Graphic.depth before the raycast in an EditMode preview.
                int index = Array.FindIndex(game.Spots.ToArray(), s => s.Type.Id == "mud");
                var stain = (RectTransform)ui.GetComponentsInChildren<Transform>().Single(t => t.name == "Spot" + index);
                var board = (RectTransform)logic.ControlMap["Board"];
                var spot = game.Spots[index];
                int resolution = PathCleaningGame.MudResolution;
                var original = Enumerable.Range(0, resolution * resolution).Select(spot.IsDirtyPixel).ToArray();
                var from = RectTransformUtility.WorldToScreenPoint(camera, stain.TransformPoint(stain.rect.center));
                var to = from + Vector2.right * (tool == "brush" ? 70 : 160);
                var data = new PointerEventData(EventSystem.current) { position = from, button = PointerEventData.InputButton.Left };
                var hits = new List<RaycastResult>();
                canvasObject.GetComponent<GraphicRaycaster>().Raycast(data, hits);
                Assert.IsNotEmpty(hits, "Visible mud must route to a UI hit target");
                data.pointerPressRaycast = hits[0];
                var pressed = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
                var drag = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
                Assert.AreEqual(board.gameObject, pressed);
                Assert.AreEqual(board.gameObject, drag);
                ExecuteEvents.Execute(drag, data, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(drag, data, ExecuteEvents.beginDragHandler);
                for(int step = 1; step <= 100; step++)
                {
                    data.position = Vector2.Lerp(from, to, step / 100f);
                    ExecuteEvents.Execute(drag, data, ExecuteEvents.dragHandler);
                }
                ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(drag, data, ExecuteEvents.endDragHandler);
                var pixels = stain.GetComponent<Image>().sprite.texture.GetPixels32();
                if(tool == "brush")
                {
                    Assert.Greater(spot.WipedFraction, .15f);
                    Assert.AreEqual(0, pixels[(resolution / 2) * resolution + resolution / 2].a, "The visible center must disappear as it is wiped");
                }
                else
                {
                    int visibleTail = Enumerable.Range(0, pixels.Length).Count(i => !original[i] && pixels[i].a > 8);
                    Assert.Greater(visibleTail, 500, "Real pointer events must produce a visible tail, not just a revision change");
                    int strongTail = Enumerable.Range(0, pixels.Length).Count(i => !original[i] && pixels[i].a > 96);
                    Assert.Greater(strongTail, 500, "The default table must produce a clearly visible deposit, not a nearly transparent trail");
                }
            }
            finally
            {
                item?.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                if(target) UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
