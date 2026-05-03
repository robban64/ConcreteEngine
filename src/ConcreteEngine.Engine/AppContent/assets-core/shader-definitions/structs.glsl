struct LightData {
    vec4 color_intensity;// rgb=color, a=intensity
    vec4 pos_range;// xyz=position, w=range (0=inf)
    vec4 dir_type;// xyz=dir (spot), w=type (1=point, 2=spot)
    vec4 spot_angles;// x=cosInner, y=cosOuter, zw=reserved
};
