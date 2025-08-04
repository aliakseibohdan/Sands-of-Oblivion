using UnityEngine;

public class InputDebugger : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (InputManager.Instance != null)
        {
            GUILayout.Label($"Current Action Map: {InputManager.Instance.CurrentMap.name}");

            if (InputManager.Instance.Gameplay.enabled)
            {
                Vector2 mousePos = InputManager.Instance.Gameplay.Point.ReadValue<Vector2>();
                GUILayout.Label($"Mouse Position: {mousePos}");
            }
        }

        GUILayout.EndArea();
    }
}