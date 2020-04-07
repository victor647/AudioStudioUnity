using AudioStudio;
using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject Emitter;
    
    void Start()
    {
        AudioInitSettings.Instance.Initialize();
        AudioManager.LoadBank("Character");
        AudioManager.PlaySound("Character_Attack", Emitter);
    }
}
