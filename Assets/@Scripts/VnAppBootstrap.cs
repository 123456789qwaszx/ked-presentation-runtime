using UnityEngine;

public class VnAppBootstrap : MonoBehaviour
{
    private VnScreenBindings _screenBindings;
    
    private void Awake()
    {
        _screenBindings = new VnScreenBindings();
    }
    
    private void Start()
    {
        _screenBindings?.GoToTitle();
    }
}