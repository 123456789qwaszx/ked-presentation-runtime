using System.Collections.Generic;
using System.Linq;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ked.Presentation.Tests
{
    /// <summary>
    /// 이모지의 움직임(docs/emoji-format.md) — 형식의 규칙 · 그 프레임의 모습 · 재생의 단계 · 접기,
    /// 그리고 런타임의 재생 시스템이 그 값을 노드에 옮기는가.
    /// </summary>
    public sealed class EmojiPlaybackTests
    {
        private const float Eps = 1e-4f;

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        // ── 견본 ─────────────────────────────────────────────────────

        private static EmojiKeyDto K(float f, float v, bool step = false) => new() { f = f, v = v, step = step };
        private static EmojiKeyDto S(float f, string sprite) => new() { f = f, s = sprite };

        private static EmojiTrackDto Tr(string rig, string channel, params EmojiKeyDto[] keys)
            => new() { rig = rig, channel = channel, keys = keys.ToList() };

        private static EmojiClipDto Clip(string name, int length, params EmojiTrackDto[] tracks)
            => new() { name = name, length = length, tracks = tracks.ToList() };

        private static List<EmojiRigDto> Rigs(params string[] names)
            => names.Select(n => new EmojiRigDto { name = n, sprite = "s" }).ToList();

        private static EmojiDto Static() => new() { name = "static", rigs = Rigs("a") };

        // 머묾 없는 이모지 — 13프레임을 한 번.
        private static EmojiDto Shot() => new()
        {
            name = "shot",
            rigs = Rigs("a"),
            clips = new List<EmojiClipDto>
            {
                Clip("m", 13,
                    Tr("a", "scaleX", K(0, 0.5f), K(12, 1f)),
                    Tr("", "y", K(0, 0f), K(12, 24f)),
                    Tr("a", "sprite", S(0, "s1"), S(6, "s2")),
                    Tr("a", "alpha", K(0, 1f, step: true), K(6, 0.5f))),
            },
            main = "m",
        };

        // 머무는 이모지 — 등장 4 · 머묾 12 · 퇴장 6.
        private static EmojiDto Hold() => new()
        {
            name = "hold",
            offset = new Float2Dto { x = 100f, y = 50f },
            rigs = Rigs("a"),
            clips = new List<EmojiClipDto>
            {
                Clip("i", 4, Tr("a", "scaleX", K(0, 1.27f), K(3, 1f))),
                Clip("h", 12, Tr("a", "scaleX", K(0, 1f), K(6, 1.08f), K(11, 1f)), Tr("", "x", K(0, 0f), K(11, 22f))),
                Clip("o", 6, Tr("", "alpha", K(0, 1f), K(5, 0f))),
            },
            @in = "i",
            hold = "h",
            @out = "o",
        };

        private static Dictionary<string, EmojiDefinition> Build(List<string> warned, params EmojiDto[] emojis)
        {
            EmojisFileDto file = new() { schema = EmojiDefinitionLoader.Schema, fps = 24f, emojis = emojis.ToList() };
            return EmojiDefinitionLoader.Build(file, (name, why) => warned?.Add($"{name}: {why}"));
        }

        private static EmojiDefinition One(EmojiDto dto)
        {
            List<string> warned = new();
            Dictionary<string, EmojiDefinition> emojis = Build(warned, dto);
            Assert.That(warned, Is.Empty);
            return emojis[dto.name];
        }

        // ── 형식의 규칙 ──────────────────────────────────────────────

        [Test]
        public void 머묾이_있는가가_종류를_정한다()
        {
            Assert.That(One(Static()).Stays, Is.True, "클립이 없으면 정의의 모습으로 서 있다");
            Assert.That(One(Hold()).Stays, Is.True);
            Assert.That(One(Shot()).Stays, Is.False);
        }

        [Test]
        public void 어긋난_이모지는_걸러지고_까닭이_남는다()
        {
            List<string> warned = new();

            Dictionary<string, EmojiDefinition> emojis = Build(warned,
                new EmojiDto { name = "badlink", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5) }, main = "zzz" },
                new EmojiDto { name = "nolink", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5) } },
                new EmojiDto { name = "inwithouthold", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5) }, main = "m", @in = "m" },
                new EmojiDto { name = "mainandhold", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5), Clip("h", 5) }, main = "m", hold = "h" },
                new EmojiDto { name = "badrig", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5, Tr("zz", "x", K(0, 0))) }, main = "m" },
                new EmojiDto { name = "badchannel", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5, Tr("a", "foo", K(0, 0))) }, main = "m" },
                new EmojiDto { name = "outofrange", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5, Tr("a", "x", K(0, 0), K(5, 1))) }, main = "m" },
                new EmojiDto { name = "unsorted", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5, Tr("a", "x", K(2, 0), K(1, 1))) }, main = "m" },
                new EmojiDto { name = "spriteonwhole", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 5, Tr("", "sprite", S(0, "x"))) }, main = "m" },
                new EmojiDto { name = "zerolength", rigs = Rigs("a"), clips = new List<EmojiClipDto> { Clip("m", 0) }, main = "m" },
                Static());

            Assert.That(emojis.Keys, Is.EquivalentTo(new[] { "static" }));
            Assert.That(warned, Has.Count.EqualTo(10), string.Join("\n", warned));
        }

        // ── 그 프레임의 모습 ─────────────────────────────────────────

        [Test]
        public void 키_사이는_곡선으로_잇고_키가_없는_채널은_중립값이다()
        {
            EmojiClip clip = One(Shot()).Main;

            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.ScaleX, 0f), Is.EqualTo(0.5f).Within(Eps));
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.ScaleX, 12f), Is.EqualTo(1f).Within(Eps));

            // 기울기를 생략하면 키에서 평평하다 — 가운데는 두 값의 가운데다.
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.ScaleX, 6f), Is.EqualTo(0.75f).Within(Eps));

            // 클립 밖은 끝값.
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.ScaleX, 99f), Is.EqualTo(1f).Within(Eps));

            // 키가 없는 채널.
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.ScaleY, 3f), Is.EqualTo(1f));
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.Rotation, 3f), Is.EqualTo(0f));
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.PositionX, 3f), Is.EqualTo(0f));

            // 빈 이름 = 이모지 전체.
            Assert.That(EmojiClipMath.Sample(clip, "", EmojiChannel.PositionY, 12f), Is.EqualTo(24f).Within(Eps));

            // 클립이 없으면 대상 전체가 중립이다.
            EmojiNodePose neutral = EmojiClipMath.SampleNode(null, "a", 3f);
            Assert.That(neutral.Scale.X, Is.EqualTo(1f));
            Assert.That(neutral.Alpha, Is.EqualTo(1f));
            Assert.That(neutral.SpriteKey, Is.Null);
        }

        [Test]
        public void 계단_키는_다음_키까지_멈춰_있다()
        {
            EmojiClip clip = One(Shot()).Main;

            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.Alpha, 0f), Is.EqualTo(1f));
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.Alpha, 5.9f), Is.EqualTo(1f));
            Assert.That(EmojiClipMath.Sample(clip, "a", EmojiChannel.Alpha, 6f), Is.EqualTo(0.5f));
        }

        [Test]
        public void 그림_바꾸기는_그_프레임부터_바뀐다()
        {
            EmojiClip clip = One(Shot()).Main;

            Assert.That(EmojiClipMath.SampleSprite(clip, "a", 0f), Is.EqualTo("s1"));
            Assert.That(EmojiClipMath.SampleSprite(clip, "a", 5.99f), Is.EqualTo("s1"));
            Assert.That(EmojiClipMath.SampleSprite(clip, "a", 6f), Is.EqualTo("s2"));
            Assert.That(EmojiClipMath.SampleSprite(clip, "a", 99f), Is.EqualTo("s2"));

            // 그림 바꾸기 채널이 없으면 null — 리그의 기본 스프라이트다.
            Assert.That(EmojiClipMath.SampleSprite(One(Hold()).Hold, "a", 0f), Is.Null);
        }

        // ── 재생의 단계 ──────────────────────────────────────────────

        [Test]
        public void 머묾_없는_이모지는_한_번_재생하고_끝난다()
        {
            EmojiPlayback playback = new(One(Shot()));

            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Main));

            playback.Advance(6f / 24f);
            Assert.That(playback.Frame, Is.EqualTo(6f).Within(1e-3f));

            // 마지막 프레임(12)도 한 프레임만큼 보인다 — 13프레임짜리다.
            playback.Advance(6.9f / 24f);
            Assert.That(playback.IsDone, Is.False);

            playback.Advance(0.2f / 24f);
            Assert.That(playback.IsDone, Is.True);
        }

        [Test]
        public void 머무는_이모지는_등장_뒤에_머묾을_되풀이한다()
        {
            EmojiPlayback playback = new(One(Hold()));

            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.In));

            // 등장(4프레임)을 넘으면 머묾의 프레임 0에서 잇는다 — 넘친 한 프레임만큼 앞서서.
            playback.Advance(5f / 24f);
            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Hold));
            Assert.That(playback.Frame, Is.EqualTo(1f).Within(1e-3f));

            // 머묾의 주기는 12프레임이다.
            playback.Advance(12f / 24f);
            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Hold));
            Assert.That(playback.Frame, Is.EqualTo(1f).Within(1e-2f));

            playback.Advance(100f);
            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Hold));
            Assert.That(playback.Frame, Is.InRange(0f, 12f));
        }

        [Test]
        public void 내리면_퇴장을_재생하고_끝난다_등장_도중이어도_바로_넘어간다()
        {
            EmojiPlayback playback = new(One(Hold()));
            playback.Advance(1f / 24f);
            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.In));

            playback.RequestOff();
            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Out));
            Assert.That(playback.Frame, Is.EqualTo(0f));

            // 퇴장 중에 다시 내려도 처음으로 돌아가지 않는다.
            playback.Advance(3f / 24f);
            playback.RequestOff();
            Assert.That(playback.Frame, Is.EqualTo(3f).Within(1e-3f));

            playback.Advance(3f / 24f);
            Assert.That(playback.IsDone, Is.True);
        }

        [Test]
        public void 퇴장이_없으면_내리는_순간_끝난다()
        {
            EmojiPlayback playback = new(One(Static()));

            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Hold));
            Assert.That(playback.Clip, Is.Null);

            playback.Advance(5f);
            Assert.That(playback.Phase, Is.EqualTo(EmojiPhase.Hold));

            playback.RequestOff();
            Assert.That(playback.IsDone, Is.True);
        }

        [Test]
        public void 건너뛴_뒤의_모습_머무는_것은_머묾의_처음_아닌_것은_없다()
        {
            EmojiPlayback hold = new(One(Hold()));
            hold.Settle();
            Assert.That(hold.Phase, Is.EqualTo(EmojiPhase.Hold));
            Assert.That(hold.Frame, Is.EqualTo(0f));

            EmojiPlayback shot = new(One(Shot()));
            shot.Settle();
            Assert.That(shot.IsDone, Is.True);
        }

        // ── 접기 ─────────────────────────────────────────────────────

        private static Float2Dto F2(float x, float y) => new() { x = x, y = y };
        private static Float3Dto F3(float x, float y, float z) => new() { x = x, y = y, z = z };

        private static StageCommand Cmd(string name, params string[] args) => new(name, args, "test.yarn:1");

        private static StageState Fold(params StageCommand[] commands)
        {
            RigSchemaNodeDto root = new()
            {
                id = "__root",
                parent = "",
                anchoredPosition = F2(0f, 0f),
                anchorMin = F2(0f, 0f),
                anchorMax = F2(1f, 1f),
                pivot = F2(0.5f, 0.5f),
                sizeDelta = F2(0f, 0f),
                localScale = F3(1f, 1f, 1f),
                localEulerAngles = F3(0f, 0f, 0f),
                measuredRectSize = F2(0f, 0f),
            };

            StageReducerTuning tuning = new()
            {
                BaseResolution = new Vec2(1920f, 1080f),
                ReferenceStageWidth = 1920f,
                RigSchemas = new RigSchemasFileDto
                {
                    capturedUnderParentSize = F2(1920f, 1080f),
                    rigs = new List<RigSchemaRigDto>
                    {
                        new() { rigKind = "character", sourcePrefab = "", nodes = new List<RigSchemaNodeDto> { root } },
                    },
                },
                Emojis = Build(null, Static(), Shot(), Hold()),
            };

            return StageReducer.ApplyAll(StageReducer.CreateInitialState(tuning), commands, tuning);
        }

        [Test]
        public void 접으면_머무는_이모지만_남는다()
        {
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "hold"),
                Cmd("emoji", "s1", "shot"),
                Cmd("emoji", "s1", "static"));

            // 머묾 없는 이모지는 접을 것이 없다 — Unhandled도 아니다.
            Assert.That(state.Unhandled, Is.Empty);
            Assert.That(state.EmojisOf("s1"), Is.EqualTo(new[] { "hold", "static" }));
        }

        [Test]
        public void 머묾_없는_이모지를_이름으로_내리면_Unhandled다()
        {
            StageState state = Fold(
                Cmd("slot", "s1"),
                Cmd("emoji", "s1", "shot"),
                Cmd("emoji_off", "s1", "shot"));

            Assert.That(state.Unhandled, Has.Count.EqualTo(1));
        }

        // ── 런타임의 재생 시스템 ─────────────────────────────────────

        private EmojiRefs Stand(EmojiRigBuilder builder, EmojiDefinition definition)
        {
            GameObject go = new("s1_CharSlot_Scale", typeof(RectTransform));
            _spawned.Add(go);

            RectTransform locator = builder.EnsureLocator((RectTransform)go.transform, "s1_");
            return builder.BuildEmoji(locator, "s1_", definition.Name, definition.Rigs.Select(r => r.Name).ToArray());
        }

        [Test]
        public void 재생을_시작하면_첫_프레임의_모습이_바로_선다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Hold());
            EmojiRefs emoji = Stand(builder, definition);

            system.Play(emoji, definition, settle: false);

            // 등장의 프레임 0 — 배율 1.27. 이모지 전체는 오프셋 값 그대로.
            Assert.That(emoji.Rigs[0].EmojiRig_Image.localScale.x, Is.EqualTo(1.27f).Within(Eps));
            Assert.That(emoji.Emoji_Offset.anchoredPosition, Is.EqualTo(new Vector2(100f, 50f)));
            Assert.That(system.IsPlaying(emoji), Is.True);
        }

        [Test]
        public void 시간을_넘기면_그_프레임의_모습을_쓴다_전체의_자리는_오프셋에_더한다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Hold());
            EmojiRefs emoji = Stand(builder, definition);

            system.Play(emoji, definition, settle: true);

            // 건너뛴 뒤 = 머묾의 프레임 0. 11프레임을 넘기면 머묾의 프레임 11이다.
            system.Tick(11f / 24f);

            Assert.That(emoji.Rigs[0].EmojiRig_Image.localScale.x, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(emoji.Emoji_Offset.anchoredPosition.x, Is.EqualTo(100f + 22f).Within(1e-2f));
            Assert.That(emoji.Emoji_Offset.anchoredPosition.y, Is.EqualTo(50f).Within(1e-3f));

            // 기본 자리(Place)는 재생이 건드리지 않는다.
            Assert.That(emoji.Rigs[0].EmojiRig_Place.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void 머묾_없는_이모지는_끝나면_스스로_부서진다_건너뛰면_바로_부서진다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Static());

            // 견본의 shot은 그림 바꾸기가 있어 Resources를 연다 — 여기서는 배율만 있는 한 번짜리를 쓴다.
            EmojiDefinition shot = One(new EmojiDto
            {
                name = "pop",
                rigs = Rigs("a"),
                clips = new List<EmojiClipDto> { Clip("m", 6, Tr("a", "scaleX", K(0, 0f), K(5, 1f))) },
                main = "m",
            });

            EmojiRefs emoji = Stand(builder, shot);
            system.Play(emoji, shot, settle: false);
            Assert.That(system.Count, Is.EqualTo(1));

            system.Tick(1f);
            Assert.That(system.Count, Is.EqualTo(0));
            Assert.That(emoji.Emoji_Offset == null, Is.True);

            EmojiRefs skipped = Stand(builder, shot);
            system.Play(skipped, shot, settle: true);
            Assert.That(system.Count, Is.EqualTo(0));
            Assert.That(skipped.Emoji_Offset == null, Is.True);

            Assert.That(definition.Stays, Is.True);
        }

        [Test]
        public void 내리면_퇴장을_재생한_뒤에_부서진다_건너뛸_때는_바로_부서진다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Hold());

            EmojiRefs emoji = Stand(builder, definition);
            system.Play(emoji, definition, settle: true);

            system.Off(emoji, immediately: false);
            Assert.That(emoji.Emoji_Offset != null, Is.True, "퇴장하는 동안은 아직 있다");

            // 퇴장의 프레임 5 — 이모지 전체의 불투명도가 0이다.
            system.Tick(5f / 24f);
            Assert.That(emoji.OffsetCanvasGroup.alpha, Is.EqualTo(0f).Within(1e-3f));

            system.Tick(2f / 24f);
            Assert.That(emoji.Emoji_Offset == null, Is.True);
            Assert.That(system.Count, Is.EqualTo(0));

            EmojiRefs skipped = Stand(builder, definition);
            system.Play(skipped, definition, settle: true);
            system.Off(skipped, immediately: true);
            Assert.That(skipped.Emoji_Offset == null, Is.True);
        }

        [Test]
        public void 슬롯과_함께_부서진_이모지는_시스템이_잊는다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Hold());

            EmojiRefs emoji = Stand(builder, definition);
            system.Play(emoji, definition, settle: true);

            Object.DestroyImmediate(emoji.Emoji_Locator.parent.gameObject);

            Assert.DoesNotThrow(() => system.Tick(0.1f));
            Assert.That(system.Count, Is.EqualTo(0));
        }

        // ── 좌우 (docs/PLAN_emoji-placement.md §3) ───────────────────

        // 오프셋 (100, 50) · 리그 둘: a는 보통, q는 바로 서야 하는 그림(기본 배율 0.5 · 회전 10°).
        private static EmojiDto Sided(bool fixedSide) => new()
        {
            name = "sided",
            fixedSide = fixedSide,
            offset = new Float2Dto { x = 100f, y = 50f },
            rigs = new List<EmojiRigDto>
            {
                new() { name = "a", sprite = "s", scale = 0.5f, rotation = 10f },
                new() { name = "q", sprite = "s", scale = 0.5f, rotation = 10f, upright = true },
            },
        };

        private static void Flip(EmojiRefs emoji, bool flipped)
            => emoji.Emoji_Locator.localScale = flipped ? new Vector3(-1f, 1f, 1f) : Vector3.one;

        [Test]
        public void 형식의_좌우_표시를_읽는다_없으면_거짓이다()
        {
            EmojiDefinition sided = One(Sided(fixedSide: true));

            Assert.That(sided.FixedSide, Is.True);
            Assert.That(sided.Rigs[0].Upright, Is.False);
            Assert.That(sided.Rigs[1].Upright, Is.True);

            EmojiDefinition plain = One(Static());

            Assert.That(plain.FixedSide, Is.False);
            Assert.That(plain.Rigs[0].Upright, Is.False);
        }

        [Test]
        public void 좌우의_부호_누가_한_번_더_뒤집는가()
        {
            EmojiDefinition follows = One(Sided(fixedSide: false));
            EmojiDefinition fixedSide = One(Sided(fixedSide: true));

            // 로케이터가 뒤집히지 않았으면 아무도 뒤집지 않는다.
            Assert.That(EmojiSide.IsMirrored(follows, false), Is.False);
            Assert.That(EmojiSide.OffsetSign(fixedSide, false), Is.EqualTo(1f));
            Assert.That(EmojiSide.PlaceSign(follows, follows.Rigs[1], false), Is.EqualTo(1f));

            // 방향을 따르는 이모지 — 통째로 거울에 비치고, 바로 서야 하는 리그만 한 번 더 뒤집는다.
            Assert.That(EmojiSide.IsMirrored(follows, true), Is.True);
            Assert.That(EmojiSide.OffsetSign(follows, true), Is.EqualTo(1f));
            Assert.That(EmojiSide.PlaceSign(follows, follows.Rigs[0], true), Is.EqualTo(1f));
            Assert.That(EmojiSide.PlaceSign(follows, follows.Rigs[1], true), Is.EqualTo(-1f));

            // 방향을 따르지 않는 이모지 — 오프셋에서 통째로 되돌린다. 리그는 더 뒤집지 않는다.
            Assert.That(EmojiSide.IsMirrored(fixedSide, true), Is.False);
            Assert.That(EmojiSide.OffsetSign(fixedSide, true), Is.EqualTo(-1f));
            Assert.That(EmojiSide.PlaceSign(fixedSide, fixedSide.Rigs[1], true), Is.EqualTo(1f));
        }

        [Test]
        public void 로케이터가_뒤집히면_이모지는_그대로이고_바로_서야_하는_리그만_되뒤집힌다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Sided(fixedSide: false));
            EmojiRefs emoji = Stand(builder, definition);

            SetEmojiCommand.ApplyDefinition(emoji, definition);
            system.Play(emoji, definition, settle: false);

            Flip(emoji, true);
            system.Tick(0f);

            // 이모지의 값은 그대로다 — 뒤집는 것은 로케이터다.
            Assert.That(emoji.Emoji_Offset.anchoredPosition, Is.EqualTo(new Vector2(100f, 50f)));
            Assert.That(emoji.Emoji_Offset.localScale.x, Is.EqualTo(1f).Within(Eps));

            // 보통 리그는 그대로, 바로 서야 하는 리그는 가로 배율과 회전의 부호가 바뀐다.
            Assert.That(emoji.Rigs[0].EmojiRig_Place.localScale.x, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(Mathf.DeltaAngle(emoji.Rigs[0].EmojiRig_Place.localEulerAngles.z, 10f), Is.EqualTo(0f).Within(1e-2f));

            Assert.That(emoji.Rigs[1].EmojiRig_Place.localScale.x, Is.EqualTo(-0.5f).Within(Eps));
            Assert.That(emoji.Rigs[1].EmojiRig_Place.localScale.y, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(Mathf.DeltaAngle(emoji.Rigs[1].EmojiRig_Place.localEulerAngles.z, -10f), Is.EqualTo(0f).Within(1e-2f));

            // 서 있는 동안 인물이 되돌아서면 따라 되돌아간다.
            Flip(emoji, false);
            system.Tick(0f);

            Assert.That(emoji.Rigs[1].EmojiRig_Place.localScale.x, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(Mathf.DeltaAngle(emoji.Rigs[1].EmojiRig_Place.localEulerAngles.z, 10f), Is.EqualTo(0f).Within(1e-2f));
        }

        [Test]
        public void 방향을_따르지_않는_이모지는_뒤집힌_로케이터_밑에서_오프셋이_되뒤집는다()
        {
            EmojiRigBuilder builder = new();
            EmojiPlaybackSystem system = new(builder);
            EmojiDefinition definition = One(Sided(fixedSide: true));
            EmojiRefs emoji = Stand(builder, definition);

            SetEmojiCommand.ApplyDefinition(emoji, definition);
            system.Play(emoji, definition, settle: false);

            Flip(emoji, true);
            system.Tick(0f);

            // 자리의 x와 가로 배율이 뒤집힌다 — 뒤집힌 로케이터와 곱해 화면에서는 만든 쪽 그대로다.
            Assert.That(emoji.Emoji_Offset.anchoredPosition, Is.EqualTo(new Vector2(-100f, 50f)));
            Assert.That(emoji.Emoji_Offset.localScale.x, Is.EqualTo(-1f).Within(Eps));
            Assert.That(emoji.Emoji_Offset.localScale.y, Is.EqualTo(1f).Within(Eps));

            // 거울에 비치지 않으므로 바로 서야 하는 리그도 더 뒤집지 않는다.
            Assert.That(emoji.Rigs[1].EmojiRig_Place.localScale.x, Is.EqualTo(0.5f).Within(Eps));

            Flip(emoji, false);
            system.Tick(0f);

            Assert.That(emoji.Emoji_Offset.anchoredPosition, Is.EqualTo(new Vector2(100f, 50f)));
            Assert.That(emoji.Emoji_Offset.localScale.x, Is.EqualTo(1f).Within(Eps));
        }
    }
}
