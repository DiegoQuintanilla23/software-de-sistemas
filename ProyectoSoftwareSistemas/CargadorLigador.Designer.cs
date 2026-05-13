namespace ProyectoSoftwareSistemas
{
    partial class CargadorLigador
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CargadorLigador));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.txtTamprog = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnLimpiar = new System.Windows.Forms.Button();
            this.btnEjecutarPaso2 = new System.Windows.Forms.Button();
            this.btnCargarPaso1 = new System.Windows.Forms.Button();
            this.txtDirProg = new System.Windows.Forms.TextBox();
            this.lblDireccionEjecucion = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.lstArchivos = new System.Windows.Forms.ListBox();
            this.dgvTabse = new System.Windows.Forms.DataGridView();
            this.dgvMemoria = new System.Windows.Forms.DataGridView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTabse)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMemoria)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.txtTamprog);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnLimpiar);
            this.panel1.Controls.Add(this.btnEjecutarPaso2);
            this.panel1.Controls.Add(this.btnCargarPaso1);
            this.panel1.Controls.Add(this.txtDirProg);
            this.panel1.Controls.Add(this.lblDireccionEjecucion);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1060, 125);
            this.panel1.TabIndex = 0;
            // 
            // txtTamprog
            // 
            this.txtTamprog.Enabled = false;
            this.txtTamprog.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTamprog.Location = new System.Drawing.Point(679, 22);
            this.txtTamprog.Name = "txtTamprog";
            this.txtTamprog.ReadOnly = true;
            this.txtTamprog.Size = new System.Drawing.Size(168, 30);
            this.txtTamprog.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(429, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(252, 29);
            this.label1.TabIndex = 11;
            this.label1.Text = "Tamaño de programa:";
            // 
            // btnLimpiar
            // 
            this.btnLimpiar.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLimpiar.Location = new System.Drawing.Point(339, 72);
            this.btnLimpiar.Name = "btnLimpiar";
            this.btnLimpiar.Size = new System.Drawing.Size(155, 29);
            this.btnLimpiar.TabIndex = 10;
            this.btnLimpiar.Text = "Limpiar";
            this.btnLimpiar.UseVisualStyleBackColor = true;
            this.btnLimpiar.Click += new System.EventHandler(this.btnLimpiar_Click);
            // 
            // btnEjecutarPaso2
            // 
            this.btnEjecutarPaso2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEjecutarPaso2.Location = new System.Drawing.Point(178, 71);
            this.btnEjecutarPaso2.Name = "btnEjecutarPaso2";
            this.btnEjecutarPaso2.Size = new System.Drawing.Size(155, 30);
            this.btnEjecutarPaso2.TabIndex = 9;
            this.btnEjecutarPaso2.Text = "Ligar";
            this.btnEjecutarPaso2.UseVisualStyleBackColor = true;
            this.btnEjecutarPaso2.Click += new System.EventHandler(this.btnEjecutarPaso2_Click);
            // 
            // btnCargarPaso1
            // 
            this.btnCargarPaso1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCargarPaso1.Location = new System.Drawing.Point(17, 68);
            this.btnCargarPaso1.Name = "btnCargarPaso1";
            this.btnCargarPaso1.Size = new System.Drawing.Size(155, 33);
            this.btnCargarPaso1.TabIndex = 8;
            this.btnCargarPaso1.Text = "Cargar";
            this.btnCargarPaso1.UseVisualStyleBackColor = true;
            this.btnCargarPaso1.Click += new System.EventHandler(this.btnCargarPaso1_Click);
            // 
            // txtDirProg
            // 
            this.txtDirProg.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDirProg.Location = new System.Drawing.Point(228, 22);
            this.txtDirProg.Name = "txtDirProg";
            this.txtDirProg.Size = new System.Drawing.Size(168, 30);
            this.txtDirProg.TabIndex = 7;
            // 
            // lblDireccionEjecucion
            // 
            this.lblDireccionEjecucion.AutoSize = true;
            this.lblDireccionEjecucion.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDireccionEjecucion.Location = new System.Drawing.Point(12, 21);
            this.lblDireccionEjecucion.Name = "lblDireccionEjecucion";
            this.lblDireccionEjecucion.Size = new System.Drawing.Size(221, 29);
            this.lblDireccionEjecucion.TabIndex = 6;
            this.lblDireccionEjecucion.Text = "Dirección de carga:";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 125);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvMemoria);
            this.splitContainer1.Size = new System.Drawing.Size(1060, 453);
            this.splitContainer1.SplitterDistance = 353;
            this.splitContainer1.TabIndex = 1;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.lstArchivos);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.dgvTabse);
            this.splitContainer2.Size = new System.Drawing.Size(353, 453);
            this.splitContainer2.SplitterDistance = 146;
            this.splitContainer2.TabIndex = 0;
            // 
            // lstArchivos
            // 
            this.lstArchivos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstArchivos.FormattingEnabled = true;
            this.lstArchivos.ItemHeight = 16;
            this.lstArchivos.Location = new System.Drawing.Point(0, 0);
            this.lstArchivos.Name = "lstArchivos";
            this.lstArchivos.Size = new System.Drawing.Size(353, 146);
            this.lstArchivos.TabIndex = 12;
            // 
            // dgvTabse
            // 
            this.dgvTabse.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTabse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTabse.Location = new System.Drawing.Point(0, 0);
            this.dgvTabse.Name = "dgvTabse";
            this.dgvTabse.RowHeadersWidth = 51;
            this.dgvTabse.RowTemplate.Height = 24;
            this.dgvTabse.Size = new System.Drawing.Size(353, 303);
            this.dgvTabse.TabIndex = 0;
            // 
            // dgvMemoria
            // 
            this.dgvMemoria.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMemoria.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMemoria.Location = new System.Drawing.Point(0, 0);
            this.dgvMemoria.Name = "dgvMemoria";
            this.dgvMemoria.RowHeadersWidth = 51;
            this.dgvMemoria.RowTemplate.Height = 24;
            this.dgvMemoria.Size = new System.Drawing.Size(703, 453);
            this.dgvMemoria.TabIndex = 0;
            // 
            // CargadorLigador
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1060, 578);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CargadorLigador";
            this.Text = "CargadorLigador";
            this.Load += new System.EventHandler(this.CargadorLigador_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTabse)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMemoria)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnLimpiar;
        private System.Windows.Forms.Button btnEjecutarPaso2;
        private System.Windows.Forms.Button btnCargarPaso1;
        private System.Windows.Forms.TextBox txtDirProg;
        private System.Windows.Forms.Label lblDireccionEjecucion;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox lstArchivos;
        private System.Windows.Forms.DataGridView dgvTabse;
        private System.Windows.Forms.DataGridView dgvMemoria;
        private System.Windows.Forms.TextBox txtTamprog;
        private System.Windows.Forms.Label label1;
    }
}