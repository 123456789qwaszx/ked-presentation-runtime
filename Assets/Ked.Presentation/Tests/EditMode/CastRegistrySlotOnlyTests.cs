using NUnit.Framework;

// 슬롯만 묶는 배역(CastRegistry.CastCharRig의 claimCharacterKey: false) — 대사창 초상이 쓴다.
// 같은 캐릭터가 무대에도 서 있을 때 캐릭터 키는 무대의 인물을 가리켜야 한다(docs/PLAN_portrait-capture.md §1.6).
public class CastRegistrySlotOnlyTests
{
    [Test]
    public void 슬롯만_묶으면_캐릭터_키는_먼저_묶인_슬롯을_그대로_가리킨다()
    {
        CastRegistry cast = new();
        cast.CastCharRig("s1", "harin");
        cast.CastCharRig("portrait", "harin", claimCharacterKey: false);

        Assert.IsTrue(cast.TryGetSlotKey("harin", out string slot));
        Assert.AreEqual("s1", slot);

        Assert.IsTrue(cast.TryPeekCharacter("portrait", out string character));
        Assert.AreEqual("harin", character, "슬롯에서 캐릭터를 묻는 길은 그대로다 — 초상화 · 포커스 점이 이것으로 찾는다");
    }

    [Test]
    public void 슬롯만_묶은_것뿐이면_캐릭터_키로는_찾지_못한다()
    {
        CastRegistry cast = new();
        cast.CastCharRig("portrait", "harin", claimCharacterKey: false);

        Assert.IsFalse(cast.TryGetSlotKey("harin", out _));
    }

    [Test]
    public void 슬롯만_묶은_것을_풀어도_무대의_캐릭터_키는_남는다()
    {
        CastRegistry cast = new();
        cast.CastCharRig("s1", "harin");
        cast.CastCharRig("portrait", "harin", claimCharacterKey: false);

        Assert.IsTrue(cast.UncastCharRig("portrait"));

        Assert.IsTrue(cast.TryGetSlotKey("harin", out string slot));
        Assert.AreEqual("s1", slot);
    }
}
