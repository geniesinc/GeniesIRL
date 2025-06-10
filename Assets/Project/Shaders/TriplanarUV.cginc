void Triplanar_float(float3 Position, float3 Normal, float Tile, float Blend, out float2 uv)
{
    float3 Node_UV = Position * Tile;
    float3 Node_Blend = pow(abs(Normal), Blend);
    Node_Blend /= dot(Node_Blend, 1.0);
    float2 Node_X = Node_UV.zy;
    float2 Node_Y = Node_UV.xz;
    float2 Node_Z = Node_UV.xy;
    uv = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
}