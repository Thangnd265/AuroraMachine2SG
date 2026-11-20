# Feature Specification: Extensible Aurora Simulation & Custom Machine Development Platform

**Feature Branch**: `002-aurora-dev-platform`

**Created**: 2026-11-20

**Status**: Approved

**Input**: User description: "aurora là nền tảng để tôi phát triển thêm lên chức năng tù biến theo máy không phải bản cuối cùng do đó tôi chỉ cần phần mềm này hoạt động được như nền tảng đó khi không cài aurora vẫn chạy được chương trình để có thể đáp ứng được tổng quan theo dõi chương trình phát triển đnag làm gì phần mềm này đáp ứng được điều đó chưa theiesy gì hay có vấn đề gì hãy sửa giúp tôi"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Live Sequence Flowchart Execution, Node-Level Debugging & Real-Time Trace (Priority: P1)

A machine software developer running on a PC without Aurora installed opens `Control > Sequence` (or toggles the bottom `Live Dev Trace` dock from any screen) to watch every registered sequence execute node-by-node on an interactive visual flowchart (`Init -> Action -> Decision -> EventAcceptor -> Final`), inspect which node is currently running, step one node at a time (`Single Step`), pause at breakpoints, inject `Force Error` on any node, publish custom machine events (`EVENT_MANUAL_LOADING`, etc.), adjust simulation speed (`0.25x` to `4x`), and read live filtered `HisLogger` logs (`INFO / WARN / ERROR`) showing exactly what the custom program is doing.

**Why this priority**: The user's primary goal is observing and verifying what their custom machine logic is doing at runtime on PCs without Aurora installed.

**Independent Test**: Open `Control > Sequence`, select `LoadingSeq` or `ProcessSeq`, click `Single Step` or `Run Sequence`, observe active node highlight, execution counter, elapsed ms, decision branch taken, and live `HisLogger` entries; inject `Force Error` on a node and confirm alarm handling triggers cleanly.

**Acceptance Scenarios**:

1. **Given** a sequence registered with `SequenceBuilder` (`Action`, `Decision`, `EventAcceptor`, loop-back edges), **When** the developer runs or single-steps the sequence, **Then** the flowchart highlights the active node, updates node hit counts and last duration, follows the returned branch of `Decision` nodes, and appends structured trace lines to `HisLogger`.
2. **Given** a node is armed with `Force Error` (or breakpoint), **When** execution reaches that node, **Then** the sequence halts/triggers `OnForceError` and raises the corresponding equipment alarm so error-recovery logic can be verified.

---

### User Story 2 - Extensible Machine Plugin SDK (`IEquipmentSetup`) & Multi-Machine Profile Switching (Priority: P2)

A developer creates or customizes a machine module implementing `IEquipmentSetup` (defining machine name, topology `Single / MultiSequence / MultiFixture BAY1..4`, custom devices, custom `SequenceBuilder` graphs, recipes, and parameters) — either in-source under `Machines/` or compiled as an external `.dll` plugin — and switches or hot-loads that machine profile inside the host platform without modifying core host code.

**Why this priority**: Enables using the software as a reusable foundation across different custom machines rather than a hardcoded single-machine app.

**Independent Test**: Switch between built-in profiles (`SGMachine RSMS-001`, `SGPractice Edu`, and `CustomMachineTemplate` / `MultiFixture 4-Bay`) or load a compiled plugin `.dll`, and verify equipment title, stations, devices, and sequence flowcharts dynamically reconfigure.

**Acceptance Scenarios**:

1. **Given** two distinct `IEquipmentSetup` implementations (`SGMachineSetup` and `SGPracticeSetup` / `CustomTemplateSetup`), **When** the user selects a different machine profile or loads an external `.dll` plugin, **Then** the host re-initializes devices, stations, and sequence flowcharts to match the selected machine.
2. **Given** a machine configured as `MultiFixture` (`BAY1..BAY4` + `Background` + `Controller`), **When** simulation runs, **Then** all bays and background/controller sequences execute and report station statuses accurately.

---

### User Story 3 - Interactive Per-Device Simulation Test Bench & External Config Import (Priority: P3)

A developer opens `Control > Device`, selects any registered device (`BCR`, `CognexVisionIF`, `KeyenceVisionIF`, `ScrewDriver`, `RFID`, `HermesUpstream`, `BALIConnDevice`, `HIVE`, `WMX3_MOTION_STAGE`), fires simulated commands/responses (`Read Barcode`, `Trigger Vision Offset`, `Run Torque Shot`, `Send HIVE HeartBeat/State`, `Import Original _config Folder`), and inspects raw request/response payloads.

**Why this priority**: Developing custom machine sequences requires testing how sequences react to simulated device outputs (OK, NG, Timeout, Custom Barcode) and real project config files.

**Independent Test**: Select `BCR` or `CognexVisionIF` in `Control > Device`, set a simulated response override (e.g., NG vision offset or custom barcode), trigger `Test Command`, and verify response log + sequence reaction.

**Acceptance Scenarios**:

1. **Given** a device is selected in `Control > Device`, **When** the developer clicks `Execute Simulated Test` or toggles `Force Device NG`, **Then** the device returns the simulated payload, logs to `HisLogger`, and exposes that behavior to running sequences.

---

### User Story 4 - Zero-Install Self-Contained Portable Package for Non-Aurora PCs (Priority: P4)

A developer copies a self-contained portable folder (`_publish/AuroraMachine2SG_Portable`) to another Windows 10/11 `x64` PC that has neither `Aurora` nor `.NET SDK` installed and runs `AuroraMachine2SG.exe` directly.

**Why this priority**: Guarantees true zero-prerequisite execution on any colleague or test PC.

**Independent Test**: Publish self-contained `win-x64` output and verify `AuroraMachine2SG.exe` launches, renders flowcharts, and passes smoke verification from `_publish/`.

**Acceptance Scenarios**:

1. **Given** the self-contained build output directory, **When** `AuroraMachine2SG.exe` is executed directly without `dotnet run`, **Then** the application starts cleanly with all bundled runtime libraries.

---

### Edge Cases

- **Infinite Loop in Custom Sequence**: Sequence runner MUST honor cancellation/pause/stop tokens between every node transition and enforce configurable step delay (`>= 15ms`) so custom loops never freeze the UI thread.
- **Invalid External Plugin `.dll`**: Selecting a `.dll` with no `IEquipmentSetup` implementation MUST show a clear diagnostic in `HisLogger` / `StatusBanner` without crashing the host.
- **Waiting `EventAcceptor` Node**: When a sequence reaches an `EventAcceptor` node (`WAITING_EVENT`), it MUST wait non-blockingly until the matching event is published or `Stop` is pressed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Platform MUST expose a clean-room `IEquipmentSetup` & `SequenceBuilder` SDK (`AddAction`, `AddDecision`, `AddEventAcceptor`, `AddEdge`/`Flow`, `OnForceError`, `OnStopped`, `SequenceContext`, `Logger`) allowing custom machine modules to be authored in C# or loaded from `.NET 8` plugin `.dll`s.
- **FR-002**: Platform MUST ship with at least 3 ready-to-run `IEquipmentSetup` profiles demonstrating platform extensibility: `SGMachine (RSMS-001 MultiSequence)`, `SGMachine MultiFixture (BAY1..BAY4)`, and `SGPractice / Custom Machine Template` (starter skeleton for new custom machines).
- **FR-003**: `Control > Sequence` MUST render an interactive visual node-and-arrow flowchart for any selected sequence, showing live node state (`Idle`, `Running`, `Completed`, `WaitingEvent`, `PausedAtBreakpoint`, `Error`), execution count, elapsed time, and controls for `Run`, `Pause`, `Single Step`, `Cycle Stop`, `Force Stop`, `Toggle Breakpoint`, `Arm Force Error`, `Publish Event`, and `Sim Speed (0.25x..4x)`.
- **FR-004**: Platform MUST provide a toggleable bottom `Live Dev Trace (HisLogger & Sequence Monitor)` dock visible across all menus, streaming timestamped logs filterable by level (`ALL`, `INFO`, `WARN`, `ERROR`) and source logger, with `Clear` and `Export Log`.
- **FR-005**: `Control > Device` MUST include an interactive right-hand **Device Simulation Test Bench** allowing developers to trigger simulated device commands, override next result (`PASS` / `NG` / custom value), and inspect raw simulated TX/RX frames.
- **FR-006**: Platform MUST use the Ornstein–Uhlenbeck mean-reverting + outlier `CpkSignalSimulator` (matching `CPKSimulator.cs`) for realistic drift/capability simulation and support importing `_config_*` JSON files from an external Aurora `Doc/Data` directory.
- **FR-007**: Build pipeline MUST produce a self-contained `win-x64` portable executable (`_publish/AuroraMachine2SG_Portable/AuroraMachine2SG.exe`) runnable on Windows PCs with neither Aurora nor `.NET` pre-installed.

### Key Entities

- **SequenceGraph & SequenceNode**: Node ID, kind (`Init`, `Action`, `Decision`, `EventAcceptor`, `Final`), outgoing edges, breakpoint flag, force-error flag, hit count, last elapsed ms, last result/branch, and live execution status.
- **EquipmentProfile (`IEquipmentSetup`)**: Pluggable machine definition registering equipment metadata, topology (`Single`, `MultiSequence`, `MultiFixture`), devices, sequences, recipes, and models.
- **LogEntry (`HisLogger`)**: Timestamp, level (`INFO`, `WARN`, `ERROR`), logger category, sequence/node context, and message.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can visually trace active node transitions, decision branches, hit counts, and logs in real time for 100% of registered sequences, including `Single Step`, `Breakpoint`, `EventAcceptor` trigger, and `Force Error`.
- **SC-002**: Switching between `RSMS-001 MultiSequence`, `RSMS-001 MultiFixture (BAY1..4)`, and `Custom Machine Starter Template` (or loading a compiled `.dll` plugin) dynamically reconfigures sequences, stations, and devices in under 1 second without restarting the app.
- **SC-003**: Self-contained `win-x64` portable executable launches cleanly and passes automated smoke & screenshot verification with zero external installations required.
- **SC-004**: 100% of existing and new automated unit tests pass (`0` failures, `0` build warnings/errors).

## Assumptions

- Custom machine plugins developed on this platform target `.NET 8` (`AuroraMachine2SG.Core.Sdk.IEquipmentSetup`), providing a clean, license-free modern C# development workflow while preserving Aurora's familiar `EquipmentSetup` + `SequenceBuilder` mental model.
