namespace EvE_Build_UI
{
    partial class Options
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Options));
            this.OptionsTabs = new System.Windows.Forms.TabControl();
            this.GeneralOptions = new System.Windows.Forms.TabPage();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.label10 = new System.Windows.Forms.Label();
            this.UpdateIntervalOptions = new System.Windows.Forms.Label();
            this.UpdateInvervalSelect = new System.Windows.Forms.NumericUpDown();
            this.StartUPOptions = new System.Windows.Forms.TabPage();
            this.updateStartup = new System.Windows.Forms.CheckBox();
            this.StationPage = new System.Windows.Forms.TabPage();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.Station5ID = new System.Windows.Forms.TextBox();
            this.Station4ID = new System.Windows.Forms.TextBox();
            this.Station2ID = new System.Windows.Forms.TextBox();
            this.Station3ID = new System.Windows.Forms.TextBox();
            this.Station5Name = new System.Windows.Forms.TextBox();
            this.Station4Name = new System.Windows.Forms.TextBox();
            this.Station3Name = new System.Windows.Forms.TextBox();
            this.Station2Name = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.Station1ID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.Station1Name = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.OptionsTabs.SuspendLayout();
            this.GeneralOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UpdateInvervalSelect)).BeginInit();
            this.StartUPOptions.SuspendLayout();
            this.StationPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // OptionsTabs
            // 
            this.OptionsTabs.Controls.Add(this.GeneralOptions);
            this.OptionsTabs.Controls.Add(this.StartUPOptions);
            this.OptionsTabs.Controls.Add(this.StationPage);
            this.OptionsTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OptionsTabs.Location = new System.Drawing.Point(0, 0);
            this.OptionsTabs.Multiline = true;
            this.OptionsTabs.Name = "OptionsTabs";
            this.OptionsTabs.SelectedIndex = 0;
            this.OptionsTabs.Size = new System.Drawing.Size(326, 396);
            this.OptionsTabs.TabIndex = 0;
            // 
            // GeneralOptions
            // 
            this.GeneralOptions.Controls.Add(this.radioButton2);
            this.GeneralOptions.Controls.Add(this.radioButton1);
            this.GeneralOptions.Controls.Add(this.label10);
            this.GeneralOptions.Controls.Add(this.UpdateIntervalOptions);
            this.GeneralOptions.Controls.Add(this.UpdateInvervalSelect);
            this.GeneralOptions.Location = new System.Drawing.Point(4, 22);
            this.GeneralOptions.Name = "GeneralOptions";
            this.GeneralOptions.Padding = new System.Windows.Forms.Padding(3);
            this.GeneralOptions.Size = new System.Drawing.Size(318, 370);
            this.GeneralOptions.TabIndex = 0;
            this.GeneralOptions.Text = "General";
            this.GeneralOptions.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(11, 45);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(65, 17);
            this.radioButton2.TabIndex = 5;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Deutsch";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(11, 22);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(59, 17);
            this.radioButton1.TabIndex = 4;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "English";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(8, 6);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 13);
            this.label10.TabIndex = 3;
            this.label10.Text = "Item language";
            // 
            // UpdateIntervalOptions
            // 
            this.UpdateIntervalOptions.AutoSize = true;
            this.UpdateIntervalOptions.Location = new System.Drawing.Point(173, 8);
            this.UpdateIntervalOptions.Name = "UpdateIntervalOptions";
            this.UpdateIntervalOptions.Size = new System.Drawing.Size(125, 13);
            this.UpdateIntervalOptions.TabIndex = 2;
            this.UpdateIntervalOptions.Text = "Update Interval (minutes)";
            // 
            // UpdateInvervalSelect
            // 
            this.UpdateInvervalSelect.Location = new System.Drawing.Point(134, 6);
            this.UpdateInvervalSelect.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
            this.UpdateInvervalSelect.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.UpdateInvervalSelect.Name = "UpdateInvervalSelect";
            this.UpdateInvervalSelect.Size = new System.Drawing.Size(33, 20);
            this.UpdateInvervalSelect.TabIndex = 1;
            this.UpdateInvervalSelect.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.UpdateInvervalSelect.ValueChanged += new System.EventHandler(this.UpdateInvervalSelect_ValueChanged);
            // 
            // StartUPOptions
            // 
            this.StartUPOptions.Controls.Add(this.updateStartup);
            this.StartUPOptions.Location = new System.Drawing.Point(4, 22);
            this.StartUPOptions.Name = "StartUPOptions";
            this.StartUPOptions.Padding = new System.Windows.Forms.Padding(3);
            this.StartUPOptions.Size = new System.Drawing.Size(318, 370);
            this.StartUPOptions.TabIndex = 1;
            this.StartUPOptions.Text = "Start-Up";
            this.StartUPOptions.UseVisualStyleBackColor = true;
            // 
            // updateStartup
            // 
            this.updateStartup.AutoSize = true;
            this.updateStartup.Location = new System.Drawing.Point(8, 6);
            this.updateStartup.Name = "updateStartup";
            this.updateStartup.Size = new System.Drawing.Size(155, 17);
            this.updateStartup.TabIndex = 0;
            this.updateStartup.Text = "Update all prices on startup";
            this.updateStartup.UseVisualStyleBackColor = true;
            this.updateStartup.CheckedChanged += new System.EventHandler(this.updateStartup_CheckedChanged);
            // 
            // StationPage
            // 
            this.StationPage.Controls.Add(this.label9);
            this.StationPage.Controls.Add(this.label8);
            this.StationPage.Controls.Add(this.Station5ID);
            this.StationPage.Controls.Add(this.Station4ID);
            this.StationPage.Controls.Add(this.Station2ID);
            this.StationPage.Controls.Add(this.Station3ID);
            this.StationPage.Controls.Add(this.Station5Name);
            this.StationPage.Controls.Add(this.Station4Name);
            this.StationPage.Controls.Add(this.Station3Name);
            this.StationPage.Controls.Add(this.Station2Name);
            this.StationPage.Controls.Add(this.label7);
            this.StationPage.Controls.Add(this.label6);
            this.StationPage.Controls.Add(this.label5);
            this.StationPage.Controls.Add(this.label4);
            this.StationPage.Controls.Add(this.label3);
            this.StationPage.Controls.Add(this.Station1ID);
            this.StationPage.Controls.Add(this.label2);
            this.StationPage.Controls.Add(this.Station1Name);
            this.StationPage.Controls.Add(this.label1);
            this.StationPage.Location = new System.Drawing.Point(4, 22);
            this.StationPage.Name = "StationPage";
            this.StationPage.Padding = new System.Windows.Forms.Padding(3);
            this.StationPage.Size = new System.Drawing.Size(318, 370);
            this.StationPage.TabIndex = 2;
            this.StationPage.Text = "Stations";
            this.StationPage.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(66, 221);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(154, 13);
            this.label9.TabIndex = 18;
            this.label9.Text = "to update the data for the items";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(66, 208);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(176, 13);
            this.label8.TabIndex = 17;
            this.label8.Text = "You will have to restart the program ";
            // 
            // Station5ID
            // 
            this.Station5ID.Location = new System.Drawing.Point(189, 135);
            this.Station5ID.MaxLength = 8;
            this.Station5ID.Name = "Station5ID";
            this.Station5ID.Size = new System.Drawing.Size(100, 20);
            this.Station5ID.TabIndex = 16;
            this.Station5ID.TextChanged += new System.EventHandler(this.Station5ID_TextChanged);
            // 
            // Station4ID
            // 
            this.Station4ID.Location = new System.Drawing.Point(189, 109);
            this.Station4ID.MaxLength = 8;
            this.Station4ID.Name = "Station4ID";
            this.Station4ID.Size = new System.Drawing.Size(100, 20);
            this.Station4ID.TabIndex = 15;
            this.Station4ID.TextChanged += new System.EventHandler(this.Station4ID_TextChanged);
            // 
            // Station2ID
            // 
            this.Station2ID.Location = new System.Drawing.Point(189, 57);
            this.Station2ID.MaxLength = 8;
            this.Station2ID.Name = "Station2ID";
            this.Station2ID.Size = new System.Drawing.Size(100, 20);
            this.Station2ID.TabIndex = 14;
            this.Station2ID.TextChanged += new System.EventHandler(this.Station2ID_TextChanged);
            // 
            // Station3ID
            // 
            this.Station3ID.Location = new System.Drawing.Point(189, 83);
            this.Station3ID.MaxLength = 8;
            this.Station3ID.Name = "Station3ID";
            this.Station3ID.Size = new System.Drawing.Size(100, 20);
            this.Station3ID.TabIndex = 13;
            this.Station3ID.TextChanged += new System.EventHandler(this.Station3ID_TextChanged);
            // 
            // Station5Name
            // 
            this.Station5Name.Location = new System.Drawing.Point(69, 135);
            this.Station5Name.Name = "Station5Name";
            this.Station5Name.Size = new System.Drawing.Size(100, 20);
            this.Station5Name.TabIndex = 12;
            this.Station5Name.TextChanged += new System.EventHandler(this.Station5Name_TextChanged);
            // 
            // Station4Name
            // 
            this.Station4Name.Location = new System.Drawing.Point(69, 109);
            this.Station4Name.Name = "Station4Name";
            this.Station4Name.Size = new System.Drawing.Size(100, 20);
            this.Station4Name.TabIndex = 11;
            this.Station4Name.TextChanged += new System.EventHandler(this.Station4Name_TextChanged);
            // 
            // Station3Name
            // 
            this.Station3Name.Location = new System.Drawing.Point(69, 83);
            this.Station3Name.Name = "Station3Name";
            this.Station3Name.Size = new System.Drawing.Size(100, 20);
            this.Station3Name.TabIndex = 10;
            this.Station3Name.TextChanged += new System.EventHandler(this.Station3Name_TextChanged);
            // 
            // Station2Name
            // 
            this.Station2Name.Location = new System.Drawing.Point(69, 57);
            this.Station2Name.Name = "Station2Name";
            this.Station2Name.Size = new System.Drawing.Size(100, 20);
            this.Station2Name.TabIndex = 9;
            this.Station2Name.TextChanged += new System.EventHandler(this.Station2Name_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 138);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(49, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "Station 5";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 86);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(49, 13);
            this.label6.TabIndex = 7;
            this.label6.Text = "Station 3";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 112);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Station 4";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Station 2";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(202, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Station ID";
            // 
            // Station1ID
            // 
            this.Station1ID.Location = new System.Drawing.Point(189, 31);
            this.Station1ID.MaxLength = 8;
            this.Station1ID.Multiline = true;
            this.Station1ID.Name = "Station1ID";
            this.Station1ID.Size = new System.Drawing.Size(100, 20);
            this.Station1ID.TabIndex = 3;
            this.Station1ID.TextChanged += new System.EventHandler(this.Station1ID_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(83, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Station Name";
            // 
            // Station1Name
            // 
            this.Station1Name.Location = new System.Drawing.Point(69, 31);
            this.Station1Name.Name = "Station1Name";
            this.Station1Name.Size = new System.Drawing.Size(100, 20);
            this.Station1Name.TabIndex = 1;
            this.Station1Name.TextChanged += new System.EventHandler(this.Station1Name_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Station 1";
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(326, 396);
            this.Controls.Add(this.OptionsTabs);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Options";
            this.Text = "Options";
            this.OptionsTabs.ResumeLayout(false);
            this.GeneralOptions.ResumeLayout(false);
            this.GeneralOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UpdateInvervalSelect)).EndInit();
            this.StartUPOptions.ResumeLayout(false);
            this.StartUPOptions.PerformLayout();
            this.StationPage.ResumeLayout(false);
            this.StationPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl OptionsTabs;
        private System.Windows.Forms.TabPage GeneralOptions;
        private System.Windows.Forms.Label UpdateIntervalOptions;
        private System.Windows.Forms.NumericUpDown UpdateInvervalSelect;
        private System.Windows.Forms.TabPage StartUPOptions;
        private System.Windows.Forms.TabPage StationPage;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox Station5ID;
        private System.Windows.Forms.TextBox Station4ID;
        private System.Windows.Forms.TextBox Station2ID;
        private System.Windows.Forms.TextBox Station3ID;
        private System.Windows.Forms.TextBox Station5Name;
        private System.Windows.Forms.TextBox Station4Name;
        private System.Windows.Forms.TextBox Station3Name;
        private System.Windows.Forms.TextBox Station2Name;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox Station1ID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox Station1Name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.CheckBox updateStartup;
    }
}