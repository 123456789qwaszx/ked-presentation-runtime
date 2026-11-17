using NUnit.Framework;

// 폴드 밖 리그 — 코어가 접지 않는 리그(컷인 판 · 팝업 그림)를 레지스트리가 표시로 들고, 등가성 하네스의 캡처가 건너뛴다
// (StageStateCapture, docs/PLAN_stage-visibility.md §3.5). 이름이 아니라 등록으로 고른다.
public class CharacterRigRegistryOutsideFoldTests
{
    [Test]
    public void 폴드_밖으로_등록한_리그만_표시가_있다()
    {
        CharacterRigRegistry registry = new();

        registry.Register("stage01_cutin_panel", null, outsideFold: true);
        registry.Register("bg_main", null);

        Assert.IsTrue(registry.IsOutsideFold("stage01_cutin_panel"));
        Assert.IsFalse(registry.IsOutsideFold("bg_main"), "배경 · 인물은 폴드 안");
        Assert.IsFalse(registry.IsOutsideFold("nope"));
        Assert.IsFalse(registry.IsOutsideFold(null));
    }

    [Test]
    public void 다시_등록하면_표시를_새로_정하고_비우면_사라진다()
    {
        CharacterRigRegistry registry = new();

        registry.Register("k", null, outsideFold: true);
        registry.Register("k", null);
        Assert.IsFalse(registry.IsOutsideFold("k"), "재선언은 새 리그다 — 표시도 새로");

        registry.Register("k", null, outsideFold: true);
        registry.Clear();
        Assert.IsFalse(registry.IsOutsideFold("k"));
    }
}
