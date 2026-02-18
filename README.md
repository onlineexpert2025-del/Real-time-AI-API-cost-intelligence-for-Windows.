# CostPulse - AI Cost Tracker & Analytics Widget

CostPulse is a modern, lightweight Windows desktop widget for tracking AI usage costs in real-time. Designed with a focus on **Professional Engineering**, **Modular Architecture**, and **Modern WPF/MVVM development**, it seamlessly integrates with your clipboard and log files to provide instant cost insights for LLM usage (OpenAI, Anthropic, etc.).

## 🚀 Features

- **Real-Time Cost Tracking**: Instantly detects AI usage from clipboard (JSON responses) and log files.
- **Portfolio Mode**: Pro features enabled by default for demonstration (Analytics, Licensing bypass).
- **Log Import Service**: Watches local log directories (`.log`, `.txt`, `.json`, `.jsonl`), parses usage data, and persists state across restarts.
- **Analytics Dashboard**: Visualize spending trends with daily/monthly charts and model breakdowns.
- **Mock Cloud Sync**: Demonstrates a cloud-ready architecture with local mock synchronization.
- **Smart Deduplication**: Prevents duplicate entries using content hashing.

## 🛠️ Architecture & Tech Stack

- **Framework**: .NET 8 (WPF)
- **Pattern**: MVVM (Model-View-ViewModel) with `ServiceContainer` for Dependency Injection.
- **Testing**: xUnit with comprehensive coverage for Pricing and Parsing logic.
- **Data Persistence**: JSON-based local storage with robust error handling.
- **Services**:
  - `ClipboardMonitorService`: Background monitoring of clipboard content.
  - `LogImportService`: FileSystemWatcher implementation with offset persistence.
  - `ICloudSyncService`: Interface-based design for future backend integration.

### Deployment

- **Single-File Publish**: Packaged as a standalone `.exe` with no external dependencies required.

## 📸 Screenshots

*(Placeholder for screenshots of Widget, Analytics Dashboard, and Settings)*

## 🗺️ Roadmap

- [x] Phase 1: Core Tracking & Widget
- [x] Phase 2: Analytics & Log Import (Current Release)
- [ ] Phase 3: Real User Authentication & SaaS Licensing
- [ ] Phase 4: Full Cloud Integration (REST API)

## 📦 How to Run

1. Download the latest Release.
2. Run `CostPulse.exe`.
3. The widget will appear in the bottom-right corner.
4. Copy any OpenAI/Anthropic JSON usage block to your clipboard to see it tracked!
5. Right-click the system tray icon to access Analytics and Settings.

**Note**: This version runs in **Portfolio Mode**, meaning all Pro features are unlocked for evaluation purposes.
