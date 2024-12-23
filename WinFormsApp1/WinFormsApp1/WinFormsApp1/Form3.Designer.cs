namespace WinFormsApp1
{
    partial class Form3
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
            btnGirisYap = new Button();
            txtTc = new TextBox();
            txtSifre = new TextBox();
            Kayitol = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();
            // 
            // btnGirisYap
            // 
            btnGirisYap.Font = new Font("Tw Cen MT", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnGirisYap.Location = new Point(165, 235);
            btnGirisYap.Name = "btnGirisYap";
            btnGirisYap.Size = new Size(94, 34);
            btnGirisYap.TabIndex = 0;
            btnGirisYap.Text = "GİRİŞ YAP";
            btnGirisYap.UseVisualStyleBackColor = true;
            btnGirisYap.Click += btnGirisYap_Click;
            // 
            // txtTc
            // 
            txtTc.Location = new Point(186, 120);
            txtTc.Name = "txtTc";
            txtTc.Size = new Size(125, 27);
            txtTc.TabIndex = 1;
            // 
            // txtSifre
            // 
            txtSifre.Location = new Point(186, 172);
            txtSifre.Name = "txtSifre";
            txtSifre.PasswordChar = '*';
            txtSifre.Size = new Size(125, 27);
            txtSifre.TabIndex = 2;
            // 
            // Kayitol
            // 
            Kayitol.Font = new Font("Tw Cen MT", 9F, FontStyle.Bold, GraphicsUnit.Point);
            Kayitol.Location = new Point(165, 284);
            Kayitol.Name = "Kayitol";
            Kayitol.Size = new Size(94, 36);
            Kayitol.TabIndex = 3;
            Kayitol.Text = "KAYIT OL";
            Kayitol.UseVisualStyleBackColor = true;
            Kayitol.Click += Kayitol_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Tahoma", 10.8F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(99, 121);
            label1.Name = "label1";
            label1.Size = new Size(81, 22);
            label1.TabIndex = 4;
            label1.Text = "TC NO : ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Tahoma", 10.8F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(106, 176);
            label2.Name = "label2";
            label2.Size = new Size(66, 22);
            label2.TabIndex = 5;
            label2.Text = "Şİfre : ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Verdana Pro Cond", 16.2F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(142, 57);
            label3.Name = "label3";
            label3.Size = new Size(148, 34);
            label3.TabIndex = 6;
            label3.Text = "GİRİŞ YAP";
            label3.TextAlign = ContentAlignment.TopCenter;
            // 
            // Form3
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(445, 437);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(Kayitol);
            Controls.Add(txtSifre);
            Controls.Add(txtTc);
            Controls.Add(btnGirisYap);
            Name = "Form3";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Giriş Yap";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnGirisYap;
        private TextBox txtTc;
        private TextBox txtSifre;
        private Button Kayitol;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}