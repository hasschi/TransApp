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
    - [x] 螢幕截圖實作 (GDI+ 基礎版，支援 DPI)。
    - [x] `Windows.Media.Ocr` 辨識邏輯 (多語言支援)。
    - [x] 翻譯快取 (Cache) 優化。
- **第四階段：翻譯顯示與 Overlay (UI/UX)**
    - [x] 滑鼠穿透 (Click-through) 視窗實作。
    - [x] Google Translate API 免費端點整合。
    - [x] Overlay 視窗高度自適應。

## 2. 技術堆疊 (Tech Stack)

- **架構：** .NET 8 (WPF)
- **快捷鍵：** NHotkey.Wpf
- **UI 元件：** Hardcodet.NotifyIcon.Wpf
- **目標 API：** Windows.Graphics.Capture, Windows.Media.Ocr

---

## 3. 重要決策與規則 (Mandates & Rules)

### 3.1 關鍵修正記錄 (Bug Fixes)

1. **DPI 縮放修復：** 原本 `ScreenCaptureService` 依賴未顯示的 `MainWindow` 導致 DPI 抓取失敗。現已改為由 `SelectionWindow` 即時捕捉 DPI 並傳遞，確保高解析度螢幕截圖座標精確。
2. **OCR 多語言化：** 原本硬編碼 `zh-Hant-TW` 導致外文辨識失效。現已改為優先偵測系統語言包並支援多語言辨識。
3. **監控穩定性：** 引入 `CancellationToken` 確保監控循環在停止時能立即中斷非同步請求，避免資源殘留與 UI 卡死。

---

## 4. 下一步計畫 (Next Steps)

1. 整合 `Windows.Graphics.Capture` API 以獲得更好的效能與現代截圖特性。
2. 增加設定介面，讓使用者可以自訂翻譯的目標語言與字體大小。
3. 實作更精準的 OCR 區塊合併邏輯。
