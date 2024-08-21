using System.Runtime.InteropServices;
using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace lib.point.generate
{
	[Guid("94753df7-3c3e-493e-8b45-b5ac0b806a0c")]
    public class RadialPoints : Instance<RadialPoints>
,ITransformable
    {

        [Output(Guid = "ca675627-a29a-4eb8-8a5d-1d3c51baf7ac")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        public RadialPoints()
        {
            OutBuffer.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Center;
        IInputSlot ITransformable.RotationInput => null;
        IInputSlot ITransformable.ScaleInput => null;

        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "0c422075-7a67-47fa-8d0b-52a22d2e0b16")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "09204a70-5fde-45e8-aeaf-f7656f8105e8")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "21f9c645-2497-4a62-bc57-03ad9b5c3bba")]
        public readonly InputSlot<float> RadiusOffset = new InputSlot<float>();

        [Input(Guid = "75677a27-97f9-4bd4-8685-9030a9cf376e")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "92ef8215-aea2-46cc-96b8-f26df89fc6ea")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "f7f7e4fc-f7bc-44e9-a655-bee3153b18ae")]
        public readonly InputSlot<float> StartAngle = new InputSlot<float>();

        [Input(Guid = "e36118a3-b0fb-4237-bbca-9b4d3687ba54")]
        public readonly InputSlot<float> Cycles = new InputSlot<float>();

        [Input(Guid = "34a9fac5-ed23-4c3a-90af-ac7cc79803fd")]
        public readonly InputSlot<System.Numerics.Vector3> Axis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "70781fb1-8851-49a6-a829-7cd082053377")]
        public readonly InputSlot<bool> CloseCircleLine = new InputSlot<bool>();

        [Input(Guid = "7005ac18-4dbf-4f77-a6a8-863b877e7cf5")]
        public readonly InputSlot<float> PointScale = new InputSlot<float>();

        [Input(Guid = "7740ab78-a000-4c21-96a8-8f0ed810a15f")]
        public readonly InputSlot<float> ScaleOffset = new InputSlot<float>();

        [Input(Guid = "2dcf997e-e38f-416d-85f3-c8f61e657700")]
        public readonly InputSlot<System.Numerics.Vector3> OrientationAxis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "555b3d2e-9496-4148-8563-1277c04aaec9")]
        public readonly InputSlot<float> OrientationAngle = new InputSlot<float>();

        [Input(Guid = "2b677db9-42b3-41af-a104-a6be69d0847b")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "809e695e-755b-42a6-8b82-2b84e9c8ee0f")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();
    }
}

