using System;
using Ked.Progression;
using Yarn.Unity;

// Chapter 진입에서 Yarn 변수 저장소를 준비하는 Host adapter.
//
// 진행 런타임은 YarnProject도 YarnVariableSnapshot도 모른다.
// 실행을 시작하기 전에 Launcher가 그 payload를 여기 staging해 두고,
// Driver가 Chapter를 열 때 BeginChapter()가 실제 복원을 수행한다.
public sealed class ProgressionChapterLifecycle : IChapterLifecycle
{
    private readonly ProgressionYarnBridge _yarnBridge;

    private YarnProject _project;
    private YarnVariableSnapshot _restoreVariables;

    public ProgressionChapterLifecycle(ProgressionYarnBridge yarnBridge)
    {
        _yarnBridge = yarnBridge;
    }

    // 실행 시작 전에 Host가 준비한다. restoreVariables가 null이면 새 게임이다.
    public void Stage(YarnProject project, YarnVariableSnapshot restoreVariables)
    {
        _project = project;
        _restoreVariables = restoreVariables;
    }

    public void BeginChapter(ChapterDefinition chapter)
    {
        if (_project == null)
        {
            throw new InvalidOperationException(
                "[진행] Chapter를 열기 전에 YarnProject가 staging되지 않았다.");
        }

        // 선언 초기값으로 되돌린 다음 저장된 덤프를 덮는다. 순서를 바꾸면 복원이 지워진다.
        _yarnBridge.BeginChapter(_project);

        if (_restoreVariables == null)
            return;

        _yarnBridge.Restore(_restoreVariables);

        // 복원은 실행당 한 번이다. 같은 실행의 다음 Chapter는 선언 초기값에서 시작한다.
        _restoreVariables = null;
    }
}
