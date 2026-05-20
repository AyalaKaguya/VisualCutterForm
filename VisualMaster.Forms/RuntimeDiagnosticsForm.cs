using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualMaster.Api;

namespace VisualMaster.Forms
{
    public class RuntimeDiagnosticsForm : Form
    {
        private readonly RuntimeDiagnosticsHub _diagnostics;
        private readonly ListView _eventList;
        private readonly Button _btnClear;

        public RuntimeDiagnosticsForm(RuntimeDiagnosticsHub diagnostics)
        {
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

            Text = "运行时诊断";
            Size = new Size(1200, 560);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 6, 8, 6),
            };

            var lblHint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "按时间倒序展示快照、触发分发和流程实例事件。",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _btnClear = new Button
            {
                Dock = DockStyle.Right,
                Width = 90,
                Text = "清空",
            };
            _btnClear.Click += (s, e) => _diagnostics.Clear();

            header.Controls.Add(lblHint);
            header.Controls.Add(_btnClear);

            _eventList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
            };
            _eventList.Columns.Add("时间", 150);
            _eventList.Columns.Add("类型", 120);
            _eventList.Columns.Add("流程", 160);
            _eventList.Columns.Add("触发器", 150);
            _eventList.Columns.Add("设备", 180);
            _eventList.Columns.Add("快照", 220);
            _eventList.Columns.Add("关联 ID", 240);
            _eventList.Columns.Add("说明", 320);

            Controls.Add(_eventList);
            Controls.Add(header);

            Load += (s, e) => RefreshEvents();
            FormClosed += (s, e) => _diagnostics.EventsChanged -= OnEventsChanged;
            _diagnostics.EventsChanged += OnEventsChanged;
        }

        private void OnEventsChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshEvents);
                return;
            }

            RefreshEvents();
        }

        private void RefreshEvents()
        {
            var events = _diagnostics.GetRecentEvents(300).ToList();
            _eventList.BeginUpdate();
            _eventList.Items.Clear();

            foreach (var item in events)
            {
                var snapshotText = string.IsNullOrWhiteSpace(item.SnapshotId)
                    ? string.Empty
                    : $"{item.SnapshotId} / #{item.SnapshotSequence}";
                var row = new ListViewItem(item.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                row.SubItems.Add(GetTypeDisplay(item.EventType));
                row.SubItems.Add(item.FlowName ?? string.Empty);
                row.SubItems.Add(item.TriggerName ?? string.Empty);
                row.SubItems.Add(item.DeviceId ?? string.Empty);
                row.SubItems.Add(snapshotText);
                row.SubItems.Add(item.CorrelationId ?? string.Empty);
                row.SubItems.Add(item.Message ?? string.Empty);
                _eventList.Items.Add(row);
            }

            _eventList.EndUpdate();
        }

        private static string GetTypeDisplay(RuntimeDiagnosticEventType type)
        {
            switch (type)
            {
                case RuntimeDiagnosticEventType.SnapshotPublished:
                    return "快照";
                case RuntimeDiagnosticEventType.TriggerDispatched:
                    return "触发";
                case RuntimeDiagnosticEventType.FlowStarted:
                    return "流程开始";
                case RuntimeDiagnosticEventType.FlowCompleted:
                    return "流程完成";
                case RuntimeDiagnosticEventType.FlowFailed:
                    return "流程失败";
                default:
                    return type.ToString();
            }
        }
    }
}