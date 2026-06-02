using UnityEngine;

public struct RenderVariant{
    public Mesh[] lodMeshes;
    public ComputeBuffer[] lodArgsBuffers;
    public ComputeBuffer[] visibleBuffers;
}
