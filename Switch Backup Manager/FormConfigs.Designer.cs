namespace Switch_Backup_Manager
{
    partial class FormConfigs
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblExample = new System.Windows.Forms.Label();
            this.rbRenamingCustom = new System.Windows.Forms.RadioButton();
            this.rbRenamingTitleIDGameNameReleaseGroup = new System.Windows.Forms.RadioButton();
            this.gbCustom = new System.Windows.Forms.GroupBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.textBoxCustomPatern = new System.Windows.Forms.TextBox();
            this.cbxTags = new System.Windows.Forms.ComboBox();
            this.rbRenamingTitleIDGameName = new System.Windows.Forms.RadioButton();
            this.rbRenamingGameName = new System.Windows.Forms.RadioButton();
            this.rbRenamingGameNameRegion = new System.Windows.Forms.RadioButton();
            this.rbRenamingGameNameRegionFirmware = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gbCustom.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnApply);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 426);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(617, 36);
            this.panel1.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tabControl1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(617, 426);
            this.panel2.TabIndex = 2;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(617, 426);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(609, 400);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(609, 400);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Visual";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(364, 6);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "&OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(447, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(530, 6);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "&Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(609, 400);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Auto renaming";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbRenamingGameNameRegionFirmware);
            this.groupBox1.Controls.Add(this.rbRenamingGameNameRegion);
            this.groupBox1.Controls.Add(this.lblExample);
            this.groupBox1.Controls.Add(this.rbRenamingCustom);
            this.groupBox1.Controls.Add(this.rbRenamingTitleIDGameNameReleaseGroup);
            this.groupBox1.Controls.Add(this.gbCustom);
            this.groupBox1.Controls.Add(this.rbRenamingTitleIDGameName);
            this.groupBox1.Controls.Add(this.rbRenamingGameName);
            this.groupBox1.Location = new System.Drawing.Point(10, 18);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(588, 364);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Choose pattern";
            // 
            // lblExample
            // 
            this.lblExample.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExample.Location = new System.Drawing.Point(11, 327);
            this.lblExample.Name = "lblExample";
            this.lblExample.Size = new System.Drawing.Size(566, 23);
            this.lblExample.TabIndex = 1;
            this.lblExample.Text = "Super Mario Odyssey.xci";
            this.lblExample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // rbRenamingCustom
            // 
            this.rbRenamingCustom.AutoSize = true;
            this.rbRenamingCustom.Location = new System.Drawing.Point(19, 145);
            this.rbRenamingCustom.Name = "rbRenamingCustom";
            this.rbRenamingCustom.Size = new System.Drawing.Size(60, 17);
            this.rbRenamingCustom.TabIndex = 4;
            this.rbRenamingCustom.Text = "Custom";
            this.rbRenamingCustom.UseVisualStyleBackColor = true;
            this.rbRenamingCustom.CheckedChanged += new System.EventHandler(this.rbRenamingCustom_CheckedChanged);
            // 
            // rbRenamingTitleIDGameNameReleaseGroup
            // 
            this.rbRenamingTitleIDGameNameReleaseGroup.AutoSize = true;
            this.rbRenamingTitleIDGameNameReleaseGroup.Location = new System.Drawing.Point(19, 122);
            this.rbRenamingTitleIDGameNameReleaseGroup.Name = "rbRenamingTitleIDGameNameReleaseGroup";
            this.rbRenamingTitleIDGameNameReleaseGroup.Size = new System.Drawing.Size(203, 17);
            this.rbRenamingTitleIDGameNameReleaseGroup.TabIndex = 3;
            this.rbRenamingTitleIDGameNameReleaseGroup.Text = "Title ID - Game name - Release group";
            this.rbRenamingTitleIDGameNameReleaseGroup.UseVisualStyleBackColor = true;
            this.rbRenamingTitleIDGameNameReleaseGroup.CheckedChanged += new System.EventHandler(this.rbRenamingTitleIDGameNameReleaseGroup_CheckedChanged);
            // 
            // gbCustom
            // 
            this.gbCustom.Controls.Add(this.btnAdd);
            this.gbCustom.Controls.Add(this.textBoxCustomPatern);
            this.gbCustom.Controls.Add(this.cbxTags);
            this.gbCustom.Location = new System.Drawing.Point(19, 168);
            this.gbCustom.Name = "gbCustom";
            this.gbCustom.Size = new System.Drawing.Size(563, 50);
            this.gbCustom.TabIndex = 2;
            this.gbCustom.TabStop = false;
            this.gbCustom.Text = "Custom pattern";
            this.gbCustom.Visible = false;
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAdd.Location = new System.Drawing.Point(85, 18);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(25, 23);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "+";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // textBoxCustomPatern
            // 
            this.textBoxCustomPatern.Location = new System.Drawing.Point(115, 19);
            this.textBoxCustomPatern.Name = "textBoxCustomPatern";
            this.textBoxCustomPatern.Size = new System.Drawing.Size(442, 20);
            this.textBoxCustomPatern.TabIndex = 1;
            this.textBoxCustomPatern.TextChanged += new System.EventHandler(this.textBoxCustomPatern_TextChanged);
            // 
            // cbxTags
            // 
            this.cbxTags.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxTags.FormattingEnabled = true;
            this.cbxTags.Items.AddRange(new object[] {
            "Name",
            "Title ID",
            "Developer",
            "Trimmed",
            "Revision"});
            this.cbxTags.Location = new System.Drawing.Point(6, 19);
            this.cbxTags.Name = "cbxTags";
            this.cbxTags.Size = new System.Drawing.Size(74, 21);
            this.cbxTags.TabIndex = 0;
            // 
            // rbRenamingTitleIDGameName
            // 
            this.rbRenamingTitleIDGameName.AutoSize = true;
            this.rbRenamingTitleIDGameName.Location = new System.Drawing.Point(19, 99);
            this.rbRenamingTitleIDGameName.Name = "rbRenamingTitleIDGameName";
            this.rbRenamingTitleIDGameName.Size = new System.Drawing.Size(125, 17);
            this.rbRenamingTitleIDGameName.TabIndex = 1;
            this.rbRenamingTitleIDGameName.Text = "Title ID - Game name";
            this.rbRenamingTitleIDGameName.UseVisualStyleBackColor = true;
            this.rbRenamingTitleIDGameName.CheckedChanged += new System.EventHandler(this.rbRenamingTitleIDGameName_CheckedChanged);
            // 
            // rbRenamingGameName
            // 
            this.rbRenamingGameName.AutoSize = true;
            this.rbRenamingGameName.Checked = true;
            this.rbRenamingGameName.Location = new System.Drawing.Point(19, 29);
            this.rbRenamingGameName.Name = "rbRenamingGameName";
            this.rbRenamingGameName.Size = new System.Drawing.Size(82, 17);
            this.rbRenamingGameName.TabIndex = 0;
            this.rbRenamingGameName.TabStop = true;
            this.rbRenamingGameName.Text = "Game name";
            this.rbRenamingGameName.UseVisualStyleBackColor = true;
            this.rbRenamingGameName.CheckedChanged += new System.EventHandler(this.rbRenamingGameName_CheckedChanged);
            // 
            // rbRenamingGameNameRegion
            // 
            this.rbRenamingGameNameRegion.AutoSize = true;
            this.rbRenamingGameNameRegion.Location = new System.Drawing.Point(19, 52);
            this.rbRenamingGameNameRegion.Name = "rbRenamingGameNameRegion";
            this.rbRenamingGameNameRegion.Size = new System.Drawing.Size(125, 17);
            this.rbRenamingGameNameRegion.TabIndex = 5;
            this.rbRenamingGameNameRegion.Text = "Game name (Region)";
            this.rbRenamingGameNameRegion.UseVisualStyleBackColor = true;
            this.rbRenamingGameNameRegion.CheckedChanged += new System.EventHandler(this.rbRenamingGameNameRegion_CheckedChanged);
            // 
            // rbRenamingGameNameRegionFirmware
            // 
            this.rbRenamingGameNameRegionFirmware.AutoSize = true;
            this.rbRenamingGameNameRegionFirmware.Location = new System.Drawing.Point(19, 75);
            this.rbRenamingGameNameRegionFirmware.Name = "rbRenamingGameNameRegionFirmware";
            this.rbRenamingGameNameRegionFirmware.Size = new System.Drawing.Size(176, 17);
            this.rbRenamingGameNameRegionFirmware.TabIndex = 6;
            this.rbRenamingGameNameRegionFirmware.Text = "Game name (Region) (Firmware)";
            this.rbRenamingGameNameRegionFirmware.UseVisualStyleBackColor = true;
            this.rbRenamingGameNameRegionFirmware.CheckedChanged += new System.EventHandler(this.rbRenamingGameNameRegionFirmware_CheckedChanged);
            // 
            // FormConfigs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(617, 462);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormConfigs";
            this.ShowInTaskbar = false;
            this.Text = "Options";
            this.TopMost = true;
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbCustom.ResumeLayout(false);
            this.gbCustom.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblExample;
        private System.Windows.Forms.RadioButton rbRenamingCustom;
        private System.Windows.Forms.RadioButton rbRenamingTitleIDGameNameReleaseGroup;
        private System.Windows.Forms.GroupBox gbCustom;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.TextBox textBoxCustomPatern;
        private System.Windows.Forms.ComboBox cbxTags;
        private System.Windows.Forms.RadioButton rbRenamingTitleIDGameName;
        private System.Windows.Forms.RadioButton rbRenamingGameName;
        private System.Windows.Forms.RadioButton rbRenamingGameNameRegion;
        private System.Windows.Forms.RadioButton rbRenamingGameNameRegionFirmware;
    }
}