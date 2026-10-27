namespace Ked.Samples.UI
{
    /// <summary>
    /// 재생 입력과 UI 입력의 소유권(Docs/IMPLEMENTATION.md §7).
    ///
    /// 재생 입력의 유일한 구동자는 VNAdvanceInputPoller다(Space·Ctrl·A·S·R, 그리고 자동·빨리 넘기기의 Tick).
    /// 메뉴·목록·설정·팝업이 열려 있거나 UI를 숨긴 동안에는 UI가 입력을 갖는다 — 폴러를 끈다. 그래서
    /// - 같은 Space·Enter가 메뉴 버튼과 아래 대사를 함께 넘기지 않는다.
    /// - 메뉴가 열린 동안 자동 진행·빨리 넘기기가 대사를 넘기지 않는다(멈춤). 닫으면 이어진다.
    /// 재생 단위·기억은 건드리지 않는다 — 입력만 막는다.
    ///
    /// 되돌려 줄 때는 그 프레임이 끝난 뒤에 켠다(LateApply · LateUpdate). 메뉴를 닫은 바로 그 키 입력을
    /// 같은 프레임에 폴러가 다시 읽지 않게 한다. 가져올 때는 바로 끈다.
    /// UI가 갖기 전의 폴러 상태(켜짐·꺼짐)를 기억했다가 한 번만 되돌린다 — 그 밖의 때에는 폴러를 만지지 않는다.
    /// </summary>
    internal sealed class KedInputOwnership
    {
        private readonly VNAdvanceInputPoller _poller;

        private bool _uiOwns;
        private bool _priorEnabled = true;
        private bool _restorePending;

        public KedInputOwnership(VNAdvanceInputPoller poller)
        {
            _poller = poller;
        }

        public bool UiOwns => _uiOwns;

        /// <summary>UI가 입력을 가져야 하는가. 가져오는 것은 즉시, 돌려주는 것은 LateApply에서.</summary>
        public void Set(bool uiOwns)
        {
            if (uiOwns == _uiOwns)
                return;

            _uiOwns = uiOwns;

            if (_poller == null)
                return;

            if (uiOwns)
            {
                // 돌려주기 전에 다시 가져가면 처음 기억한 상태를 그대로 둔다.
                if (!_restorePending)
                    _priorEnabled = _poller.enabled;

                _restorePending = false;
                _poller.enabled = false;
                return;
            }

            _restorePending = true;
        }

        public void LateApply()
        {
            if (!_restorePending || _uiOwns || _poller == null)
                return;

            _restorePending = false;
            _poller.enabled = _priorEnabled;
        }
    }
}
