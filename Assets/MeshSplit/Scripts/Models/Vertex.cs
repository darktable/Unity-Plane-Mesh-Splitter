using Unity.Mathematics;

namespace MeshSplit.Scripts.Models
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Vertex
    {
        public float4 position;
        public half4 normal;
        public half4 color;
        public half4 uv0;
        public half4 uv1;
        public half4 uv2;
        public half4 uv3;
        public half4 uv4;
        public half4 uv5;
        public half4 uv6;
        public half4 uv7;
    }
}