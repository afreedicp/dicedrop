using System.Windows;
using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.Views.Board;

internal readonly record struct TokenKey(int Seat, int Token);

/// <summary>Where to draw a pawn this frame.</summary>
/// <param name="Point">Centre of the square the pawn is over.</param>
/// <param name="Lift">Height above the board, in cells.</param>
/// <param name="Squash">0 at rest, 1 fully squashed.</param>
internal readonly record struct TokenPose(Point Point, double Lift, double Squash, bool IsMoving);

/// <summary>
/// Turns board snapshots into motion. When a token's progress grows it hops square by square, as when
/// counting out a move by hand; when it drops back to the yard (a capture) it slides home along its path,
/// after the capturing pawn has landed. Motions play one after another, in the order they happened.
/// </summary>
internal sealed class TokenAnimator
{
    private const double HopSeconds = 0.17;
    private const double EntrySeconds = 0.32;
    private const double SettleSeconds = 0.12;
    private const double SlideStepSeconds = 0.035;
    private const double HopHeight = 0.55;
    private const double EntryHeight = 1.1;

    private readonly Dictionary<TokenKey, int> _known = [];
    private readonly Queue<Motion> _queue = new();
    private Motion? _current;
    private double _currentStart;

    public bool IsBusy => _current is not null || _queue.Count > 0;

    /// <summary>Takes the snapshot's positions as they are, with nothing to animate.</summary>
    public void Reset(GameStateView? game)
    {
        _known.Clear();
        _queue.Clear();
        _current = null;
        if (game is null)
        {
            return;
        }

        foreach (var seat in game.Seats)
        {
            foreach (var token in seat.Tokens)
            {
                _known[new TokenKey(seat.Seat, token.Index)] = token.Progress;
            }
        }
    }

    public void Update(GameStateView game, double now)
    {
        var forward = new List<Motion>();
        var back = new List<Motion>();
        foreach (var seat in game.Seats)
        {
            foreach (var token in seat.Tokens)
            {
                var key = new TokenKey(seat.Seat, token.Index);
                if (_known.TryGetValue(key, out var old) && old != token.Progress && seat.Standing != SeatStanding.Left)
                {
                    (token.Progress > old ? forward : back).Add(new Motion(key, old, token.Progress));
                }

                _known[key] = token.Progress;
            }
        }

        // The capturing pawn lands first, then its victims slide home.
        foreach (var motion in forward.Concat(back))
        {
            _queue.Enqueue(motion);
        }

        Tick(now);
    }

    public void Tick(double now)
    {
        while (true)
        {
            if (_current is { } running)
            {
                var end = _currentStart + Duration(running);
                if (now < end)
                {
                    return;
                }

                _current = null;
                if (_queue.Count == 0)
                {
                    return;
                }

                // Chain on from where the last one ended so a slow frame doesn't stretch the sequence.
                _currentStart = end;
            }
            else if (_queue.Count > 0)
            {
                _currentStart = now;
            }
            else
            {
                return;
            }

            _current = _queue.Dequeue();

            // Play faster when moves pile up, so the board never falls far behind the game.
            _current.Speed = _queue.Count > 1 ? 2.5 : 1;
        }
    }

    /// <param name="path">Square centre for a progress value along this token's path.</param>
    public TokenPose Pose(TokenKey key, Func<int, Point> path, double now)
    {
        if (_current is { } motion && motion.Key == key)
        {
            return motion.IsCapture ? Slide(motion, path, now - _currentStart) : Hop(motion, path, now - _currentStart);
        }

        // Waiting for its turn to move: stay where the queued motion starts.
        foreach (var queued in _queue)
        {
            if (queued.Key == key)
            {
                return new TokenPose(path(queued.From), 0, 0, false);
            }
        }

        return new TokenPose(path(_known.GetValueOrDefault(key, -1)), 0, 0, false);
    }

    private static double Duration(Motion motion) => motion.IsCapture
        ? Math.Clamp((motion.From + 2) * SlideStepSeconds, 0.35, 0.9) / motion.Speed
        : ((motion.From < 0 ? EntrySeconds : HopSeconds * (motion.To - motion.From)) + SettleSeconds) / motion.Speed;

    private static TokenPose Hop(Motion motion, Func<int, Point> path, double elapsed)
    {
        elapsed *= motion.Speed;
        var entering = motion.From < 0;
        var hop = entering ? EntrySeconds : HopSeconds;
        var hops = entering ? 1 : motion.To - motion.From;
        if (elapsed >= hops * hop)
        {
            var settle = Math.Clamp((elapsed - (hops * hop)) / SettleSeconds, 0, 1);
            return new TokenPose(path(motion.To), 0, 1 - settle, true);
        }

        var index = (int)(elapsed / hop);
        var t = (elapsed / hop) - index;
        var from = entering ? -1 : motion.From + index;
        var to = entering ? motion.To : from + 1;
        var height = entering ? EntryHeight : HopHeight;

        // Each landing squashes the pawn briefly before it pushes off again.
        var squash = index > 0 && t < 0.22 ? 1 - (t / 0.22) : 0;
        return new TokenPose(Lerp(path(from), path(to), Ease(t)), Math.Sin(Math.PI * t) * height, squash, true);
    }

    private static TokenPose Slide(Motion motion, Func<int, Point> path, double elapsed)
    {
        // Back along the track to the start square, then one hop into the yard.
        var segments = motion.From + 1;
        var progress = Math.Clamp(elapsed / Duration(motion), 0, 1) * segments;
        var index = Math.Min((int)progress, segments - 1);
        var t = progress - index;
        var from = motion.From - index;
        var lift = from == 0 ? Math.Sin(Math.PI * t) * HopHeight : 0.12;
        return new TokenPose(Lerp(path(from), path(from - 1), t), lift, 0, true);
    }

    private static double Ease(double t) => t * t * (3 - (2 * t));

    private static Point Lerp(Point a, Point b, double t) => a + ((b - a) * t);

    private sealed record Motion(TokenKey Key, int From, int To)
    {
        public bool IsCapture => To < From;

        public double Speed { get; set; } = 1;
    }
}
