using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.CMH.Rewards;
using Game.Table;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed class ABoxFixture : Game.OfferingSort.IOfferingSortService, Game.PathCleaning.IPathCleaningService
        {
            public void Invoke(string key,string id,string payload,Action<string> complete,Action<string> fail)
                => complete(("{\"ElapsedMs\":1000,\"Outcome\":\"success\",\"RewardId\":\"cmh_box_fast\"}").Replace("cmh_box_fast",key=="cmh.a002.path_cleaning"?"cmh_clean_fast":"cmh_box_fast"));
            public void Abort(string id){}
        }
        [TearDown] public void ABoxFixtureCleanup(){Ember.Core.EmberServiceLocator.Unregister<Game.OfferingSort.IOfferingSortService>();Ember.Core.EmberServiceLocator.Unregister<Game.PathCleaning.IPathCleaningService>();}
        private sealed class AResources : INovelResources
        {
            public NarrativeStorySO Story;
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object
                => new Lease<T> { IsDone = true, Asset = typeof(T) == typeof(NarrativeStorySO) ? Story as T : Resources.Load<T>(path) };
        }
        private NarrativeStorySO AStory(int visits, int first)
        {
            NovelRewardService.Install(_engine.Database);
            var original = Resources.Load<NarrativeStorySO>("Config/Narrative/CallMeHeartless/Story_CallMeHeartless");
            var story = UnityEngine.Object.Instantiate(original); _assets.Add(story);
            var data = new SerializedObject(story);
            var globals = data.FindProperty("_globals");
            for (int i = 0; i < globals.arraySize; i++)
            {
                var row = globals.GetArrayElementAtIndex(i); string id = row.FindPropertyRelative("_id").stringValue;
                if (id == "ch2_aVisits" || id == "ch2_aFirstEvent")
                    row.FindPropertyRelative("_value").FindPropertyRelative("_int").intValue = id == "ch2_aVisits" ? visits : first;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); return story;
        }
        private NovelSession ASession(NarrativeStorySO story)
        {
            Ember.Core.EmberServiceLocator.Register<Game.OfferingSort.IOfferingSortService>(new ABoxFixture());Ember.Core.EmberServiceLocator.Register<Game.PathCleaning.IPathCleaningService>(new ABoxFixture());
            var chapter = story.Chapters.Single(c => c.ChapterId == "a9dbb0cd472a4c99a5a1d1ece01bcaa7");
            var call = chapter.Nodes.Single(n => n.name.EndsWith("_A_Call", StringComparison.Ordinal));
            var session = new NovelSession(new NovelNewGameRequest(chapterId: chapter.ChapterId, nodeId: call.NodeId), () => _tables, new AResources { Story = story });
            session.AttachView(new View()); session.Tick(0, 0); return session;
        }
        private void ADrive(NovelSession session, ref long frame)
        {
            session.Tick(10, ++frame);
            Assert.AreNotEqual(NarrativeState.Faulted, session.Snapshot.State, session.Snapshot.Error?.ToString());
            if (session.Snapshot.State == NarrativeState.AwaitingChoice)
            {
                var snapshot = session.Snapshot;
                session.Choose(snapshot.SessionGeneration, snapshot.PositionVersion, snapshot.Options[0].Id, ++frame);
            }
            else session.Advance(++frame);
        }
        [TestCase(0, 0, "random")]
        [TestCase(1, 1, "A002")]
        [TestCase(1, 2, "A001")]
        [TestCase(2, 1, "A003_Intro")]
        [TestCase(3, 2, "A004_Intro")]
        [TestCase(4, 1, "A005")]
        [TestCase(5, 1, "random")]
        [TestCase(6, 2, "random")]
        public void ALineUsesCumulativeVisitRulesAndCompletesOnce(int visits, int first, string expected)
        {
            var story = AStory(visits, first);
            var chapter = story.Chapters.Single(c => c.ChapterId == "a9dbb0cd472a4c99a5a1d1ece01bcaa7");
            using var session = ASession(story); long frame = 0;
            for (int i = 0; i < 30 && session.Snapshot.State != NarrativeState.Revealing && session.Snapshot.State != NarrativeState.AwaitingAdvance; i++) ADrive(session, ref frame);
            var node = chapter.Nodes.Single(n => n.NodeId == session.Snapshot.NodeId);
            string selected = node.name.Replace("CH03_Chapter02_", "");
            if (expected == "random") Assert.That(selected, Is.EqualTo("A001").Or.EqualTo("A002"));
            else Assert.AreEqual(expected, selected);
            Assert.AreEqual(visits + 1, session.Snapshot.GlobalVariables["ch2_aVisits"].Int);
            for (int i = 0; i < 200 && session.Snapshot.GlobalVariables["ch2_monthClean"].Int == 0; i++) ADrive(session, ref frame);
            Assert.AreEqual(1, session.Snapshot.GlobalVariables["ch2_monthClean"].Int);
            Assert.AreEqual(visits + 1, session.Snapshot.GlobalVariables["ch2_aVisits"].Int);
            if (visits == 0) Assert.AreEqual(selected == "A001" ? 1 : 2, session.Snapshot.GlobalVariables["ch2_aFirstEvent"].Int);
            else Assert.AreEqual(first, session.Snapshot.GlobalVariables["ch2_aFirstEvent"].Int);
            if (visits == 2) Assert.AreEqual(15, session.Snapshot.GlobalVariables["player_affection_lin"].Int);
        }
        [Test]
        public void ALineCheckpointRestoresVisitAndFirstChoiceWithoutReentry()
        {
            var story = AStory(1, 1); using var session = ASession(story); long frame = 0;
            for (int i = 0; i < 20 && session.Snapshot.State != NarrativeState.AwaitingAdvance; i++) session.Tick(10, ++frame);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
            using var restored = new NovelSession(checkpoint, () => _tables, new AResources { Story = story });
            for (int i = 0; i < 20 && !restored.RestoreReady && restored.Snapshot.State != NarrativeState.Faulted; i++) restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
            restored.CommitRestore(new View());
            Assert.AreEqual(2, restored.Snapshot.GlobalVariables["ch2_aVisits"].Int);
            Assert.AreEqual(1, restored.Snapshot.GlobalVariables["ch2_aFirstEvent"].Int);
            Assert.AreEqual(session.Snapshot.NodeId, restored.Snapshot.NodeId);
            for (int i = 0; i < 50 && restored.Snapshot.GlobalVariables["ch2_monthClean"].Int == 0; i++) ADrive(restored, ref frame);
            Assert.AreEqual(1, restored.Snapshot.GlobalVariables["ch2_monthClean"].Int);
            Assert.AreEqual(2, restored.Snapshot.GlobalVariables["ch2_aVisits"].Int);
        }
        [Test]
        public void TableRewardReadsTargetAmountMultiplierAndBoundsAtExecution()
        {
            NovelRewardService.Install(_engine.Database);
            var row = new NovelRewardRow("test_redirect", "score", 7, "count", 0, 20, true);
            // Isolated test fixture changes in-memory rows only; authoring CSV and baked assets stay untouched.
            var rowsField = typeof(NovelRewardService).GetField("_rows", BindingFlags.Static | BindingFlags.NonPublic);
            var originalRows = rowsField.GetValue(null);
            var fingerprint = typeof(NovelRewardService).GetProperty("Fingerprint"); string originalHash = NovelRewardService.Fingerprint;
            try
            {
                rowsField.SetValue(null, new Dictionary<string, NovelRewardRow> { { row.Id, row } });
                fingerprint.SetValue(null, NovelRewardService.ConfigurationFingerprint(new[] { row }));
                JsonUtility.FromJsonOverwrite("{\"_globals\":[{\"_id\":\"score\",\"_value\":{\"_type\":1,\"_int\":5}},{\"_id\":\"count\",\"_value\":{\"_type\":1,\"_int\":3}}]}", _story);
                var step = Create<TableRewardStepSO>(); Set(step, "_tableFingerprint", NovelRewardService.Fingerprint);
                var command = new NovelCommand("reward", NovelCommandKind.CustomStep, customStepId: step.ScriptId, value: new NovelValue(row.Id));
                using var session = StepSession(new View(), step, command); session.Tick(0, 1);
                Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, session.Snapshot.Error?.ToString());
                Assert.AreEqual(20, session.Snapshot.GlobalVariables["score"].Int);
                Assert.AreEqual(3, session.Snapshot.GlobalVariables["count"].Int);
                Assert.AreNotEqual(originalHash, NovelRewardService.Fingerprint);
                Assert.AreEqual(5, NovelRewardService.Calculate(new NovelRewardRow("off", "score", 7, "", 0, 20, false), 5, 3));
            }
            finally { rowsField.SetValue(null, originalRows); fingerprint.SetValue(null, originalHash); }
        }
    }
}



