using Panosse.Core.ViewModels;
using Panosse.Services;
using Xunit;

namespace Panosse.Core.Tests;

public class PnpUtilXmlParserTests
{
    private const string DriversXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <PnpUtil Version="10.0.26300" Command="/enum-drivers /format xml /devices">
            <Driver DriverName="oem67.inf">
                <OriginalName>amdfendr.inf</OriginalName>
                <ProviderName>Advanced Micro Devices, Inc.</ProviderName>
                <ClassName>System</ClassName>
                <DriverVersion>02/24/2026 26.10.0.2</DriverVersion>
                <Devices>
                    <Device InstanceId="ROOT\AMDLOG\0000">
                        <DeviceDescription>AMD Crash Defender</DeviceDescription>
                        <Status>Started</Status>
                    </Device>
                </Devices>
            </Driver>
            <Driver DriverName="oem2.inf">
                <OriginalName>amdfendr.inf</OriginalName>
                <ProviderName>Advanced Micro Devices, Inc.</ProviderName>
                <ClassName>System</ClassName>
                <DriverVersion>02/06/2026 25.30.0.5</DriverVersion>
            </Driver>
        </PnpUtil>
        """;

    private const string DevicesXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <PnpUtil Version="10.0.26300" Command="/enum-devices /drivers /format xml">
            <Device InstanceId="USB\VID_0416&amp;PID_7395&amp;MI_00\8&amp;1e605747&amp;2d&amp;0000">
                <DeviceDescription>LianLi-GA_II-LCD_v1.6</DeviceDescription>
                <ClassName>USBDevice</ClassName>
                <Status>Disconnected</Status>
                <DriverName>winusb.inf</DriverName>
                <MatchingDrivers>
                    <DriverName DriverName="winusb.inf">
                        <Status>BestRanked/Installed</Status>
                    </DriverName>
                    <DriverName DriverName="oem99.inf">
                        <Status>Outranked</Status>
                    </DriverName>
                </MatchingDrivers>
            </Device>
        </PnpUtil>
        """;

    [Fact]
    public void ParseDrivers_ReadsPackagesVersionsAndDevices()
    {
        IReadOnlyList<DriverPackageInfo> packages = PnpUtilXmlParser.ParseDrivers(DriversXml);

        Assert.Equal(2, packages.Count);
        DriverPackageInfo current = packages[0];
        Assert.Equal("oem67.inf", current.PublishedName);
        Assert.Equal("amdfendr.inf", current.OriginalName);
        Assert.Equal(new Version(26, 10, 0, 2), current.Version);
        Assert.Equal(new DateTime(2026, 2, 24), current.Date);
        Assert.Equal(["ROOT\\AMDLOG\\0000"], current.DeviceInstanceIds);
        Assert.Empty(packages[1].DeviceInstanceIds);
    }

    [Fact]
    public void ParseDevices_ReadsStatusAndOnlyInstalledMatchingDrivers()
    {
        PnpDeviceInfo device = Assert.Single(PnpUtilXmlParser.ParseDevices(DevicesXml));

        Assert.Equal("USB\\VID_0416&PID_7395&MI_00\\8&1e605747&2d&0000", device.InstanceId);
        Assert.True(device.IsDisconnected);
        Assert.Equal("winusb.inf", device.DriverName);
        Assert.Equal(["winusb.inf"], device.InstalledMatchingDrivers);
    }

    [Theory]
    [InlineData("08/21/2025 25.30.0.1", 2025, 8, 21, "25.30.0.1")]
    [InlineData("07/18/1968 10.1.46.3", 1968, 7, 18, "10.1.46.3")]
    public void ParseDriverVersion_UsesInvariantMonthDayYear(string text, int year, int month, int day, string version)
    {
        (DateTime? date, Version? parsed) = PnpUtilXmlParser.ParseDriverVersion(text);
        Assert.Equal(new DateTime(year, month, day), date);
        Assert.Equal(Version.Parse(version), parsed);
    }
}

public class DriverAnalyzerTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0);

    private static DriverPackageInfo Package(string published, string original, string version, string className = "System", params string[] devices) =>
        new()
        {
            PublishedName = published,
            OriginalName = original,
            Provider = "Fournisseur",
            ClassName = className,
            Version = Version.Parse(version),
            VersionText = version,
            DeviceInstanceIds = devices
        };

    private static IReadOnlyList<DriverIssue> Analyze(
        IEnumerable<DriverPackageInfo> packages,
        IEnumerable<PnpDeviceInfo>? devices = null,
        Dictionary<string, DateTime>? lastSeen = null,
        int minimumAgeDays = 90) =>
        DriverAnalyzer.Analyze(
            new DriverInventory
            {
                Packages = packages.ToList(),
                Devices = devices?.ToList() ?? [],
                LastSeenByInstanceId = lastSeen ?? new Dictionary<string, DateTime>()
            },
            minimumAgeDays,
            Now);

    [Fact]
    public void OlderUnusedVersion_IsObsoleteAndLowRisk()
    {
        IReadOnlyList<DriverIssue> issues = Analyze(
        [
            Package("oem67.inf", "amdfendr.inf", "26.10.0.2", "System", "ROOT\\AMDLOG\\0000"),
            Package("oem2.inf", "amdfendr.inf", "25.30.0.5")
        ]);

        DriverIssue issue = Assert.Single(issues);
        Assert.Equal(DriverIssueKind.ObsoletePackage, issue.Kind);
        Assert.Equal("oem2.inf", issue.Target);
        Assert.Equal("Low", issue.RiskLevel);
        Assert.Contains("26.10.0.2", issue.Details);
    }

    [Fact]
    public void PackageInstalledOnAnyDevice_IsNeverFlagged()
    {
        PnpDeviceInfo[] devices =
        [
            new() { InstanceId = "PCI\\GPU", Status = "Started", DriverName = "oem128.inf" },
            new() { InstanceId = "USB\\OLD", Status = "Disconnected", DriverName = "oem5.inf" },
            new() { InstanceId = "PCI\\EXT", Status = "Started", DriverName = "pci.inf", InstalledMatchingDrivers = ["oem7.inf"] }
        ];

        IReadOnlyList<DriverIssue> issues = Analyze(
        [
            Package("oem128.inf", "gpu.inf", "1.0.0.0"),
            Package("oem129.inf", "gpu.inf", "2.0.0.0", "Display", "PCI\\GPU2"),
            Package("oem5.inf", "old.inf", "1.0.0.0", "USB"),
            Package("oem6.inf", "old.inf", "2.0.0.0", "USB", "USB\\NEW"),
            Package("oem7.inf", "ext.inf", "1.0.0.0", "Extension"),
            Package("oem8.inf", "ext.inf", "2.0.0.0", "Extension", "PCI\\EXT2")
        ], devices);

        Assert.Empty(issues);
    }

    [Fact]
    public void SameVersionDuplicates_AreNotObsolete()
    {
        IReadOnlyList<DriverIssue> issues = Analyze(
        [
            Package("oem87.inf", "ibtusb.inf", "24.70.0.4", "Bluetooth", "USB\\BT"),
            Package("oem85.inf", "ibtusb.inf", "24.70.0.4", "Bluetooth")
        ]);

        DriverIssue issue = Assert.Single(issues);
        Assert.Equal(DriverIssueKind.UnusedPackage, issue.Kind);
        Assert.Equal("Medium", issue.RiskLevel);
    }

    [Theory]
    [InlineData("Printer")]
    [InlineData("Extension")]
    [InlineData("SoftwareComponent")]
    [InlineData("AudioProcessingObject")]
    public void ClassesNotLinkedToDevices_NeverGetUnusedVerdict(string className)
    {
        Assert.Empty(Analyze([Package("oem40.inf", "single.inf", "1.0.0.0", className)]));
    }

    [Fact]
    public void PrinterDrivers_AreNeverFlaggedEvenWhenSuperseded()
    {
        Assert.Empty(Analyze(
        [
            Package("oem0.inf", "prnms009.inf", "10.0.1.0", "Printer"),
            Package("oem1.inf", "prnms009.inf", "10.0.2.0", "Printer")
        ]));
    }

    [Fact]
    public void InboxDrivers_AreNeverFlagged()
    {
        Assert.Empty(Analyze(
        [
            Package("usbport.inf", "usbport.inf", "1.0.0.0", "USB"),
            Package("usbport2.inf", "usbport.inf", "2.0.0.0", "USB")
        ]));
    }

    [Fact]
    public void PhantomDevices_RespectAgeAndClassRisk()
    {
        PnpDeviceInfo[] devices =
        [
            new() { InstanceId = "USB\\STICK", Description = "Clé USB", ClassName = "DiskDrive", Status = "Disconnected" },
            new() { InstanceId = "PCI\\NIC", Description = "Carte réseau", ClassName = "Net", Status = "Disconnected" },
            new() { InstanceId = "USB\\RECENT", Description = "Souris", ClassName = "Mouse", Status = "Disconnected" },
            new() { InstanceId = "USB\\UNDATED", Description = "Inconnu", ClassName = "USB", Status = "Disconnected" },
            new() { InstanceId = "USB\\PRESENT", Description = "Clavier", ClassName = "Keyboard", Status = "Started" }
        ];
        var lastSeen = new Dictionary<string, DateTime>
        {
            ["USB\\STICK"] = Now.AddDays(-400),
            ["PCI\\NIC"] = Now.AddDays(-120),
            ["USB\\RECENT"] = Now.AddDays(-10),
            ["USB\\PRESENT"] = Now.AddDays(-999)
        };

        IReadOnlyList<DriverIssue> issues = Analyze([], devices, lastSeen, minimumAgeDays: 90);

        Assert.Equal(2, issues.Count);
        DriverIssue stick = Assert.Single(issues, i => i.Target == "USB\\STICK");
        Assert.Equal("Low", stick.RiskLevel);
        Assert.Contains("400 jours", stick.Details);
        Assert.Equal("Medium", Assert.Single(issues, i => i.Target == "PCI\\NIC").RiskLevel);

        Assert.Equal(3, Analyze([], devices, lastSeen, minimumAgeDays: 7).Count);
    }

    [Fact]
    public void PackageLoadedByService_IsProtectedButOlderCopiesAreNot()
    {
        DriverPackageInfo running = Package("oem123.inf", "gameflt.inf", "10.0.22018.0", "HSM");
        running.StoreFolder = "gameflt.inf_amd64_c03f7ce96acee3d6";
        DriverPackageInfo newer = Package("oem124.inf", "gameflt.inf", "10.0.22021.3", "HSM");
        newer.StoreFolder = "gameflt.inf_amd64_aaaa";

        IReadOnlyList<DriverIssue> issues = DriverAnalyzer.Analyze(
            new DriverInventory
            {
                Packages = [running, newer],
                ServiceStoreFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GAMEFLT.INF_AMD64_C03F7CE96ACEE3D6" }
            },
            90,
            Now);

        Assert.Empty(issues);
    }

    [Fact]
    public void UnresolvedStoreFolder_FallsBackToOriginalNamePrefix()
    {
        IReadOnlyList<DriverIssue> issues = DriverAnalyzer.Analyze(
            new DriverInventory
            {
                Packages = [Package("oem36.inf", "pawnio.inf", "2.2.0.0", "System"), Package("oem37.inf", "other.inf", "1.0.0.0", "System")],
                ServiceStoreFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pawnio.inf_amd64_a72a2f969b8b7496" }
            },
            90,
            Now);

        Assert.Equal("oem37.inf", Assert.Single(issues).Target);
    }

    [Theory]
    [InlineData(@"\SystemRoot\System32\DriverStore\FileRepository\xvdd.inf_amd64_1e22073ba7136f03\xvdd.sys", "xvdd.inf_amd64_1e22073ba7136f03")]
    [InlineData("\"C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\amdfendr.inf_amd64_b08e\\amdfendrsr.exe\" -k", "amdfendr.inf_amd64_b08e")]
    [InlineData(@"\SystemRoot\System32\drivers\tcpip.sys", null)]
    public void ServiceImagePath_ExtractsFileRepositoryFolder(string imagePath, string? expected)
    {
        var folders = new HashSet<string>();
        DriverStoreReferences.AddFolder(folders, imagePath);
        Assert.Equal(expected is null ? [] : [expected], folders);
    }

    [Fact]
    public void Summarize_KeepsErrorLineInsteadOfCounters()
    {
        const string output = "Utilitaire Plug-and-Play Microsoft\r\n\r\nÉchec de la suppression du package de pilotes : Un ou plusieurs périphériques utilisent l'INF.\r\n\r\nNombre total : 0";
        Assert.Equal(
            "Échec de la suppression du package de pilotes : Un ou plusieurs périphériques utilisent l'INF. — code 5",
            DriverCleanerService.Summarize(output, 5));
        Assert.Equal("code 2", DriverCleanerService.Summarize("", 2));
    }

    [Fact]
    public void PendingCleanup_SurvivesRestartOnceAndExpires()
    {
        string path = Path.Combine(Path.GetTempPath(), $"panosse-pending-{Guid.NewGuid():N}.txt");
        try
        {
            var pending = new PendingDriverCleanup(path);
            pending.Save(["oem2.inf", "USB\\VID_0416&PID_7395\\1"]);

            Assert.True(pending.HasPending);
            Assert.Equal(new HashSet<string> { "oem2.inf", "USB\\VID_0416&PID_7395\\1" }, pending.Take());
            Assert.False(pending.HasPending);
            Assert.Empty(pending.Take());

            pending.Save(["oem3.inf"]);
            File.SetLastWriteTime(path, DateTime.Now.AddHours(-1));
            Assert.Empty(pending.Take());
            Assert.False(pending.HasPending);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ConfirmMessage_NamesSingleItemAndExplainsElevation()
    {
        DriverIssue single = new() { Kind = DriverIssueKind.ObsoletePackage, Title = "xvdd.inf — 10.0.22025.3" };
        string elevated = DriversViewModel.BuildConfirmMessage([single], isElevated: true);
        Assert.StartsWith("Supprimer « xvdd.inf — 10.0.22025.3 » ?", elevated);
        Assert.DoesNotContain("autorisation administrateur", elevated);

        string standard = DriversViewModel.BuildConfirmMessage(
            [single, new DriverIssue { Kind = DriverIssueKind.PhantomDevice, Title = "Manette" }],
            isElevated: false);
        Assert.StartsWith("Supprimer 1 pilote(s) et 1 périphérique(s) caché(s) ?", standard);
        Assert.Contains("autorisation administrateur", standard);
    }

    [Fact]
    public void DriverIssueItem_PreselectsOnlyLowRisk()
    {
        Assert.True(new DriverIssueItem(new DriverIssue { RiskLevel = "Low" }).IsSelected);
        Assert.False(new DriverIssueItem(new DriverIssue { RiskLevel = "Medium" }).IsSelected);
    }

    [Fact]
    public void ResultMessage_MentionsSkippedAndReboot()
    {
        string message = DriversViewModel.BuildResultMessage(new DriverCleanResult
        {
            PackagesRemoved = 12,
            DevicesRemoved = 40,
            SkippedCount = 2,
            RebootRequired = true
        });

        Assert.Contains("12 pilote(s)", message);
        Assert.Contains("40 périphérique(s)", message);
        Assert.Contains("2 conservé(s)", message);
        Assert.Contains("Redémarrez", message);
    }

    [Fact]
    public void IsSuccess_AcceptsRebootRequired()
    {
        Assert.True(DriverCleanerService.IsSuccess(0));
        Assert.True(DriverCleanerService.IsSuccess(3010));
        Assert.False(DriverCleanerService.IsSuccess(5));
    }

    [Fact]
    public void SanitizeFileName_ReplacesInvalidCharacters()
    {
        Assert.Equal("a_b.inf", DriverCleanerService.SanitizeFileName("a:b.inf"));
        Assert.Equal("pilote", DriverCleanerService.SanitizeFileName("  "));
    }
}
