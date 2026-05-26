using System;
using System.Drawing;
using System.Windows.Forms;
using VisualMaster.WorkFlow.Triggers;

namespace VisualMaster.Forms.TriggerEditor
{
    partial class TriggerEditorForm
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._split = new System.Windows.Forms.SplitContainer();
            this._leftPanel = new System.Windows.Forms.Panel();
            this._triggerList = new System.Windows.Forms.ListView();
            this._btnPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnAdd = new System.Windows.Forms.Button();
            this._btnDelete = new System.Windows.Forms.Button();
            this._btnFire = new System.Windows.Forms.Button();
            this._rightPanel = new System.Windows.Forms.Panel();
            this._lblHeader = new System.Windows.Forms.Label();
            this._propPanel = new System.Windows.Forms.Panel();
            this._chkEnabled = new System.Windows.Forms.CheckBox();
            this._txtName = new System.Windows.Forms.TextBox();
            this._cmbSourceType = new System.Windows.Forms.ComboBox();
            this._lstTargetSubGraphs = new System.Windows.Forms.CheckedListBox();
            this._numMaxConcurrent = new System.Windows.Forms.NumericUpDown();
            this._cmbCameraSlot = new System.Windows.Forms.ComboBox();
            this._numTimerInterval = new System.Windows.Forms.NumericUpDown();
            this._cmbSerialSlot = new System.Windows.Forms.ComboBox();
            this._lblCameraSlot = new System.Windows.Forms.Label();
            this._lblTimerInterval = new System.Windows.Forms.Label();
            this._lblSerialSlot = new System.Windows.Forms.Label();
            this._lblMaxConcurrent = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
            this._split.Panel1.SuspendLayout();
            this._split.Panel2.SuspendLayout();
            this._split.SuspendLayout();
            this._leftPanel.SuspendLayout();
            this._btnPanel.SuspendLayout();
            this._rightPanel.SuspendLayout();
            this._propPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numMaxConcurrent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numTimerInterval)).BeginInit();
            this.SuspendLayout();
            // 
            // _split
            // 
            this._split.Dock = System.Windows.Forms.DockStyle.Fill;
            this._split.Location = new System.Drawing.Point(0, 0);
            this._split.Name = "_split";
            this._split.Orientation = System.Windows.Forms.Orientation.Vertical;
            this._split.Panel1MinSize = 200;
            this._split.Panel1.Controls.Add(this._leftPanel);
            this._split.Panel2.Controls.Add(this._rightPanel);
            this._split.Size = new System.Drawing.Size(850, 550);
            this._split.SplitterDistance = 260;
            this._split.TabIndex = 0;
            // 
            // _leftPanel
            // 
            this._leftPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
            this._leftPanel.Controls.Add(this._triggerList);
            this._leftPanel.Controls.Add(this._btnPanel);
            this._leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._leftPanel.Location = new System.Drawing.Point(0, 0);
            this._leftPanel.Name = "_leftPanel";
            this._leftPanel.Padding = new System.Windows.Forms.Padding(6);
            this._leftPanel.Size = new System.Drawing.Size(256, 550);
            this._leftPanel.TabIndex = 0;
            // 
            // _triggerList
            // 
            this._triggerList.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this._triggerList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._triggerList.CheckBoxes = true;
            this._triggerList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._triggerList.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._triggerList.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this._triggerList.FullRowSelect = true;
            this._triggerList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._triggerList.Location = new System.Drawing.Point(6, 40);
            this._triggerList.MultiSelect = false;
            this._triggerList.Name = "_triggerList";
            this._triggerList.Size = new System.Drawing.Size(244, 504);
            this._triggerList.TabIndex = 1;
            this._triggerList.View = System.Windows.Forms.View.Details;
            this._triggerList.Columns.Add("触发器", 240);
            // 
            // _btnPanel
            // 
            this._btnPanel.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this._btnPanel.Controls.Add(this._btnAdd);
            this._btnPanel.Controls.Add(this._btnDelete);
            this._btnPanel.Controls.Add(this._btnFire);
            this._btnPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._btnPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this._btnPanel.Location = new System.Drawing.Point(6, 6);
            this._btnPanel.Name = "_btnPanel";
            this._btnPanel.Padding = new System.Windows.Forms.Padding(4, 3, 4, 0);
            this._btnPanel.Size = new System.Drawing.Size(244, 34);
            this._btnPanel.TabIndex = 0;
            // 
            // _btnAdd
            // 
            this._btnAdd.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAdd.ForeColor = System.Drawing.Color.White;
            this._btnAdd.Name = "_btnAdd";
            this._btnAdd.Size = new System.Drawing.Size(55, 26);
            this._btnAdd.TabIndex = 0;
            this._btnAdd.Text = "新建";
            this._btnAdd.UseVisualStyleBackColor = false;
            // 
            // _btnDelete
            // 
            this._btnDelete.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._btnDelete.Enabled = false;
            this._btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDelete.ForeColor = System.Drawing.Color.White;
            this._btnDelete.Name = "_btnDelete";
            this._btnDelete.Size = new System.Drawing.Size(55, 26);
            this._btnDelete.TabIndex = 1;
            this._btnDelete.Text = "删除";
            this._btnDelete.UseVisualStyleBackColor = false;
            // 
            // _btnFire
            // 
            this._btnFire.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._btnFire.Enabled = false;
            this._btnFire.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnFire.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113);
            this._btnFire.Name = "_btnFire";
            this._btnFire.Size = new System.Drawing.Size(90, 26);
            this._btnFire.TabIndex = 2;
            this._btnFire.Text = "▶ 立即触发";
            this._btnFire.UseVisualStyleBackColor = false;
            // 
            // _rightPanel
            // 
            this._rightPanel.AutoScroll = true;
            this._rightPanel.BackColor = System.Drawing.Color.FromArgb(48, 48, 48);
            this._rightPanel.Controls.Add(this._propPanel);
            this._rightPanel.Controls.Add(this._lblHeader);
            this._rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightPanel.Location = new System.Drawing.Point(0, 0);
            this._rightPanel.Name = "_rightPanel";
            this._rightPanel.Padding = new System.Windows.Forms.Padding(8);
            this._rightPanel.Size = new System.Drawing.Size(582, 550);
            this._rightPanel.TabIndex = 0;
            // 
            // _lblHeader
            // 
            this._lblHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblHeader.Font = new System.Drawing.Font("Microsoft YaHei", 10F, System.Drawing.FontStyle.Bold);
            this._lblHeader.ForeColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this._lblHeader.Location = new System.Drawing.Point(8, 8);
            this._lblHeader.Name = "_lblHeader";
            this._lblHeader.Padding = new System.Windows.Forms.Padding(4, 6, 0, 0);
            this._lblHeader.Size = new System.Drawing.Size(566, 32);
            this._lblHeader.TabIndex = 0;
            this._lblHeader.Text = "触发器属性";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _propPanel
            // 
            this._propPanel.BackColor = System.Drawing.Color.FromArgb(48, 48, 48);
            this._propPanel.Controls.Add(this._chkEnabled);
            this._propPanel.Controls.Add(this._txtName);
            this._propPanel.Controls.Add(this._cmbSourceType);
            this._propPanel.Controls.Add(this._lstTargetSubGraphs);
            this._propPanel.Controls.Add(this._numMaxConcurrent);
            this._propPanel.Controls.Add(this._cmbCameraSlot);
            this._propPanel.Controls.Add(this._numTimerInterval);
            this._propPanel.Controls.Add(this._cmbSerialSlot);
            this._propPanel.Controls.Add(this._lblCameraSlot);
            this._propPanel.Controls.Add(this._lblTimerInterval);
            this._propPanel.Controls.Add(this._lblSerialSlot);
            this._propPanel.Controls.Add(this._lblMaxConcurrent);
            this._propPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._propPanel.Location = new System.Drawing.Point(8, 40);
            this._propPanel.Name = "_propPanel";
            this._propPanel.Size = new System.Drawing.Size(566, 502);
            this._propPanel.TabIndex = 1;
            // 
            // _chkEnabled
            // 
            this._chkEnabled.AutoSize = true;
            this._chkEnabled.BackColor = System.Drawing.Color.FromArgb(48, 48, 48);
            this._chkEnabled.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this._chkEnabled.Location = new System.Drawing.Point(14, 4);
            this._chkEnabled.Name = "_chkEnabled";
            this._chkEnabled.Size = new System.Drawing.Size(60, 28);
            this._chkEnabled.TabIndex = 0;
            this._chkEnabled.Text = "启用";
            this._chkEnabled.UseVisualStyleBackColor = false;
            // 
            // lblPropName
            // 
            this._propPanel.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "名称:",
                Location = new System.Drawing.Point(14, 38),
                Size = new System.Drawing.Size(100, 22),
                ForeColor = System.Drawing.Color.FromArgb(180, 180, 180),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            });
            // 
            // _txtName
            // 
            this._txtName.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._txtName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtName.ForeColor = System.Drawing.Color.White;
            this._txtName.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._txtName.Location = new System.Drawing.Point(120, 34);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(300, 27);
            this._txtName.TabIndex = 1;
            // 
            // lblPropType
            // 
            this._propPanel.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "类型:",
                Location = new System.Drawing.Point(14, 72),
                Size = new System.Drawing.Size(100, 22),
                ForeColor = System.Drawing.Color.FromArgb(180, 180, 180),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            });
            // 
            // _cmbSourceType
            // 
            this._cmbSourceType.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._cmbSourceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbSourceType.ForeColor = System.Drawing.Color.White;
            this._cmbSourceType.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbSourceType.Items.AddRange(new object[] { "手动触发", "相机帧触发", "定时器触发", "串口匹配触发" });
            this._cmbSourceType.Location = new System.Drawing.Point(120, 68);
            this._cmbSourceType.Name = "_cmbSourceType";
            this._cmbSourceType.Size = new System.Drawing.Size(200, 32);
            this._cmbSourceType.TabIndex = 2;
            // 
            // _lblMaxConcurrent
            // 
            this._lblMaxConcurrent.AutoSize = true;
            this._lblMaxConcurrent.Location = new System.Drawing.Point(14, 106);
            this._lblMaxConcurrent.Name = "_lblMaxConcurrent";
            this._lblMaxConcurrent.Size = new System.Drawing.Size(100, 24);
            this._lblMaxConcurrent.TabIndex = 10;
            this._lblMaxConcurrent.Text = "最大并发:";
            this._lblMaxConcurrent.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._lblMaxConcurrent.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            // 
            // _numMaxConcurrent
            // 
            this._numMaxConcurrent.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._numMaxConcurrent.ForeColor = System.Drawing.Color.White;
            this._numMaxConcurrent.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._numMaxConcurrent.Location = new System.Drawing.Point(120, 104);
            this._numMaxConcurrent.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this._numMaxConcurrent.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this._numMaxConcurrent.Name = "_numMaxConcurrent";
            this._numMaxConcurrent.Size = new System.Drawing.Size(80, 27);
            this._numMaxConcurrent.TabIndex = 3;
            this._numMaxConcurrent.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblPropSubGraph
            // 
            this._propPanel.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "目标流程:",
                Location = new System.Drawing.Point(14, 140),
                Size = new System.Drawing.Size(100, 22),
                ForeColor = System.Drawing.Color.FromArgb(180, 180, 180),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            });
            // 
            // _lstTargetSubGraphs
            // 
            this._lstTargetSubGraphs.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._lstTargetSubGraphs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lstTargetSubGraphs.CheckOnClick = true;
            this._lstTargetSubGraphs.ForeColor = System.Drawing.Color.White;
            this._lstTargetSubGraphs.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._lstTargetSubGraphs.Location = new System.Drawing.Point(120, 136);
            this._lstTargetSubGraphs.Name = "_lstTargetSubGraphs";
            this._lstTargetSubGraphs.Size = new System.Drawing.Size(300, 70);
            this._lstTargetSubGraphs.TabIndex = 4;
            // 
            // _lblCameraSlot
            // 
            this._lblCameraSlot.AutoSize = true;
            this._lblCameraSlot.Location = new System.Drawing.Point(14, 216);
            this._lblCameraSlot.Name = "_lblCameraSlot";
            this._lblCameraSlot.Size = new System.Drawing.Size(100, 24);
            this._lblCameraSlot.TabIndex = 11;
            this._lblCameraSlot.Text = "相机设备:";
            this._lblCameraSlot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._lblCameraSlot.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            // 
            // _cmbCameraSlot
            // 
            this._cmbCameraSlot.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._cmbCameraSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbCameraSlot.ForeColor = System.Drawing.Color.White;
            this._cmbCameraSlot.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbCameraSlot.Location = new System.Drawing.Point(120, 212);
            this._cmbCameraSlot.Name = "_cmbCameraSlot";
            this._cmbCameraSlot.Size = new System.Drawing.Size(300, 32);
            this._cmbCameraSlot.TabIndex = 5;
            // 
            // _lblTimerInterval
            // 
            this._lblTimerInterval.AutoSize = true;
            this._lblTimerInterval.Location = new System.Drawing.Point(14, 254);
            this._lblTimerInterval.Name = "_lblTimerInterval";
            this._lblTimerInterval.Size = new System.Drawing.Size(100, 24);
            this._lblTimerInterval.TabIndex = 12;
            this._lblTimerInterval.Text = "定时间隔(ms):";
            this._lblTimerInterval.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._lblTimerInterval.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            // 
            // _numTimerInterval
            // 
            this._numTimerInterval.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._numTimerInterval.ForeColor = System.Drawing.Color.White;
            this._numTimerInterval.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._numTimerInterval.Location = new System.Drawing.Point(120, 250);
            this._numTimerInterval.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this._numTimerInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this._numTimerInterval.Name = "_numTimerInterval";
            this._numTimerInterval.Size = new System.Drawing.Size(100, 27);
            this._numTimerInterval.TabIndex = 6;
            this._numTimerInterval.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // _lblSerialSlot
            // 
            this._lblSerialSlot.AutoSize = true;
            this._lblSerialSlot.Location = new System.Drawing.Point(14, 292);
            this._lblSerialSlot.Name = "_lblSerialSlot";
            this._lblSerialSlot.Size = new System.Drawing.Size(100, 24);
            this._lblSerialSlot.TabIndex = 13;
            this._lblSerialSlot.Text = "串口设备:";
            this._lblSerialSlot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._lblSerialSlot.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180);
            // 
            // _cmbSerialSlot
            // 
            this._cmbSerialSlot.BackColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this._cmbSerialSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbSerialSlot.ForeColor = System.Drawing.Color.White;
            this._cmbSerialSlot.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this._cmbSerialSlot.Location = new System.Drawing.Point(120, 288);
            this._cmbSerialSlot.Name = "_cmbSerialSlot";
            this._cmbSerialSlot.Size = new System.Drawing.Size(300, 32);
            this._cmbSerialSlot.TabIndex = 7;
            // 
            // TriggerEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.ClientSize = new System.Drawing.Size(850, 550);
            this.Controls.Add(this._split);
            this.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.Name = "TriggerEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "触发器管理器";
            ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
            this._split.Panel1.ResumeLayout(false);
            this._split.Panel2.ResumeLayout(false);
            this._split.ResumeLayout(false);
            this._leftPanel.ResumeLayout(false);
            this._btnPanel.ResumeLayout(false);
            this._rightPanel.ResumeLayout(false);
            this._propPanel.ResumeLayout(false);
            this._propPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numMaxConcurrent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numTimerInterval)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
