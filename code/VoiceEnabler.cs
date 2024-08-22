using Mini.UI;
using Sandbox;
using System.Threading.Tasks;

namespace Mini;

public class VoiceEnabler : Component
{
    [Property, RequireComponent]
    public Voice Voice { get; set; } = null!;

    [Property]
    public bool PushToTalk { get; set; } = true;

    [Sync]
    public bool IsListening { get; set; } = false;
    [Sync]
    public float Amplitude { get; set; } = 0f;

    protected override Task OnLoad()
    {
        if(VoiceList.Instance.IsValid())
            VoiceList.Instance.Add(this);

        return Task.CompletedTask;
    }

    protected override void OnDestroy()
    {
        if(VoiceList.Instance.IsValid())
            VoiceList.Instance.Remove(this);
    }

    protected override void OnEnabled()
    {
        if(IsProxy)
            return;

        Voice.Mode = Voice.ActivateMode.Manual;
    }

    protected override void OnUpdate()
    {
        if(IsProxy)
            return;

        IsListening = PushToTalk ? Input.Down(Voice.PushToTalkInput) : true;
        Voice.IsListening = IsListening;
        Amplitude = Voice.Amplitude;
    }

    protected override void OnDisabled()
    {
        if(IsProxy)
            return;

        IsListening = false;
        Voice.IsListening = false;
    }
}
