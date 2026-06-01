using UnityEngine;
using UnityEngine.VFX;

public class VFXTrigger : MonoBehaviour
{
  public VisualEffect effect;

    public void TriggerVFX(Vector3 position)
    {
        VFXEventAttribute att = effect.CreateVFXEventAttribute();
        att.SetVector3("position", position);
        effect.SendEvent("OnPlay", att);
        
    }
}
