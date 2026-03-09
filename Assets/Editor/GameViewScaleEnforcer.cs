#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameViewScaleEnforcer
{
    private const float TargetScale = 1f;

    static GameViewScaleEnforcer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        EditorApplication.delayCall += ForceGameViewScale;
    }

    private static void ForceGameViewScale()
    {
        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
        {
            return;
        }

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        if (gameView == null)
        {
            return;
        }

        FieldInfo zoomAreaField = gameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
        if (zoomAreaField == null)
        {
            return;
        }

        object zoomArea = zoomAreaField.GetValue(gameView);
        if (zoomArea == null)
        {
            return;
        }

        Type zoomAreaType = zoomArea.GetType();
        FieldInfo scaleField = zoomAreaType.GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
        if (scaleField == null)
        {
            return;
        }

        scaleField.SetValue(zoomArea, new Vector2(TargetScale, TargetScale));
        gameView.Repaint();
    }
}
#endif
