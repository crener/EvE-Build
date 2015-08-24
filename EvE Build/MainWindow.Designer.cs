namespace EvE_Build
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.Tab = new System.Windows.Forms.TabControl();
            this.TabManufacture = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.DisplayBType = new System.Windows.Forms.Label();
            this.TEL = new System.Windows.Forms.Label();
            this.TESlider = new System.Windows.Forms.TrackBar();
            this.DisplayType = new System.Windows.Forms.Label();
            this.ProfitView = new System.Windows.Forms.DataGridView();
            this.MEL = new System.Windows.Forms.Label();
            this.DisplayName = new System.Windows.Forms.Label();
            this.MESlider = new System.Windows.Forms.TrackBar();
            this.ManufacturingTable = new System.Windows.Forms.DataGridView();
            this.itemSelectAll = new System.Windows.Forms.ListBox();
            this.ItemTabs = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ToolProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.ToolProgLbl = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolError = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.Tab.SuspendLayout();
            this.TabManufacture.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TESlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ProfitView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MESlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ManufacturingTable)).BeginInit();
            this.ItemTabs.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.SuspendLayout();
            // 
            // Tab
            // 
            this.Tab.Controls.Add(this.TabManufacture);
            this.Tab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Tab.Location = new System.Drawing.Point(0, 0);
            this.Tab.Name = "Tab";
            this.Tab.SelectedIndex = 0;
            this.Tab.Size = new System.Drawing.Size(739, 510);
            this.Tab.TabIndex = 1;
            // 
            // TabManufacture
            // 
            this.TabManufacture.Controls.Add(this.splitContainer2);
            this.TabManufacture.Location = new System.Drawing.Point(4, 22);
            this.TabManufacture.Name = "TabManufacture";
            this.TabManufacture.Padding = new System.Windows.Forms.Padding(3);
            this.TabManufacture.Size = new System.Drawing.Size(731, 484);
            this.TabManufacture.TabIndex = 0;
            this.TabManufacture.Text = "Manufacture";
            this.TabManufacture.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.DisplayBType);
            this.splitContainer2.Panel1.Controls.Add(this.TEL);
            this.splitContainer2.Panel1.Controls.Add(this.TESlider);
            this.splitContainer2.Panel1.Controls.Add(this.DisplayType);
            this.splitContainer2.Panel1.Controls.Add(this.ProfitView);
            this.splitContainer2.Panel1.Controls.Add(this.MEL);
            this.splitContainer2.Panel1.Controls.Add(this.DisplayName);
            this.splitContainer2.Panel1.Controls.Add(this.MESlider);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.ManufacturingTable);
            this.splitContainer2.Size = new System.Drawing.Size(725, 478);
            this.splitContainer2.SplitterDistance = 311;
            this.splitContainer2.TabIndex = 2;
            // 
            // DisplayBType
            // 
            this.DisplayBType.AutoSize = true;
            this.DisplayBType.Location = new System.Drawing.Point(56, 26);
            this.DisplayBType.Name = "DisplayBType";
            this.DisplayBType.Size = new System.Drawing.Size(46, 13);
            this.DisplayBType.TabIndex = 7;
            this.DisplayBType.Text = "*BType*";
            // 
            // TEL
            // 
            this.TEL.AutoSize = true;
            this.TEL.Location = new System.Drawing.Point(120, 131);
            this.TEL.Name = "TEL";
            this.TEL.Size = new System.Drawing.Size(50, 13);
            this.TEL.TabIndex = 6;
            this.TEL.Text = "TE Level";
            // 
            // TESlider
            // 
            this.TESlider.Location = new System.Drawing.Point(106, 147);
            this.TESlider.Maximum = 20;
            this.TESlider.Name = "TESlider";
            this.TESlider.Size = new System.Drawing.Size(94, 45);
            this.TESlider.TabIndex = 5;
            this.TESlider.Value = 20;
            this.TESlider.Scroll += new System.EventHandler(this.TESlider_Scroll);
            // 
            // DisplayType
            // 
            this.DisplayType.AutoSize = true;
            this.DisplayType.Location = new System.Drawing.Point(3, 26);
            this.DisplayType.Name = "DisplayType";
            this.DisplayType.Size = new System.Drawing.Size(50, 13);
            this.DisplayType.TabIndex = 4;
            this.DisplayType.Text = "*TypeID*";
            // 
            // ProfitView
            // 
            this.ProfitView.AllowUserToAddRows = false;
            this.ProfitView.AllowUserToDeleteRows = false;
            this.ProfitView.AllowUserToResizeColumns = false;
            this.ProfitView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ProfitView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ProfitView.Location = new System.Drawing.Point(0, 198);
            this.ProfitView.Name = "ProfitView";
            this.ProfitView.RowHeadersWidth = 21;
            this.ProfitView.RowTemplate.Height = 18;
            this.ProfitView.Size = new System.Drawing.Size(725, 113);
            this.ProfitView.TabIndex = 3;
            // 
            // MEL
            // 
            this.MEL.AutoSize = true;
            this.MEL.Location = new System.Drawing.Point(20, 131);
            this.MEL.Name = "MEL";
            this.MEL.Size = new System.Drawing.Size(52, 13);
            this.MEL.TabIndex = 2;
            this.MEL.Text = "ME Level";
            // 
            // DisplayName
            // 
            this.DisplayName.AutoSize = true;
            this.DisplayName.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DisplayName.Location = new System.Drawing.Point(3, 0);
            this.DisplayName.Name = "DisplayName";
            this.DisplayName.Size = new System.Drawing.Size(91, 26);
            this.DisplayName.TabIndex = 1;
            this.DisplayName.Text = "*name*";
            // 
            // MESlider
            // 
            this.MESlider.Location = new System.Drawing.Point(6, 147);
            this.MESlider.Name = "MESlider";
            this.MESlider.Size = new System.Drawing.Size(94, 45);
            this.MESlider.TabIndex = 0;
            this.MESlider.Value = 10;
            this.MESlider.Scroll += new System.EventHandler(this.MESlider_Scroll);
            // 
            // ManufacturingTable
            // 
            this.ManufacturingTable.AllowUserToAddRows = false;
            this.ManufacturingTable.AllowUserToDeleteRows = false;
            this.ManufacturingTable.AllowUserToResizeColumns = false;
            this.ManufacturingTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ManufacturingTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ManufacturingTable.Location = new System.Drawing.Point(0, 0);
            this.ManufacturingTable.Name = "ManufacturingTable";
            this.ManufacturingTable.ReadOnly = true;
            this.ManufacturingTable.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.ManufacturingTable.RowTemplate.Height = 18;
            this.ManufacturingTable.Size = new System.Drawing.Size(725, 163);
            this.ManufacturingTable.TabIndex = 0;
            // 
            // itemSelectAll
            // 
            this.itemSelectAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemSelectAll.FormattingEnabled = true;
            this.itemSelectAll.Location = new System.Drawing.Point(0, 0);
            this.itemSelectAll.Name = "itemSelectAll";
            this.itemSelectAll.Size = new System.Drawing.Size(144, 445);
            this.itemSelectAll.TabIndex = 0;
            this.itemSelectAll.SelectedIndexChanged += new System.EventHandler(this.itemSelectAll_SelectedIndexChanged);
            // 
            // ItemTabs
            // 
            this.ItemTabs.Controls.Add(this.tabPage1);
            this.ItemTabs.Controls.Add(this.tabPage2);
            this.ItemTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ItemTabs.Location = new System.Drawing.Point(0, 0);
            this.ItemTabs.Name = "ItemTabs";
            this.ItemTabs.SelectedIndex = 0;
            this.ItemTabs.Size = new System.Drawing.Size(158, 510);
            this.ItemTabs.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.splitContainer3);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(150, 484);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "All Items";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.IsSplitterFixed = true;
            this.splitContainer3.Location = new System.Drawing.Point(3, 3);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.searchBox);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.itemSelectAll);
            this.splitContainer3.Size = new System.Drawing.Size(144, 478);
            this.splitContainer3.SplitterDistance = 29;
            this.splitContainer3.TabIndex = 0;
            // 
            // searchBox
            // 
            this.searchBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchBox.Location = new System.Drawing.Point(0, 0);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(144, 20);
            this.searchBox.TabIndex = 0;
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(150, 484);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Groups";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ItemTabs);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.Tab);
            this.splitContainer1.Size = new System.Drawing.Size(901, 510);
            this.splitContainer1.SplitterDistance = 158;
            this.splitContainer1.TabIndex = 3;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(901, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolProgress,
            this.ToolProgLbl,
            this.ToolError});
            this.statusStrip1.Location = new System.Drawing.Point(0, 4);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(901, 22);
            this.statusStrip1.TabIndex = 5;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // ToolProgress
            // 
            this.ToolProgress.Name = "ToolProgress";
            this.ToolProgress.Size = new System.Drawing.Size(100, 16);
            // 
            // ToolProgLbl
            // 
            this.ToolProgLbl.Name = "ToolProgLbl";
            this.ToolProgLbl.Size = new System.Drawing.Size(118, 17);
            this.ToolProgLbl.Text = "toolStripStatusLabel1";
            // 
            // ToolError
            // 
            this.ToolError.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.ToolError.ForeColor = System.Drawing.Color.Red;
            this.ToolError.Name = "ToolError";
            this.ToolError.Size = new System.Drawing.Size(57, 17);
            this.ToolError.Text = "Error Text";
            this.ToolError.VisitedLinkColor = System.Drawing.Color.Red;
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 24);
            this.splitContainer4.Name = "splitContainer4";
            this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.splitContainer1);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainer4.Size = new System.Drawing.Size(901, 540);
            this.splitContainer4.SplitterDistance = 510;
            this.splitContainer4.TabIndex = 6;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(901, 564);
            this.Controls.Add(this.splitContainer4);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "EvE Build";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_Close);
            this.Shown += new System.EventHandler(this.MainWindow_Load);
            this.Tab.ResumeLayout(false);
            this.TabManufacture.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TESlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ProfitView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MESlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ManufacturingTable)).EndInit();
            this.ItemTabs.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel1.PerformLayout();
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            this.splitContainer4.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl Tab;
        private System.Windows.Forms.TabPage TabManufacture;
        private System.Windows.Forms.TabControl ItemTabs;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.TrackBar MESlider;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox itemSelectAll;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.DataGridView ManufacturingTable;
        private System.Windows.Forms.DataGridView ProfitView;
        private System.Windows.Forms.Label MEL;
        private System.Windows.Forms.Label DisplayName;
        private System.Windows.Forms.Label DisplayType;
        private System.Windows.Forms.Label TEL;
        private System.Windows.Forms.TrackBar TESlider;
        private System.Windows.Forms.Label DisplayBType;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar ToolProgress;
        private System.Windows.Forms.ToolStripStatusLabel ToolProgLbl;
        private System.Windows.Forms.SplitContainer splitContainer4;
        public System.Windows.Forms.ToolStripStatusLabel ToolError;
    }
}

