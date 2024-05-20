using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("c1a736b5-8dec-417e-b6cb-94a6a7d5ad82")]
    public class LookTest06 : Instance<LookTest06>
    {

        [Output(Guid = "d6775a29-1ba2-4404-87f2-ae2600f4b8cb")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

