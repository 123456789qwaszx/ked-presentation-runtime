using System;
using System.Collections.Generic;

namespace Ked.Samples.UI
{
    // 화면 바인딩의 바탕 — 재생·저장 쪽 상태를 읽어 View 모델로 바꾸고, View 이벤트를 동작으로 잇는다.
    // View 이벤트는 열 때 묶고(BindView · AddBinding) 닫힐 때 푼다(Unbind). 닫힘은 흐름이 UIPF 닫힘 콜백으로 알린다.
    internal abstract class ScreenBinding : IDisposable
    {
        private readonly List<UIBase> _views = new();
        private readonly Dictionary<UIBase, List<Action>> _cleanupByView = new();

        protected ScreenBinding(KedUIFlow flow, KedUIServices services)
        {
            Flow = flow;
            Services = services;
        }

        protected KedUIFlow Flow { get; }
        protected KedUIServices Services { get; }

        // 이 바인딩이 맡는 화면. 흐름이 닫힘과 Esc를 이리로 보낸다.
        public IReadOnlyList<UIBase> Views => _views;

        // 재생·호스트가 이어진 뒤 한 번.
        public virtual void OnConfigured()
        {
        }

        public virtual void Tick()
        {
        }

        // Esc — 맨 위 Panel이 이 바인딩의 화면일 때. 기본은 한 층 닫기.
        public virtual void Back(UIBase view) => Flow.ClosePanel();

        public virtual void Dispose()
        {
            foreach (List<Action> cleanups in _cleanupByView.Values)
                RunCleanups(cleanups);

            _cleanupByView.Clear();
        }

        public void Unbind(UIBase view)
        {
            if (!_cleanupByView.TryGetValue(view, out List<Action> cleanups))
                return;

            _cleanupByView.Remove(view);
            RunCleanups(cleanups);
        }

        protected T Own<T>(T view) where T : UIBase
        {
            _views.Add(view);
            return view;
        }

        // 다시 열려도(같은 Root로 전환 · 쌓인 Panel로 돌아가기) 한 벌만 묶이게 먼저 푼다.
        protected void BindView<T>(T view, Action<T> apply) where T : UIBase
        {
            Unbind(view);
            apply(view);
        }

        protected void AddBinding<T>(T view, Action<T> attach, Action<T> detach) where T : UIBase
        {
            attach(view);

            if (!_cleanupByView.TryGetValue(view, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByView[view] = cleanups;
            }

            cleanups.Add(() => detach(view));
        }

        protected string Text(string key) => Services.Text(key);
        protected string Format(string key, params object[] args) => Services.Format(key, args);

        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }
    }
}
