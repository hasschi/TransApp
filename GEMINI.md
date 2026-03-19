# TransApp - Real-time Screen Translator 專案概況

此專案旨在開發一個 Windows 常駐應用程式，能即時監控螢幕特定範圍、辨識文字 (OCR) 並翻譯，最後以「滑鼠穿透」的 Overlay 顯示結果。

## 1. 目前實作進度 (Current Status)

- **第一階段：系統常駐與快捷鍵 (System Infrastructure)**
    - [x] 系統匣圖示 (System Tray) 實作 (`Hardcodet.NotifyIcon.Wpf`)。
    - [x] 全域快捷鍵監聽 (`NHotkey.Wpf`, `Alt + Q`)。
    - [x] Git 版本控制與 .gitignore 配置。
- **第二階段：範圍選取介面 (Snipping Module)**
    - [x] 全螢幕選取視窗 (`SelectionWindow`)。
    - [x] 矩形選取範圍邏輯 (滑鼠拖曳選取)。
    - [x] DPI 縮放支援 (app.manifest 設定為 PerMonitorV2)。
    - [x] 座標映射事件回呼。
- **第三階段：高效能即時監控引擎 (Core Pipeline)**
    - [x] 螢幕截圖實作 (GDI+ 基礎版)。
    - [x] `Windows.Media.Ocr` 辨識邏輯。
    - [x] 翻譯快取 (Cache) 優化。
- **第四階段：翻譯顯示與 Overlay (UI/UX)**
    - [x] 滑鼠穿透 (Click-through) 視窗實作。
    - [x] Google Translate API 免費端點整合。

## 2. 技術堆疊 (Tech Stack)

- **架構：** .NET 8 (WPF)
- **快捷鍵：** NHotkey.Wpf
- **UI 元件：** Hardcodet.NotifyIcon.Wpf
- **目標 API：** Windows.Graphics.Capture, Windows.Media.Ocr

---

## 3. 重要決策與規則 (Mandates & Rules)

### 3.1 即時資訊更新規則 (Real-time Context Maintenance)

為了確保在不同會話中資訊的準確性，每次會話開始或重大進度更新時，必須遵循以下規則：

1. **進度檢核：** 每次實作新功能後，應立即更新 `GEMINI.md` 中的「目前實作進度」。
2. **技術決策記錄：** 若引入新的函式庫、修改核心架構或變更 Win32 API 呼叫方式，必須在 `GEMINI.md` 中記錄原因與影響。
3. **環境變更：** 若專案配置（如 `csproj` 屬性、`app.manifest`）有變動，應同步更新文件。

### 3.2 開發準則 (Development Mandates)

- **語言偏好：** 除了程式碼與註解，所有文件與溝通必須使用繁體中文。
- **效能優先：** OCR 與翻譯請求必須非同步處理，避免 UI 卡頓。
- **安全第一：** 嚴禁硬編碼 (Hard-code) 任何 API Key。
- **DPI 感知：** 必須確保 `app.manifest` 設置正確以支援 `PerMonitorV2`。

### 3.3 優先規則 (Priority Rules)

0. **Git 自動版本控制 (最高優先級)：** 每當 Agent 完成檔案的新增、修改或刪除後，**必須主動執行 Git 提交**。提交訊息必須具備語意化 (例如: `Feat: 新增 Bun 管理`, `Fix: 修正路徑偵測`)。

---

## 4. 下一步計畫 (Next Steps)

1. 實作 `app.manifest` 以支援高 DPI 縮放。
2. 整合 `Windows.Graphics.Capture` API 進行範圍截圖。
3. 實作 `Windows.Media.Ocr` 以辨識選取區域內的文字。
