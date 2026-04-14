using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace BubbleDisplay
{
    public partial class MainWindow : Window
    {
        private const int MAX_CHARS = 120;

        public MainWindow()
        {
            InitializeComponent();
            InitWebView();
        }

        // ── WebView2 Init ─────────────────────────────────────────
        private async void InitWebView()
        {
            // Guard for OS version before touching WebView2/CoreWebView2 APIs
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                // WebView2 unsupported on this OS — skip initialization
                return;
            }

            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            string htmlPath = Path.Combine(Path.GetTempPath(), "bubble_display.html");
            File.WriteAllText(htmlPath, GetDisplayHtml());
            webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        }

        // ── Event Handlers ────────────────────────────────────────
        private void TxtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lblCharCount == null) return;
            int len = txtMessage.Text.Length;
            lblCharCount.Text = $"{len}/{MAX_CHARS}";
            if (len >= MAX_CHARS)
                lblCharCount.Foreground = new SolidColorBrush(Color.FromRgb(122, 26, 26));
            else if (len >= MAX_CHARS * 0.8)
                lblCharCount.Foreground = new SolidColorBrush(Color.FromRgb(122, 80, 16));
            else
                lblCharCount.Foreground = new SolidColorBrush(Color.FromRgb(58, 58, 58));
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SendBubble();
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) => SendBubble();
        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearBubbles();

        private void ChkShowName_Changed(object sender, RoutedEventArgs e)
        {
            if (txtName != null)
                txtName.IsEnabled = chkShowName.IsChecked == true;
        }

        // ── Actions ───────────────────────────────────────────────
        private async void SendBubble()
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            string name = chkShowName.IsChecked == true ? txtName.Text.Trim() : "";
            bool showName = chkShowName.IsChecked == true && !string.IsNullOrEmpty(name);
            int speed = (int)sldSpeed.Value;

            string safeText = text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");
            string safeName = name.Replace("\\", "\\\\").Replace("'", "\\'");

            string js = $@"
        if (typeof showBubble === 'function') {{
            showBubble('{safeText}', '{safeName}', {(showName ? "true" : "false")}, {speed});
        }}
    ";

            // Only call WebView2 script execution on supported Windows versions and if CoreWebView2 is ready
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) && webView.CoreWebView2 != null)
            {
                try { await webView.CoreWebView2.ExecuteScriptAsync(js); }
                catch { /* WebView not ready or execution failed */ }
            }

            txtMessage.Clear();
            txtMessage.Focus();
            ShowStatus("sent ✓");
        }

        private async void ClearBubbles()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) && webView.CoreWebView2 != null)
            {
                try
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(
                        "document.querySelectorAll('.bubble-wrap').forEach(b => b.remove());"
                    );
                }
                catch { }
            }
            ShowStatus("cleared");
        }

        private async void ShowStatus(string msg)
        {
            lblStatus.Text = msg;
            await System.Threading.Tasks.Task.Delay(1500);
            if (lblStatus.Text == msg) lblStatus.Text = "";
        }

        // ── Embedded HTML ─────────────────────────────────────────
        private string GetDisplayHtml() => @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>Bubble Display</title>
<link href='https://fonts.googleapis.com/css2?family=Press+Start+2P&display=swap' rel='stylesheet'>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body { width: 100%; height: 100%; background: #00ff00 !important; overflow: hidden; }
  #app {
    width: 100vw; height: 100vh; background: #00ff00 !important;
    display: flex; flex-direction: column; justify-content: center;
    align-items: center; padding: 10px 12px; gap: 8px; --s: 1;
  }
  .bubble-wrap {
    display: flex; flex-direction: column; align-items: flex-start;
    gap: calc(3px * var(--s)); animation: popIn 0.12s ease;
    flex-shrink: 0; max-width: min(90vw, calc(460px * var(--s)));
  }
  @keyframes popIn {
    from { opacity: 0; transform: scale(0.9) translateY(5px); }
    to   { opacity: 1; transform: scale(1) translateY(0); }
  }
  .bubble-name {
    font-family: 'Press Start 2P', monospace; font-size: calc(8px * var(--s));
    color: #ffe97d; text-shadow: 1px 1px 0 #3a2010; margin-left: calc(6px * var(--s));
  }
  .bubble-box {
    position: relative; background: #f5e6c8;
    border: calc(3px * var(--s)) solid #5c3a1e;
    box-shadow: calc(3px * var(--s)) calc(3px * var(--s)) 0 #3a2010;
    padding: calc(12px * var(--s)) calc(20px * var(--s));
    display: block; width: 100%; border-radius: 2px; margin-bottom: calc(10px * var(--s));
  }
  .bubble-box::after {
    content: ''; position: absolute; bottom: calc(-11px * var(--s)); left: calc(14px * var(--s));
    border-left: calc(9px * var(--s)) solid transparent;
    border-top: calc(11px * var(--s)) solid #5c3a1e;
  }
  .bubble-box::before {
    content: ''; position: absolute; bottom: calc(-6px * var(--s)); left: calc(17px * var(--s));
    border-left: calc(6px * var(--s)) solid transparent;
    border-top: calc(8px * var(--s)) solid #f5e6c8; z-index: 1;
  }
  .bubble-text {
    font-family: 'Press Start 2P', monospace; font-size: calc(9px * var(--s));
    color: #3b2009; line-height: 1.8; word-break: normal;
    overflow-wrap: break-word; white-space: pre-wrap; display: block; width: 100%;
  }
  .fading { transition: opacity 0.6s ease; opacity: 0 !important; }
  #typing-wrap {
    display: flex; flex-direction: column; align-items: flex-start;
    max-width: min(90vw, calc(460px * var(--s)));
    opacity: 0; pointer-events: none; transition: opacity 0.2s ease;
  }
  #typing-wrap.visible { opacity: 1; }
  .typing-name {
    font-family: 'Press Start 2P', monospace; font-size: calc(8px * var(--s));
    color: #ffe97d; text-shadow: 1px 1px 0 #3a2010;
    margin-left: calc(6px * var(--s)); margin-bottom: calc(3px * var(--s));
  }
  .typing-box {
    position: relative; background: #f5e6c8;
    border: calc(3px * var(--s)) solid #5c3a1e;
    box-shadow: calc(3px * var(--s)) calc(3px * var(--s)) 0 #3a2010;
    padding: calc(10px * var(--s)) calc(18px * var(--s));
    border-radius: 2px; margin-bottom: calc(10px * var(--s));
    display: flex; align-items: center; gap: calc(5px * var(--s));
  }
  .typing-box::after {
    content: ''; position: absolute; bottom: calc(-11px * var(--s)); left: calc(14px * var(--s));
    border-left: calc(9px * var(--s)) solid transparent;
    border-top: calc(11px * var(--s)) solid #5c3a1e;
  }
  .typing-box::before {
    content: ''; position: absolute; bottom: calc(-6px * var(--s)); left: calc(17px * var(--s));
    border-left: calc(6px * var(--s)) solid transparent;
    border-top: calc(8px * var(--s)) solid #f5e6c8; z-index: 1;
  }
  .dot {
    width: calc(6px * var(--s)); height: calc(6px * var(--s));
    background: #5c3a1e; border-radius: 50%;
    animation: dotBounce 1s ease-in-out infinite; flex-shrink: 0;
  }
  .dot:nth-child(2) { animation-delay: 0.18s; }
  .dot:nth-child(3) { animation-delay: 0.36s; }
  @keyframes dotBounce {
    0%, 80%, 100% { transform: translateY(0); opacity: 0.4; }
    40%           { transform: translateY(calc(-6px * var(--s))); opacity: 1; }
  }
</style>
</head>
<body>
<div id='app'>
  <div id='typing-wrap'>
    <div class='typing-name' id='typing-name'></div>
    <div class='typing-box'>
      <div class='dot'></div><div class='dot'></div><div class='dot'></div>
    </div>
  </div>
</div>
<script>
  const app = document.getElementById('app');
  const typingWrap = document.getElementById('typing-wrap');
  const typingNameEl = document.getElementById('typing-name');
  function ensureTypingLast() { app.appendChild(typingWrap); }
  let typingTimeout = null;
  function updateScale() {
    const s = Math.max(0.3, Math.min(3, window.innerWidth / 400));
    app.style.setProperty('--s', s.toFixed(3));
  }
  window.addEventListener('resize', updateScale);
  updateScale();
  function typeText(el, text, speed, onDone) {
    let i = 0;
    const iv = setInterval(() => {
      el.textContent += text[i]; i++;
      if (i >= text.length) { clearInterval(iv); if (onDone) onDone(); }
    }, speed || 35);
  }
  function showTyping(name, showName) {
    typingNameEl.textContent = name || '';
    typingNameEl.style.display = showName ? 'block' : 'none';
    typingWrap.classList.add('visible');
    ensureTypingLast();
    clearTimeout(typingTimeout);
    typingTimeout = setTimeout(hideTyping, 10000);
  }
  function hideTyping() {
    clearTimeout(typingTimeout);
    typingWrap.classList.remove('visible');
  }
  function showBubble(text, name, showName, speed) {
    hideTyping();
    const all = app.querySelectorAll('.bubble-wrap');
    if (all.length >= 3) {
      const oldest = all[0];
      oldest.classList.add('fading');
      setTimeout(() => oldest.remove(), 600);
    }
    const wrap = document.createElement('div');
    wrap.className = 'bubble-wrap';
    if (showName) {
      const nameEl = document.createElement('div');
      nameEl.className = 'bubble-name';
      nameEl.textContent = name;
      wrap.appendChild(nameEl);
    }
    const box = document.createElement('div');
    box.className = 'bubble-box';
    const textEl = document.createElement('span');
    textEl.className = 'bubble-text';
    box.appendChild(textEl);
    wrap.appendChild(box);
    app.insertBefore(wrap, typingWrap);
    typeText(textEl, text, speed, () => {
      setTimeout(() => {
        wrap.classList.add('fading');
        setTimeout(() => wrap.remove(), 600);
      }, 5000);
    });
  }
</script>
</body>
</html>";
    }
}