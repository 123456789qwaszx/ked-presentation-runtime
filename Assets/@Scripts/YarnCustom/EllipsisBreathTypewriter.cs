using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;
using Random = UnityEngine.Random;

// Replacement markup (palette/styles) is processed by the LineProvider.
// This handler only processes action markup (pause/signal/move, etc.) via ActionMarkupHandler,
// and the typewriter only controls TMP's maxVisibleCharacters.
public sealed class EllipsisBreathTypewriter : MonoBehaviour, IAsyncTypewriter
{
    // ===== Yarn Commands / Public API =====
    [YarnCommand("SetSpeedPerSec")]
    public void SetTypeSpeed(float speed) => unitsPerSecond = Mathf.Max(0f, speed);
    
    [YarnCommand("SetSpeedMul")]
    public void SetSpeedMultiplierCommand(float multiplier) => SetSpeedMultiplier(multiplier);

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Clamp(multiplier, 0.05f, 20f);
    }
    
    public void SetTextView(TMP_Text textView)
    {
        if (_typewriterText == textView)
            return;

        if (_isRunning)
            AbortRun();

        ClearCurrentTextView();

        _typewriterText = textView;

        ClearCurrentTextView();
    }

    [YarnCommand("ClearTextView")]
    public void ClearTextView()
    {
        if (_isRunning)
            AbortRun();

        ClearCurrentTextView();
    }

    private void ClearCurrentTextView()
    {
        if (_typewriterText == null)
            return;
        //Debug.Log($"[Typewriter.ClearCurrentTextView] frame={Time.frameCount}, target={_typewriterText?.name}, textBefore='{_typewriterText?.text}'");

        _typewriterText.maxVisibleCharacters = 0;
        _typewriterText.SetText(string.Empty);
        _typewriterText.ForceMeshUpdate();
    }

    // ---- Inspector / Tuning ----
    [Header("Speed")]
    [Min(0f)] public float unitsPerSecond = 30f;
    
    [Header("Speedup")]
    [Min(0.05f)] [SerializeField] private float _speedMultiplier = 1f;

    [Header("Breath Multipliers")] 
    [Min(0f)] public float multWhitespace      = 0.35f;
    [Min(0f)] public float multComma           = 1.7f;
    [Min(0f)] public float multColonSemicolon  = 2.0f;
    [Min(0f)] public float multPeriod          = 3.2f;
    [Min(0f)] public float multQuestionExclaim = 3.6f;
    [Min(0f)] public float multEllipsisChar    = 5.2f;
    [Min(0f)] public float multTripleDotToken  = 0.8f; // "..." token as a whole

    [Tooltip("Small random jitter factor (0~0.2). 0.08 is usually enough.")]
    [Range(0f, 0.2f)] public float jitterRatio = 0.08f;

    [Tooltip("Slightly slower ramp at the beginning of the line (0 disables).")]
    [Range(0, 40)] public int startRampUnits = 10;

    [Tooltip("Extra delay multiplier at the beginning (1 = no extra delay). Higher = slower start.")]
    [Range(1f, 2f)] public float startSlowdownMultiplier = 1.25f;
    
    // ---- Runtime Binding ----
    private TMP_Text _typewriterText;
    
    // ---- Runtime State ----
    private bool _hasActiveLine = false;
    private bool _isRunning = false;
    private int _totalCharacterCount = 0;
    private int _runId = 0;
    
    public List<IActionMarkupHandler> ActionMarkupHandlers { get; } = new();
    
    public bool IsComplete
    {
        get
        {
            if (_typewriterText == null) return true;
            if (_isRunning) return false;
            if (!_hasActiveLine) return true;

            return _typewriterText.maxVisibleCharacters >= _totalCharacterCount;
        }
    }

    public void PrepareForContent(MarkupParseResult line)
    {
        if (_typewriterText == null)
        {
            Debug.LogError(
                "[Typewriter] PrepareForContent: no text view is available. Run the Prepare command first, then call Yarn StartDialogue.");
            return;
        }

        _hasActiveLine = true;
        _isRunning = false;

        _typewriterText.maxVisibleCharacters = 0;
        _typewriterText.SetText(line.Text);
        _typewriterText.ForceMeshUpdate(true, true);

        _totalCharacterCount = _typewriterText.textInfo.characterCount;
        
        //Debug.Log($"textInfo.characterCount={_typewriterText.textInfo.characterCount}, text='{line.Text}'");

        InvokeHandlers(h => h.OnPrepareForLine(line, _typewriterText));
    }

    public async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken cancellationToken)
    {
        if (_typewriterText == null) return;

        int myRunId = ++_runId;
        _isRunning = true;

        try
        {
            float charsPerSecond = Mathf.Max(0f, unitsPerSecond) * Mathf.Max(0.01f, _speedMultiplier);
            double secondPerChar = (charsPerSecond > 0f) ? (1.0 / charsPerSecond) : 0.0;

            InvokeHandlers(h => h.OnLineDisplayBegin(line, _typewriterText));

            TMP_TextInfo textInfo = _typewriterText.textInfo;
            int totalLineCharacterCount = _totalCharacterCount;

            List<int> revealCounts = BuildRevealCountsFromTMP(textInfo, totalLineCharacterCount);

            int prevVisibleCount = 0;

            for (int unit = 0; unit < revealCounts.Count; unit++)
            {
                if (myRunId != _runId)
                    return;

                if (cancellationToken.IsCancellationRequested)
                {
                    RevealAllAndComplete(line, totalLineCharacterCount);
                    
                    //Debug.Log("IsCancellationRequested1");
                    return;
                }

                int targetVisibleCount = Mathf.Clamp(revealCounts[unit], 0, totalLineCharacterCount);

                for (int visibleCount = prevVisibleCount; visibleCount < targetVisibleCount; visibleCount++)
                {
                    if (myRunId != _runId)
                        return;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        RevealAllAndComplete(line, totalLineCharacterCount);
                        //Debug.Log("IsCancellationRequested2");
                        return;
                    }

                    if ((uint)visibleCount >= (uint)totalLineCharacterCount)
                        continue;

                    int plainIndex = visibleCount;

                    for (int i = 0; i < ActionMarkupHandlers.Count; i++)
                    {
                        try
                        {
                            await ActionMarkupHandlers[i].OnCharacterWillAppear(plainIndex, line, cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            RevealAllAndComplete(line, totalLineCharacterCount);
                            return;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
#if UNITY_EDITOR
                            throw;
#endif
                        }

                        if (myRunId != _runId || _typewriterText == null)
                            return;
                    }

                    if (myRunId != _runId || _typewriterText == null)
                        return;

                    _typewriterText.maxVisibleCharacters = visibleCount + 1;
                }

                prevVisibleCount = targetVisibleCount;

                if (secondPerChar > 0)
                {
                    double delay = secondPerChar;

                    if (targetVisibleCount > 0)
                    {
                        // How many chars were visible at the end of the previous unit
                        int prevUnitTargetCount = (unit > 0)
                            ? Mathf.Clamp(revealCounts[unit - 1], 0, totalLineCharacterCount)
                            : 0;

                        // Number of chars revealed in this unit (e.g. 3 for "...")
                        int tokenLength = Mathf.Clamp(targetVisibleCount - prevUnitTargetCount, 1, 8);

                        // Last character revealed in this unit
                        char lastChar = '\0';
                        int lastIndex = targetVisibleCount - 1;
                        if ((uint)lastIndex < (uint)textInfo.characterCount)
                            lastChar = textInfo.characterInfo[lastIndex].character;

                        double multiplier = GetDelayMultiplier(lastChar);

                        if (tokenLength == 3 && lastChar == '.')
                            multiplier = Mathf.Max((float)multiplier, multTripleDotToken);

                        if (startRampUnits > 0 && startSlowdownMultiplier > 1f && unit < startRampUnits)
                        {
                            float rampProgress = (startRampUnits <= 1) 
                                ? 1f 
                                : (unit / (float)(startRampUnits - 1));
                            float ramp = Mathf.Lerp(startSlowdownMultiplier, 1f, rampProgress);
                            multiplier *= ramp;
                        }
                        
                        if (_speedMultiplier > 1.01f)
                        {
                            multiplier = 1.0 + (multiplier - 1.0) * 0.2;
                        }
                        
                        delay *= multiplier;

                        if (jitterRatio > 0f)
                        {
                            float jitter = Random.Range(-jitterRatio, jitterRatio);
                            delay *= (1.0 + jitter);
                        }

                        double cap = (_speedMultiplier > 1.01f) 
                            ? 0.08 
                            : 0.35;
                        delay = Math.Max(0.0, Math.Min(delay, cap));
                    }
                    
                    double waited = 0;
                    while (myRunId == _runId && !cancellationToken.IsCancellationRequested && waited < delay)
                    {
                        double t0 = Time.timeAsDouble;
                        await YarnTask.Yield();
                        double t1 = Time.timeAsDouble;
                        waited += (t1 - t0); // Accumulate actual elapsed time
                    }
                }
            }

            RevealAllAndComplete(line, totalLineCharacterCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //Debug.Log("IsCancellationRequested4");
            RevealAllAndComplete(line, _totalCharacterCount);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            if (_typewriterText != null)
            {
                _typewriterText.maxVisibleCharacters = int.MaxValue;

                InvokeHandlers(h => h.OnLineDisplayComplete());
            }
        }
        finally
        {
            if (myRunId == _runId)
                _isRunning = false;
        }
    }
    
    public void ContentWillDismiss()
    {
        _hasActiveLine = false;
        _isRunning = false;

        InvokeHandlers(h => h.OnLineWillDismiss());
    }


    // ===== Breath Timing Rules =====
    private double GetDelayMultiplier(char c)
    {
        // Whitespace: avoid stopping; let it flow
        if (c == ' ' || c == '\t' || c == '\u00A0') return multWhitespace;

        // Comma-ish
        if (c == ',' || c == '，') return multComma;

        // Colon / semicolon
        if (c == ':' || c == ';') return multColonSemicolon;

        // Strong stops
        if (c == '.' || c == '。') return multPeriod;
        if (c == '?' || c == '？' || c == '!' || c == '！') return multQuestionExclaim;

        // Ellipsis char
        if (c == '…') return multEllipsisChar;

        // Default
        return 1.0;
    }

    // Builds a list of maxVisibleCharacters targets by grouping "..." as a single reveal unit.
    // Each value represents the visible character count at the end of a unit.
    private static List<int> BuildRevealCountsFromTMP(TMP_TextInfo info, int visibleCharacterCount)
    {
        var result = new List<int>(Mathf.Max(8, visibleCharacterCount));
        int lastAdded = 0;

        void AddCount(int count)
        {
            count = Mathf.Clamp(count, 0, visibleCharacterCount);
            if (count <= lastAdded)
                return;
            
            result.Add(count);
            lastAdded = count;
        }

        int i = 0;
        while (i < visibleCharacterCount)
        {
            if (lastAdded >= visibleCharacterCount)
                break;

            int tokenLen = 1;

            char c0 = info.characterInfo[i].character;

            // "..."
            if (c0 == '.' && i + 2 < visibleCharacterCount)
            {
                char c1 = info.characterInfo[i + 1].character;
                char c2 = info.characterInfo[i + 2].character;
                if (c1 == '.' && c2 == '.')
                    tokenLen = 3;
            }

            AddCount(i + tokenLen);
            i += tokenLen;
        }

        return result;
    }

    private void RevealAllAndComplete(MarkupParseResult line, int count)
    {
        if (_typewriterText == null) return;

        _typewriterText.maxVisibleCharacters = count;
        _totalCharacterCount = Mathf.Max(_totalCharacterCount, count);

        InvokeHandlers(h => h.OnLineDisplayComplete());

        _isRunning = false;
    }

    private void InvokeHandlers(Action<IActionMarkupHandler> action)
    {
        for (int i = 0; i < ActionMarkupHandlers.Count; i++)
        {
            try
            {
                action(ActionMarkupHandlers[i]);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private void AbortRun()
    {
        _runId++;
        _hasActiveLine = false;
        _isRunning = false;
        _totalCharacterCount = 0;
    }


    #region ResetSettings

    // ===== Default tuning values =====
    private const float DefaultUnitsPerSecond = 30f;
    
    private const float DefaultMultWhitespace = 0.35f;
    private const float DefaultMultComma = 1.7f;
    private const float DefaultMultColonSemicolon = 2.0f;
    private const float DefaultMultPeriod = 3.2f;
    private const float DefaultMultQuestionExclaim = 3.6f;
    private const float DefaultMultEllipsisChar = 5.2f;
    private const float DefaultMultTripleDotToken = 0.8f;
    
    private const float DefaultJitterRatio = 0.08f;
    private const int   DefaultStartRampUnits = 10;
    private const float DefaultStartSlowdownMultiplier = 1.25f;

    [ContextMenu("Typewriter/Reset Rhythm Settings To Defaults")]
    public void ResetRhythmSettingsToDefaults()
    {
        unitsPerSecond          = DefaultUnitsPerSecond;

        multWhitespace          = DefaultMultWhitespace;
        multComma               = DefaultMultComma;
        multColonSemicolon      = DefaultMultColonSemicolon;
        multPeriod              = DefaultMultPeriod;
        multQuestionExclaim     = DefaultMultQuestionExclaim;
        multEllipsisChar        = DefaultMultEllipsisChar;
        multTripleDotToken      = DefaultMultTripleDotToken;

        jitterRatio             = DefaultJitterRatio;
        startRampUnits          = DefaultStartRampUnits;
        startSlowdownMultiplier = DefaultStartSlowdownMultiplier;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    #endregion
}