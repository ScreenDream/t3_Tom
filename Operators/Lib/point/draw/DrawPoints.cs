using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib.point.draw
{
	[Guid("b063987b-f1e7-4dea-bda3-26b4f2ec28e3")]
    public class DrawPoints : Instance<DrawPoints>
    {
        [Output(Guid = "48776178-de4c-4e1b-8ee2-e1f628f1895d")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "b6f0edf9-f9dd-499a-bad7-7f97767f2447")]
        public readonly InputSlot<BufferWithViews> GPoints = new InputSlot<BufferWithViews>();

        [Input(Guid = "15cb1fde-8194-4641-865f-f03c708c9d6b")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "fa39eb26-af9a-4423-a9c6-8267a8f3e377")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "7692a7c9-97c6-4a0d-a318-2fd244606b41")]
        public readonly InputSlot<bool> UseSizeAttribute = new InputSlot<bool>();

        [Input(Guid = "8f9f9511-9513-4f7c-a14e-8c8d3da72941", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "b2c46a56-71db-4de2-875b-32751d4656fa")]
        public readonly InputSlot<float> FadeNearest = new InputSlot<float>();

        [Input(Guid = "9918a66a-07cf-4d53-8d46-3456682adab9")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

        [Input(Guid = "a5fb98a9-0414-4a2e-82b0-373f2cb71f6f")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "d628635d-35c6-424c-9387-171c56ade309")]
        public readonly InputSlot<Texture2D> Texture_ = new InputSlot<Texture2D>();
    }
}

