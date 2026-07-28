using System.Collections.Generic;
using Yarn.Unity;

// 구매 가능한 공용 능력을 계산하고,
// 플레이어의 구매/장착 결정을 캠페인 상태에 반영.
public sealed class NightPrepFlow
{
    private readonly DungeonCafeContentDB _content;
    private readonly VnScreenBindings _screens;

    public NightPrepFlow(
        DungeonCafeContentDB content,
        VnScreenBindings screens)
    {
        _content = content;
        _screens = screens;
    }

    public async YarnTask RunAsync(CampaignState campaign)
    {
        List<PlayerAbilityDefinition> purchasable = ResolveShopAndAutoGrant(campaign);
        int slotLimit = campaign.Tuning.GetAbilitySlots(campaign.ShopLevel);

        NightPrepRequest request = new(
            purchasable,
            new List<string>(campaign.Abilities.Owned),
            campaign.Abilities.Equipped,
            slotLimit,
            campaign.Ledger.Held);

        NightPrepResponse response =
            await _screens.RequestNightPrepAsync(request);

        // 구매 적용
        for (int i = 0; i < response.PurchaseIds.Count; i++)
        {
            PlayerAbilityDefinition ability =
                _content.GetAbility(response.PurchaseIds[i]);

            if (ability != null)
                AbilityRules.TryPurchase(campaign, ability);
        }
        
        // 장착 적용
        var equippedBefore = new List<string>(campaign.Abilities.Equipped);

        for (int i = 0; i < equippedBefore.Count; i++)
            campaign.Abilities.Unequip(equippedBefore[i]);

        for (int i = 0; i < response.EquipIds.Count; i++)
            campaign.Abilities.Equip(response.EquipIds[i], slotLimit);
    }

    private List<PlayerAbilityDefinition> ResolveShopAndAutoGrant(
        CampaignState campaign)
    {
        var result = new List<PlayerAbilityDefinition>();

        for (int i = 0; i < _content.Abilities.Count; i++)
        {
            PlayerAbilityDefinition ability = _content.Abilities[i];

            if (campaign.Abilities.Owns(ability.Id))
                continue;

            // 메이드 전용 능력은 상점이 아니라 관계 단계에서 자동 습득.
            if (ability.OwnerMaidId != null)
            {
                if (AbilityRules.MeetsConditions(campaign, ability))
                    campaign.Abilities.Grant(ability.Id);
                
                continue;
            }

            if (AbilityRules.MeetsConditions(campaign, ability))
                result.Add(ability);
        }

        return result;
    }
}