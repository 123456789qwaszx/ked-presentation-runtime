using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// StageCommand → CommandSpec 파서 — 표의 생략값과 풀림(한 커맨드 → 여러 스펙)을 고정한다.
// 핸들러 본문은 브리지에서 그대로 옮긴 것이라 값 자체는 종전 식에서 온다.
public class StageCommandParserTests
{
    private static StageCommand Cmd(string name, params string[] args) => new(name, args, "test.yarn:1");

    private static List<CommandSpecBase> Parse(StageCommandParser parser, StageCommand cmd)
    {
        List<CommandSpecBase> output = new();
        Assert.IsTrue(parser.TryParse(cmd, output), $"{cmd}를 몰라서는 안 된다");
        return output;
    }

    // 리그 프리팹 없이(모션도 없이) 세운다. 배경도 캐릭터 리그다.
    private static StageCommandParser NewParser() => new(null);

    [Test]
    public void 표에_있는_이름을_안다()
    {
        Assert.IsTrue(StageCommandParser.Knows("place"));
        Assert.IsTrue(StageCommandParser.Knows("left"));
        Assert.IsTrue(StageCommandParser.Knows("stage_mask"));
        Assert.IsTrue(StageCommandParser.Knows("pause"));
        Assert.IsFalse(StageCommandParser.Knows("1fr"), "프레임 별칭은 걷었다 — <<pause 1fr>>(docs/PLAN_line-beat.md D4)");
        Assert.IsFalse(StageCommandParser.Knows("24fr"));
        Assert.IsFalse(StageCommandParser.Knows("surface_reset"), "대사창은 무대 커맨드가 아니다");
        Assert.IsFalse(StageCommandParser.Knows(null));
    }

    // 인자 하나를 이름에 박아 다시 부르는 이름은 없다.
    // 예외: char_visual_<효과> 일곱.
    [TestCase("slot01")]
    [TestCase("bg_slot01")]
    [TestCase("stage_blur_s1")]
    [TestCase("place_tl")]
    [TestCase("place_inner_br")]
    [TestCase("place_left")]
    [TestCase("place_center")]
    [TestCase("place_right")]
    [TestCase("size_close")]
    [TestCase("stage_mask_daze_in")]
    [TestCase("stage_mask_hstrip_open")]
    [TestCase("ui_patch")]
    public void 걷은_별칭은_모른다(string name)
    {
        Assert.IsFalse(StageCommandParser.Knows(name));
    }

    [Test]
    public void 리그를_다른_무대_층으로_옮기는_커맨드는_없다()
    {
        // 리그는 slot이 세운 무대 · 층에 산다. 다른 자리에 서려면 fade_out 뒤 새 slot(2026-11-06).
        Assert.IsFalse(StageCommandParser.Knows("char_to"));
        Assert.IsFalse(StageCommandParser.Knows("char_to_s1"));
    }

    [TestCase("char_visual")]
    [TestCase("char_visual_focus")]
    [TestCase("char_visual_defocus")]
    [TestCase("char_visual_dim")]
    [TestCase("char_visual_silhouette")]
    [TestCase("char_visual_inner_rim")]
    [TestCase("char_visual_outer_rim")]
    [TestCase("char_visual_clear")]
    public void 남긴_예외는_안다(string name)
    {
        Assert.IsTrue(StageCommandParser.Knows(name));
    }

    [Test]
    public void 모르는_이름은_false이고_아무것도_내지_않는다()
    {
        List<CommandSpecBase> output = new();

        Assert.IsFalse(NewParser().TryParse(Cmd("nope", "c1"), output));
        Assert.IsEmpty(output);
    }

    [Test]
    public void 생략한_인자는_표의_생략값으로_채운다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("pause"));

        Assert.AreEqual(1, specs.Count);
        Assert.AreEqual(0.18f, ((WaitCommandSpec)specs[0]).duration, 1e-6f);
        Assert.IsTrue(specs[0].wait, "pause는 기다린다(전경)");

        specs = Parse(NewParser(), Cmd("pause", "30fr"));
        Assert.AreEqual(1f, ((WaitCommandSpec)specs[0]).duration, 1e-6f, "30fps 기준 30fr = 1초 — 다른 커맨드와 같은 시간 토큰");

        specs = Parse(NewParser(), Cmd("pause", "0.5"));
        Assert.AreEqual(0.5f, ((WaitCommandSpec)specs[0]).duration, 1e-6f, "맨 숫자는 초");
    }

    // 기다리는 명령은 두 곳이 안다 — 재생은 스펙의 wait로, 폴드의 원문 검사는 이름으로(docs/PLAN_line-beat.md R8). 같아야 한다.
    [TestCase("pause")]
    [TestCase("pause 12fr")]
    [TestCase("screen_cover")]
    [TestCase("screen_cover white 0.3s")]
    [TestCase("screen_cover_clear 0.5s")]
    [TestCase("stage_dim all 0.4")]
    [TestCase("face c1 3")]
    [TestCase("fade_in c1")]
    [TestCase("stage_mask iris_in")]
    public void 기다리는_명령은_파서의_wait와_코어의_이름이_같다(string line)
    {
        string[] tokens = line.Split(' ');
        string[] args = new string[tokens.Length - 1];
        System.Array.Copy(tokens, 1, args, 0, args.Length);

        List<CommandSpecBase> specs = Parse(NewParser(), Cmd(tokens[0], args));
        bool waits = specs.Exists(s => s.wait);

        Assert.AreEqual(YarnCommandTextExtractor.IsWaitingCommand(tokens[0]), waits, line);
    }

    [TestCase("show")]
    [TestCase("shot_to")]
    [TestCase("shot_focus_to")]
    [TestCase("move_by")]
    [TestCase("slide_in")]
    [TestCase("slide_out")]
    [TestCase("scale_by")]
    [TestCase("scale_reset")]
    [TestCase("focus_on")]
    [TestCase("focus_clear")]
    public void 걷은_이름은_모른다(string name)
    {
        // 무대를 읽어 값을 푸는 것 · 이름 하나가 여럿을 내는 것은 커맨드가 아니다. 노드와 함께 사라진 커맨드도 있다.
        // 블러의 초점은 depth_focus_on · depth_focus_clear로 이름을 바꿨다(docs/PLAN_rig-layers.md §4).
        // move_by · slide_in · slide_out은 별칭 없이 걷었다 — 자리는 place, 그 위의 움직임은 left · right · up · down(docs/PLAN_move-axes.md).
        Assert.IsFalse(StageCommandParser.Knows(name));
    }

    [Test]
    public void size는_깊이와_길이만_받는다()
    {
        // 셋째 칸은 길이다 — 보존할 초점을 받던 칸은 걷었다. 무대를 재지 않고 레벨의 값을 그대로 놓는다.
        SetDepthCommandSpecCharR size = (SetDepthCommandSpecCharR)Parse(NewParser(), Cmd("size", "c1", "14", "30fr", "InOutCubic"))[0];

        Assert.IsTrue(size.useLevel);
        Assert.IsFalse(size.useScale);
        Assert.IsFalse(size.keepY);
        Assert.AreEqual(14f, size.level, 1e-6f);
        Assert.AreEqual(1f, size.duration, 1e-6f, "30fr = 1초");
        Assert.AreEqual(DG.Tweening.Ease.InOutCubic, size.ease);
        Assert.AreEqual(CharacterRigNode.CharacterPortrait_DepthOffset, size.depthYNode);
        Assert.AreEqual(CharacterRigNode.CharacterPortrait_DepthScale, size.depthScaleNode);
    }

    [Test]
    public void size의_셋째_칸이_keep_y면_길이는_그_뒤다()
    {
        SetDepthCommandSpecCharR size = (SetDepthCommandSpecCharR)Parse(NewParser(), Cmd("size", "c1", "front", "keep_y", "30fr", "OutCubic"))[0];

        Assert.IsTrue(size.keepY);
        Assert.IsFalse(size.useLevel);
        Assert.AreEqual(CharacterDepthKey.Front, size.preset);
        Assert.AreEqual(1f, size.duration, 1e-6f);
        Assert.AreEqual(DG.Tweening.Ease.OutCubic, size.ease);
    }

    [Test]
    public void size는_x배율을_직접_받는다()
    {
        SetDepthCommandSpecCharR size = (SetDepthCommandSpecCharR)Parse(NewParser(), Cmd("size", "c1", "x1.04", "15fr"))[0];

        Assert.IsTrue(size.useScale);
        Assert.AreEqual(1.04f, size.scale, 1e-6f);
        Assert.IsFalse(size.keepY, "x배율은 keep_y 없이도 Y를 건드리지 않는다 — 실행 커맨드가 정한다");
        Assert.AreEqual(0.5f, size.duration, 1e-6f);
    }

    [Test]
    public void place와_focus_move는_제_노드를_절대값으로_놓는다()
    {
        MoveByCommandSpecCharR place = (MoveByCommandSpecCharR)Parse(NewParser(), Cmd("place", "c1", "-3u", "1.5u", "24fr", "OutBack"))[0];

        Assert.AreEqual(CharacterRigNode.CharSlot_Track, place.node);
        Assert.IsTrue(place.useAbsolutePosition);
        Assert.AreEqual(new Vector2(-120f, 60f), place.delta);
        Assert.AreEqual(0.8f, place.duration, 1e-6f);
        Assert.AreEqual(DG.Tweening.Ease.OutBack, place.ease);

        MoveByCommandSpecCharR focus = (MoveByCommandSpecCharR)Parse(NewParser(), Cmd("focus_move", "c1", "2u", "-1u"))[0];

        Assert.AreEqual(CharacterRigNode.CharFocus_Track, focus.node);
        Assert.IsTrue(focus.useAbsolutePosition);
        Assert.AreEqual(new Vector2(80f, -40f), focus.delta);
        Assert.AreEqual(0.4f, focus.duration, 1e-6f, "생략 0.4s — place와 같다");
    }

    [Test]
    public void focus_rotate와_focus_scale은_제_노드를_절대값으로_놓는다()
    {
        RotateToCommandSpecCharR rotate = (RotateToCommandSpecCharR)Parse(NewParser(), Cmd("focus_rotate", "c1", "8"))[0];

        Assert.AreEqual(CharacterRigNode.CharFocus_Rotation, rotate.node);
        Assert.IsFalse(rotate.relativeToCurrent);
        Assert.AreEqual(8f, rotate.toEuler.z, 1e-6f);

        ScaleToCommandSpecCharR scale = (ScaleToCommandSpecCharR)Parse(NewParser(), Cmd("focus_scale", "c1", "1.25", "12fr"))[0];

        Assert.AreEqual(CharacterRigNode.CharFocus_Scale, scale.node);
        Assert.IsFalse(scale.relativeToCurrent);
        Assert.AreEqual(new Vector2(1.25f, 1.25f), scale.toScale);
    }

    [Test]
    public void focus_reset은_포커스의_셋을_본래대로_놓는다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("focus_reset", "c1", "8fr"));

        Assert.AreEqual(3, specs.Count, "노드가 셋 — 한 노드에 트윈 하나");

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)specs[0];
        Assert.AreEqual(CharacterRigNode.CharFocus_Track, move.node);
        Assert.IsTrue(move.useAbsolutePosition);
        Assert.AreEqual(Vector2.zero, move.delta);

        RotateToCommandSpecCharR rotate = (RotateToCommandSpecCharR)specs[1];
        Assert.AreEqual(CharacterRigNode.CharFocus_Rotation, rotate.node);
        Assert.AreEqual(Vector3.zero, rotate.toEuler);

        ScaleToCommandSpecCharR scale = (ScaleToCommandSpecCharR)specs[2];
        Assert.AreEqual(CharacterRigNode.CharFocus_Scale, scale.node);
        Assert.AreEqual(Vector2.one, scale.toScale);

        foreach (CommandSpecBase spec in specs)
        {
            Assert.AreEqual("c1", ((CharacterRigCommandSpecBase)spec).roleKey);
            Assert.AreEqual(8f / 30f, Duration(spec), 1e-6f, "8fr");
        }
    }

    private static float Duration(CommandSpecBase spec) => spec switch
    {
        MoveByCommandSpecCharR m => m.duration,
        RotateToCommandSpecCharR r => r.duration,
        ScaleToCommandSpecCharR s => s.duration,
        _ => -1f,
    };

    [Test]
    public void depth_focus_on은_블러의_초점이다()
    {
        Assert.IsTrue(StageCommandParser.Knows("depth_focus_on"));
        Assert.IsTrue(StageCommandParser.Knows("depth_focus_clear"));
        Assert.IsInstanceOf<DepthFocusCommandSpecCharR>(Parse(NewParser(), Cmd("depth_focus_on", "c1"))[0]);
    }

    [Test]
    public void move_reset은_움직임의_두_노드를_0으로_놓는다()
    {
        // 본래 자리(Track)는 건드리지 않는다(docs/PLAN_move-axes.md M6). 노드가 둘이라 스펙도 둘이다.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("move_reset", "c1", "12fr"));

        Assert.AreEqual(2, specs.Count);

        MoveByCommandSpecCharR x = (MoveByCommandSpecCharR)specs[0];
        MoveByCommandSpecCharR y = (MoveByCommandSpecCharR)specs[1];
        Assert.AreEqual(CharacterRigNode.CharSlot_Move_X, x.node);
        Assert.AreEqual(CharacterRigNode.CharSlot_Move_Y, y.node);

        foreach (MoveByCommandSpecCharR reset in new[] { x, y })
        {
            Assert.AreEqual("c1", reset.roleKey);
            Assert.IsTrue(reset.useAbsolutePosition);
            Assert.AreEqual(Vector2.zero, reset.delta);
            Assert.AreEqual(12f / 30f, reset.duration, 1e-6f, "12fr");
        }
    }

    // 1u = 1920 / 48 = 40px.
    [TestCase("left", CharacterRigNode.CharSlot_Move_X, -120f, 0f)]
    [TestCase("right", CharacterRigNode.CharSlot_Move_X, 120f, 0f)]
    [TestCase("up", CharacterRigNode.CharSlot_Move_Y, 0f, 120f)]
    [TestCase("down", CharacterRigNode.CharSlot_Move_Y, 0f, -120f)]
    public void 슬롯의_움직임은_제_노드를_상대값으로_민다(string name, CharacterRigNode node, float x, float y)
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd(name, "c1", "3u", "12fr", "Linear"));

        Assert.AreEqual(1, specs.Count);

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)specs[0];
        Assert.AreEqual("c1", move.roleKey);
        Assert.AreEqual(node, move.node);
        Assert.IsFalse(move.useAbsolutePosition, "상대값 — 쌓인다");
        Assert.AreEqual(x, move.delta.x, 1e-4f);
        Assert.AreEqual(y, move.delta.y, 1e-4f);
        Assert.AreEqual(12f / 30f, move.duration, 1e-6f);
        Assert.AreEqual(DG.Tweening.Ease.Linear, move.ease);
    }

    [Test]
    public void 슬롯의_움직임은_생략하면_0_4초_OutCubic이고_음수는_반대_방향이다()
    {
        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)Parse(NewParser(), Cmd("left", "c1", "-2u"))[0];

        Assert.AreEqual(80f, move.delta.x, 1e-4f, "left -2u = right 2u");
        Assert.AreEqual(0.4f, move.duration, 1e-6f);
        Assert.AreEqual(DG.Tweening.Ease.OutCubic, move.ease);
    }

    [Test]
    public void 거리가_없는_슬롯의_움직임은_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Error, new Regex("<<right>>에 거리가 없다"));
        Assert.IsEmpty(Parse(NewParser(), Cmd("right", "c1")));

        LogAssert.Expect(LogType.Error, new Regex("<<down>>의 거리를 읽지 못했다"));
        Assert.IsEmpty(Parse(NewParser(), Cmd("down", "c1", "멀리")));
    }

    [Test]
    public void stage_mask는_프리셋과_무대와_시간을_받는다()
    {
        StageMaskMotionCommandSpec a = (StageMaskMotionCommandSpec)Parse(NewParser(), Cmd("stage_mask", "daze_close", "stage00"))[0];
        Assert.AreEqual("daze_close", a.presetKey);
        Assert.AreEqual("stage00", a.stage);
        Assert.AreEqual(-1f, a.durationOverride, "시간이 비면 프리셋 자체 시간");

        StageMaskMotionCommandSpec b = (StageMaskMotionCommandSpec)Parse(NewParser(), Cmd("stage_mask", "iris_in", "", "15fr"))[0];
        Assert.AreEqual("stage01", b.stage, "무대 생략값은 stage01");
        Assert.AreEqual(0.5f, b.durationOverride, 1e-6f);
    }

    [Test]
    public void stage_mask와_마스크_지우기는_마스크_커맨드_하나만_낸다()
    {
        // stage_mask는 마스크만 움직인다 — 틀(컷인 창)을 모른다. 컷인 판 · 팝업 그림은 무대 안의 리그라 마스크가 함께 자른다(docs/PLAN_stage-visibility.md §3.1).
        List<CommandSpecBase> mask = Parse(NewParser(), Cmd("stage_mask", "daze_close", "stage01"));
        Assert.AreEqual(1, mask.Count);
        Assert.IsInstanceOf<StageMaskMotionCommandSpec>(mask[0]);

        List<CommandSpecBase> clear = Parse(NewParser(), Cmd("stage_mask_reset"));
        Assert.AreEqual(1, clear.Count);
        Assert.IsTrue(((StageMaskClearCommandSpec)clear[0]).allStages);
    }

    // ── 컷인 (docs/PLAN_cutin.md) ────────────────────────────────

    [Test]
    public void cutin_open은_무대_판_자리_배율을_받는다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("cutin_open", "stage02", "pink_stars", "-16u", "1.25u", "0.86"));
        CutInOpenCommandSpec s = (CutInOpenCommandSpec)specs[specs.Count - 1];

        Assert.AreEqual("stage02", s.stage);
        Assert.AreEqual(-640f, s.center.x, 1e-3f, "1u = 40px, 음수 그대로");
        Assert.AreEqual(50f, s.center.y, 1e-3f);
        Assert.AreEqual(0.86f, s.scale, 1e-6f);
    }

    [Test]
    public void cutin_open은_판_리그를_먼저_세우고_창을_연다()
    {
        // 판 그림은 배경처럼 그 무대의 far 층에 서는 리그다 — 무대 안이라 마스크가 판까지 자른다(docs/PLAN_stage-visibility.md §3.2).
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("cutin_open", "stage02", "pink_stars", "-16u", "1.25u", "0.86"));
        string key = StageCommandParser.CutInPanelSlotKey("stage02");

        Assert.AreEqual(6, specs.Count);

        SetupCharRigCommandSpec setup = (SetupCharRigCommandSpec)specs[0];
        Assert.AreEqual(key, setup.roleKey);
        Assert.AreEqual("stage02", setup.stage);
        Assert.AreEqual(StageLayerLabels.Far, setup.layerDepth, "인물(mid · front)보다 뒤");
        Assert.IsTrue(setup.asBackground);
        Assert.IsTrue(setup.outsideFold, "폴드 밖 — 하네스가 캡처하지 않는다");

        SetSpriteCommandSpecCharR sprite = (SetSpriteCommandSpecCharR)specs[1];
        Assert.AreEqual(key, sprite.roleKey);
        Assert.AreEqual(CutInPresetDBSO.PanelResourcesRoot, sprite.resourcesRoot);
        Assert.AreEqual("pink_stars", sprite.spriteKey);
        Assert.AreEqual(CharRigImageSizingMode.SourcePixels, sprite.sizingMode);

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)specs[2];
        Assert.IsTrue(move.useAbsolutePosition);
        Assert.AreEqual(new Vector2(-640f, 50f), move.delta, "판 중심 = 창의 자리");
        Assert.AreEqual(0f, move.duration);

        ScaleToCommandSpecCharR scale = (ScaleToCommandSpecCharR)specs[3];
        Assert.AreEqual(CharacterRigNode.CharacterPortrait_ActingScale, scale.node);
        Assert.AreEqual(0.86f / CutInPresetDBSO.PanelPictureResolution, scale.toScale.x, 1e-6f, "판 그림은 캔버스의 2배 해상도");

        FadeCommandSpecCharR appear = (FadeCommandSpecCharR)specs[4];
        Assert.AreEqual(1f, appear.targetAlpha);
        Assert.AreEqual(0f, appear.duration);

        Assert.IsInstanceOf<CutInOpenCommandSpec>(specs[5], "창은 판 다음");
    }

    [Test]
    public void cutin_open의_생략값은_stage01_가운데_배율_1이다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("cutin_open", "", "blue_question"));
        CutInOpenCommandSpec s = (CutInOpenCommandSpec)specs[specs.Count - 1];

        Assert.AreEqual("stage01", s.stage);
        Assert.AreEqual(Vector2.zero, s.center);
        Assert.AreEqual(1f, s.scale);
        Assert.AreEqual("stage01_cutin_panel", ((SetupCharRigCommandSpec)specs[0]).roleKey);
    }

    [Test]
    public void cutin은_본_무대에_쓰면_경고하고_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<cutin_open>> 본 무대"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("cutin_open", "stage00", "pink_stars")).Count);

        LogAssert.Expect(LogType.Warning, new Regex("<<cutin_close>> 본 무대"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("cutin_close", "stage00")).Count);
    }

    [Test]
    public void cutin_open은_판이_없으면_경고하고_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<cutin_open>>에 판 키가 없다"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("cutin_open", "stage01")).Count);
    }

    [Test]
    public void cutin_close는_무대_하나를_받는다()
    {
        CutInCloseCommandSpec s = (CutInCloseCommandSpec)Parse(NewParser(), Cmd("cutin_close"))[0];
        Assert.AreEqual("stage01", s.stage, "무대 생략값은 stage01");
        Assert.AreEqual(CutInCloseMotion.PopOut, s.motion, "모션 생략값은 튀어 닫힘");
    }

    [Test]
    public void cutin의_끝_인자는_여닫는_모양이다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1.25u", "1"));
        Assert.AreEqual(CutInOpenMotion.PopIn, ((CutInOpenCommandSpec)specs[specs.Count - 1]).motion, "모션 생략값은 튀어나옴");

        specs = Parse(NewParser(), Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1.25u", "1", "cutin_pop_in"));
        Assert.AreEqual(CutInOpenMotion.PopIn, ((CutInOpenCommandSpec)specs[specs.Count - 1]).motion);

        specs = Parse(NewParser(), Cmd("cutin_open", "stage01", "pink_stars", "-16u", "1.25u", "1.4", "cutin_flash_in"));
        CutInOpenCommandSpec open = (CutInOpenCommandSpec)specs[specs.Count - 1];
        Assert.AreEqual(CutInOpenMotion.FlashIn, open.motion);
        Assert.AreEqual(1.4f, open.scale, 1e-6f, "모션을 줘도 앞 인자는 그대로");
        Assert.AreEqual(6, specs.Count, "흰빛 열림도 판 리그 다섯 + 창 하나");

        CutInCloseCommandSpec close = (CutInCloseCommandSpec)Parse(NewParser(), Cmd("cutin_close", "stage01", "cutin_fade_out"))[0];
        Assert.AreEqual(CutInCloseMotion.FadeOut, close.motion);

        close = (CutInCloseCommandSpec)Parse(NewParser(), Cmd("cutin_close", "stage01", "cutin_pop_out"))[0];
        Assert.AreEqual(CutInCloseMotion.PopOut, close.motion);
    }

    [Test]
    public void cutin은_모르는_모션이면_경고하고_기본값으로_간다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<cutin_open>> 모르는 모션 'cutin_spin_in'"));
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("cutin_open", "stage01", "pink_stars", "0u", "0u", "1", "cutin_spin_in"));
        Assert.AreEqual(CutInOpenMotion.PopIn, ((CutInOpenCommandSpec)specs[specs.Count - 1]).motion);

        LogAssert.Expect(LogType.Warning, new Regex("<<cutin_close>> 모르는 모션 'cutin_flash_in'"));
        CutInCloseCommandSpec close = (CutInCloseCommandSpec)Parse(NewParser(), Cmd("cutin_close", "stage01", "cutin_flash_in"))[0];
        Assert.AreEqual(CutInCloseMotion.PopOut, close.motion, "여는 모션 이름은 닫는 모션이 아니다");
    }

    // ── 그림 팝업 (docs/PLAN_popup.md) ───────────────────────────

    [Test]
    public void popup은_그림_리그를_close_층에_세우고_페이드_인한다()
    {
        // 팝업은 그림 하나다 — 배경처럼 그 무대의 close 층에 서는 리그(docs/PLAN_stage-visibility.md §3.3). 뒷막은 대본이 stage_dim으로 깐다.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("popup", "stage02", "sample_letter", "2u", "-1.5u", "1.2"));
        string key = StageCommandParser.PopupSlotKey("stage02");

        Assert.AreEqual(5, specs.Count);

        SetupCharRigCommandSpec setup = (SetupCharRigCommandSpec)specs[0];
        Assert.AreEqual(key, setup.roleKey);
        Assert.AreEqual("stage02", setup.stage);
        Assert.AreEqual(StageLayerLabels.Close, setup.layerDepth, "그 무대의 가장 앞 층");
        Assert.IsTrue(setup.asBackground);
        Assert.IsTrue(setup.outsideFold, "폴드 밖 — 하네스가 캡처하지 않는다");

        SetSpriteCommandSpecCharR sprite = (SetSpriteCommandSpecCharR)specs[1];
        Assert.AreEqual(PopupPresetDBSO.PictureResourcesRoot, sprite.resourcesRoot);
        Assert.AreEqual("sample_letter", sprite.spriteKey);
        Assert.AreEqual(CharRigImageSizingMode.SourcePixels, sprite.sizingMode);

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)specs[2];
        Assert.IsTrue(move.useAbsolutePosition);
        Assert.AreEqual(new Vector2(80f, -60f), move.delta, "1u = 40px");

        ScaleToCommandSpecCharR scale = (ScaleToCommandSpecCharR)specs[3];
        Assert.AreEqual(PopupPresetDBSO.DefaultPictureScale * 1.2f, scale.toScale.x, 1e-6f, "원본 픽셀 × 0.91 × 대본 배율");

        FadeCommandSpecCharR fade = (FadeCommandSpecCharR)specs[4];
        Assert.AreEqual(1f, fade.targetAlpha);
        Assert.AreEqual(PopupPresetDBSO.DefaultOpenSeconds, fade.duration, 1e-6f);
    }

    [Test]
    public void popup의_생략값은_stage01_가운데_배율_1이다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("popup", "", "sample_seedling"));

        Assert.AreEqual("stage01", ((SetupCharRigCommandSpec)specs[0]).stage);
        Assert.AreEqual("stage01_popup", ((SetupCharRigCommandSpec)specs[0]).roleKey);
        Assert.AreEqual(Vector2.zero, ((MoveByCommandSpecCharR)specs[2]).delta);
        Assert.AreEqual(PopupPresetDBSO.DefaultPictureScale, ((ScaleToCommandSpecCharR)specs[3]).toScale.x, 1e-6f);
    }

    [Test]
    public void popup은_본_무대에_쓰거나_그림이_없으면_경고하고_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<popup>> 본 무대"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("popup", "stage00", "sample_letter")).Count);

        LogAssert.Expect(LogType.Warning, new Regex("<<popup>>에 그림 키가 없다"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("popup", "stage01")).Count);

        LogAssert.Expect(LogType.Warning, new Regex("<<popup_close>> 본 무대"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("popup_close", "stage00")).Count);
    }

    [Test]
    public void popup_close는_그_무대의_그림_리그를_페이드_아웃한다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("popup_close"));

        Assert.AreEqual(1, specs.Count);
        FadeCommandSpecCharR fade = (FadeCommandSpecCharR)specs[0];
        Assert.AreEqual("stage01_popup", fade.roleKey);
        Assert.AreEqual(0f, fade.targetAlpha);
        Assert.AreEqual(PopupPresetDBSO.DefaultCloseSeconds, fade.duration, 1e-6f);
    }

    [Test]
    public void stage_mask는_프리셋이_없으면_경고하고_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<stage_mask>>에 프리셋이 없다"));

        List<CommandSpecBase> output = new();
        Assert.IsTrue(NewParser().TryParse(Cmd("stage_mask"), output), "이름은 안다");
        Assert.AreEqual(0, output.Count);
    }

    [Test]
    public void stage_blur는_셋째_인자로_무대를_받는다()
    {
        StageLayerBlurCommandSpec a = (StageLayerBlurCommandSpec)Parse(NewParser(), Cmd("stage_blur", "back", "0.5"))[0];
        Assert.AreEqual("stage00", a.stage, "무대 생략값은 stage00");
        Assert.AreEqual(StageLayerLabels.Back, a.layerDepth);
        Assert.AreEqual(0.5f, a.amount, 1e-6f);

        StageLayerBlurCommandSpec b = (StageLayerBlurCommandSpec)Parse(NewParser(), Cmd("stage_blur", "mid", "1", "stage02"))[0];
        Assert.AreEqual("stage02", b.stage);
    }

    [Test]
    public void 빈_문자열_인자도_생략으로_친다()
    {
        // Yarn의 생략 인자가 빈 문자열로 오는 경우가 있다 — StageCommand.Arg 규약과 같다.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("place", "c1", "", "", "", ""));

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)specs[0];

        Assert.AreEqual(Vector2.zero, move.delta);
        Assert.AreEqual(0.4f, move.duration, 1e-6f);
    }

    [Test]
    public void 한_커맨드가_스펙_여럿으로_풀린다()
    {
        // cast = 배역 · 포즈 · 표정 · 앵커 리셋, 이 순서.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("cast", "c1", "tyrant"));

        Assert.AreEqual(4, specs.Count);
        Assert.IsInstanceOf<CastCharacterCommandSpec>(specs[0]);
        Assert.IsInstanceOf<SetPortraitPoseCommandSpecCharR>(specs[1]);
        Assert.IsInstanceOf<SetPortraitSpriteCommandSpecCharR>(specs[2]);
        Assert.IsInstanceOf<SetAnchorCommandSpecCharR>(specs[3]);
        Assert.IsTrue(((SetAnchorCommandSpecCharR)specs[3]).resetSlotPos, "cast는 자리 축을 되돌린다");
    }

    [Test]
    public void 숫자가_아닌_숫자_인자는_경고하고_생략값으로_간다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("StageCommandParser.*숫자가 아니다"));

        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("rotate_by", "c1", "abc"));

        Assert.AreEqual(0f, ((RotateToCommandSpecCharR)specs[0]).toEuler.z, 1e-6f);
    }

    // ── shot — 축마다 하나, 값 인자에는 생략값이 없다 ─────────────────────

    [Test]
    public void shot_축_명령은_그_축의_값을_절대값으로_싣는다()
    {
        StageCommandParser parser = NewParser();

        Assert.AreEqual(3f, ((ShotZoomCommandSpec)Parse(parser, Cmd("shot_zoom", "3"))[0]).zoom, 1e-6f);
        Assert.AreEqual(new Vector2(80f, -20f), ((ShotPanCommandSpec)Parse(parser, Cmd("shot_pan", "2u", "-0.5u"))[0]).pan);
        Assert.AreEqual(new Vector2(120f, 40f), ((ShotTrackCommandSpec)Parse(parser, Cmd("shot_track", "3u", "1u"))[0]).track);
        Assert.AreEqual(4f, ((ShotDollyCommandSpec)Parse(parser, Cmd("shot_dolly", "4"))[0]).dolly, 1e-6f);
        Assert.AreEqual(-8f, ((ShotRollCommandSpec)Parse(parser, Cmd("shot_roll", "-8"))[0]).roll, 1e-6f);

        Assert.AreEqual(new Vector2(240f, 40f), ((ShotFocusCommandSpec)Parse(parser, Cmd("shot_focus", "6u", "1u"))[0]).focus);

        ShotPanCommandSpec timed = (ShotPanCommandSpec)Parse(parser, Cmd("shot_pan", "-2u", "1u", "30fr"))[0];
        Assert.AreEqual(new Vector2(-80f, 40f), timed.pan);
        Assert.AreEqual(1f, timed.duration, 1e-6f, "30fr = 1초");
    }

    [Test]
    public void shot_reset은_렌즈만_shot_reset_all은_통째로다()
    {
        StageCommandParser parser = NewParser();

        Assert.IsFalse(((ShotResetCommandSpec)Parse(parser, Cmd("shot_reset", "0fr"))[0]).all);
        Assert.IsTrue(((ShotResetCommandSpec)Parse(parser, Cmd("shot_reset_all", "0fr"))[0]).all);
        Assert.IsFalse(((ShotResetCommandSpec)Parse(parser, Cmd("shot_reset"))[0]).all, "길이는 생략할 수 있다");
    }

    [Test]
    public void 값이_빠진_shot_명령은_소리_내고_아무것도_내지_않는다()
    {
        // 뜻 없는 생략값(zoom 1 · x 2.5u)으로 조용히 가지 않는다. 코어 폴드도 같은 줄을 Unhandled로 남긴다.
        StageCommandParser parser = NewParser();

        StageCommand[] broken =
        {
            Cmd("shot_zoom"),
            Cmd("shot_zoom", "abc"),
            Cmd("shot_pan", "1u"),
            Cmd("shot_focus", "1u"),
            Cmd("shot_track"),
            Cmd("shot_dolly"),
            Cmd("shot_roll", "x"),
        };

        foreach (StageCommand cmd in broken)
        {
            LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*이 커맨드는 실행하지 않는다"));

            Assert.AreEqual(0, Parse(parser, cmd).Count, cmd.ToString());
        }
    }

    [Test]
    public void fade는_목표_alpha를_싣고_범위_밖은_경고하고_자른다()
    {
        FadeCommandSpecCharR half = (FadeCommandSpecCharR)Parse(NewParser(), Cmd("fade", "c1", "0.5", "15fr"))[0];
        Assert.AreEqual(0.5f, half.targetAlpha, 1e-6f);
        Assert.AreEqual(CharacterRigNode.CharacterPortraitSprite_Root, half.node, "fade_in · fade_out과 같은 표적");
        Assert.AreEqual(0.5f, half.duration, 1e-5f, "15fr");

        LogAssert.Expect(LogType.Warning, new Regex("fade.*밖이다"));
        FadeCommandSpecCharR clamped = (FadeCommandSpecCharR)Parse(NewParser(), Cmd("fade", "c1", "-0.2"))[0];
        Assert.AreEqual(0f, clamped.targetAlpha, 1e-6f);
    }

    [Test]
    public void 출력_리스트_뒤에_붙인다()
    {
        List<CommandSpecBase> output = new() { new WaitCommandSpec() };

        NewParser().TryParse(Cmd("fade_in", "c1"), output);

        Assert.AreEqual(2, output.Count);
        Assert.IsInstanceOf<FadeCommandSpecCharR>(output[1]);
        Assert.AreEqual(NodeTargets.FadeInAlpha, ((FadeCommandSpecCharR)output[1]).targetAlpha);
    }

    [Test]
    public void motion은_저장소의_클립을_그대로_싣고_모르는_이름은_오류_후_아무것도_내지_않는다()
    {
        const string json = @"{ ""schema"": ""motions/2"", ""motions"": [ { ""name"": ""nod"", ""rig"": ""character"", ""duration"": 0.6, ""end"": ""restore"",
            ""channels"": [ { ""node"": ""CharacterPortrait_Track_Move"", ""kind"": ""position"",
            ""y"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 0.6, ""v"": 0 } ] } ] } ] }";

        StageCommandParser parser = new(null, MotionLibrary.Parse(json, "test"));

        List<CommandSpecBase> specs = Parse(parser, Cmd("motion", "c1", "nod"));
        MotionCommandSpecCharR spec = (MotionCommandSpecCharR)specs[0];

        Assert.AreEqual("c1", spec.roleKey);
        Assert.AreEqual("nod", spec.clip.Name);
        Assert.AreEqual(0.6f, spec.clip.Duration, 0f);
        Assert.AreEqual(MotionChannelMode.Relative, spec.mode, "생략한 mode는 relative");

        MotionCommandSpecCharR absolute = (MotionCommandSpecCharR)Parse(parser, Cmd("motion", "c1", "nod", "absolute"))[0];
        Assert.AreEqual(MotionChannelMode.Absolute, absolute.mode);

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*Unknown motion"));
        Assert.IsEmpty(Parse(parser, Cmd("motion", "c1", "nope")), "기본 모션은 없다 — 아무것도 내지 않는다");

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*mode"));
        Assert.IsEmpty(Parse(parser, Cmd("motion", "c1", "nod", "offset")), "모르는 mode 낱말은 재생하지 않는다");
    }

    // ── 배경 = 캐스팅하지 않은 캐릭터 슬롯 ─────────────────────────────
    // 코어 StageReducer.Background와 같은 노드·같은 값이어야 등가성 하네스가 맞는다.

    [Test]
    public void bg_spawn은_far에_배경으로_세우고_그림을_넣는다_보이게는_bg_fade_in이()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("bg_spawn", "bg_main", "class_day"));

        Assert.AreEqual(2, specs.Count, "캐릭터 slot처럼 숨은 채 선다 — 가시성 스펙은 없다");

        SetupCharRigCommandSpec setup = (SetupCharRigCommandSpec)specs[0];
        Assert.AreEqual("bg_main", setup.roleKey);
        Assert.AreEqual("stage00", setup.stage);
        Assert.AreEqual(StageLayerLabels.Far, setup.layerDepth);
        Assert.IsTrue(setup.asBackground, "배역 있는 슬롯을 덮으면 배역을 푸는 경로");

        SetSpriteCommandSpecCharR sprite = (SetSpriteCommandSpecCharR)specs[1];
        Assert.AreEqual("bg_main", sprite.roleKey);
        Assert.AreEqual(CharacterRigNode.CharacterPortraitSprite_Image, sprite.node);
        Assert.AreEqual(ResourcesSpriteResolver.BackgroundsRoot, sprite.resourcesRoot);
        Assert.AreEqual("class_day", sprite.spriteKey);
        Assert.AreEqual(CharRigImageSizingMode.SourcePixels, sprite.sizingMode, "배경은 원본 픽셀 크기 그대로");
        Assert.IsFalse(sprite.raycastTarget, "화면 전체를 덮는 그림은 클릭을 받지 않는다");
    }

    [Test]
    public void bg_spawn은_셋째_넷째_인자로_무대와_층을_받는다()
    {
        SetupCharRigCommandSpec a = (SetupCharRigCommandSpec)Parse(NewParser(), Cmd("bg_spawn", "bg_a", "class_day", "stage01"))[0];
        Assert.AreEqual("stage01", a.stage);
        Assert.AreEqual(StageLayerLabels.Far, a.layerDepth, "층 생략값은 far");

        SetupCharRigCommandSpec b = (SetupCharRigCommandSpec)Parse(NewParser(), Cmd("bg_spawn", "bg_b", "class_day", "stage02", "mid"))[0];
        Assert.AreEqual("stage02", b.stage);
        Assert.AreEqual(StageLayerLabels.Mid, b.layerDepth);
    }

    [Test]
    public void bg_fade_move_scale은_캐릭터와_같은_노드를_잡는다()
    {
        FadeCommandSpecCharR fade = (FadeCommandSpecCharR)Parse(NewParser(), Cmd("bg_fade_out", "bg_main"))[0];
        Assert.AreEqual(CharacterRigNode.CharacterPortraitSprite_Root, fade.node);
        Assert.AreEqual(NodeTargets.FadeOutAlpha, fade.targetAlpha);
        Assert.AreEqual(13f / 30f, fade.duration, 1e-5f, "생략값 13fr");

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)Parse(NewParser(), Cmd("bg_move", "bg_main", "2u", "-1u"))[0];
        Assert.AreEqual(CharacterRigNode.CharSlot_Track, move.node, "배경은 본래 자리에 상대값 — 캐릭터의 움직임 노드가 아니다");
        Assert.IsFalse(move.useAbsolutePosition);
        Assert.Less(move.delta.y, 0f, "부호 있는 단위");

        ScaleToCommandSpecCharR scale = (ScaleToCommandSpecCharR)Parse(NewParser(), Cmd("bg_scale", "bg_main", "1.2"))[0];
        Assert.AreEqual(CharacterRigNode.CharacterPortrait_ActingScale, scale.node, "가운데 피벗 — 깊이(CharacterPortrait_DepthScale)는 발 피벗");
        Assert.IsFalse(scale.relativeToCurrent);
        Assert.AreEqual(1.2f, scale.toScale.x, 1e-6f);
    }

    [Test]
    public void 지운_bg_size와_bg_place는_모른다()
    {
        Assert.IsFalse(StageCommandParser.Knows("bg_size"));
        Assert.IsFalse(StageCommandParser.Knows("bg_place"));

        SetSpriteCommandSpecCharR sprite = (SetSpriteCommandSpecCharR)Parse(
            NewParser(), Cmd("bg_sprite", "bg_main", "studio_evening", "back"))[0];
        Assert.AreEqual("studio_evening", sprite.spriteKey, "셋째 인자는 더 읽지 않는다");
    }

    // ── shot_shake — 카메라 리그의 흔들림 클립 ────────────────────────────

    private const string ShakeAndNod = "{ \"schema\": \"motions/2\", \"motions\": ["
        + " { \"name\": \"hit\", \"rig\": \"camera\", \"duration\": 0.3, \"end\": \"restore\","
        + " \"channels\": [ { \"node\": \"Camera_Shake\", \"kind\": \"position\","
        + " \"x\": [ { \"t\": 0, \"v\": 0 }, { \"t\": 0.1, \"v\": 14 }, { \"t\": 0.3, \"v\": 0 } ] } ] },"
        + " { \"name\": \"nod\", \"rig\": \"character\", \"duration\": 0.6, \"end\": \"restore\","
        + " \"channels\": [ { \"node\": \"CharacterPortrait_Track_Move\", \"kind\": \"position\","
        + " \"y\": [ { \"t\": 0, \"v\": 0 }, { \"t\": 0.6, \"v\": 0 } ] } ] } ] }";

    [Test]
    public void shot_shake는_카메라_리그의_클립과_세기를_싣는다()
    {
        StageCommandParser parser = new(null, MotionLibrary.Parse(ShakeAndNod, "test", MotionLibrary.StageRigNodeIds));

        ShotShakeCommandSpec spec = (ShotShakeCommandSpec)Parse(parser, Cmd("shot_shake", "hit"))[0];
        Assert.AreEqual("hit", spec.clip.Name);
        Assert.AreEqual(1f, spec.intensity, 1e-6f, "세기는 생략하면 1이다");

        Assert.AreEqual(0.5f, ((ShotShakeCommandSpec)Parse(parser, Cmd("shot_shake", "hit", "0.5"))[0]).intensity, 1e-6f);
    }

    [Test]
    public void 흔들림과_캐릭터_모션은_서로의_명령으로_틀_수_없다()
    {
        StageCommandParser parser = new(null, MotionLibrary.Parse(ShakeAndNod, "test", MotionLibrary.StageRigNodeIds));

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*shot_shake 'nod'.*리그의 모션이다"));
        Assert.IsEmpty(Parse(parser, Cmd("shot_shake", "nod")), "캐릭터 모션은 화면 흔들림이 아니다");

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*motion 'hit'.*캐릭터 슬롯에 틀 수 없다"));
        Assert.IsEmpty(Parse(parser, Cmd("motion", "c1", "hit")), "흔들림 클립은 캐릭터 슬롯에 틀 수 없다");

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*Unknown shake"));
        Assert.IsEmpty(Parse(parser, Cmd("shot_shake", "nope")), "기본 흔들림은 없다");
    }

    // ── 대사창 초상 (docs/PLAN_portrait-capture.md) ───────────────────────────

    [Test]
    public void portrait는_초상_리그를_촬영소에_세우고_슬롯만_묶어_바로_보인다()
    {
        // 팝업 · 컷인과 같은 결 — 대본은 뜻만 말하고, 슬롯 키 · 서는 자리는 파서가 정한다.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("portrait", "harin", "b", "3"));

        SetupCharRigCommandSpec setup = (SetupCharRigCommandSpec)specs[0];
        Assert.AreEqual(PortraitStudio.SlotKey, setup.roleKey);
        Assert.AreEqual(PortraitStudio.StageKey, setup.stage, "촬영소 — 대본의 무대가 아니다");
        Assert.IsFalse(setup.asBackground);
        Assert.IsTrue(setup.outsideFold, "폴드 밖 — 코어는 초상 인물을 모른다");

        CastCharacterCommandSpec cast = (CastCharacterCommandSpec)specs[1];
        Assert.AreEqual(PortraitStudio.SlotKey, cast.roleKey);
        Assert.AreEqual("harin", cast.characterKey);
        Assert.IsTrue(cast.slotOnly, "캐릭터 키는 무대의 인물 것이다");

        FadeCommandSpecCharR fade = (FadeCommandSpecCharR)specs[specs.Count - 1];
        Assert.AreEqual(PortraitStudio.SlotKey, fade.roleKey);
        Assert.AreEqual(CharacterRigNode.CharacterPortraitSprite_Root, fade.node);
        Assert.AreEqual(1f, fade.targetAlpha);
        Assert.AreEqual(0f, fade.duration, "페이드는 대사창의 것이다");

        // 그 사이의 포즈 · 표정 · 앵커는 보통 cast가 내는 것과 같다(슬롯 키만 다르다).
        List<CommandSpecBase> plainCast = Parse(NewParser(), Cmd("cast", PortraitStudio.SlotKey, "harin", "b", "3"));
        Assert.AreEqual(plainCast.Count + 2, specs.Count);
    }

    [Test]
    public void portrait는_캐릭터가_없으면_경고하고_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<portrait>>에 캐릭터 키가 없다"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("portrait")).Count);
    }

    [Test]
    public void portrait_close는_초상_리그를_숨긴다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("portrait_close"));

        Assert.AreEqual(1, specs.Count);

        FadeCommandSpecCharR fade = (FadeCommandSpecCharR)specs[0];
        Assert.AreEqual(PortraitStudio.SlotKey, fade.roleKey);
        Assert.AreEqual(0f, fade.targetAlpha);
        Assert.AreEqual(0f, fade.duration);
    }

    [Test]
    public void 촬영소에는_대본이_직접_세우지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("촬영소\\(studio\\)에는 대본이 직접 세우지 않는다"));
        Assert.AreEqual(0, Parse(NewParser(), Cmd("slot", "pov", PortraitStudio.StageKey)).Count);
    }
}
