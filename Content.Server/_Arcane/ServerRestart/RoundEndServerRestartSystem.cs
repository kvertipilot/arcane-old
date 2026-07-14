using Content.Shared._Arcane.CCVars;
using Content.Shared.GameTicking;
using Robust.Server;
using Robust.Shared.Configuration;

namespace Content.Server._Arcane.ServerRestart;

public sealed class RoundEndServerRestartSystem : EntitySystem
{
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _restartOnRoundEnd;
    private bool _roundStarted;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Subs.CVar(_cfg, ACCVars.RestartServerOnRoundEnd, OnRestartServerOnRoundEnd, true);
    }

    private void OnRestartServerOnRoundEnd(bool value)
    {
        _restartOnRoundEnd = value;
    }

    private void OnRoundStarted(RoundStartedEvent args)
    {
        _roundStarted = true;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        if (!_restartOnRoundEnd)
            return;

        if (!_roundStarted)
            return;
        _roundStarted = false;

        _baseServer.Shutdown("Раунд завершился, сервер перезапускается.");
    }
}
