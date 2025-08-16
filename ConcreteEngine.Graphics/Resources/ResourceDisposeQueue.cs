using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

public class ResourceDisposeQueue(Action<IGraphicsResource> removeHandler)
{
    private const int DrainPerFrame = 4;
    private const int DrainDelayTicks = 4;
    
    private readonly record struct ResourceDeleteData(IGraphicsResource Resource, ushort ResourceId);

    private readonly Queue<ResourceDeleteData> _resourceDisposeQueue = new (8);
    
    private int _drainDelayTicks;
    
    public void Enqueue(IGraphicsResource resource, ushort resourceId)
    {
        //debug
        if (resource.IsDisposed) GraphicsException.ThrowResourceIsDisposed(resourceId);

        _resourceDisposeQueue.Enqueue(new ResourceDeleteData(resource, resourceId));
    }

    public void Drain(bool drainAll = false)
    {
        if (_resourceDisposeQueue.Count == 0) return;

        _drainDelayTicks++;
        if(_drainDelayTicks < DrainPerFrame) return;
        
        int index = 0;
        while (true)
        {
            if (_resourceDisposeQueue.Count == 0) break;
            if (index >= DrainPerFrame && !drainAll) break;
            
            var resource = _resourceDisposeQueue.Dequeue();
            removeHandler(resource.Resource);
            index++;
        }

        _drainDelayTicks = 0;

    }
    
    
}