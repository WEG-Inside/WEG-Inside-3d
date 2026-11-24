using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WegInside.Tests
{
    public sealed class MotorStateTests
    {
        [Test]
        public void ExplodingStopsOperationAndOnlyAssembledCanRun()
        {
            var state = new MotorStateManager();
            Assert.That(state.TryRun(),Is.True);
            state.RequestSeparation(1);
            Assert.That(state.Assembly,Is.EqualTo(AssemblyState.Exploding));
            Assert.That(state.Operation,Is.EqualTo(OperationState.Paused));
            Assert.That(state.TryRun(),Is.False);
            state.Advance(.35f);
            Assert.That(state.Separation,Is.EqualTo(.35f));
            state.RequestSeparation(0);
            Assert.That(state.Assembly,Is.EqualTo(AssemblyState.Assembling));
            Assert.That(state.TryRun(),Is.False);
            state.Advance(1);
            Assert.That(state.Assembly,Is.EqualTo(AssemblyState.Assembled));
            Assert.That(state.TryRun(),Is.True);
        }

        [Test]
        public void ImmediateSeparationClampsAndRemainsIncompatibleWithRunning()
        {
            var state = new MotorStateManager();
            state.RequestSeparation(9,true);
            Assert.That(state.Separation,Is.EqualTo(1));
            state.RequestSeparation(.4f,true);
            Assert.That(state.Assembly,Is.EqualTo(AssemblyState.Exploded));
            Assert.That(state.TryRun(),Is.False);
            state.RequestSeparation(-1,true);
            Assert.That(state.Separation,Is.Zero);
            Assert.That(state.TryRun(),Is.True);
        }

        [Test]
        public void IndependentModesAndAssemblyResetTogether()
        {
            var state = new MotorStateManager();
            state.SetXRay(true);
            state.SetIsolation(true);
            state.RequestSeparation(1,true);
            Assert.That(state.XRay && state.Isolated,Is.True);
            state.Reset();
            Assert.That(state.XRay || state.Isolated,Is.False);
            Assert.That(state.Assembly,Is.EqualTo(AssemblyState.Assembled));
            Assert.That(state.Operation,Is.EqualTo(OperationState.Paused));
        }

        [Test]
        public void SavedPrefabHasIndependentPartsAndPersistentGeometry()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Motor_W22.prefab");
            Assert.That(prefab,Is.Not.Null);
            var parts = prefab.GetComponentsInChildren<MotorPart>(true);
            Assert.That(parts.Length,Is.EqualTo(11));
            Assert.That(parts.Select(part => part.Data.Id).Distinct().Count(),Is.EqualTo(11));
            Assert.That(parts.Count(part => part.Rotates),Is.EqualTo(3));
            foreach (var part in parts)
            {
                Assert.That(EditorUtility.IsPersistent(part.Data),Is.True,part.name);
                Assert.That(part.GetComponentsInChildren<Collider>().Length,Is.GreaterThan(0),part.name);
                Assert.That(part.GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThan(0),part.name);
            }
            foreach (var mesh in prefab.GetComponentsInChildren<MeshFilter>())
            {
                Assert.That(EditorUtility.IsPersistent(mesh.sharedMesh),Is.True,mesh.name);
                Assert.That(EditorUtility.IsPersistent(mesh.GetComponent<MeshRenderer>().sharedMaterial),Is.True,mesh.name);
            }
            foreach (var collider in prefab.GetComponentsInChildren<MeshCollider>())
                Assert.That(collider.sharedMesh.HasPreBakeCollisionMesh(false),Is.True,collider.name);
            foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject),Is.Zero,node.name);
        }
    }
}
