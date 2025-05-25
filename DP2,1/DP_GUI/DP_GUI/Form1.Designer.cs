// ───────────────────────────────────────────────────────────────
// File: Form1.Designer.cs
// ───────────────────────────────────────────────────────────────
using System.Windows.Forms;
using ScottPlot.WinForms;

namespace DP_GUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Верхня панель керування
        private FlowLayoutPanel flowTop;
        private ComboBox comboScenarios;
        private Label labelInfo;      // для показу кількості логічних процесорів
        private Label labelSize;
        private NumericUpDown numericSize;
        private Label labelIter;
        private NumericUpDown numericIter;
        private Button btnRun;
        private Button btnExport;

        // Відображення даних
        private FormsPlot formsPlot1;
        private TextBox txtOutput;
        private DataGridView gridMetrics;

        // SplitContainer-и
        private SplitContainer splitMain;    // горизонтальний
        private SplitContainer splitBottom;  // вертикальний

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ─── 1. Верхня панель ─────────────────────────────────────────────
            flowTop = new FlowLayoutPanel();
            comboScenarios = new ComboBox();
            labelInfo = new Label();       // ініціалізуємо новий Label
            labelSize = new Label();
            numericSize = new NumericUpDown();
            labelIter = new Label();
            numericIter = new NumericUpDown();
            btnRun = new Button();
            btnExport = new Button();

            flowTop.AutoSize = true;
            flowTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowTop.Dock = DockStyle.Top;
            flowTop.Padding = new Padding(10);
            flowTop.WrapContents = false;

            comboScenarios.DropDownStyle = ComboBoxStyle.DropDownList;
            comboScenarios.Width = 180;
            comboScenarios.Margin = new Padding(5);

            // ── властивості labelInfo ───────────────────────────────────────────
            labelInfo.AutoSize = true;
            labelInfo.Margin = new Padding(5, 8, 5, 5);
            labelInfo.Text = "";  // заповнимо у Form1 конструкторі

            labelSize.AutoSize = true;
            labelSize.Text = "Розмір N:";
            labelSize.Margin = new Padding(5, 8, 5, 5);

            numericSize.Minimum = 1;
            numericSize.Maximum = 10_000_000;
            numericSize.Increment = 1_000;
            numericSize.Value = 10_000;
            numericSize.Width = 80;
            numericSize.Margin = new Padding(5);

            labelIter.AutoSize = true;
            labelIter.Text = "Ітерацій:";
            labelIter.Margin = new Padding(5, 8, 5, 5);

            numericIter.Minimum = 1;
            numericIter.Maximum = 1_000;
            numericIter.Value = 5;
            numericIter.Width = 60;
            numericIter.Margin = new Padding(5);

            btnRun.Text = "Виконати";
            btnRun.AutoSize = true;
            btnRun.Margin = new Padding(5);
            btnRun.Click += btnRun_Click;

            btnExport.Text = "Експорт";
            btnExport.AutoSize = true;
            btnExport.Margin = new Padding(5);
            btnExport.Click += btnExport_Click;

            flowTop.Controls.AddRange(new Control[]
            {
                comboScenarios,
                labelSize, numericSize,
                labelIter, numericIter,
                btnRun, btnExport,
                labelInfo          // додаємо сюди
            });

            // ─── 2. Основні елементи ─────────────────────────────────────────
            formsPlot1 = new FormsPlot { Dock = DockStyle.Fill };

            txtOutput = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };

            gridMetrics = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            // ─── 3. SplitContainer-и ─────────────────────────────────────────
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            splitBottom = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };

            // Розміщуємо контроли у панелях
            splitMain.Panel1.Controls.Add(formsPlot1);
            splitMain.Panel2.Controls.Add(splitBottom);

            splitBottom.Panel1.Controls.Add(txtOutput);
            splitBottom.Panel2.Controls.Add(gridMetrics);

            // ─── 4. Налаштування форми ────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 650);
            Text = "DP Modeling GUI";

            Controls.Add(splitMain);
            Controls.Add(flowTop);

            ((System.ComponentModel.ISupportInitialize)numericSize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericIter).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridMetrics).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
