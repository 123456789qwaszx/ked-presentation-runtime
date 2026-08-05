using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ked.Presentation.Core.Tests
{
    /// <summary>
    /// b-2 수용 기준 그 자체:
    /// "실제 리그 스키마를 넣으면 유니티 계층과 같은 부모 관계가 재현되고,
    ///  같은 노드에 대해 b-1 하네스와 같은 좌표가 나온다."
    ///
    /// 실제 덤프(ExportedTuning/rig-schemas.json)를 JsonUtility로 코어 DTO에 넣고
    /// (= 유니티 호스트의 실제 역직렬화 경로 검증),
    /// 같은 리그를 실제 빌더·실제 프리팹(덤프에 기록된 sourcePrefab)으로 세워 비교한다.
    ///
    /// 이 테스트는 덤프가 낡아도 잡는다 — 빌더나 프리팹이 바뀌었는데 덤프를
    /// 다시 내보내지 않았다면 여기서 소리가 난다. 그것이 의도다.
    /// </summary>
    public sealed class RigSchemaTreeUnityParityTests
    {
        private const float Eps = 0.01f; // b-1 하네스와 같은 ε (Documentation~ 참조)

        private static readonly Vector3[] SamplePoints =
        {
            Vector3.zero,
            new Vector3(123.4f, -56.7f, 0f),
            new Vector3(-321f, 210f, 0f),
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private static RigSchemasFileDto LoadDump()
        {
            string path = Path.Combine(Application.dataPath, "..", "ExportedTuning", "rig-schemas.json");

            if (!File.Exists(path))
            {
                Assert.Fail(
                    $"U12 덤프가 없다: {path}\n" +
                    "메뉴 Ked/U12/Export Presentation Tuning Dump 를 먼저 실행할 것.");
            }

            RigSchemasFileDto file = JsonUtility.FromJson<RigSchemasFileDto>(File.ReadAllText(path));

            Assert.That(file, Is.Not.Null, "역직렬화 실패");
            Assert.That(file.rigs, Is.Not.Empty, "rigs가 비었다");

            return file;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);
            }

            _spawned.Clear();
        }

        [TestCase("character")]
        [TestCase("background")]
        [TestCase("overlay")]
        [TestCase("screenEffect")]
        public void 리그_4종_부모_관계와_좌표가_유니티와_같다(string rigKind)
        {
            RigSchemasFileDto file = LoadDump();
            RigSchemaRigDto rigDto = file.rigs.Find(r => r.rigKind == rigKind);
            Assert.That(rigDto, Is.Not.Null, $"덤프에 '{rigKind}'가 없다");

            // 1) 코어: 덤프 → 트리.
            RectNodeTree tree = RigSchemaLoader.BuildTree(file, rigKind);

            // 2) 유니티: 익스포터와 같은 조건으로 실물 리그를 세운다.
            RectTransform stage = CreateStage(file.capturedUnderParentSize.ToVec2());
            RectTransform rigRoot = BuildUnityRig(rigKind, rigDto.sourcePrefab);
            rigRoot.SetParent(stage, false);

            // 3) 부모 관계: 덤프의 모든 노드가 유니티 계층에서 같은 부모를 가진다.
            foreach (RigSchemaNodeDto node in rigDto.nodes)
            {
                if (node.id == RigSchemaLoader.RootKey)
                    continue;

                RectTransform rect = FindByName(rigRoot, node.id);
                Assert.That(rect, Is.Not.Null, $"{rigKind}: 유니티 리그에 '{node.id}'가 없다");

                string unityParent = rect.parent == rigRoot
                    ? RigSchemaLoader.RootKey
                    : rect.parent.name;

                Assert.That(unityParent, Is.EqualTo(node.parent),
                    $"{rigKind}: '{node.id}'의 부모가 다르다");
                Assert.That(tree.GetParentKey(node.id), Is.EqualTo(node.parent),
                    $"{rigKind}: 트리의 '{node.id}' 부모가 덤프와 다르다");
            }

            // 4) 좌표: 트리 조회가 실물 RectTransform과 같은 값을 낸다.
            foreach (RigSchemaNodeDto node in rigDto.nodes)
            {
                RectTransform rect = node.id == RigSchemaLoader.RootKey
                    ? rigRoot
                    : FindByName(rigRoot, node.id);

                foreach (Vector3 p in SamplePoints)
                {
                    Vector3 unityWorld = stage.InverseTransformPoint(rect.TransformPoint(p));
                    Vec3 coreWorld = tree.TransformPoint(node.id, new Vec3(p.x, p.y, p.z));

                    Assert.That(coreWorld.X, Is.EqualTo(unityWorld.x).Within(Eps),
                        $"{rigKind}/{node.id} TransformPoint({p}) X");
                    Assert.That(coreWorld.Y, Is.EqualTo(unityWorld.y).Within(Eps),
                        $"{rigKind}/{node.id} TransformPoint({p}) Y");
                }

                // rect 크기도 파생이 맞는지 본다.
                Vec2 coreSize = tree.GetRectSize(node.id);
                Assert.That(coreSize.X, Is.EqualTo(rect.rect.size.x).Within(Eps),
                    $"{rigKind}/{node.id} rect width");
                Assert.That(coreSize.Y, Is.EqualTo(rect.rect.size.y).Within(Eps),
                    $"{rigKind}/{node.id} rect height");
            }
        }

        [Test]
        public void 인스턴스_prefix로_같은_리그를_두_벌_세울_수_있다()
        {
            RigSchemasFileDto file = LoadDump();

            RectNodeTree c1 = RigSchemaLoader.BuildTree(file, "character", "c1/");
            RectNodeTree c2 = RigSchemaLoader.BuildTree(file, "character", "c2/");

            Assert.That(c1.Contains("c1/CharSlot_Track"), Is.True);
            Assert.That(c2.Contains("c2/CharSlot_Track"), Is.True);
            Assert.That(c1.Contains("c2/CharSlot_Track"), Is.False);
        }

        // ── helper ───────────────────────────────────────────────────

        private RectTransform CreateStage(Vec2 size)
        {
            GameObject go = new GameObject("ParityStage", typeof(RectTransform));
            _spawned.Add(go);

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f); // 익스포터의 캡처 스테이지와 동일
            rt.sizeDelta = new Vector2(size.X, size.Y);

            return rt;
        }

        /// <summary>익스포터와 같은 경로: 덤프에 기록된 프리팹 + 실제 빌더 + 그래프 검증.</summary>
        private RectTransform BuildUnityRig(string rigKind, string sourcePrefabPath)
        {
            RectTransform prefab = string.IsNullOrEmpty(sourcePrefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<RectTransform>(sourcePrefabPath);

            if (!string.IsNullOrEmpty(sourcePrefabPath))
            {
                Assert.That(prefab, Is.Not.Null,
                    $"{rigKind}: 덤프가 기록한 프리팹을 못 찾았다: {sourcePrefabPath}");
            }

            RectTransform root;

            switch (rigKind)
            {
                case "character":
                {
                    CharacterRigBuilder builder = new CharacterRigBuilder();
                    root = builder.BuildCharacterRigRoot(prefab);
                    _spawned.Add(root.gameObject);
                    builder.BindRefsFromRoot(root, "", out _);
                    break;
                }
                case "background":
                {
                    BackgroundRigBuilder builder = new BackgroundRigBuilder();
                    root = builder.BuildBackgroundRigRoot(prefab);
                    _spawned.Add(root.gameObject);
                    builder.BindRefsFromRoot(root, "", out _);
                    break;
                }
                case "overlay":
                {
                    OverlayRigBuilder builder = new OverlayRigBuilder();
                    root = builder.BuildOverlayRoot(prefab);
                    _spawned.Add(root.gameObject);
                    builder.BindRefsFromRoot(root, "", out _);
                    break;
                }
                case "screenEffect":
                {
                    ScreenEffectRigBuilder builder = new ScreenEffectRigBuilder();
                    root = builder.BuildRigRoot();
                    _spawned.Add(root.gameObject);
                    builder.BindRefsFromRoot(root, out _);
                    break;
                }
                default:
                    Assert.Fail($"모르는 rigKind: {rigKind}");
                    return null;
            }

            return root;
        }

        private static RectTransform FindByName(Transform root, string name)
        {
            if (root.name == name)
                return root as RectTransform;

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform found = FindByName(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
