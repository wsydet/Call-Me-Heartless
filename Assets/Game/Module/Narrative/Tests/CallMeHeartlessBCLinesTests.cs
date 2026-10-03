using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core;
using Game.CMH.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private string _bcChoiceNodeId;
        private sealed class BCGardenService : INovelStepService
        {
            public int Invokes, Aborts; public string Payload;
            public Action<string> Complete, Fail;
            public void Invoke(string key, string id, string payload, Action<string> complete, Action<string> fail)
            { Assert.AreEqual("cmh.b.garden_care", key); Invokes++; Payload=payload; Complete = complete; Fail = fail; }
            public void Abort(string id) { Aborts++; }
        }
        private NarrativeStorySO BCStory(params (string id, object value)[] values)
        {
            NovelRewardService.Install(_engine.Database);
            var story = UnityEngine.Object.Instantiate(Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless"));
            _assets.Add(story);
            var so = new SerializedObject(story); var globals = so.FindProperty("_globals");
            foreach (var entry in values)
            {
                bool found = false;
                for (int i = 0; i < globals.arraySize; ++i)
                {
                    var row = globals.GetArrayElementAtIndex(i);
                    if (row.FindPropertyRelative("_id").stringValue != entry.id) continue;
                    var val = row.FindPropertyRelative("_value"); found = true;
                    if (entry.value is bool b) val.FindPropertyRelative("_bool").boolValue = b;
                    else val.FindPropertyRelative("_int").intValue = (int)entry.value;
                }
                Assert.IsTrue(found, entry.id);
            }
            so.ApplyModifiedPropertiesWithoutUndo(); return story;
        }
        private NovelSession BCSession(NarrativeStorySO story, string branch)
        {
            var chapter = story.Chapters.Single(c => c.ChapterId == "a9dbb0cd472a4c99a5a1d1ece01bcaa7");
            var call = chapter.Nodes.Single(n => n.name == "CH03_Chapter02_" + branch + (branch == "Evening" ? "" : "_Call"));
            _bcChoiceNodeId = chapter.Nodes.Single(n => n.name == "CH03_Chapter02_C003_Choice").NodeId;
            var session = new NovelSession(new NovelNewGameRequest(chapterId: chapter.ChapterId, nodeId: call.NodeId), () => _tables, new AResources { Story = story });
            session.AttachView(new View()); session.Tick(0, 0); return session;
        }
        private string BCNode(NarrativeStorySO story, NovelSession session)
            => story.Chapters.SelectMany(c => c.Nodes).Single(n => n.NodeId == session.Snapshot.NodeId).name.Replace("CH03_Chapter02_", "");
        private void BCDrive(NovelSession session, ref long frame, int choice = 0)
        {
            session.Tick(1, ++frame);
            Assert.AreNotEqual(NarrativeState.Faulted, session.Snapshot.State, session.Snapshot.Error?.ToString());
            if (session.Snapshot.State == NarrativeState.AwaitingChoice)
            {
                if (session.Snapshot.NodeId != _bcChoiceNodeId) return;
                var snapshot = session.Snapshot;
                session.Choose(snapshot.SessionGeneration, snapshot.PositionVersion, snapshot.Options[choice].Id, ++frame);
            }
            else session.Advance(++frame);
        }
        private void BCAssertSingleReturn(NovelSession session, ref long frame, string counter, int value)
        {
            for (int i = 0; i < 30 && session.Snapshot.State != NarrativeState.AwaitingChoice; ++i) BCDrive(session, ref frame);
            Assert.AreEqual(NarrativeState.AwaitingChoice, session.Snapshot.State);
            Assert.AreEqual(value, session.Snapshot.GlobalVariables[counter].Int);
            Assert.AreEqual(1, session.Snapshot.GlobalVariables["ch2_monthStep"].Int);
            for (int i = 0; i < 5; ++i) session.Tick(1, ++frame);
            Assert.AreEqual(value, session.Snapshot.GlobalVariables[counter].Int);
            Assert.AreEqual(1, session.Snapshot.GlobalVariables["ch2_monthStep"].Int);
        }

        [TestCase(0, 0, "C001", 0)]
        [TestCase(1, 0, "C002", 0)]
        [TestCase(2, 0, "C003_Intro", 15)]
        [TestCase(2, 1, "C003_Intro", 0)]
        [TestCase(3, 0, "C004", 0)]
        [TestCase(7, 0, "C004", 0)]
        public void BCCLinePlaysExpectedEventAndSettlesOnce(int visits, int choice, string expected, int reward)
        {
            var story = BCStory(("ch2_cVisits", visits));
            using var session = BCSession(story, "C"); long frame = 0;
            for (int i = 0; i < 20 && session.Snapshot.State != NarrativeState.Revealing && session.Snapshot.State != NarrativeState.AwaitingAdvance; ++i) BCDrive(session, ref frame);
            Assert.AreEqual(expected, BCNode(story, session));
            Assert.AreEqual(visits, session.Snapshot.GlobalVariables["ch2_cVisits"].Int);
            for (int i = 0; i < 180 && session.Snapshot.GlobalVariables["ch2_cVisits"].Int == visits; ++i) BCDrive(session, ref frame, choice);
            Assert.AreEqual(visits + 1, session.Snapshot.GlobalVariables["ch2_cVisits"].Int);
            Assert.AreEqual(reward, session.Snapshot.GlobalVariables["player_affection_shen"].Int);
            BCAssertSingleReturn(session, ref frame, "ch2_cVisits", visits + 1);
            // 后山属于傍晚菜单；将本次真实结算出的次数传入该菜单验证可见性。
            using var evening = BCSession(BCStory(("ch2_cVisits", session.Snapshot.GlobalVariables["ch2_cVisits"].Int)), "Evening");
            Assert.AreEqual(visits >= 1, evening.Snapshot.Options.Any(o => o.Text.Contains("后山")));
        }

        [TestCase(true, false, true, false, "B003")]
        [TestCase(true, true, true, false, "B004")]
        [TestCase(false, false, true, false, "B004")]
        public void BCSpecialBEventsRespectPrerequisitesPriorityAndPlayOnce(bool met, bool played3, bool outing, bool played4, string expected)
        {
            var story = BCStory(("ch2_metMurong", met), ("ch2_b003Played", played3), ("ch2_hasOutingWithYan", outing), ("ch2_b004Played", played4));
            using var session = BCSession(story, "B"); long frame = 0;
            for (int i = 0; i < 20 && session.Snapshot.State != NarrativeState.Revealing && session.Snapshot.State != NarrativeState.AwaitingAdvance; ++i) BCDrive(session, ref frame);
            Assert.AreEqual(expected, BCNode(story, session));
            Assert.AreEqual(0, session.Snapshot.GlobalVariables["ch2_monthPlant"].Int);
            for (int i = 0; i < 120 && session.Snapshot.GlobalVariables["ch2_monthPlant"].Int == 0; ++i) BCDrive(session, ref frame);
            Assert.IsTrue(session.Snapshot.GlobalVariables[expected == "B003" ? "ch2_b003Played" : "ch2_b004Played"].Bool);
            Assert.AreEqual(0, session.Snapshot.GlobalVariables["player_affection_murong"].Int);
            BCAssertSingleReturn(session, ref frame, "ch2_monthPlant", 1);
        }

        [TestCase("completed")]
        [TestCase("timeout")]
        [TestCase("failure")]
        [TestCase("cancel")]
        [TestCase("restore")]
        public void BCGardenWaitsWithoutIncomeAndHandlesCompletionFailureAndCancellation(string result)
        {
            var service = new BCGardenService(); var previous = EmberServiceLocator.TryResolve<INovelStepService>();
            EmberServiceLocator.Register<INovelStepService>(service);
            try
            {
                int previousVisits = result == "timeout" ? 1 : 0;
                var story = BCStory(("ch2_metMurong", true), ("ch2_hasOutingWithYan", true), ("ch2_b003Played", true), ("ch2_b004Played", true),
                    ("ch2_gardenLearned", result == "timeout"), ("ch2_gardenVisits_0", previousVisits), ("ch2_gardenVisits_1", previousVisits));
                using var session = BCSession(story, "B"); long frame = 0;
                for (int i = 0; i < 20 && service.Invokes == 0; ++i) BCDrive(session, ref frame);
                Assert.AreEqual(1, service.Invokes);
                if(result=="timeout") StringAssert.Contains("\"tutorial\":false",service.Payload);
                if(result!="timeout")
                {
                    StringAssert.Contains("\"tutorial\":true",service.Payload);
                    session.Tick(600,++frame);
                    Assert.AreNotEqual(NarrativeState.Faulted,session.Snapshot.State,"First visit must not hit bridge timeout");
                }
                Assert.AreEqual(0, session.Snapshot.GlobalVariables["ch2_monthPlant"].Int);
                Assert.IsFalse(session.TryCapture(out _, out _), "An active external game is not a stable checkpoint");
                session.Pause("test"); session.Tick(200, ++frame);
                Assert.AreNotEqual(NarrativeState.Faulted, session.Snapshot.State); session.Resume("test");
                if (result == "failure")
                {
                    service.Fail("test failure"); session.Tick(0, ++frame);
                    Assert.AreEqual(NarrativeState.Faulted, session.Snapshot.State);
                    Assert.AreEqual(0, session.Snapshot.GlobalVariables["ch2_monthPlant"].Int); return;
                }
                if (result == "cancel")
                {
                    session.Dispose(); service.Complete("completed"); session.Tick(0, ++frame);
                    Assert.AreEqual(NarrativeState.Cancelled, session.Snapshot.State); Assert.AreEqual(1, service.Aborts); return;
                }
                service.Complete(result == "restore" ? "completed" : result); service.Complete(result);
                if (result == "restore")
                {
                    session.Tick(10, ++frame);
                    Assert.AreEqual(0, session.Snapshot.GlobalVariables["ch2_monthPlant"].Int);
                    Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
                    using var restored = new NovelSession(checkpoint, () => _tables, new AResources { Story = story });
                    for (int i = 0; i < 20 && !restored.RestoreReady; ++i) restored.Tick(0, ++frame);
                    Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString()); restored.CommitRestore(new View());
                    for (int i = 0; i < 30 && restored.Snapshot.GlobalVariables["ch2_monthPlant"].Int == 0; ++i) BCDrive(restored, ref frame);
                    BCAssertSingleReturn(restored, ref frame, "ch2_monthPlant", 1);
                    Assert.AreEqual(1, service.Invokes, "Restoring after the game must not launch it again"); return;
                }
                for (int i = 0; i < 30 && session.Snapshot.GlobalVariables["ch2_monthPlant"].Int == 0; ++i) BCDrive(session, ref frame);
                Assert.AreEqual(1, service.Invokes);
                BCAssertSingleReturn(session, ref frame, "ch2_monthPlant", 1);
            }
            finally
            {
                if (previous != null) EmberServiceLocator.Register<INovelStepService>(previous);
                else EmberServiceLocator.Unregister<INovelStepService>();
            }
        }

        [Test]
        public void BCCRewardCheckpointDoesNotReplayRewardOrVisitOnRestore()
        {
            var story = BCStory(("ch2_cVisits", 2)); using var session = BCSession(story, "C"); long frame = 0;
            for (int i = 0; i < 160 && BCNode(story, session) != "C003_Common"; ++i) BCDrive(session, ref frame);
            session.Tick(10, ++frame);
            Assert.AreEqual(15, session.Snapshot.GlobalVariables["player_affection_shen"].Int);
            Assert.AreEqual(2, session.Snapshot.GlobalVariables["ch2_cVisits"].Int);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
            using var restored = new NovelSession(checkpoint, () => _tables, new AResources { Story = story });
            for (int i = 0; i < 20 && !restored.RestoreReady; ++i) restored.Tick(0, ++frame);
            Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString()); restored.CommitRestore(new View());
            for (int i = 0; i < 30 && restored.Snapshot.GlobalVariables["ch2_cVisits"].Int == 2; ++i) BCDrive(restored, ref frame);
            Assert.AreEqual(15, restored.Snapshot.GlobalVariables["player_affection_shen"].Int);
            BCAssertSingleReturn(restored, ref frame, "ch2_cVisits", 3);
        }
    }
}
