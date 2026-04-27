//using UnityEditor;
//using UnityEngine;
//[CustomEditor(typeof(VoiceNetworker))]
//public class VoiceNetworkerEditor : Editor
//{
//    private SerializedProperty _voiceChatModeProp;
//    private SerializedProperty _playbackOwnVoiceProp;
//    private SerializedProperty _meterSensitivityProp;

//    private bool _modeFoldout = true;
//    private bool _playbackFoldout = true;
//    private bool _meterFoldout = true;
//    private bool _statusFoldout = true;

//    private static readonly Color AccentBlue = new Color(0.3f, 0.55f, 0.95f);
//    private static readonly Color AccentGreen = new Color(0.25f, 0.8f, 0.35f);
//    private static readonly Color AccentRed = new Color(0.9f, 0.3f, 0.3f);
//    private static readonly Color AccentYellow = new Color(0.95f, 0.85f, 0.2f);
//    private static readonly Color AccentOrange = new Color(0.95f, 0.55f, 0.15f);
//    private static readonly Color DarkBg = new Color(0.12f, 0.12f, 0.12f);
//    private static readonly Color MediumBg = new Color(0.18f, 0.18f, 0.18f);
//    private static readonly Color SegmentOff = new Color(0.22f, 0.22f, 0.22f);
//    private static readonly Color BorderColor = new Color(0.1f, 0.1f, 0.1f);
//    private static readonly Color HeaderBg = new Color(0.16f, 0.16f, 0.2f);

//    private GUIStyle _headerStyle;
//    private GUIStyle _subHeaderStyle;
//    private GUIStyle _descriptionStyle;
//    private GUIStyle _meterLabelStyle;
//    private GUIStyle _meterValueStyle;
//    private GUIStyle _statusLabelStyle;
//    private GUIStyle _centeredMiniLabel;
//    private void OnEnable()
//    {
//        _voiceChatModeProp = serializedObject.FindProperty("_voiceChatMode");
//        _playbackOwnVoiceProp = serializedObject.FindProperty("_playbackOwnVoice");
//        _meterSensitivityProp = serializedObject.FindProperty("_meterSensitivity");
//    }
//    private void InitStyles()
//    {
//        if (_headerStyle != null) return;
//        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
//        {
//            fontSize = 14,
//            alignment = TextAnchor.MiddleLeft,
//            padding = new RectOffset(8, 0, 4, 4)
//        };
//        _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
//        {
//            fontSize = 11,
//            padding = new RectOffset(4, 0, 2, 2)
//        };
//        _descriptionStyle = new GUIStyle(EditorStyles.miniLabel)
//        {
//            wordWrap = true,
//            richText = true,
//            padding = new RectOffset(6, 6, 2, 2)
//        };
//        _descriptionStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
//        _meterLabelStyle = new GUIStyle(EditorStyles.miniLabel)
//        {
//            alignment = TextAnchor.MiddleLeft,
//            fontSize = 10
//        };
//        _meterLabelStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
//        _meterValueStyle = new GUIStyle(EditorStyles.boldLabel)
//        {
//            alignment = TextAnchor.MiddleRight,
//            fontSize = 11
//        };
//        _statusLabelStyle = new GUIStyle(EditorStyles.label)
//        {
//            richText = true,
//            fontSize = 11,
//            padding = new RectOffset(6, 0, 1, 1)
//        };
//        _centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel)
//        {
//            alignment = TextAnchor.MiddleCenter,
//            fontSize = 9
//        };
//        _centeredMiniLabel.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
//    }
//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();
//        InitStyles();
//        VoiceNetworker voiceNetworker = (VoiceNetworker)target;
//        DrawMainHeader();
//        EditorGUILayout.Space(4);
//        DrawVoiceChatModeSection(voiceNetworker);
//        EditorGUILayout.Space(2);
//        DrawSelfPlaybackSection(voiceNetworker);
//        EditorGUILayout.Space(2);
//        DrawVoiceLevelMeterSection(voiceNetworker);
//        if (Application.isPlaying)
//        {
//            EditorGUILayout.Space(2);
//            DrawRuntimeStatusSection(voiceNetworker);
//        }
//        EditorGUILayout.Space(6);
//        serializedObject.ApplyModifiedProperties();
//    }

//    private void DrawMainHeader()
//    {
//        Rect headerRect = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));

//        EditorGUI.DrawRect(headerRect, HeaderBg);

//        Rect accentBar = new Rect(headerRect.x, headerRect.y, 3, headerRect.height);
//        EditorGUI.DrawRect(accentBar, AccentBlue);

//        _headerStyle.normal.textColor = AccentBlue;
//        Rect titleRect = new Rect(headerRect.x + 10, headerRect.y, headerRect.width - 10, headerRect.height);
//        EditorGUI.LabelField(titleRect, "\u266a Voice Networker", _headerStyle);

//        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
//        {
//            alignment = TextAnchor.MiddleRight,
//            fontSize = 9
//        };
//        badgeStyle.normal.textColor = new Color(0.45f, 0.45f, 0.5f);
//        Rect badgeRect = new Rect(headerRect.xMax - 60, headerRect.y, 55, headerRect.height);
//        EditorGUI.LabelField(badgeRect, "v1.2", badgeStyle);
//    }

//    private void DrawVoiceChatModeSection(VoiceNetworker voiceNetworker)
//    {
//        _modeFoldout = DrawSectionHeader("\u25b6 Voice Chat Mode", _modeFoldout, AccentBlue);
//        if (!_modeFoldout) return;
//        DrawBoxStart();
//        VoiceChatMode currentMode = (VoiceChatMode)_voiceChatModeProp.enumValueIndex;
//        EditorGUILayout.BeginHorizontal();
//        GUILayout.FlexibleSpace();
//        // Push To Talk button
//        bool isPTT = currentMode == VoiceChatMode.PushToTalk;
//        GUI.backgroundColor = isPTT ? AccentBlue : Color.gray;
//        GUIStyle pttStyle = new GUIStyle("Button")
//        {
//            fontStyle = isPTT ? FontStyle.Bold : FontStyle.Normal,
//            fontSize = 11,
//            fixedHeight = 30,
//            fixedWidth = 150
//        };
//        if (GUILayout.Button("\u25c9  Push To Talk", pttStyle))
//        {
//            _voiceChatModeProp.enumValueIndex = (int)VoiceChatMode.PushToTalk;
//            if (Application.isPlaying)
//                voiceNetworker.SetVoiceChatMode(VoiceChatMode.PushToTalk);
//        }
//        GUILayout.Space(6);
//        // Toggle button
//        bool isToggle = currentMode == VoiceChatMode.Toggle;
//        GUI.backgroundColor = isToggle ? AccentBlue : Color.gray;
//        GUIStyle toggleStyle = new GUIStyle("Button")
//        {
//            fontStyle = isToggle ? FontStyle.Bold : FontStyle.Normal,
//            fontSize = 11,
//            fixedHeight = 30,
//            fixedWidth = 150
//        };
//        if (GUILayout.Button("\u21c5  Toggle", toggleStyle))
//        {
//            _voiceChatModeProp.enumValueIndex = (int)VoiceChatMode.Toggle;
//            if (Application.isPlaying)
//                voiceNetworker.SetVoiceChatMode(VoiceChatMode.Toggle);
//        }
//        GUI.backgroundColor = Color.white;
//        GUILayout.FlexibleSpace();
//        EditorGUILayout.EndHorizontal();
//        EditorGUILayout.Space(4);
//        // Description
//        string description = currentMode == VoiceChatMode.PushToTalk
//            ? "Hold the voice key to transmit. Release to stop."
//            : "Press the voice key once to start. Press again to stop.";
//        // Mode indicator
//        Rect descRect = EditorGUILayout.GetControlRect(false, 18);
//        Rect dotRect = new Rect(descRect.x + 4, descRect.y + 5, 8, 8);
//        EditorGUI.DrawRect(dotRect, isPTT ? new Color(0.4f, 0.7f, 1f) : new Color(0.6f, 0.4f, 1f));
//        Rect textRect = new Rect(descRect.x + 18, descRect.y, descRect.width - 18, descRect.height);
//        EditorGUI.LabelField(textRect, description, _descriptionStyle);
//        DrawBoxEnd();
//    }

//    private void DrawSelfPlaybackSection(VoiceNetworker voiceNetworker)
//    {
//        _playbackFoldout = DrawSectionHeader("\u266b Self Playback", _playbackFoldout, AccentGreen);
//        if (!_playbackFoldout) return;
//        DrawBoxStart();
//        bool isEnabled = _playbackOwnVoiceProp.boolValue;
//        EditorGUILayout.BeginHorizontal();
//        GUILayout.FlexibleSpace();
//        GUI.backgroundColor = isEnabled ? AccentGreen : new Color(0.5f, 0.5f, 0.5f);
//        GUIStyle btnStyle = new GUIStyle("Button")
//        {
//            fontStyle = FontStyle.Bold,
//            fontSize = 12,
//            fixedHeight = 32,
//            fixedWidth = 240,
//            richText = true
//        };
//        string btnText = isEnabled
//            ? "\u2714  Hearing Own Voice"
//            : "\u2716  Own Voice Muted";
//        if (GUILayout.Button(btnText, btnStyle))
//        {
//            _playbackOwnVoiceProp.boolValue = !isEnabled;
//            if (Application.isPlaying)
//                voiceNetworker.SetPlaybackOwnVoice(!isEnabled);
//        }
//        GUI.backgroundColor = Color.white;
//        GUILayout.FlexibleSpace();
//        EditorGUILayout.EndHorizontal();
//        EditorGUILayout.Space(2);

//        string desc = isEnabled
//            ? "You will hear your own transmitted voice (useful for testing)."
//            : "Your own voice is muted locally. Others can still hear you.";
//        Rect descRect = EditorGUILayout.GetControlRect(false, 18);
//        Rect dotRect = new Rect(descRect.x + 4, descRect.y + 5, 8, 8);
//        EditorGUI.DrawRect(dotRect, isEnabled ? AccentGreen : new Color(0.5f, 0.5f, 0.5f));
//        Rect textRect = new Rect(descRect.x + 18, descRect.y, descRect.width - 18, descRect.height);
//        EditorGUI.LabelField(textRect, desc, _descriptionStyle);
//        DrawBoxEnd();
//    }
//    private void DrawVoiceLevelMeterSection(VoiceNetworker voiceNetworker)
//    {
//        _meterFoldout = DrawSectionHeader("\u25cf Voice Level Monitor", _meterFoldout, AccentOrange);
//        if (!_meterFoldout) return;
//        DrawBoxStart();
//        if (!Application.isPlaying)
//        {
//            EditorGUILayout.Space(4);
//            Rect infoRect = EditorGUILayout.GetControlRect(false, 40);
//            Rect infoBg = new Rect(infoRect.x, infoRect.y, infoRect.width, infoRect.height);
//            EditorGUI.DrawRect(infoBg, new Color(0.2f, 0.2f, 0.25f));
//            Rect infoAccent = new Rect(infoRect.x, infoRect.y, 3, infoRect.height);
//            EditorGUI.DrawRect(infoAccent, AccentBlue);
//            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
//            {
//                alignment = TextAnchor.MiddleCenter,
//                fontSize = 11,
//                wordWrap = true
//            };
//            infoStyle.normal.textColor = new Color(0.6f, 0.65f, 0.75f);
//            EditorGUI.LabelField(infoBg, "  \u25b6  Enter Play Mode to monitor voice level", infoStyle);
//            EditorGUILayout.Space(6);
//            // Sensitivity slider (still editable outside play mode)
//            DrawSensitivitySlider();
//            // Draw inactive meter preview
//            EditorGUILayout.Space(4);
//            DrawMeterBar(0f, 0f);
//            DrawMeterScaleLabels();
//            DrawBoxEnd();
//            return;
//        }
//        float level = voiceNetworker.CurrentVoiceLevel;
//        float peak = voiceNetworker.PeakVoiceLevel;
//        bool isTransmitting = voiceNetworker.IsVoiceActive;
//        EditorGUILayout.Space(4);
//        // Level info row
//        EditorGUILayout.BeginHorizontal();
//        // Transmission indicator
//        GUIStyle txStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
//        txStyle.normal.textColor = isTransmitting ? AccentGreen : new Color(0.5f, 0.5f, 0.5f);
//        GUILayout.Label(isTransmitting ? "\u25cf TX" : "\u25cb TX", txStyle, GUILayout.Width(30));
//        GUILayout.FlexibleSpace();
//        // Level percentage
//        _meterValueStyle.normal.textColor = GetLevelColor(level);
//        GUILayout.Label($"{(level * 100f):F0}%", _meterValueStyle, GUILayout.Width(45));
//        GUILayout.Space(8);
//        // Peak percentage
//        GUIStyle peakStyle = new GUIStyle(_meterValueStyle);
//        peakStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
//        peakStyle.fontSize = 10;
//        GUILayout.Label($"Peak: {(peak * 100f):F0}%", peakStyle, GUILayout.Width(70));
//        EditorGUILayout.EndHorizontal();
//        EditorGUILayout.Space(2);
//        // Main VU Meter
//        DrawMeterBar(level, peak);
//        // Scale labels
//        DrawMeterScaleLabels();
//        EditorGUILayout.Space(4);
//        // Sensitivity slider
//        DrawSensitivitySlider();
//        DrawBoxEnd();
//        // Force repaint for animation
//        if (isTransmitting || level > 0.001f)
//        {
//            Repaint();
//        }
//    }
//    private void DrawMeterBar(float level, float peak)
//    {
//        Rect barRect = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
//        // Outer border
//        EditorGUI.DrawRect(barRect, BorderColor);
//        // Inner background
//        Rect innerRect = new Rect(barRect.x + 1, barRect.y + 1, barRect.width - 2, barRect.height - 2);
//        EditorGUI.DrawRect(innerRect, DarkBg);
//        // Draw segments
//        int totalSegments = 40;
//        float padding = 3f;
//        float availableWidth = innerRect.width - padding * 2;
//        float segmentGap = 1.5f;
//        float segmentWidth = (availableWidth - segmentGap * (totalSegments - 1)) / totalSegments;
//        float segmentHeight = innerRect.height - padding * 2;
//        int filledSegments = Mathf.RoundToInt(level * totalSegments);
//        int peakSegment = Mathf.Clamp(Mathf.RoundToInt(peak * totalSegments) - 1, 0, totalSegments - 1);
//        for (int i = 0; i < totalSegments; i++)
//        {
//            float x = innerRect.x + padding + i * (segmentWidth + segmentGap);
//            Rect segRect = new Rect(x, innerRect.y + padding, segmentWidth, segmentHeight);
//            float t = (float)i / totalSegments;
//            bool isFilled = i < filledSegments;
//            bool isPeak = (peak > 0.01f) && (i == peakSegment);
//            Color segColor;
//            if (isPeak)
//            {
//                segColor = Color.white;
//            }
//            else if (isFilled)
//            {
//                segColor = GetSegmentColor(t);
//            }
//            else
//            {
//                // Dim version of the segment color for "off" state
//                Color baseColor = GetSegmentColor(t);
//                segColor = Color.Lerp(SegmentOff, baseColor, 0.15f);
//            }
//            EditorGUI.DrawRect(segRect, segColor);
//            // Glow effect for filled segments near peak
//            if (isFilled && t > 0.7f)
//            {
//                Color glowColor = segColor;
//                glowColor.a = 0.3f;
//                Rect glowRect = new Rect(segRect.x - 1, segRect.y - 1, segRect.width + 2, segRect.height + 2);
//                EditorGUI.DrawRect(glowRect, new Color(glowColor.r, glowColor.g, glowColor.b, 0.15f));
//            }
//        }
//    }
//    private void DrawMeterScaleLabels()
//    {
//        Rect scaleRect = GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));
//        float padding = 4f;
//        float availableWidth = scaleRect.width - padding * 2;
//        string[] labels = { "0", "25", "50", "75", "100" };
//        float[] positions = { 0f, 0.25f, 0.5f, 0.75f, 1f };
//        for (int i = 0; i < labels.Length; i++)
//        {
//            float x = scaleRect.x + padding + availableWidth * positions[i];
//            Rect labelRect = new Rect(x - 12, scaleRect.y, 24, scaleRect.height);
//            EditorGUI.LabelField(labelRect, labels[i], _centeredMiniLabel);
//        }
//    }
//    private void DrawSensitivitySlider()
//    {
//        EditorGUILayout.BeginHorizontal();
//        GUILayout.Space(4);
//        GUIStyle sensLabel = new GUIStyle(_meterLabelStyle) { fixedWidth = 70 };
//        GUILayout.Label("Sensitivity", sensLabel);
//        EditorGUI.BeginChangeCheck();
//        float newSens = EditorGUILayout.Slider(_meterSensitivityProp.floatValue, 1f, 20f);
//        if (EditorGUI.EndChangeCheck())
//        {
//            _meterSensitivityProp.floatValue = newSens;
//        }
//        GUILayout.Space(4);
//        EditorGUILayout.EndHorizontal();
//    }
//    private Color GetSegmentColor(float t)
//    {
//        if (t < 0.55f)
//            return Color.Lerp(new Color(0.15f, 0.75f, 0.3f), new Color(0.3f, 0.85f, 0.25f), t / 0.55f);
//        else if (t < 0.75f)
//            return Color.Lerp(new Color(0.85f, 0.85f, 0.15f), AccentOrange, (t - 0.55f) / 0.2f);
//        else
//            return Color.Lerp(AccentOrange, new Color(0.95f, 0.2f, 0.15f), (t - 0.75f) / 0.25f);
//    }
//    private Color GetLevelColor(float level)
//    {
//        if (level < 0.55f)
//            return AccentGreen;
//        else if (level < 0.75f)
//            return AccentYellow;
//        else
//            return AccentRed;
//    }
//    // ─────────────────────────────────────────────────────────
//    //  RUNTIME STATUS
//    // ─────────────────────────────────────────────────────────
//    private void DrawRuntimeStatusSection(VoiceNetworker voiceNetworker)
//    {
//        _statusFoldout = DrawSectionHeader("\u25c8 Runtime Status", _statusFoldout, AccentYellow);
//        if (!_statusFoldout) return;
//        DrawBoxStart();
//        bool isActive = voiceNetworker.IsVoiceActive;
//        VoiceChatMode mode = voiceNetworker.CurrentVoiceChatMode;
//        bool playback = voiceNetworker.IsPlaybackOwnVoiceEnabled;
//        // Voice state
//        DrawStatusRow(
//            "Voice",
//            isActive ? "TRANSMITTING" : "SILENT",
//            isActive ? AccentGreen : new Color(0.5f, 0.5f, 0.5f)
//        );
//        // Mode
//        DrawStatusRow(
//            "Mode",
//            mode == VoiceChatMode.PushToTalk ? "Push To Talk" : "Toggle",
//            AccentBlue
//        );
//        // Self-playback
//        DrawStatusRow(
//            "Self Playback",
//            playback ? "Enabled" : "Disabled",
//            playback ? AccentGreen : new Color(0.5f, 0.5f, 0.5f)
//        );
//        // Transmit indicator bar
//        if (isActive)
//        {
//            EditorGUILayout.Space(4);
//            DrawTransmitIndicator();
//        }
//        DrawBoxEnd();
//        if (isActive) Repaint();
//    }
//    private void DrawStatusRow(string label, string value, Color valueColor)
//    {
//        Rect rowRect = EditorGUILayout.GetControlRect(false, 20);
//        // Dot indicator
//        Rect dotRect = new Rect(rowRect.x + 6, rowRect.y + 6, 8, 8);
//        DrawCircle(dotRect, valueColor);
//        // Label
//        Rect labelRect = new Rect(rowRect.x + 20, rowRect.y, 100, rowRect.height);
//        EditorGUI.LabelField(labelRect, label, _meterLabelStyle);
//        // Value
//        GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
//        {
//            fontStyle = FontStyle.Bold,
//            fontSize = 11
//        };
//        valueStyle.normal.textColor = valueColor;
//        Rect valueRect = new Rect(rowRect.x + 120, rowRect.y, rowRect.width - 120, rowRect.height);
//        EditorGUI.LabelField(valueRect, value, valueStyle);
//    }
//    private void DrawTransmitIndicator()
//    {
//        Rect barRect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
//        EditorGUI.DrawRect(barRect, DarkBg);
//        // Animated pulse
//        float pulse = Mathf.PingPong((float)EditorApplication.timeSinceStartup * 2f, 1f);
//        float pulseWidth = barRect.width * 0.3f;
//        float pulseX = barRect.x + (barRect.width - pulseWidth) * pulse;
//        Rect pulseRect = new Rect(pulseX, barRect.y, pulseWidth, barRect.height);
//        Color pulseColor = AccentGreen;
//        pulseColor.a = 0.7f;
//        EditorGUI.DrawRect(pulseRect, pulseColor);
//    }
//    // ─────────────────────────────────────────────────────────
//    //  UTILITY DRAWING METHODS
//    // ─────────────────────────────────────────────────────────
//    private bool DrawSectionHeader(string title, bool foldout, Color accentColor)
//    {
//        Rect headerRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
//        // Background
//        Color bgColor = foldout ? new Color(0.22f, 0.22f, 0.26f) : new Color(0.19f, 0.19f, 0.22f);
//        EditorGUI.DrawRect(headerRect, bgColor);
//        // Left accent
//        Rect accentRect = new Rect(headerRect.x, headerRect.y, 3, headerRect.height);
//        EditorGUI.DrawRect(accentRect, accentColor);
//        // Foldout arrow
//        _subHeaderStyle.normal.textColor = accentColor;
//        string arrow = foldout ? "\u25bc" : "\u25b6";
//        Rect arrowRect = new Rect(headerRect.x + 8, headerRect.y, 16, headerRect.height);
//        EditorGUI.LabelField(arrowRect, arrow, _subHeaderStyle);
//        // Title
//        Rect titleRect = new Rect(headerRect.x + 22, headerRect.y, headerRect.width - 22, headerRect.height);
//        EditorGUI.LabelField(titleRect, title, _subHeaderStyle);
//        // Click to toggle
//        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
//        {
//            foldout = !foldout;
//            Event.current.Use();
//        }
//        EditorGUIUtility.AddCursorRect(headerRect, MouseCursor.Link);
//        return foldout;
//    }
//    private void DrawBoxStart()
//    {
//        Rect boxRect = EditorGUILayout.BeginVertical();
//        EditorGUI.DrawRect(
//            new Rect(boxRect.x, boxRect.y, boxRect.width, boxRect.height),
//            new Color(0.17f, 0.17f, 0.2f, 0.5f)
//        );
//        // Left border continuation
//        EditorGUI.DrawRect(
//            new Rect(boxRect.x, boxRect.y, 1, boxRect.height),
//            new Color(0.3f, 0.3f, 0.35f, 0.3f)
//        );
//        EditorGUI.DrawRect(
//            new Rect(boxRect.xMax - 1, boxRect.y, 1, boxRect.height),
//            new Color(0.3f, 0.3f, 0.35f, 0.3f)
//        );
//        GUILayout.Space(4);
//    }
//    private void DrawBoxEnd()
//    {
//        GUILayout.Space(4);
//        EditorGUILayout.EndVertical();
//    }
//    private void DrawCircle(Rect rect, Color color)
//    {
//        // Approximate circle with filled rect + highlight
//        EditorGUI.DrawRect(rect, color);
//        // Inner highlight for 3D effect
//        Rect highlight = new Rect(rect.x + 1, rect.y + 1, rect.width - 3, rect.height - 3);
//        Color lightColor = new Color(
//            Mathf.Min(color.r + 0.2f, 1f),
//            Mathf.Min(color.g + 0.2f, 1f),
//            Mathf.Min(color.b + 0.2f, 1f),
//            0.4f
//        );
//        EditorGUI.DrawRect(highlight, lightColor);
//    }
//}