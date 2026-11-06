using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 백로그 줄의 화자 이름. 본체의 백로그 기록(DialogueLogEntry)에는 화자가 없다(본문만 — 이름을 뺀 글자).
    /// 저장 포맷을 바꾸지 않으려고 기록에 칸을 더하지 않고, 줄 ID로 Yarn 문자열 표에서 원문("하린: …")을 찾아 앞의 이름을 읽는다.
    /// 불러온 회차의 옛 줄도 같은 길로 이름이 나온다. 찾지 못하면 빈 이름(지문·내레이션과 같게 보인다).
    /// </summary>
    internal sealed class YarnSpeakerNames
    {
        private const int MaxNameLength = 24;

        private readonly DialogueRunner _runner;
        private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);
        private bool _warned;

        public YarnSpeakerNames(DialogueRunner runner)
        {
            _runner = runner;
        }

        public string Find(string lineId)
        {
            if (string.IsNullOrEmpty(lineId) || _runner == null)
                return string.Empty;

            if (_cache.TryGetValue(lineId, out string cached))
                return cached;

            string name = ReadName(ReadSource(lineId));
            _cache[lineId] = name;
            return name;
        }

        private string ReadSource(string lineId)
        {
            try
            {
                YarnProject project = _runner.YarnProject;
                if (project == null)
                    return null;

                Localization localization = null;
                string locale = _runner.LineProvider != null ? _runner.LineProvider.LocaleCode : null;

                if (!string.IsNullOrEmpty(locale))
                    localization = project.GetLocalization(locale);

                if (localization == null)
                    localization = project.baseLocalization;

                return localization != null ? localization.GetLocalizedString(lineId) : null;
            }
            catch (Exception error)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[Ked UI] 백로그 화자 이름을 대본 표에서 읽지 못했다. 이름 없이 보인다. — {error.Message}");
                }

                return null;
            }
        }

        // Yarn의 "이름: 대사" 규칙. 첫 쌍점 앞이 이름이다. 마크업·식이 든 앞부분은 이름으로 보지 않는다.
        private static string ReadName(string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            int colon = source.IndexOf(':');
            if (colon <= 0)
                return string.Empty;

            string name = source.Substring(0, colon).Trim();

            if (name.Length == 0 || name.Length > MaxNameLength)
                return string.Empty;

            if (name.IndexOfAny(new[] { '[', ']', '{', '}', '\\', '<', '>' }) >= 0)
                return string.Empty;

            return name;
        }
    }
}
