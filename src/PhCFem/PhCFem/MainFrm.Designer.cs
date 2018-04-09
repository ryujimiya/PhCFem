namespace PhCFem
{
    partial class MainFrm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrm));
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Title title2 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend3 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Title title3 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.CadPanel = new System.Windows.Forms.Panel();
            this.FValuePanel = new System.Windows.Forms.Panel();
            this.btnNextFValuePanel = new System.Windows.Forms.Button();
            this.btnPrevFValuePanel = new System.Windows.Forms.Button();
            this.btnCalc = new System.Windows.Forms.Button();
            this.GroupBoxCadMode = new System.Windows.Forms.GroupBox();
            this.imgcbxCellType = new PhCFem.ImageCombobox();
            this.imageListCellType = new System.Windows.Forms.ImageList(this.components);
            this.radioBtnLocation = new System.Windows.Forms.RadioButton();
            this.radioBtnNone = new System.Windows.Forms.RadioButton();
            this.radioBtnPortNumbering = new System.Windows.Forms.RadioButton();
            this.radioBtnIncidentPort = new System.Windows.Forms.RadioButton();
            this.radioBtnErase = new System.Windows.Forms.RadioButton();
            this.radioBtnPort = new System.Windows.Forms.RadioButton();
            this.radioBtnArea = new System.Windows.Forms.RadioButton();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnPrevFreq = new System.Windows.Forms.Button();
            this.btnNextFreq = new System.Windows.Forms.Button();
            this.btnRedo = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnSaveAs = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnNew = new System.Windows.Forms.Button();
            this.labelFreq = new System.Windows.Forms.Label();
            this.SMatChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.SMatChartContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMILogGraph = new System.Windows.Forms.ToolStripMenuItem();
            this.FValueLegendPanel = new System.Windows.Forms.Panel();
            this.labelFreqValue = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.BetaChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.EigenVecChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.linkLblEigenShow = new System.Windows.Forms.LinkLabel();
            this.linkLabelMeshShow = new System.Windows.Forms.LinkLabel();
            this.labelCalcMode = new System.Windows.Forms.Label();
            this.btnLoadCancel = new System.Windows.Forms.Button();
            this.btnSetting = new System.Windows.Forms.Button();
            this.btnEigenFieldShow = new System.Windows.Forms.Button();
            this.FValuePanel.SuspendLayout();
            this.GroupBoxCadMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SMatChart)).BeginInit();
            this.SMatChartContextMenuStrip.SuspendLayout();
            this.FValueLegendPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BetaChart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.EigenVecChart)).BeginInit();
            this.SuspendLayout();
            // 
            // CadPanel
            // 
            this.CadPanel.Location = new System.Drawing.Point(-1, 46);
            this.CadPanel.Margin = new System.Windows.Forms.Padding(0);
            this.CadPanel.Name = "CadPanel";
            this.CadPanel.Size = new System.Drawing.Size(480, 480);
            this.CadPanel.TabIndex = 0;
            this.CadPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.CadPanel_Paint);
            this.CadPanel.DoubleClick += new System.EventHandler(this.CadPanel_DoubleClick);
            this.CadPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.CadPanel_MouseClick);
            this.CadPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CadPanel_MouseDown);
            this.CadPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CadPanel_MouseMove);
            this.CadPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CadPanel_MouseUp);
            // 
            // FValuePanel
            // 
            this.FValuePanel.Controls.Add(this.btnNextFValuePanel);
            this.FValuePanel.Controls.Add(this.btnPrevFValuePanel);
            this.FValuePanel.Location = new System.Drawing.Point(522, 3);
            this.FValuePanel.Margin = new System.Windows.Forms.Padding(0);
            this.FValuePanel.Name = "FValuePanel";
            this.FValuePanel.Size = new System.Drawing.Size(360, 360);
            this.FValuePanel.TabIndex = 0;
            this.FValuePanel.Paint += new System.Windows.Forms.PaintEventHandler(this.FValuePanel_Paint);
            this.FValuePanel.DoubleClick += new System.EventHandler(this.FValuePanel_DoubleClick);
            this.FValuePanel.MouseEnter += new System.EventHandler(this.FValuePanel_MouseEnter);
            this.FValuePanel.MouseLeave += new System.EventHandler(this.FValuePanel_MouseLeave);
            this.FValuePanel.MouseHover += new System.EventHandler(this.FValuePanel_MouseHover);
            this.FValuePanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FValuePanel_MouseMove);
            // 
            // btnNextFValuePanel
            // 
            this.btnNextFValuePanel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNextFValuePanel.Location = new System.Drawing.Point(3, 205);
            this.btnNextFValuePanel.Name = "btnNextFValuePanel";
            this.btnNextFValuePanel.Size = new System.Drawing.Size(40, 40);
            this.btnNextFValuePanel.TabIndex = 0;
            this.btnNextFValuePanel.Text = ">";
            this.toolTip1.SetToolTip(this.btnNextFValuePanel, "次のパネル");
            this.btnNextFValuePanel.UseVisualStyleBackColor = true;
            this.btnNextFValuePanel.Click += new System.EventHandler(this.btnNextFValuePanel_Click);
            this.btnNextFValuePanel.MouseEnter += new System.EventHandler(this.btnNextFValuePanel_MouseEnter);
            this.btnNextFValuePanel.MouseLeave += new System.EventHandler(this.btnNextFValuePanel_MouseLeave);
            // 
            // btnPrevFValuePanel
            // 
            this.btnPrevFValuePanel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrevFValuePanel.Location = new System.Drawing.Point(3, 160);
            this.btnPrevFValuePanel.Name = "btnPrevFValuePanel";
            this.btnPrevFValuePanel.Size = new System.Drawing.Size(40, 40);
            this.btnPrevFValuePanel.TabIndex = 0;
            this.btnPrevFValuePanel.Text = "<";
            this.toolTip1.SetToolTip(this.btnPrevFValuePanel, "前のパネル");
            this.btnPrevFValuePanel.UseVisualStyleBackColor = true;
            this.btnPrevFValuePanel.Click += new System.EventHandler(this.btnPrevFValuePanel_Click);
            this.btnPrevFValuePanel.MouseEnter += new System.EventHandler(this.btnPrevFValuePanel_MouseEnter);
            this.btnPrevFValuePanel.MouseLeave += new System.EventHandler(this.btnPrevFValuePanel_MouseLeave);
            // 
            // btnCalc
            // 
            this.btnCalc.AutoSize = true;
            this.btnCalc.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCalc.ForeColor = System.Drawing.Color.Black;
            this.btnCalc.Location = new System.Drawing.Point(319, 15);
            this.btnCalc.Name = "btnCalc";
            this.btnCalc.Padding = new System.Windows.Forms.Padding(3);
            this.btnCalc.Size = new System.Drawing.Size(69, 28);
            this.btnCalc.TabIndex = 8;
            this.btnCalc.Text = "計算開始";
            this.btnCalc.UseVisualStyleBackColor = true;
            this.btnCalc.Click += new System.EventHandler(this.btnCalc_Click);
            // 
            // GroupBoxCadMode
            // 
            this.GroupBoxCadMode.Controls.Add(this.imgcbxCellType);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnLocation);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnNone);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnPortNumbering);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnIncidentPort);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnErase);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnPort);
            this.GroupBoxCadMode.Controls.Add(this.radioBtnArea);
            this.GroupBoxCadMode.Location = new System.Drawing.Point(-1, 531);
            this.GroupBoxCadMode.Margin = new System.Windows.Forms.Padding(0);
            this.GroupBoxCadMode.Name = "GroupBoxCadMode";
            this.GroupBoxCadMode.Padding = new System.Windows.Forms.Padding(0);
            this.GroupBoxCadMode.Size = new System.Drawing.Size(223, 71);
            this.GroupBoxCadMode.TabIndex = 10;
            this.GroupBoxCadMode.TabStop = false;
            // 
            // imgcbxCellType
            // 
            this.imgcbxCellType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.imgcbxCellType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.imgcbxCellType.FormattingEnabled = true;
            this.imgcbxCellType.ImageList = this.imageListCellType;
            this.imgcbxCellType.Location = new System.Drawing.Point(66, 38);
            this.imgcbxCellType.Name = "imgcbxCellType";
            this.imgcbxCellType.Size = new System.Drawing.Size(121, 20);
            this.imgcbxCellType.TabIndex = 9;
            this.imgcbxCellType.SelectedIndexChanged += new System.EventHandler(this.imgcbxCadModeArea_SelectedIndexChanged);
            // 
            // imageListCellType
            // 
            this.imageListCellType.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListCellType.ImageStream")));
            this.imageListCellType.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListCellType.Images.SetKeyName(0, "セル欠陥25x25.png");
            this.imageListCellType.Images.SetKeyName(1, "セルロッド25x25.png");
            // 
            // radioBtnLocation
            // 
            this.radioBtnLocation.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnLocation.Image = ((System.Drawing.Image)(resources.GetObject("radioBtnLocation.Image")));
            this.radioBtnLocation.Location = new System.Drawing.Point(36, 2);
            this.radioBtnLocation.Name = "radioBtnLocation";
            this.radioBtnLocation.Size = new System.Drawing.Size(30, 30);
            this.radioBtnLocation.TabIndex = 1;
            this.radioBtnLocation.TabStop = true;
            this.radioBtnLocation.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnLocation, "位置移動");
            this.radioBtnLocation.UseVisualStyleBackColor = true;
            this.radioBtnLocation.CheckedChanged += new System.EventHandler(this.radioBtnLocation_CheckedChanged);
            // 
            // radioBtnNone
            // 
            this.radioBtnNone.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnNone.Image = global::PhCFem.Properties.Resources.モードクリア25x25;
            this.radioBtnNone.Location = new System.Drawing.Point(6, 2);
            this.radioBtnNone.Name = "radioBtnNone";
            this.radioBtnNone.Size = new System.Drawing.Size(30, 30);
            this.radioBtnNone.TabIndex = 0;
            this.radioBtnNone.TabStop = true;
            this.radioBtnNone.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnNone, "描画モード解除");
            this.radioBtnNone.UseVisualStyleBackColor = true;
            this.radioBtnNone.CheckedChanged += new System.EventHandler(this.radioBtnNone_CheckedChanged);
            // 
            // radioBtnPortNumbering
            // 
            this.radioBtnPortNumbering.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnPortNumbering.Image = global::PhCFem.Properties.Resources.入射ポート番号振り25x25;
            this.radioBtnPortNumbering.Location = new System.Drawing.Point(156, 2);
            this.radioBtnPortNumbering.Name = "radioBtnPortNumbering";
            this.radioBtnPortNumbering.Size = new System.Drawing.Size(30, 30);
            this.radioBtnPortNumbering.TabIndex = 6;
            this.radioBtnPortNumbering.TabStop = true;
            this.radioBtnPortNumbering.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnPortNumbering, "ポート番号振り");
            this.radioBtnPortNumbering.UseVisualStyleBackColor = true;
            this.radioBtnPortNumbering.CheckedChanged += new System.EventHandler(this.radioBtnPortNumbering_CheckedChanged);
            // 
            // radioBtnIncidentPort
            // 
            this.radioBtnIncidentPort.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnIncidentPort.Image = global::PhCFem.Properties.Resources.入射ポート選択25x25;
            this.radioBtnIncidentPort.Location = new System.Drawing.Point(126, 2);
            this.radioBtnIncidentPort.Name = "radioBtnIncidentPort";
            this.radioBtnIncidentPort.Size = new System.Drawing.Size(30, 30);
            this.radioBtnIncidentPort.TabIndex = 5;
            this.radioBtnIncidentPort.TabStop = true;
            this.radioBtnIncidentPort.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnIncidentPort, "入射ポート選択");
            this.radioBtnIncidentPort.UseVisualStyleBackColor = true;
            this.radioBtnIncidentPort.CheckedChanged += new System.EventHandler(this.radioBtnIncidentPort_CheckedChanged);
            // 
            // radioBtnErase
            // 
            this.radioBtnErase.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnErase.Image = global::PhCFem.Properties.Resources.消しゴム長方形25x25;
            this.radioBtnErase.Location = new System.Drawing.Point(186, 2);
            this.radioBtnErase.Name = "radioBtnErase";
            this.radioBtnErase.Size = new System.Drawing.Size(30, 30);
            this.radioBtnErase.TabIndex = 7;
            this.radioBtnErase.TabStop = true;
            this.radioBtnErase.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnErase, "消しゴム");
            this.radioBtnErase.UseVisualStyleBackColor = true;
            this.radioBtnErase.CheckedChanged += new System.EventHandler(this.radioBtnErase_CheckedChanged);
            // 
            // radioBtnPort
            // 
            this.radioBtnPort.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnPort.Image = global::PhCFem.Properties.Resources.境界選択25x25;
            this.radioBtnPort.Location = new System.Drawing.Point(96, 2);
            this.radioBtnPort.Name = "radioBtnPort";
            this.radioBtnPort.Size = new System.Drawing.Size(30, 30);
            this.radioBtnPort.TabIndex = 4;
            this.radioBtnPort.TabStop = true;
            this.radioBtnPort.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnPort, "ポーt境界");
            this.radioBtnPort.UseVisualStyleBackColor = true;
            this.radioBtnPort.CheckedChanged += new System.EventHandler(this.radioBtnPort_CheckedChanged);
            // 
            // radioBtnArea
            // 
            this.radioBtnArea.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioBtnArea.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.radioBtnArea.Image = global::PhCFem.Properties.Resources.エリア選択25x25;
            this.radioBtnArea.Location = new System.Drawing.Point(66, 2);
            this.radioBtnArea.Name = "radioBtnArea";
            this.radioBtnArea.Size = new System.Drawing.Size(30, 30);
            this.radioBtnArea.TabIndex = 2;
            this.radioBtnArea.TabStop = true;
            this.radioBtnArea.Text = "　";
            this.toolTip1.SetToolTip(this.radioBtnArea, "マス目選択");
            this.radioBtnArea.UseVisualStyleBackColor = true;
            this.radioBtnArea.CheckedChanged += new System.EventHandler(this.radioBtnArea_CheckedChanged);
            // 
            // btnPrevFreq
            // 
            this.btnPrevFreq.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnPrevFreq.ForeColor = System.Drawing.Color.Black;
            this.btnPrevFreq.Location = new System.Drawing.Point(0, 274);
            this.btnPrevFreq.Name = "btnPrevFreq";
            this.btnPrevFreq.Size = new System.Drawing.Size(30, 30);
            this.btnPrevFreq.TabIndex = 0;
            this.btnPrevFreq.Text = "◀";
            this.toolTip1.SetToolTip(this.btnPrevFreq, "前の周波数");
            this.btnPrevFreq.UseVisualStyleBackColor = true;
            this.btnPrevFreq.Click += new System.EventHandler(this.btnPrevFreq_Click);
            // 
            // btnNextFreq
            // 
            this.btnNextFreq.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnNextFreq.ForeColor = System.Drawing.Color.Black;
            this.btnNextFreq.Location = new System.Drawing.Point(36, 274);
            this.btnNextFreq.Name = "btnNextFreq";
            this.btnNextFreq.Size = new System.Drawing.Size(30, 30);
            this.btnNextFreq.TabIndex = 1;
            this.btnNextFreq.Text = "▶";
            this.toolTip1.SetToolTip(this.btnNextFreq, "次の周波数");
            this.btnNextFreq.UseVisualStyleBackColor = true;
            this.btnNextFreq.Click += new System.EventHandler(this.btnNextFreq_Click);
            // 
            // btnRedo
            // 
            this.btnRedo.Image = global::PhCFem.Properties.Resources.やり直し;
            this.btnRedo.Location = new System.Drawing.Point(199, 3);
            this.btnRedo.Name = "btnRedo";
            this.btnRedo.Size = new System.Drawing.Size(40, 40);
            this.btnRedo.TabIndex = 5;
            this.toolTip1.SetToolTip(this.btnRedo, "やり直し Ctrl+Y");
            this.btnRedo.UseVisualStyleBackColor = true;
            this.btnRedo.Click += new System.EventHandler(this.btnRedo_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.Image = global::PhCFem.Properties.Resources.元に戻す;
            this.btnUndo.Location = new System.Drawing.Point(159, 3);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(40, 40);
            this.btnUndo.TabIndex = 4;
            this.toolTip1.SetToolTip(this.btnUndo, "元に戻す Ctrl+Z");
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.Image = global::PhCFem.Properties.Resources.名前を付けて保存;
            this.btnSaveAs.Location = new System.Drawing.Point(119, 3);
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(40, 40);
            this.btnSaveAs.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnSaveAs, "名前を付けて保存");
            this.btnSaveAs.UseVisualStyleBackColor = true;
            this.btnSaveAs.Click += new System.EventHandler(this.btnSaveAs_Click);
            // 
            // btnSave
            // 
            this.btnSave.Image = global::PhCFem.Properties.Resources.上書き保存;
            this.btnSave.Location = new System.Drawing.Point(79, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(40, 40);
            this.btnSave.TabIndex = 2;
            this.toolTip1.SetToolTip(this.btnSave, "上書き保存 Ctrl+S");
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Image = global::PhCFem.Properties.Resources.開く;
            this.btnOpen.Location = new System.Drawing.Point(39, 3);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(40, 40);
            this.btnOpen.TabIndex = 1;
            this.toolTip1.SetToolTip(this.btnOpen, "開く Ctrl+O");
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnNew
            // 
            this.btnNew.Image = global::PhCFem.Properties.Resources.新規;
            this.btnNew.Location = new System.Drawing.Point(-1, 3);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(40, 40);
            this.btnNew.TabIndex = 0;
            this.toolTip1.SetToolTip(this.btnNew, "新規作成");
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            // 
            // labelFreq
            // 
            this.labelFreq.AutoSize = true;
            this.labelFreq.Location = new System.Drawing.Point(0, 235);
            this.labelFreq.Name = "labelFreq";
            this.labelFreq.Size = new System.Drawing.Size(39, 12);
            this.labelFreq.TabIndex = 0;
            this.labelFreq.Text = "a/λ =";
            // 
            // SMatChart
            // 
            chartArea1.Name = "ChartArea1";
            this.SMatChart.ChartAreas.Add(chartArea1);
            this.SMatChart.ContextMenuStrip = this.SMatChartContextMenuStrip;
            legend1.Name = "Legend1";
            this.SMatChart.Legends.Add(legend1);
            this.SMatChart.Location = new System.Drawing.Point(502, 366);
            this.SMatChart.Name = "SMatChart";
            this.SMatChart.Size = new System.Drawing.Size(480, 270);
            this.SMatChart.TabIndex = 0;
            this.SMatChart.TabStop = false;
            this.SMatChart.Text = "chart1";
            title1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title1.Name = "Title1";
            this.SMatChart.Titles.Add(title1);
            this.SMatChart.DoubleClick += new System.EventHandler(this.SMatChart_DoubleClick);
            // 
            // SMatChartContextMenuStrip
            // 
            this.SMatChartContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMILogGraph});
            this.SMatChartContextMenuStrip.Name = "SMatChartContextMenuStrip";
            this.SMatChartContextMenuStrip.Size = new System.Drawing.Size(125, 26);
            // 
            // toolStripMILogGraph
            // 
            this.toolStripMILogGraph.Name = "toolStripMILogGraph";
            this.toolStripMILogGraph.Size = new System.Drawing.Size(124, 22);
            this.toolStripMILogGraph.Text = "対数表示";
            this.toolStripMILogGraph.Click += new System.EventHandler(this.toolStripMILogGraph_Click);
            // 
            // FValueLegendPanel
            // 
            this.FValueLegendPanel.BackColor = System.Drawing.Color.White;
            this.FValueLegendPanel.Controls.Add(this.labelFreqValue);
            this.FValueLegendPanel.Controls.Add(this.btnNextFreq);
            this.FValueLegendPanel.Controls.Add(this.btnPrevFreq);
            this.FValueLegendPanel.Controls.Add(this.labelFreq);
            this.FValueLegendPanel.Location = new System.Drawing.Point(882, 3);
            this.FValueLegendPanel.Margin = new System.Windows.Forms.Padding(0);
            this.FValueLegendPanel.Name = "FValueLegendPanel";
            this.FValueLegendPanel.Size = new System.Drawing.Size(100, 360);
            this.FValueLegendPanel.TabIndex = 13;
            // 
            // labelFreqValue
            // 
            this.labelFreqValue.AutoSize = true;
            this.labelFreqValue.Font = new System.Drawing.Font("MS UI Gothic", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelFreqValue.Location = new System.Drawing.Point(5, 251);
            this.labelFreqValue.Name = "labelFreqValue";
            this.labelFreqValue.Size = new System.Drawing.Size(42, 19);
            this.labelFreqValue.TabIndex = 3;
            this.labelFreqValue.Text = "---";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "cad";
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "CADデータ(*.cad)|*.cad";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "CADデータ(*.cad)|*.cad";
            // 
            // BetaChart
            // 
            chartArea2.Name = "ChartArea1";
            this.BetaChart.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.BetaChart.Legends.Add(legend2);
            this.BetaChart.Location = new System.Drawing.Point(-1, 633);
            this.BetaChart.Name = "BetaChart";
            this.BetaChart.Size = new System.Drawing.Size(480, 270);
            this.BetaChart.TabIndex = 0;
            this.BetaChart.TabStop = false;
            this.BetaChart.Text = "chart1";
            title2.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title2.Name = "Title1";
            this.BetaChart.Titles.Add(title2);
            this.BetaChart.DoubleClick += new System.EventHandler(this.BetaChart_DoubleClick);
            // 
            // EigenVecChart
            // 
            chartArea3.Name = "ChartArea1";
            this.EigenVecChart.ChartAreas.Add(chartArea3);
            legend3.Name = "Legend1";
            this.EigenVecChart.Legends.Add(legend3);
            this.EigenVecChart.Location = new System.Drawing.Point(502, 633);
            this.EigenVecChart.Name = "EigenVecChart";
            this.EigenVecChart.Size = new System.Drawing.Size(480, 270);
            this.EigenVecChart.TabIndex = 0;
            this.EigenVecChart.TabStop = false;
            this.EigenVecChart.Text = "chart1";
            title3.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title3.Name = "Title1";
            this.EigenVecChart.Titles.Add(title3);
            this.EigenVecChart.DoubleClick += new System.EventHandler(this.EigenVecChart_DoubleClick);
            // 
            // linkLblEigenShow
            // 
            this.linkLblEigenShow.AutoSize = true;
            this.linkLblEigenShow.Location = new System.Drawing.Point(236, 579);
            this.linkLblEigenShow.Name = "linkLblEigenShow";
            this.linkLblEigenShow.Size = new System.Drawing.Size(87, 12);
            this.linkLblEigenShow.TabIndex = 12;
            this.linkLblEigenShow.TabStop = true;
            this.linkLblEigenShow.Text = "固有モードを見る";
            this.linkLblEigenShow.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLblEigenShow_LinkClicked);
            // 
            // linkLabelMeshShow
            // 
            this.linkLabelMeshShow.AutoSize = true;
            this.linkLabelMeshShow.Location = new System.Drawing.Point(245, 31);
            this.linkLabelMeshShow.Name = "linkLabelMeshShow";
            this.linkLabelMeshShow.Size = new System.Drawing.Size(68, 12);
            this.linkLabelMeshShow.TabIndex = 6;
            this.linkLabelMeshShow.TabStop = true;
            this.linkLabelMeshShow.Text = "メッシュを見る";
            this.linkLabelMeshShow.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelMeshShow_LinkClicked);
            // 
            // labelCalcMode
            // 
            this.labelCalcMode.AutoSize = true;
            this.labelCalcMode.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelCalcMode.ForeColor = System.Drawing.Color.Navy;
            this.labelCalcMode.Location = new System.Drawing.Point(245, 9);
            this.labelCalcMode.Name = "labelCalcMode";
            this.labelCalcMode.Size = new System.Drawing.Size(48, 12);
            this.labelCalcMode.TabIndex = 0;
            this.labelCalcMode.Text = "H面 TE";
            // 
            // btnLoadCancel
            // 
            this.btnLoadCancel.AutoSize = true;
            this.btnLoadCancel.ForeColor = System.Drawing.Color.Black;
            this.btnLoadCancel.Location = new System.Drawing.Point(394, 9);
            this.btnLoadCancel.Name = "btnLoadCancel";
            this.btnLoadCancel.Size = new System.Drawing.Size(75, 34);
            this.btnLoadCancel.TabIndex = 9;
            this.btnLoadCancel.Text = "読み込み\r\nキャンセル";
            this.btnLoadCancel.UseVisualStyleBackColor = true;
            this.btnLoadCancel.Click += new System.EventHandler(this.btnLoadCancel_Click);
            // 
            // btnSetting
            // 
            this.btnSetting.Location = new System.Drawing.Point(238, 544);
            this.btnSetting.Name = "btnSetting";
            this.btnSetting.Size = new System.Drawing.Size(75, 23);
            this.btnSetting.TabIndex = 11;
            this.btnSetting.Text = "設定";
            this.btnSetting.UseVisualStyleBackColor = true;
            this.btnSetting.Click += new System.EventHandler(this.btnSetting_Click);
            // 
            // btnEigenFieldShow
            // 
            this.btnEigenFieldShow.Location = new System.Drawing.Point(534, 657);
            this.btnEigenFieldShow.Name = "btnEigenFieldShow";
            this.btnEigenFieldShow.Size = new System.Drawing.Size(75, 23);
            this.btnEigenFieldShow.TabIndex = 14;
            this.btnEigenFieldShow.Text = "分布表示";
            this.btnEigenFieldShow.UseVisualStyleBackColor = true;
            this.btnEigenFieldShow.Click += new System.EventHandler(this.btnEigenFValueShow_Click);
            // 
            // MainFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1057, 636);
            this.Controls.Add(this.btnEigenFieldShow);
            this.Controls.Add(this.btnSetting);
            this.Controls.Add(this.btnLoadCancel);
            this.Controls.Add(this.linkLabelMeshShow);
            this.Controls.Add(this.labelCalcMode);
            this.Controls.Add(this.FValueLegendPanel);
            this.Controls.Add(this.linkLblEigenShow);
            this.Controls.Add(this.GroupBoxCadMode);
            this.Controls.Add(this.btnCalc);
            this.Controls.Add(this.btnRedo);
            this.Controls.Add(this.btnUndo);
            this.Controls.Add(this.btnSaveAs);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.EigenVecChart);
            this.Controls.Add(this.BetaChart);
            this.Controls.Add(this.SMatChart);
            this.Controls.Add(this.FValuePanel);
            this.Controls.Add(this.CadPanel);
            this.ForeColor = System.Drawing.Color.Black;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "MainFrm";
            this.Text = "PhCFem";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.SizeChanged += new System.EventHandler(this.Form1_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.FValuePanel.ResumeLayout(false);
            this.GroupBoxCadMode.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SMatChart)).EndInit();
            this.SMatChartContextMenuStrip.ResumeLayout(false);
            this.FValueLegendPanel.ResumeLayout(false);
            this.FValueLegendPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BetaChart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.EigenVecChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel CadPanel;
        private System.Windows.Forms.Panel FValuePanel;
        private System.Windows.Forms.Button btnCalc;
        private System.Windows.Forms.RadioButton radioBtnArea;
        private System.Windows.Forms.RadioButton radioBtnPort;
        private System.Windows.Forms.RadioButton radioBtnErase;
        private System.Windows.Forms.GroupBox GroupBoxCadMode;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.DataVisualization.Charting.Chart SMatChart;
        private System.Windows.Forms.Panel FValueLegendPanel;
        private System.Windows.Forms.Button btnNextFreq;
        private System.Windows.Forms.Button btnPrevFreq;
        private System.Windows.Forms.Label labelFreq;
        private System.Windows.Forms.Label labelFreqValue;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.RadioButton radioBtnIncidentPort;
        private System.Windows.Forms.Button btnSaveAs;
        private System.Windows.Forms.RadioButton radioBtnNone;
        private System.Windows.Forms.RadioButton radioBtnPortNumbering;
        private System.Windows.Forms.DataVisualization.Charting.Chart BetaChart;
        private System.Windows.Forms.DataVisualization.Charting.Chart EigenVecChart;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnRedo;
        private System.Windows.Forms.LinkLabel linkLblEigenShow;
        private System.Windows.Forms.Label labelCalcMode;
        private System.Windows.Forms.LinkLabel linkLabelMeshShow;
        private System.Windows.Forms.Button btnLoadCancel;
        private System.Windows.Forms.Button btnPrevFValuePanel;
        private System.Windows.Forms.Button btnNextFValuePanel;
        private System.Windows.Forms.RadioButton radioBtnLocation;
        private ImageCombobox imgcbxCellType;
        private System.Windows.Forms.ImageList imageListCellType;
        private System.Windows.Forms.ContextMenuStrip SMatChartContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem toolStripMILogGraph;
        private System.Windows.Forms.Button btnSetting;
        private System.Windows.Forms.Button btnEigenFieldShow;
    }
}

