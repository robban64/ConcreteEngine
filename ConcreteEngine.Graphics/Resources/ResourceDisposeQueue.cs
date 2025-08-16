using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

public class ResourceDisposeQueue(Action<IGraphicsResource> removeHandler)
{
    private readonly record struct ResourceDeleteData(IGraphicsResource Resource, ushort ResourceId);

    private readonly Queue<ResourceDeleteData> _resourceDisposeQueue = new (8);
    
    public void Enqueue(IGraphicsResource resource, ushort resourceId)
    {
        //debug
        if (resource.IsDisposed) GraphicsException.ThrowResourceIsDisposed(resourceId);

        _resourceDisposeQueue.Enqueue(new ResourceDeleteData(resource, resourceId));
    }

    public void Drain(bool drainAll = false)
    {
        if (_resourceDisposeQueue.Count == 0) return;
        int index = 0;
        while (true)
        {
            if (_resourceDisposeQueue.Count == 0) break;
            if (index >= 1 && !drainAll) break;
            
            var resource = _resourceDisposeQueue.Dequeue();
            removeHandler(resource.Resource);
            index++;
        }
        /*
        foreach (var deleteData in _resourceDisposeQueue)
        {
            if (!drainAll && index >= 4) break;
            index++;

            removeHandler(deleteData.Resource);
        }*/
        
    }
    
    
}