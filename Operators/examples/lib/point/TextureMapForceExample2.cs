using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
    [Guid("4bf73e3c-1d8c-4007-b155-b0edf00c2e2e")]
    public class TextureMapForceExample2 : Instance<TextureMapForceExample2>
    {
        [Output(Guid = "97420db1-fd37-4f4b-81b2-03146cb542a9")]
        public readonly Slot<Texture2D> TextureOut = new Slot<Texture2D>();


    }
}

