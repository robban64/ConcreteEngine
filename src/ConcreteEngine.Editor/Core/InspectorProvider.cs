namespace ConcreteEngine.Editor.Core;
/*
internal static class InspectorProvider
{
    private static Camera Camera => Inspector<Camera>.Target!;
    private static AssetObject Asset => Inspector<AssetObject>.Target!;
    private static Material Material => (Material)Inspector<AssetObject>.Target!;

    public static void RegisterAsset()
    {
        Inspector<AssetObject>.Register(
            "",
            () => (Float1)Material.Specular,
            value => Material.Specular = (float)value,
            new FloatInput<Float1>("Specular", FieldWidgetKind.Slider) { Min = 0, Max = 50 });
        Inspector<AssetObject>.Register("",
            () => (Float1)Material.Shininess,
            value => Material.Shininess = (float)value,
            new FloatInput<Float1>("Shininess", FieldWidgetKind.Slider) { Min = 0, Max = 50 });

        Inspector<AssetObject>.Register("",
            () => (Float1)Material.UvRepeat,
            value => Material.UvRepeat = (float)value,
            new FloatInput<Float1>("UV Repeat", FieldWidgetKind.Slider));

        Inspector<AssetObject>.Register<Int1>("",
            () => (int)Material.Pipeline.PassFunctions.Blend,
            value => Material.SetPassFunction(Material.Pipeline.PassFunctions with { Blend = (BlendMode)value.X }),
            ComboInput.MakeFromEnumCache<BlendMode>("Blend Mode"));

        Inspector<AssetObject>.Register<Int1>("",
            () => (int)Material.Pipeline.PassFunctions.Cull,
            value => Material.SetPassFunction(Material.Pipeline.PassFunctions with { Cull = (CullMode)value.X }),
            ComboInput.MakeFromEnumCache<CullMode>("Cul Model"));

        Inspector<AssetObject>.Register<Int1>("Depth Mode",
            () => (int)Material.Pipeline.PassFunctions.Depth,
            value => Material.SetPassFunction(Material.Pipeline.PassFunctions with { Depth = (DepthMode)value.X }),
            ComboInput.MakeFromEnumCache<DepthMode>("Depth Mode"));

        Inspector<AssetObject>.Register<Int1>("Polygon Offset",
            () => (int)Material.Pipeline.PassFunctions.PolygonOffset,
            value => Material.SetPassFunction(Material.Pipeline.PassFunctions with
            {
                PolygonOffset = (PolygonOffsetLevel)value.X
            }),
            ComboInput.MakeFromEnumCache<PolygonOffsetLevel>("Polygon Offset"));
    }

    public static void RegisterCamera()
    {
        Inspector<Camera>.Bind(EditorCamera.Instance.Camera);
        Inspector<Camera>.Register(
            nameof(Camera.Translation),
            static () => (Float3)Camera.Translation,
            static (v) => Camera.Translation = (Vector3)v,
            new FloatInput<Float3>(nameof(Camera.Translation), FieldWidgetKind.Input) { Format = "%.3f" });

        Inspector<Camera>.Register(
            nameof(Camera.Orientation),
            static () => new Float2(Camera.Orientation.Yaw, Camera.Orientation.Pitch),
            static (v) => Camera.Orientation = new YawPitch(v.X, v.Y),
            new FloatInput<Float2>(nameof(Camera.Orientation), FieldWidgetKind.Input) { Format = "%.3f" });

        Inspector<Camera>.Register(
            "Plane",
            static () => new Float2(Camera.NearPlane, Camera.FarPlane),
            static (v) =>
            {
                Camera.NearPlane = v.X;
                Camera.FarPlane = v.Y;
            },
            new FloatInput<Float2>("Near/Far", FieldWidgetKind.Input),
            FieldGetDelay.High
        );
        Inspector<Camera>.Register(
            "Fov",
            static () => (Float1)Camera.Fov,
            static (v) => Camera.Fov = (float)v,
            new FloatInput<Float1>("Field of view", FieldWidgetKind.Slider) { Min = 10f, Max = 179f },
            FieldGetDelay.High
        );
    }
}*/