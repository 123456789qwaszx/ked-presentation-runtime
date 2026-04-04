using System.Collections.Generic;

public readonly struct SpritePortBinding
{
    public readonly string PortId;
    public readonly string Address;

    public SpritePortBinding(string portId, string address)
    {
        PortId = portId;
        Address = address;
    }
}

public sealed class SpritePatchResolver
{
    public List<SpritePortBinding> BuildBindings(
        IUISpritePortProvider ui,
        in UIContext ctx)
    {
        var result = new List<SpritePortBinding>();

        var ports = ui.GetSpritePortIds();
        for (int i = 0; i < ports.Count; i++)
        {
            string portId = ports[i];
            string address = BuildDefaultAddress(portId, ctx);
            if (string.IsNullOrEmpty(address))
                continue;

            result.Add(new SpritePortBinding(portId, address));
        }

        return result;
    }

    private static string BuildDefaultAddress(string portId, in UIContext ctx)
    {
        return $"ui/{ctx.ThemeId}/{portId}";
    }
}