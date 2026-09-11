using UnityEngine;

/// <summary>
/// Локальный режим на двоих: одна физическая арена, один таймер и один поток
/// событий, но у каждого игрока своя змея и своя половина экрана.
/// </summary>
public class LocalMultiplayerBootstrap : RuntimeGameBootstrap
{
    protected override bool IsMultiplayer => true;
    protected override int PlayerCount => 2;
}
