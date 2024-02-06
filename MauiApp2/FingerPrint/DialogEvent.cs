using Android.Content;
using Java.Interop;

namespace MauiApp2.FingerPrint;

public class DialogEvent : Java.Lang.Object, IDialogInterfaceOnClickListener
{
    public void Dispose()
    {
    }

    public IntPtr Handle
    {
        get { return base.Handle; }
    }

    public void SetJniIdentityHashCode(int value)
    {
    }

    public void SetPeerReference(JniObjectReference reference)
    {
    }

    public void SetJniManagedPeerState(JniManagedPeerStates value)
    {
    }

    public void UnregisterFromRuntime()
    {
    }

    public void DisposeUnlessReferenced()
    {
    }

    public void Disposed()
    {
    }

    public void Finalized()
    {
    }

    public int JniIdentityHashCode
    {
        get { return base.JniIdentityHashCode; }
    }

    public JniObjectReference PeerReference
    {
        get
        {
            return base.PeerReference;
        }
    }

    public JniPeerMembers JniPeerMembers
    {
        get
        {
            return base.JniPeerMembers;
        }
    }

    public JniManagedPeerStates JniManagedPeerState { get; }

    public void OnClick(IDialogInterface dialog, int which)
    {
        //dialog.Cancel();
    }
}