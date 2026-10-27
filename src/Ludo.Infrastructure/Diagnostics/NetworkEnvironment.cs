using System.Collections;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Ludo.Application.Abstractions;

namespace Ludo.Infrastructure.Diagnostics;

/// <summary>
/// Writes what about this PC decides whether others can reach it: its network adapters and the Windows
/// Firewall settings and rules for this exe. Read-only; it never changes anything.
/// </summary>
internal static class NetworkEnvironment
{
    private const string Source = "env";

    // NET_FW_* constants from the Windows Firewall COM API.
    private const int DirectionIn = 1;
    private const int ActionBlock = 0;
    private const int ProtocolTcp = 6;
    private const int ProtocolUdp = 17;
    private const int ProtocolAny = 256;

    private static readonly (int Bit, string Name)[] Profiles = [(1, "Domain"), (2, "Private"), (4, "Public")];

    public static void Describe(IDiagnosticLog log)
    {
        try
        {
            DescribeAdapters(log);
        }
        catch (Exception ex)
        {
            log.Write(Source, $"Couldn't list network adapters: {ex.Message}");
        }

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            DescribeFirewall(log);
        }
        catch (Exception ex)
        {
            log.Write(Source, $"Couldn't read Windows Firewall settings: {ex.Message}");
        }
    }

    private static void DescribeAdapters(IDiagnosticLog log)
    {
        var any = false;
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var properties = adapter.GetIPProperties();
            var addresses = properties.UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => $"{a.Address}/{a.PrefixLength}")
                .ToArray();
            if (addresses.Length == 0)
            {
                continue;
            }

            any = true;
            var gateway = properties.GatewayAddresses.FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
            var note = addresses.Any(a => a.StartsWith("169.254.", StringComparison.Ordinal))
                ? " — self-assigned address, this adapter has no working network"
                : gateway is null ? " — no gateway (virtual or isolated adapter?)" : string.Empty;
            log.Write(Source, $"Adapter \"{adapter.Name}\" ({adapter.NetworkInterfaceType}): {string.Join(", ", addresses)}, gateway {gateway?.ToString() ?? "none"}{note}.");
        }

        if (!any)
        {
            log.Write(Source, "No connected network adapter with an IPv4 address.");
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void DescribeFirewall(IDiagnosticLog log)
    {
        var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
        if (type is null || Activator.CreateInstance(type) is not { } policy)
        {
            log.Write(Source, "Windows Firewall API not available.");
            return;
        }

        var active = Get<int>(policy, "CurrentProfileTypes");
        var activeProfiles = Profiles.Where(p => (active & p.Bit) != 0).ToArray();
        var protectedProfiles = new List<string>();
        foreach (var (bit, name) in activeProfiles)
        {
            var enabled = Get<bool>(policy, "FirewallEnabled", bit);
            if (!enabled)
            {
                log.Write(Source, $"Network profile in use: {name}. Windows Firewall is OFF for it.");
                continue;
            }

            protectedProfiles.Add(name);
            var blockAll = Get<bool>(policy, "BlockAllInboundTraffic", bit);
            var inbound = Get<int>(policy, "DefaultInboundAction", bit) == ActionBlock ? "block" : "allow";
            log.Write(Source, $"Network profile in use: {name}. Firewall on, unlisted incoming traffic: {inbound}.");
            if (blockAll)
            {
                log.Write(Source, $"PROBLEM: \"Block all incoming connections\" is on for {name} networks, so no other PC can reach DiceDrop. Turn it off in Windows Security > Firewall.");
            }
        }

        if (protectedProfiles.Count == 0)
        {
            return;
        }

        var exe = Environment.ProcessPath ?? string.Empty;
        var exeName = Path.GetFileName(exe);
        var activeMask = activeProfiles.Aggregate(0, (mask, p) => mask | p.Bit);
        var blocked = false;
        var allowsTcp = false;
        var allowsUdp = false;
        var rulesSeen = 0;
        foreach (var rule in (IEnumerable)Get<object>(policy, "Rules"))
        {
            var app = Get<string?>(rule, "ApplicationName");
            if (string.IsNullOrEmpty(app) || !string.Equals(Path.GetFileName(app), exeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = Get<string?>(rule, "Name");
            if (!string.Equals(app, exe, StringComparison.OrdinalIgnoreCase))
            {
                log.Write(Source, $"Firewall rule \"{name}\" is for another copy at {app}; it does not cover this one at {exe}.");
                continue;
            }

            rulesSeen++;
            var enabled = Get<bool>(rule, "Enabled");
            var inbound = Get<int>(rule, "Direction") == DirectionIn;
            var block = Get<int>(rule, "Action") == ActionBlock;
            var profiles = Get<int>(rule, "Profiles");
            var protocol = Get<int>(rule, "Protocol");
            var protocolName = protocol switch { ProtocolTcp => "TCP", ProtocolUdp => "UDP", ProtocolAny => "any protocol", _ => $"protocol {protocol}" };
            var profileNames = string.Join("/", Profiles.Where(p => (profiles & p.Bit) != 0).Select(p => p.Name));
            log.Write(Source, $"Firewall rule \"{name}\": {(enabled ? "enabled" : "disabled")}, {(inbound ? "inbound" : "outbound")}, {(block ? "BLOCK" : "allow")}, {protocolName}, profiles {profileNames}.");

            if (!enabled || !inbound || (profiles & activeMask) == 0)
            {
                continue;
            }

            blocked |= block;
            allowsTcp |= !block && protocol is ProtocolTcp or ProtocolAny;
            allowsUdp |= !block && protocol is ProtocolUdp or ProtocolAny;
        }

        var where = string.Join("/", protectedProfiles);
        if (blocked)
        {
            log.Write(Source,
                $"PROBLEM: an inbound BLOCK rule for this exe applies on {where} networks, so other PCs can't reach it. " +
                "Windows adds these when its firewall prompt is cancelled. Fix: Windows Security > Firewall > Allow an app through firewall, " +
                "tick DiceDrop for Private and Public (or delete the block rule).");
        }
        else if (!allowsTcp || !allowsUdp)
        {
            var missing = !allowsTcp && !allowsUdp ? "TCP and UDP" : !allowsTcp ? "TCP" : "UDP";
            log.Write(Source,
                $"PROBLEM (likely): {(rulesSeen == 0 ? "there is no firewall rule" : "no enabled firewall rule is")} letting {missing} into this exe on {where} networks, " +
                "so other PCs probably can't reach it. Fix: Windows Security > Firewall > Allow an app through firewall > Allow another app, " +
                $"pick {exe} and tick Private and Public. A rule keyed to ports or another program may still allow it.");
        }
        else
        {
            log.Write(Source, $"Firewall allows incoming TCP and UDP for this exe on {where} networks.");
        }
    }

    /// <summary>Reads a COM property late-bound, so no interop assembly is needed.</summary>
    private static T Get<T>(object target, string property, params object[] args) =>
        (T)target.GetType().InvokeMember(property, BindingFlags.GetProperty, null, target, args)!;
}
